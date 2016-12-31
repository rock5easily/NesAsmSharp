using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class MacroProcessor : ProcessorBase
    {
        public MacroProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        /* .macro pseudo */
        public void DoMacro(ref int ip)
        {
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
            else
            {
                /* error checking */
                if (ctx.IsExpandMacro)
                {
                    outPr.Error("Can not nest macro definitions!");
                    return;
                }
                if (ctx.LablPtr == null)
                {
                    /* skip spaces */
                    while (CharUtil.IsSpace(ctx.PrLnBuf[ip]))
                        ip++;

                    /* search a label after the .macro */
                    if (symPr.ColSym(ref ip) == 0)
                    {
                        outPr.Error("No name for this macro!");
                        return;
                    }

                    /* put the macro name in the symbol table */
                    if ((ctx.LablPtr = symPr.STLook(1)) == null)
                        return;
                }
                if (ctx.LablPtr.RefCnt != 0)
                {
                    switch (ctx.LablPtr.Type)
                    {
                    case SymbolFlag.MACRO:
                        outPr.FatalError("Macro already defined!");
                        return;

                    case SymbolFlag.FUNC:
                        outPr.FatalError("Symbol already used by a function!");
                        return;

                    default:
                        outPr.FatalError("Symbol already used by a label!");
                        return;
                    }
                }
                if (asmPr.CheckEOL(ref ip) == 0) return;

                /* install this new macro in the hash table */
                if (MacroInstall() == 0) return;
            }
            ctx.InMacro = true;
        }

        /* .endm pseudo */
        public void Do_Endm(ref int ip)
        {
            outPr.Error("Unexpected ENDM!");
            return;
        }

        /* search a macro in the hash table */

        public NesAsmMacro MacroLook(ref int ip)
        {
            NesAsmMacro ptr;
            char c;
            int hash;
            int l;
            var sb = new StringBuilder();

            /* calculate the symbol hash value and check syntax */
            l = 0;
            hash = 0;
            for (;;)
            {
                c = ctx.PrLnBuf[ip];
                if (c == '\0' || c == ' ' || c == '\t' || c == ';') break;
                if (!CharUtil.IsAlNum(c) && c != '_') return null;
                if (l == 0)
                {
                    if (char.IsDigit(c)) return null;
                }
                if (l == 31) return null;
                sb.Append(c);
                l++;
                hash += c;
                hash = (hash << 3) + (hash >> 5) + c;
                ip++;
            }
            var name = sb.ToString();
            hash &= 0xFF;

            /* browse the hash table */
            ptr = ctx.MacroTbl[hash];
            while (ptr != null)
            {
                if (name == ptr.Name) break;
                ptr = ptr.Next;
            }

            /* return result */
            return ptr;
        }

        /* extract macro arguments */
        public int MacroGetArgs(int ip)
        {
            char c, t;
            int i, j, f, arg;
            int level;

            /* can not nest too much macros */
            if (ctx.MIdx == 7)
            {
                outPr.Error("Too many nested macro calls!");
                return (0);
            }

            /* initialize args */
            ctx.MCntStack.Push(ctx.MCounter);
            ctx.MStack.Push(ctx.MLPtr);
            ctx.MIdx++;
            var ptr = new ArrayPointer<char>(ctx.MArg[ctx.MIdx, 0]);
            arg = 0;

            for (i = 0; i < 9; i++)
            {
                ctx.MArg[ctx.MIdx, i][0] = '\0';
            }

            /* extract args */
            for (;;)
            {
                /* skip spaces */
                while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

                c = ctx.PrLnBuf[ip++];
                switch (c)
                {
                /* no arg */
                case ',':
                    arg++;
                    ptr = new ArrayPointer<char>(ctx.MArg[ctx.MIdx, arg]);
                    if (arg == 9)
                    {
                        outPr.Error("Too many arguments for a macro!");
                        return (0);
                    }
                    break;
                /* string */
                case '{':
                    c = '}';
                    goto case '\"';
                case '\"':
                    i = 0;
                    if (c == '\"')
                    {
                        ptr[i++] = c;
                    }
                    for (;;)
                    {
                        t = ctx.PrLnBuf[ip++];
                        if (t == '\0')
                        {
                            outPr.Error("Unterminated string!");
                            return (0);
                        }
                        if (i == 80)
                        {
                            outPr.Error("String too long, max. 80 characters!");
                            return (0);
                        }
                        if (t == c) break;
                        ptr[i++] = t;
                    }
                    if (c == '\"') ptr[i++] = t;

                    /* skip spaces */
                    while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

                    /* check end of arg */
                    switch (ctx.PrLnBuf[ip])
                    {
                    case '\0':
                    case ',':
                    case ';':
                        break;
                    default:
                        outPr.Error("Syntax error!");
                        return (0);
                    }

                    /* end arg string */
                    ptr[i] = '\0';
                    break;
                /* end of line */
                case ';':
                case '\0':
                    return (1);
                /* continuation char */
                case '\\':
                    /* skip spaces */
                    i = ip;
                    while (CharUtil.IsSpace(ctx.PrLnBuf[i])) i++;

                    /* check */
                    if (ctx.PrLnBuf[i] == ';' || ctx.PrLnBuf[i] == '\0')
                    {
                        /* output line */
                        if (ctx.Pass == PassFlag.LAST_PASS)
                        {
                            outPr.PrintLn();
                            outPr.ClearLn();
                        }

                        /* read a new line */
                        if (inPr.ReadLine() == -1) return (0);

                        /* rewind line pointer and continue */
                        ip = Definition.SFIELD;
                        break;
                    }
                    goto default;
                /* other */
                default:
                    i = 0;
                    j = 0;
                    f = 0;
                    level = 0;
                    while (c != 0)
                    {
                        if (c == ',')
                        {
                            if (level == 0) break;
                        }
                        else if ((c == '(') || (c == '['))
                        {
                            level++;
                        }
                        else if ((c == ')') || (c == ']'))
                        {
                            if (level != 0)
                            {
                                level--;
                            }
                        }
                        else if (c == ';')
                        {
                            break;
                        }

                        if (f != 0)
                        {
                            if (c != ' ')
                            {
                                while (i < j) ptr[i++] = ' ';
                                ptr[i++] = c;
                                f = 0;
                            }
                        }
                        else if (c == ' ')
                        {
                            f = 1;
                        }
                        else
                        {
                            ptr[i++] = c;
                        }
                        if (i == 80)
                        {
                            outPr.Error("Macro argument string too long, max. 80 characters!");
                            return (0);
                        }
                        j++;
                        c = ctx.PrLnBuf[ip++];
                    }
                    ptr[i] = '\0';
                    ip--;

                    /* check if arg is X or Y */
                    if (ptr.GetLengthAsNullTerminated() > 0 && arg > 0)
                    {
                        c = char.ToLower(ptr[0]);

                        if ((c == 'x') || (c == 'y'))
                        {
                            var str = ptr.ToStringFromNullTerminated().ToLower();
                            if (str == "x++" || str == "y++" || str.Length == 1)
                            {
                                arg--;
                                ptr = new ArrayPointer<char>(ctx.MArg[ctx.MIdx, arg]);

                                /* check string length */
                                if (ptr.GetLengthAsNullTerminated() > 75)
                                {
                                    outPr.Error("Macro argument string too long, max. 80 characters!");
                                    return (0);
                                }

                                /* attach current arg to the previous one */
                                ptr.AppendAsNullTerminated(",");
                                ptr.AppendAsNullTerminated(ctx.MArg[ctx.MIdx, arg + 1]);
                                ptr = new ArrayPointer<char>(ctx.MArg[ctx.MIdx, arg + 1]);
                                ptr[0] = '\0';
                            }
                        }
                    }
                    break;
                }
            }
        }

        /* install a macro in the hash table */

        public int MacroInstall()
        {
            var symstr = ctx.Symbol.ToStringFromNullTerminated();

            /* mark the macro name as reserved */
            ctx.LablPtr.Type = SymbolFlag.MACRO;

            /* check macro name syntax */
            if (symstr.Contains("."))
            {
                outPr.Error("Invalid macro name!");
                return 0;
            }

            /* calculate symbol hash value */
            var hash = symPr.SymHash();

            /* allocate a macro struct */
            var mptr = new NesAsmMacro();
            /* initialize it */
            mptr.Name = symstr;
            mptr.Next = ctx.MacroTbl[hash];
            ctx.MacroTbl[hash] = mptr;
            ctx.MPtr = mptr;
            ctx.MLPtr = null;

            /* ok */
            return 1;
        }

        /* send back the addressing mode of a macro arg */
        public MacroArgumentType MacroGetArgType(string argstr)
        {
            NesAsmSymbol sym;
            char c = '\0';
            int i;
            var arg = argstr.ToNullTerminatedCharArray();
            int arg_ptr = 0;

            /* skip spaces */
            while (CharUtil.IsSpace(arg[arg_ptr]))
            {
                arg_ptr++;
            }

            /* get type */
            switch (char.ToUpper(arg[arg_ptr++]))
            {
            case '\0':
                return (MacroArgumentType.NO_ARG);
            case '"':
                return (MacroArgumentType.ARG_STRING);
            case '#':
                return (MacroArgumentType.ARG_IMM);
            case '[':
                return (MacroArgumentType.ARG_INDIRECT);
            case 'A':
            case 'X':
            case 'Y':
                if (arg_ptr == arg.GetLengthAsNullTerminated())
                {
                    return (MacroArgumentType.ARG_REG);
                }
                goto default;
            default:
                /* symbol */
                for (i = 0; i < Definition.SBOLSZ; i++)
                {
                    c = arg[i];
                    if (char.IsDigit(c) && (i == 0)) break;
                    if ((!CharUtil.IsAlNum(c)) && (c != '_') && (c != '.')) break;
                }

                if (i == 0)
                {
                    return (MacroArgumentType.ARG_ABS);
                }
                else
                {
                    if (c != '\0')
                    {
                        return (MacroArgumentType.ARG_ABS);
                    }
                    else
                    {
                        ctx.Symbol.CopyAsNullTerminated(arg, i);

                        if ((sym = symPr.STLook(0)) == null)
                        {
                            return (MacroArgumentType.ARG_LABEL);
                        }
                        else
                        {
                            if (sym.Type == SymbolFlag.UNDEF || sym.Type == SymbolFlag.IFUNDEF) return (MacroArgumentType.ARG_LABEL);

                            if (sym.Bank == Definition.RESERVED_BANK)
                            {
                                return (MacroArgumentType.ARG_ABS);
                            }
                            else
                            {
                                return (MacroArgumentType.ARG_LABEL);
                            }
                        }
                    }
                }
            }
        }


    }
}
