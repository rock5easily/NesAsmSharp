﻿using NesAsmSharp.Assembler.Util;
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
        /// calculate the hash value of a symbol
        /// </summary>
        /// <returns></returns>
        public int SymHash()
        {
            var symbol = ctx.Symbol;
            int hash = 0;
            var i = 0;
            char c;

            /* hash value */
            while ((c = symbol[i++]) != '\0')
            {
                hash += c;
                hash = (hash << 3) + (hash >> 5) + c;
            }

            /* ok */
            return (hash & 0xFF);
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

            /* get the symbol */
            for (;;)
            {
                c = ctx.PrLnBuf[ip];
                if (char.IsDigit(c) && (i == 0)) break;
                if ((!CharUtil.IsAlNum(c)) && (c != '_') && (c != '.')) break;
                if (i < (Definition.SBOLSZ))
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

            /* check if it's a reserved symbol */
            if (i == 1)
            {
                switch (char.ToUpper(symbol[0]))
                {
                case 'A':
                case 'X':
                case 'Y':
                    err = true;
                    break;
                }
            }
            if (exprPr.CheckKeyword() != 0) err = true;

            /* error */
            if (err)
            {
                outPr.FatalError("Reserved symbol!");
                symbol[0] = '\0';
                return 0;
            }

            /* ok */
            symbol[i] = '\0';
            return i;
        }

        /// <summary>
        /// symbol table lookup
        /// if found, return pointer to symbol
        /// else, install symbol as undefined and return pointer
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public NesAsmSymbol STLook(int flag)
        {
            NesAsmSymbol sym;
            bool sym_flag = false;
            int hash;
            var symstr = ctx.Symbol.ToStringFromNullTerminated();

            /* local symbol */
            if (symstr[0] == '.')
            {
                if (ctx.GLablPtr != null)
                {
                    /* search the symbol in the local list */
                    sym = ctx.GLablPtr.Local;

                    while (sym != null)
                    {
                        if (symstr == sym.Name) break;
                        sym = sym.Next;
                    }

                    /* new symbol */
                    if (sym == null)
                    {
                        if (flag != 0)
                        {
                            sym = STInstall(0, 1);
                            sym_flag = true;
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
                hash = SymHash();
                sym = ctx.HashTbl[hash];
                while (sym != null)
                {
                    if (symstr == sym.Name) break;
                    sym = sym.Next;
                }

                /* new symbol */
                if (sym == null)
                {
                    if (flag != 0)
                    {
                        sym = STInstall(hash, 0);
                        sym_flag = true;
                    }
                }
            }

            /* incremente symbol reference counter */
            if (!sym_flag)
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
        public NesAsmSymbol STInstall(int hash, int type)
        {
            NesAsmSymbol sym = new NesAsmSymbol();

            /* init the symbol struct */
            sym.Type = ctx.IfExpr ? SymbolFlag.IFUNDEF : SymbolFlag.UNDEF;
            sym.Value = 0;
            sym.Local = null;
            sym.Proc = null;
            sym.Bank = Definition.RESERVED_BANK;
            sym.Nb = 0;
            sym.Size = 0;
            sym.Page = -1;
            sym.Vram = -1;
            sym.Pal = -1;
            sym.RefCnt = 0;
            sym.Reserved = 0;
            sym.DataType = AsmDirective.P_UNDEFINED;
            sym.DataSize = 0;
            sym.Name = ctx.Symbol.ToStringFromNullTerminated();


            /* add the symbol to the hash table */
            if (type != 0)
            {
                /* local */
                sym.Next = ctx.GLablPtr.Local;
                ctx.GLablPtr.Local = sym;
            }
            else
            {
                /* global */
                sym.Next = ctx.HashTbl[hash];
                ctx.HashTbl[hash] = sym;
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
            var lablPtr = ctx.LablPtr;
            if (lablPtr == null) return (0);

            /* adjust symbol address */
            if (flag != 0)
            {
                lval = (lval & 0x1FFF) | (ctx.Page << 13);
            }
            // printf(" value=%d lval=%d  flag=%d  bank=%d bank_limit=%d  lablptr->bank=%d bank_base=%d\n",lablptr->value,lval,flag,bank,bank_limit,lablptr->bank,bank_base);

            /* first pass */
            if (ctx.Pass == PassFlag.FIRST_PASS)
            {
                switch (lablPtr.Type)
                {
                /* undefined */
                case SymbolFlag.UNDEF:
                    lablPtr.Type = SymbolFlag.DEFABS;
                    lablPtr.Value = lval;
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
                    if (lablPtr.Reserved != 0)
                    {
                        outPr.FatalError("Reserved symbol!");
                        return (-1);
                    }

                    /* compare the values */
                    if (lablPtr.Value == lval) break;

                    /* normal label */
                    lablPtr.Type = SymbolFlag.MDEF;
                    lablPtr.Value = 0;
                    outPr.Error("Label multiply defined!");
                    return (-1);
                }
            }

            /* second pass */
            else
            {
                if ((lablPtr.Value != lval) ||
                   ((flag != 0) && (ctx.Bank < ctx.BankLimit) && (lablPtr.Bank != ctx.BankBase + ctx.Bank)))
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
                    lablPtr.Proc = ctx.ProcPtr;
                }
                lablPtr.Bank = ctx.BankBase + ctx.Bank;
                lablPtr.Page = ctx.Page;

                /* check if it's a local or global symbol */
                c = lablPtr.Name[0];
                if (c == '.')
                    /* local */
                    ctx.LastLabl = null;
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

                var lablPtr = ctx.LablPtr;
                if (lablPtr != null)
                {
                    lablPtr.Type = SymbolFlag.DEFABS;
                    lablPtr.Value = val;
                    lablPtr.Reserved = 1;
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
            NesAsmSymbol sym;

            int i;

            /* browse the symbol table */
            for (i = 0; i < 256; i++)
            {
                sym = ctx.HashTbl[i];
                while (sym != null)
                {
                    /* remap the bank */
                    if (sym.Bank <= ctx.BankLimit)
                    {
                        sym.Bank += ctx.BankBase;
                    }
                    sym = sym.Next;
                }
            }
        }


    }
}