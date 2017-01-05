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
        public string ReadSymbolNameFromPrLnBuf(ref int ip)
        {
            int i = 0;
            char c;
            char[] buf = new char[Definition.SBOLSZ + 1];

            // get the symbol
            for (;;)
            {
                c = ctx.PrLnBuf[ip];
                if (char.IsDigit(c) && (i == 0)) break;
                if ((!CharUtil.IsAlNum(c)) && (c != '_') && (c != '.')) break;
                if (i < Definition.SBOLSZ)
                {
                    buf[i++] = c;
                }
                else
                {
                    outPr.FatalError("Symbol name is too long!");
                    return null;
                }
                ip++;
            }
            buf[i] = '\0';
            var name = buf.ToStringFromNullTerminated();

            // check if it's a reserved symbol
            if (i == 1)
            {
                c = char.ToUpper(buf[0]);
                if (c == 'A' || c == 'X' || c == 'Y')
                {
                    outPr.FatalError("Reserved symbol (A, X or Y)!");
                    return null;
                }
            }
            else if (exprPr.CheckKeyword(name) != 0)
            {
                outPr.FatalError("Reserved symbol!");
                return null;
            }

            // ok
            return name;
        }

        /// <summary>
        /// シンボル名をキーにシンボルテーブルからNesAsmSymbolオブジェクトを取得する
        /// 見つからなかった場合は以下の動作となる
        /// createFlag = trueのとき、NesAsmSymbolオブジェクトを新規作成して返す
        /// createFlag = falseのとき、nullを返す
        /// </summary>
        /// <param name="name"></param>
        /// <param name="createFlag"></param>
        /// <returns></returns>
        public NesAsmSymbol LookUpSymbolTable(string name, bool createFlag)
        {
            NesAsmSymbol sym;
            var symbolInstalled = false;

            /* local symbol */
            if (name[0] == '.')
            {
                if (ctx.GLablPtr != null)
                {
                    /* search the symbol in the local list */
                    sym = ctx.GLablPtr.Local?.FirstOrDefault(s => name == s.Name);

                    /* new symbol */
                    if (sym == null)
                    {
                        if (createFlag)
                        {
                            sym = InstallSymbol(name, SymbolScope.LOCAL);
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
                sym = ctx.HashTbl.GetValueOrDefault(name);

                /* new symbol */
                if (sym == null)
                {
                    if (createFlag)
                    {
                        sym = InstallSymbol(name, SymbolScope.GLOBAL);
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
        public NesAsmSymbol InstallSymbol(string name, SymbolScope scope)
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
                Name = name
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
                ctx.HashTbl[name] = sym;
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
        public void SetReservedLabel(string name, int val)
        {
            ctx.LablPtr = null;

            if (name.Length != 0)
            {
                ctx.LablPtr = LookUpSymbolTable(name, true);

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

        public void RemapAllLabels()
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
