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
            string name = null;
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
                    while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

                    /* search a label after the .macro */
                    if ((name = symPr.ReadSymbolNameFromPrLnBuf(ref ip)) == null)
                    {
                        outPr.Error("No name for this macro!");
                        return;
                    }

                    /* put the macro name in the symbol table */
                    if ((ctx.LablPtr = symPr.LookUpSymbolTable(name, true)) == null) return;
                }
                else
                {
                    name = ctx.LablPtr.Name;
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
                if (MacroInstall(name) == 0) return;
            }
            ctx.InMacro = true;
        }

        /* .endm pseudo */
        public void DoEndm(ref int ip)
        {
            outPr.Error("Unexpected ENDM!");
            return;
        }

        /* search a macro in the hash table */

        public NesAsmMacro MacroLook(ref int ip)
        {
            char c;
            char[] buf = new char[Definition.MACROSZ + 1];

            /* calculate the symbol hash value and check syntax */
            var l = 0;
            for (;;)
            {
                c = ctx.PrLnBuf[ip];
                if (c == '\0' || c == ' ' || c == '\t' || c == ';') break;
                if (!CharUtil.IsAlNum(c) && c != '_') return null;
                if (l == 0 && char.IsDigit(c)) return null;
                if (l == Definition.MACROSZ) return null;
                buf[l++] = c;
                ip++;
            }
            buf[l] = '\0';

            var name = buf.ToStringFromNullTerminated();

            /* browse the hash table */
            /* return result */
            return ctx.MacroTbl.GetValueOrDefault(name);
        }

        /* extract macro arguments */
        public int MacroGetArgs(int ip)
        {
            char c, t;
            int i, j, f, arg;
            int level;

            /* can not nest too much macros */
            if (ctx.MacroIdx == 7)
            {
                outPr.Error("Too many nested macro calls!");
                return (0);
            }

            /* initialize args */
            ctx.MacroCntStack.Push(ctx.MacroCounter);
            ctx.MacroStack.Push(ctx.MacroLinePtr);
            ctx.MacroIdx++;
            var ptr = new ArrayPointer<char>(ctx.MacroArg[ctx.MacroIdx, 0]);
            arg = 0;

            for (i = 0; i < 9; i++)
            {
                ctx.MacroArg[ctx.MacroIdx, i][0] = '\0';
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
                    ptr = new ArrayPointer<char>(ctx.MacroArg[ctx.MacroIdx, arg]);
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
                            outPr.ClearPrLnBuf();
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
                                ptr = new ArrayPointer<char>(ctx.MacroArg[ctx.MacroIdx, arg]);

                                /* check string length */
                                if (ptr.GetLengthAsNullTerminated() > 75)
                                {
                                    outPr.Error("Macro argument string too long, max. 80 characters!");
                                    return (0);
                                }

                                /* attach current arg to the previous one */
                                ptr.AppendAsNullTerminated(",");
                                ptr.AppendAsNullTerminated(ctx.MacroArg[ctx.MacroIdx, arg + 1]);
                                ptr = new ArrayPointer<char>(ctx.MacroArg[ctx.MacroIdx, arg + 1]);
                                ptr[0] = '\0';
                            }
                        }
                    }
                    break;
                }
            }
        }

        /* install a macro in the hash table */

        public int MacroInstall(string name)
        {
            /* mark the macro name as reserved */
            ctx.LablPtr.Type = SymbolFlag.MACRO;

            /* check macro name syntax */
            if (name.Contains("."))
            {
                outPr.Error("Invalid macro name!");
                return 0;
            }

            /* allocate a macro struct */
            var mptr = new NesAsmMacro();
            /* initialize it */
            mptr.Name = name;
            ctx.MacroTbl[name] = mptr;
            ctx.MacroPtr = mptr;
            ctx.MacroLinePtr = null;

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
                        var name = arg.ToStringFromNullTerminated(0, i);
                        if ((sym = symPr.LookUpSymbolTable(name, false)) == null)
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
