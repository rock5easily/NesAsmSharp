using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class SymbolProcessor : ProcessorBase
    {
        public SymbolProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        /// <summary>
        /// collect a symbol from prlnbuf into symbol[],
        /// leaves prlnbuf pointer at first invalid symbol character,
        /// returns 0 if no symbol collected
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public int ColSym(ref int ip)
        {
            bool err = false;
            int i = 0;
            char c;
            var symbol = ctx.Symbol;

            // get the symbol
            for (;;)
            {
                c = ctx.PrLnBuf[ip];
                if (char.IsDigit(c) && (i == 0)) break;
                if ((!CharUtil.IsAlNum(c)) && (c != '_') && (c != '.')) break;
                if (i < Definition.SBOLSZ)
                {
                    symbol[i++] = c;
                }
                else
                {
                    outPr.FatalError("Symbol name is too long!");
                    symbol[0] = '\0';
                    return 0;
                }
                ip++;
            }

            // check if it's a reserved symbol
            if (i == 1)
            {
                c = char.ToUpper(symbol[0]);
                if (c == 'A' || c == 'X' || c == 'Y') err = true;
            }
            else if (exprPr.CheckKeyword() != 0)
            {
                err = true;
            }

            // error
            if (err)
            {
                outPr.FatalError("Reserved symbol!");
                symbol[0] = '\0';
                return 0;
            }

            // ok
            symbol[i] = '\0';
            return i;
        }

        /// <summary>
        /// symbol table lookup
        /// if found, return pointer to symbol
        /// else, install symbol as undefined and return pointer
        /// </summary>
        /// <param name="createFlag"></param>
        /// <returns></returns>
        public NesAsmSymbol STLook(int createFlag)
        {
            NesAsmSymbol sym;
            bool symbolInstalled = false;
            var symstr = ctx.Symbol.ToStringFromNullTerminated();

            /* local symbol */
            if (symstr[0] == '.')
            {
                if (ctx.GLablPtr != null)
                {
                    /* search the symbol in the local list */
                    sym = ctx.GLablPtr.Local?.FirstOrDefault(s => symstr == s.Name);

                    /* new symbol */
                    if (sym == null)
                    {
                        if (createFlag != 0)
                        {
                            sym = STInstall(symstr, SymbolScope.LOCAL);
                            symbolInstalled = true;
                        }
                    }
                }
                else
                {
                    outPr.Error("Local symbol not allowed here!");
                    return null;
                }
            }
            /* global symbol */
            else
            {
                /* search symbol */
                sym = ctx.HashTbl.GetValueOrDefault(symstr);

                /* new symbol */
                if (sym == null)
                {
                    if (createFlag != 0)
                    {
                        sym = STInstall(symstr, SymbolScope.GLOBAL);
                        symbolInstalled = true;
                    }
                }
            }

            /* incremente symbol reference counter */
            if (!symbolInstalled)
            {
                if (sym != null) sym.RefCnt++;
            }

            /* ok */
            return sym;
        }

        /// <summary>
        /// install symbol into symbol hash table
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public NesAsmSymbol STInstall(string symName, SymbolScope scope)
        {
            NesAsmSymbol sym = new NesAsmSymbol()
            {
                Type = ctx.IfExpr ? SymbolFlag.IFUNDEF : SymbolFlag.UNDEF,
                Value = 0,
                Local = null,
                Proc = null,
                Bank = Definition.RESERVED_BANK,
                Nb = 0,
                Size = 0,
                Page = -1,
                Vram = -1,
                Pal = -1,
                RefCnt = 0,
                Reserved = false,
                DataType = AsmDirective.P_UNDEFINED,
                DataSize = 0,
                Name = symName
            };

            /* add the symbol to the hash table */
            if (scope == SymbolScope.LOCAL)
            {
                /* local */
                if (ctx.GLablPtr.Local == null)
                {
                    ctx.GLablPtr.Local = new List<NesAsmSymbol>();
                }
                ctx.GLablPtr.Local.Add(sym);
            }
            else
            {
                /* global */
                ctx.HashTbl[symName] = sym;
            }

            /* ok */
            return sym;
        }

        /// <summary>
        /// assign <lval> to label pointed to by lablptr,
        /// checking for valid definition, etc.
        /// </summary>
        /// <param name="lval"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public int LablDef(int lval, int flag)
        {
            char c;

            /* check for NULL ptr */
            if (ctx.LablPtr == null) return (0);

            /* adjust symbol address */
            if (flag != 0)
            {
                lval = (lval & 0x1FFF) | (ctx.Page << 13);
            }
            // printf(" value=%d lval=%d  flag=%d  bank=%d bank_limit=%d  lablptr->bank=%d bank_base=%d\n",lablptr->value,lval,flag,bank,bank_limit,lablptr->bank,bank_base);

            /* first pass */
            if (ctx.Pass == PassFlag.FIRST_PASS)
            {
                switch (ctx.LablPtr.Type)
                {
                /* undefined */
                case SymbolFlag.UNDEF:
                    ctx.LablPtr.Type = SymbolFlag.DEFABS;
                    ctx.LablPtr.Value = lval;
                    break;
                /* already defined - error */
                case SymbolFlag.IFUNDEF:
                    outPr.Error("Can not define this label, declared as undefined in an IF expression!");
                    return (-1);
                case SymbolFlag.MACRO:
                    outPr.Error("Symbol already used by a macro!");
                    return (-1);
                case SymbolFlag.FUNC:
                    outPr.Error("Symbol already used by a function!");
                    return (-1);
                default:
                    /* reserved label */
                    if (ctx.LablPtr.Reserved)
                    {
                        outPr.FatalError("Reserved symbol!");
                        return (-1);
                    }

                    /* compare the values */
                    if (ctx.LablPtr.Value == lval) break;

                    /* normal label */
                    ctx.LablPtr.Type = SymbolFlag.MDEF;
                    ctx.LablPtr.Value = 0;
                    outPr.Error("Label multiply defined!");
                    return (-1);
                }
            }
            /* second pass */
            else
            {
                if ((ctx.LablPtr.Value != lval) ||
                   ((flag != 0) && (ctx.Bank < ctx.BankLimit) && (ctx.LablPtr.Bank != ctx.BankBase + ctx.Bank)))
                {
                    outPr.FatalError("Internal error[1]!");
                    return (-1);
                }
            }

            /* update symbol data */
            if (flag != 0)
            {
                if (ctx.Section == SectionType.S_CODE)
                {
                    ctx.LablPtr.Proc = ctx.ProcPtr;
                }
                ctx.LablPtr.Bank = ctx.BankBase + ctx.Bank;
                ctx.LablPtr.Page = ctx.Page;

                /* check if it's a local or global symbol */
                c = ctx.LablPtr.Name[0];
                if (c == '.')
                {
                    /* local */
                    ctx.LastLabl = null;
                }
                else
                {
                    /* global */
                    ctx.GLablPtr = ctx.LablPtr;
                    ctx.LastLabl = ctx.LablPtr;
                }
            }

            /* ok */
            return (0);
        }

        /// <summary>
        /// create/update a reserved symbol
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void LablSet(string name, int val)
        {
            ctx.LablPtr = null;

            if (name.Length != 0)
            {
                ctx.Symbol.CopyAsNullTerminated(name);
                ctx.LablPtr = STLook(1);

                if (ctx.LablPtr != null)
                {
                    ctx.LablPtr.Type = SymbolFlag.DEFABS;
                    ctx.LablPtr.Value = val;
                    ctx.LablPtr.Reserved = true;
                }
            }

            /* ok */
            return;
        }


        /* ----
         * lablremap()
         * ----
         * remap all the labels
         */

        public void LablRemap()
        {
            /* browse the symbol table */
            foreach (var sym in ctx.HashTbl.Values)
            {
                sym.Local?.ForEach(lsym =>
                {
                    /* remap the bank */
                    if (lsym.Bank <= ctx.BankLimit)
                    {
                        lsym.Bank += ctx.BankBase;
                    }
                });
            }
        }

    }
}
