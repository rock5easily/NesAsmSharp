using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class FuncProcessor : ProcessorBase
    {
        public FuncProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        /// <summary>
        /// .func pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoFunc(ref int ip)
        {
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
            else
            {
                var lablPtr = ctx.LablPtr;
                /* error checking */
                if (lablPtr == null)
                {
                    outPr.Error("No name for this function!");
                    return;
                }
                if (lablPtr.RefCnt > 0)
                {
                    switch (lablPtr.Type)
                    {
                    case SymbolFlag.MACRO:
                        outPr.FatalError("Symbol already used by a macro!");
                        return;
                    case SymbolFlag.FUNC:
                        outPr.FatalError("Function already defined!");
                        return;
                    default:
                        outPr.FatalError("Symbol already used by a label!");
                        return;
                    }
                }

                /* install this new function in the hash table */
                if (FuncInstall(ip) == 0) return;
            }
        }

        /// <summary>
        /// search a function
        /// </summary>
        /// <returns></returns>
        public int FuncLook()
        {
            var symstr = ctx.Symbol.ToStringFromNullTerminated();

            /* search the function in the hash table */
            if (ctx.FuncTbl.ContainsKey(symstr))
            {
                ctx.FuncPtr = ctx.FuncTbl[symstr];
                /* ok */
                return 1;
            }

            /* didn't find a function with this name */
            return 0;
        }

        /// <summary>
        /// install a function in the hash table
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public int FuncInstall(int ip)
        {
            /* mark the function name as reserved */
            ctx.LablPtr.Type = SymbolFlag.FUNC;
            var symstr = ctx.Symbol.ToStringFromNullTerminated();

            /* check function name syntax */
            if (symstr.Contains("."))
            {
                outPr.Error("Invalid function name!");
                return 0;
            }

            /* extract function body */
            if (FuncExtract(ip) == -1) return 0;

            /* allocate a new func struct */
            var func = new NesAsmFunc();
            /* initialize it */
            func.Name = symstr;
            func.Line.CopyAsNullTerminated(ctx.FuncLine);

            ctx.FuncTbl[symstr] = func;
            ctx.FuncPtr = func;
            /* ok */
            return 1;
        }

        /// <summary>
        /// extract function body
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public int FuncExtract(int ip)
        {
            char c;
            int arg, max_arg;
            bool end;

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

            /* get function body */
            var line = ctx.FuncLine;
            var line_ptr = 0;
            max_arg = 0;
            end = false;

            while (!end)
            {
                c = ctx.PrLnBuf[ip++];
                switch (c)
                {
                /* end of line */
                case ';':
                case '\0':
                    line[line_ptr++] = '\0';
                    end = true;
                    break;
                /* function arg */
                case '\\':
                    line[line_ptr++] = c;
                    c = ctx.PrLnBuf[ip++];
                    if ((c < '1') || (c > '9'))
                    {
                        outPr.Error("Invalid function argument!");
                        return (-1);
                    }
                    arg = c - '1';
                    if (max_arg < arg) max_arg = arg;
                    goto default;
                /* other */
                default:
                    line[line_ptr++] = c;
                    if (line_ptr == line.Length - 1)
                    {
                        outPr.Error("Function line too long!");
                        return (-1);
                    }
                    break;
                }
            }

            /* return the number of args */
            return (max_arg);
        }

        /// <summary>
        /// extract function args
        /// </summary>
        /// <returns></returns>
        public int FuncGetArgs()
        {
            char c;
            int level;
            bool space, flag;
            int i, x;

            /* can not nest too much macros */
            if (ctx.FuncIdx == 7)
            {
                outPr.Error("Too many nested function calls!");
                return (0);
            }

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.Expr.Value)) ctx.Expr++;

            /* function args must be enclosed in parenthesis */
            if (ctx.Expr.Read() != '(') return (0);

            /* initialize args */
            ArrayPointer<char> line = null;
            var ptr = new ArrayPointer<char>(ctx.FuncArg[ctx.FuncIdx, 0], 0);
            var arg = 0;

            for (i = 0; i < 9; i++)
            {
                ctx.FuncArg[ctx.FuncIdx, i][0] = '\0';
            }

            /* get args one by one */
            for (;;)
            {
                /* skip spaces */
                while (CharUtil.IsSpace(ctx.Expr.Value)) ctx.Expr++;

                c = ctx.Expr.Read();
                switch (c)
                {
                /* empty arg */
                case ',':
                    arg++;
                    ptr = new ArrayPointer<char>(ctx.FuncArg[ctx.FuncIdx, arg], 0);
                    if (arg == 9)
                    {
                        outPr.Error("Too many arguments for a function!");
                        return (0);
                    }
                    break;
                /* end of line */
                case ';':
                case '\0':
                    outPr.Error("Syntax error in function call!");
                    return (0);
                /* end of function */
                case ')':
                    return (1);
                /* arg */
                default:
                    space = false;
                    level = 0;
                    flag = false;
                    i = 0;
                    x = 0;
                    for (;;)
                    {
                        if (c == '\0')
                        {
                            if (!flag)
                            {
                                break;
                            }
                            else
                            {
                                flag = false;
                                c = ctx.Expr.Read();
                                continue;
                            }
                        }
                        else if (c == ';')
                        {
                            break;
                        }
                        else if (c == ',')
                        {
                            if (level == 0) break;
                        }
                        else if (c == '\\')
                        {
                            if (ctx.FuncIdx == 0)
                            {
                                outPr.Error("Syntax error!");
                                return (0);
                            }
                            c = ctx.Expr.Read();
                            if (c < '1' || c > '9')
                            {
                                outPr.Error("Invalid function argument index!");
                                return (0);
                            }
                            line = new ArrayPointer<char>(ctx.FuncArg[ctx.FuncIdx - 1, c - '1'], 0);
                            flag = true;
                            c = line.Read();
                            continue;
                        }
                        else if (c == '(')
                        {
                            level++;
                        }
                        else if (c == ')')
                        {
                            if (level == 0) break;
                            level--;
                        }

                        if (space)
                        {
                            if (c != ' ')
                            {
                                while (i < x)
                                {
                                    ptr[i++] = ' ';
                                }
                                ptr[i++] = c;
                                space = false;
                            }
                        }
                        else if (c == ' ')
                        {
                            space = true;
                        }
                        else
                        {
                            ptr[i++] = c;
                        }

                        if (i > Definition.FUNC_ARG_MAX_LEN)
                        {
                            outPr.Error("Invalid function argument length!");
                            return (0);
                        }
                        x++;
                        if (flag)
                        {
                            c = line.Read();
                        }
                        else
                        {
                            c = ctx.Expr.Read();
                        }
                    }
                    ptr[i] = '\0';
                    ctx.Expr--;
                    break;
                }
            }
        }
    }
}
