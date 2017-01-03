using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class ExprProcessor : ProcessorBase
    {
        /* operator priority */
        public static readonly int[] OpPriority = {
            0 /* START */,  0 /* OPEN  */,
            7 /* ADD   */,  7 /* SUB   */,  8 /* MUL   */,  8 /* DIV   */,
            8 /* MOD   */, 10 /* NEG   */,  6 /* SHL   */,  6 /* SHR   */,
            1 /* OR    */,  2 /* XOR   */,  3 /* AND   */, 10 /* COM   */,
            9 /* NOT   */,  4 /* =     */,  4 /* <>    */,  5 /* <     */,
            5 /* <=    */,  5 /* >     */,  5 /* >=    */,
            10 /* DEFIN.*/, 10 /* HIGH  */, 10 /* LOW   */, 10 /* PAGE  */,
            10 /* BANK  */, 10 /* VRAM  */, 10 /* PAL   */, 10 /* SIZEOF*/,
            10 /* REGIONSIZE */
        };

        public static readonly string[] Keyword = {    /* predefined functions */
            "DEFINED",
            "HIGH",
            "LOW",
            "PAGE",
            "BANK",
            "VRAM",
            "PAL",
            "SIZEOF",
            "REGIONSIZE",
        };

        public ExprProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        /// <summary>
        /// evaluate an expression
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="last_char"></param>
        /// <returns></returns>
        public int Evaluate(ref int ip, char last_char)
        {
            int level;
            int end;
            OperatorType op;
            ValueType type;
            int arg;
            int i;
            char c;

            end = 0;
            level = 0;
            ctx.Undef = 0;
            ctx.Value = 0;
            ctx.OpStack.Clear();
            ctx.ValStack.Clear();
            ctx.ExprStack.Clear();
            ctx.NeedOperator = false;
            ctx.ExprLablPtr = null;
            ctx.ExprLablCnt = 0;

            op = OperatorType.OP_START;
            ctx.OpStack.Push(op);
            ctx.FuncIdx = 0;

            /* array index to pointer */
            ctx.Expr = new ArrayPointer<char>(ctx.PrLnBuf, ip);

        /* skip spaces */
        cont:
            while (CharUtil.IsSpace(ctx.Expr.Value)) ctx.Expr++;

            /* search for a continuation char */
            if (ctx.Expr.Value == '\\')
            {
                /* skip spaces */
                i = 1;
                while (CharUtil.IsSpace(ctx.Expr[i])) i++;

                /* check if end of line */
                if (ctx.Expr[i] == ';' || ctx.Expr[i] == '\0')
                {
                    /* output */
                    if (!ctx.ContinuedLine)
                    {
                        /* replace '\' with three dots */
                        ctx.Expr.CopyAsNullTerminated("...");

                        /* store the current line */
                        ctx.TmpLnBuf.CopyAsNullTerminated(ctx.PrLnBuf);
                    }

                    /* ok */
                    ctx.ContinuedLine = true;

                    /* read a new line */
                    if (inPr.ReadLine() == -1) return (0);

                    /* rewind line pointer and continue */
                    ctx.Expr = new ArrayPointer<char>(ctx.PrLnBuf, Definition.SFIELD);
                    goto cont;
                }
            }

            /* parser main loop */
            while (end == 0)
            {
                c = ctx.Expr.Value;

                /* number */
                if (char.IsDigit(c))
                {
                    if (ctx.NeedOperator) goto error;
                    if (PushVal(ValueType.T_DECIMAL) == 0) return (0);
                }
                /* symbol */
                else if (CharUtil.IsAlpha(c) || c == '_' || c == '.')
                {
                    if (ctx.NeedOperator) goto error;
                    if (PushVal(ValueType.T_SYMBOL) == 0) return (0);
                }
                /* operators */
                else
                {
                    switch (c)
                    {
                    /* function arg */
                    case '\\':
                        if (ctx.FuncIdx == 0)
                        {
                            outPr.Error("Syntax error in expression!");
                            return (0);
                        }
                        ctx.Expr++;
                        c = ctx.Expr.Read();
                        if (c < '1' || c > '9')
                        {
                            outPr.Error("Invalid function argument index!");
                            return (0);
                        }
                        arg = c - '1';

                        ctx.ExprStack.Push(ctx.Expr);
                        ctx.FuncIdx++;

                        ctx.Expr = new ArrayPointer<char>(ctx.FuncArg[ctx.FuncIdx - 2, arg]);
                        break;
                    /* hexa prefix */
                    case '$':
                        if (ctx.NeedOperator) goto error;
                        if (PushVal(ValueType.T_HEXA) == 0) return (0);
                        break;
                    /* character prefix */
                    case '\'':
                        if (ctx.NeedOperator) goto error;
                        if (PushVal(ValueType.T_CHAR) == 0) return (0);
                        break;
                    /* round brackets */
                    case '(':
                        if (ctx.NeedOperator) goto error;
                        if (PushOp(OperatorType.OP_OPEN) == 0) return (0);
                        level++;
                        ctx.Expr++;
                        break;
                    case ')':
                        if (!ctx.NeedOperator) goto error;
                        if (level == 0) goto error;
                        while (ctx.OpStack.Peek() != OperatorType.OP_OPEN)
                        {
                            if (DoOp() == 0) return (0);
                        }
                        ctx.OpStack.Pop();
                        level--;
                        ctx.Expr++;
                        break;
                    /* not equal, left shift, lower, lower or equal */
                    case '<':
                        if (!ctx.NeedOperator) goto error;
                        ctx.Expr++;
                        switch (ctx.Expr.Value)
                        {
                        case '>':
                            op = OperatorType.OP_NOT_EQUAL;
                            ctx.Expr++;
                            break;
                        case '<':
                            op = OperatorType.OP_SHL;
                            ctx.Expr++;
                            break;
                        case '=':
                            op = OperatorType.OP_LOWER_EQUAL;
                            ctx.Expr++;
                            break;
                        default:
                            op = OperatorType.OP_LOWER;
                            break;
                        }
                        if (PushOp(op) == 0) return (0);
                        break;
                    /* right shift, higher, higher or equal */
                    case '>':
                        if (!ctx.NeedOperator) goto error;
                        ctx.Expr++;
                        switch (ctx.Expr.Value)
                        {
                        case '>':
                            op = OperatorType.OP_SHR;
                            ctx.Expr++;
                            break;
                        case '=':
                            op = OperatorType.OP_HIGHER_EQUAL;
                            ctx.Expr++;
                            break;
                        default:
                            op = OperatorType.OP_HIGHER;
                            break;
                        }
                        if (PushOp(op) == 0) return (0);
                        break;
                    /* equal */
                    case '=':
                        if (!ctx.NeedOperator) goto error;
                        if (PushOp(OperatorType.OP_EQUAL) == 0) return (0);
                        ctx.Expr++;
                        break;
                    /* one complement */
                    case '~':
                        if (ctx.NeedOperator) goto error;
                        if (PushOp(OperatorType.OP_COM) == 0) return (0);
                        ctx.Expr++;
                        break;
                    /* sub, neg */
                    case '-':
                        if (ctx.NeedOperator)
                        {
                            op = OperatorType.OP_SUB;
                        }
                        else
                        {
                            op = OperatorType.OP_NEG;
                        }

                        if (PushOp(op) == 0) return (0);
                        ctx.Expr++;
                        break;
                    /* not, not equal */
                    case '!':
                        if (!ctx.NeedOperator)
                        {
                            op = OperatorType.OP_NOT;
                        }
                        else
                        {
                            op = OperatorType.OP_NOT_EQUAL;
                            ctx.Expr++;
                            if (ctx.Expr.Value != '=') goto error;
                        }
                        if (PushOp(op) == 0) return (0);
                        ctx.Expr++;
                        break;
                    /* binary prefix, current PC */
                    case '%':
                    case '*':
                        if (!ctx.NeedOperator)
                        {
                            if (c == '%')
                            {
                                type = ValueType.T_BINARY;
                            }
                            else
                            {
                                type = ValueType.T_PC;
                            }
                            if (PushVal(type) == 0) return (0);
                            break;
                        }
                        goto case '+';
                    /* modulo, mul, add, div, and, xor, or */
                    case '+':
                    case '/':
                    case '&':
                    case '^':
                    case '|':
                        if (!ctx.NeedOperator) goto error;
                        switch (c)
                        {
                        case '%': op = OperatorType.OP_MOD; break;
                        case '*': op = OperatorType.OP_MUL; break;
                        case '+': op = OperatorType.OP_ADD; break;
                        case '/': op = OperatorType.OP_DIV; break;
                        case '&': op = OperatorType.OP_AND; break;
                        case '^': op = OperatorType.OP_XOR; break;
                        case '|': op = OperatorType.OP_OR; break;
                        }
                        if (PushOp(op) == 0) return (0);
                        ctx.Expr++;
                        break;
                    /* skip immediate operand prefix if in macro */
                    case '#':
                        if (ctx.IsExpandMacro)
                        {
                            ctx.Expr++;
                        }
                        else
                        {
                            end = 3;
                        }
                        break;
                    // string
                    case '\"':
                        if (PushVal(ValueType.T_STRING) == 0) return (0);
                        break;
                    /* space or tab */
                    case ' ':
                    case '\t':
                        ctx.Expr++;
                        break;
                    /* end of line */
                    case '\0':
                        if (ctx.FuncIdx != 0)
                        {
                            ctx.FuncIdx--;
                            ctx.Expr = ctx.ExprStack.Pop();
                            break;
                        }
                        goto case ';';
                    case ';':
                        end = 1;
                        break;
                    case ',':
                        end = 2;
                        break;
                    default:
                        end = 3;
                        break;
                    }
                }
            }

            if (!ctx.NeedOperator) goto error;
            if (level != 0) goto error;

            while (ctx.OpStack.Peek() != OperatorType.OP_START)
            {
                if (DoOp() == 0) return (0);
            }

            /* get the expression value */
            ctx.Value = ctx.ValStack.Pop();

            /* any undefined symbols? trap that if in the last pass */
            if (ctx.Undef != 0)
            {
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.Error("Undefined symbol in operand field!");
                }
            }

            /* check if the last char is what the user asked for */
            switch (last_char)
            {
            case ';':
                if (end != 1) goto error;
                ctx.Expr++;
                break;
            case ',':
                if (end != 2)
                {
                    outPr.Error("Argument missing!");
                    return (0);
                }
                ctx.Expr++;
                break;
            }

            /* convert back the pointer to an array index */
            ip = ctx.Expr.Current;

            /* ok */
            return (1);

        /* syntax error */
        error:
            outPr.Error("Syntax error in expression!");
            return (0);
        }

        /// <summary>
        /// extract a number and push it on the value stack
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int PushVal(ValueType type)
        {
            uint mul, val;
            OperatorType op;

            val = 0;
            var c = ctx.Expr.Value;

            switch (type)
            {
            /* program counter */
            case ValueType.T_PC:
                if (ctx.DataLocCnt == -1)
                {
                    val = (uint)(ctx.LocCnt + (ctx.Page << 13));
                }
                else
                {
                    val = (uint)(ctx.DataLocCnt + (ctx.Page << 13));
                }
                ctx.Expr++;
                break;
            /* char ascii value */
            case ValueType.T_CHAR:
                ctx.Expr++;
                val = ctx.Expr.Read();
                if ((ctx.Expr.Value != c) || (val == 0))
                {
                    outPr.Error("Syntax Error!");
                    return (0);
                }
                ctx.Expr++;
                break;
            /* symbol */
            case ValueType.T_SYMBOL:
                /* extract it */
                if (GetSym() == 0) return (0);

                /* an user function? */
                if (funcPr.FuncLook() != 0)
                {
                    if (funcPr.FuncGetArgs() == 0) return (0);

                    ctx.ExprStack.Push(ctx.Expr);
                    ctx.FuncIdx++;

                    ctx.Expr = new ArrayPointer<char>(ctx.FuncPtr.Line);
                    return (1);
                }

                /* a predefined function? */
                op = CheckKeyword();
                if (op != 0)
                {
                    if (PushOp(op) == 0)
                    {
                        return (0);
                    }
                    else
                    {
                        return (1);
                    }
                }

                /* search the symbol */
                ctx.ExprLablPtr = symPr.STLook(1);

                /* check if undefined, if not get its value */
                if (ctx.ExprLablPtr == null)
                {
                    return (0);
                }
                else if (ctx.ExprLablPtr.Type == SymbolFlag.UNDEF)
                {
                    ctx.Undef++;
                }
                else if (ctx.ExprLablPtr.Type == SymbolFlag.IFUNDEF)
                {
                    ctx.Undef++;
                }
                else
                {
                    val = (uint)ctx.ExprLablPtr.Value;
                }

                /* remember we have seen a symbol in the expression */
                ctx.ExprLablCnt++;
                break;
            /* binary number %1100_0011 */
            case ValueType.T_BINARY:
                mul = 2;
                goto extract;
            /* hexa number $15AF */
            case ValueType.T_HEXA:
                mul = 16;
                goto extract;
            /* decimal number 48 (or hexa 0x5F) */
            case ValueType.T_DECIMAL:
                if ((c == '0') && (char.ToUpper(ctx.Expr[1]) == 'X'))
                {
                    mul = 16;
                    ctx.Expr++;
                }
                else
                {
                    mul = 10;
                    val = (uint)(c - '0');
                }
            /* extract a number */
            extract:
                for (;;)
                {
                    ctx.Expr++;
                    c = ctx.Expr.Value;

                    if (char.IsDigit(c))
                    {
                        c -= '0';
                    }
                    else if (CharUtil.IsAlpha(c))
                    {
                        c = char.ToUpper(c);
                        if (c < 'A' && c > 'F')
                        {
                            break;
                        }
                        else
                        {
                            c -= 'A';
                            c = (char)(c + 10);
                        }
                    }
                    else if (c == '_' && mul == 2)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }

                    if (c >= mul) break;
                    val = (val * mul) + c;
                }
                break;
            case ValueType.T_STRING:
                var strId = RegisterString(ctx.Expr);
                if (strId < 0) return (0);
                val = (uint)strId;
                break;
            }

            /* check for too big expression */
            if (ctx.ValStack.Count > Definition.VALSTACK_MAX)
            {
                outPr.Error("Expression too complex!");
                return (0);
            }

            /* push the result on the value stack */
            ctx.ValStack.Push(val);

            /* next must be an operator */
            ctx.NeedOperator = true;

            /* ok */
            return (1);
        }

        /// <summary>
        /// extract a symbol name from the input string
        /// </summary>
        /// <returns></returns>
        public int GetSym()
        {
            var symbol = ctx.Symbol;
            bool valid;
            int i;
            char c;

            valid = true;
            i = 0;

            /* get the symbol, stop to the first 'non symbol' char */
            while (valid)
            {
                c = ctx.Expr.Value;
                // 最初の1文字目は数字ダメ
                if (CharUtil.IsAlpha(c) || c == '_' || c == '.' || (char.IsDigit(c) && i >= 1))
                {
                    if (i < Definition.SBOLSZ)
                    {
                        symbol[i++] = c;
                    }
                    else
                    {
                        outPr.Error("Symbol name is too long!");
                        i = 0;
                        break;
                    }
                    ctx.Expr++;
                }
                else
                {
                    valid = false;
                }
            }

            /* is it a reserved symbol? */
            if (i == 1)
            {
                switch (char.ToUpper(symbol[0]))
                {
                case 'A':
                case 'X':
                case 'Y':
                    outPr.Error("Symbol is reserved (A, X or Y)!");
                    i = 0;
                    break;
                }
            }

            /* store symbol length */
            symbol[i] = '\0';
            return i;
        }


        /// <summary>
        /// verify if the current symbol is a reserved function
        /// </summary>
        /// <returns></returns>
        public OperatorType CheckKeyword()
        {
            OperatorType op = 0;
            var symupstr = ctx.Symbol.ToStringFromNullTerminated().ToUpper();

            /* check if its an assembler function */
            if (symupstr == Keyword[0])
                op = OperatorType.OP_DEFINED;
            else if (symupstr == Keyword[1])
                op = OperatorType.OP_HIGH;
            else if (symupstr == Keyword[2])
                op = OperatorType.OP_LOW;
            else if (symupstr == Keyword[3])
                op = OperatorType.OP_PAGE;
            else if (symupstr == Keyword[4])
                op = OperatorType.OP_BANK;
            else if (symupstr == Keyword[7])
                op = OperatorType.OP_SIZEOF;
            else if (symupstr == Keyword[8])
                op = OperatorType.OP_REGIONSIZE;
            else
            {
                if (ctx.Machine.Type == MachineType.MACHINE_PCE)
                {
                    /* PCE specific functions */
                    if (symupstr == Keyword[5])
                        op = OperatorType.OP_VRAM;
                    else if (symupstr == Keyword[6])
                        op = OperatorType.OP_PAL;
                }
            }

            /* extra setup for functions that send back symbol infos */
            switch (op)
            {
            case OperatorType.OP_DEFINED:
            case OperatorType.OP_HIGH:
            case OperatorType.OP_LOW:
            case OperatorType.OP_PAGE:
            case OperatorType.OP_BANK:
            case OperatorType.OP_VRAM:
            case OperatorType.OP_PAL:
            case OperatorType.OP_SIZEOF:
                ctx.ExprLablPtr = null;
                ctx.ExprLablCnt = 0;
                break;
            }

            /* ok */
            return op;
        }

        /// <summary>
        /// push an operator on the stack
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public int PushOp(OperatorType op)
        {
            if (op != OperatorType.OP_OPEN)
            {
                while (OpPriority[(int)ctx.OpStack.Peek()] >= OpPriority[(int)op])
                {
                    if (DoOp() == 0) return (0);
                }
            }
            if (ctx.OpStack.Count > Definition.OPSTACK_MAX)
            {
                outPr.Error("Expression too complex!");
                return (0);
            }
            ctx.OpStack.Push(op);
            ctx.NeedOperator = false;
            return (1);
        }

        /// <summary>
        /// apply an operator to the value stack
        /// </summary>
        /// <returns></returns>
        public int DoOp()
        {
            int val0, val1 = 0;

            /* operator */
            var op = ctx.OpStack.Pop();

            /* first arg */
            val0 = (int)ctx.ValStack.Pop();

            /* second arg */
            if (OpPriority[(int)op] < 9)
            {
                val1 = (int)ctx.ValStack.Pop();
            }

            switch (op)
            {
            /* BANK */
            case OperatorType.OP_BANK:
                if (CheckFuncArgs("BANK") == 0) return (0);
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    if (ctx.ExprLablPtr.Bank == Definition.RESERVED_BANK)
                    {
                        outPr.Error("No BANK index for this symbol!");
                        val0 = 0;
                        break;
                    }
                }
                val0 = ctx.ExprLablPtr.Bank;
                break;
            /* PAGE */
            case OperatorType.OP_PAGE:
                if (CheckFuncArgs("PAGE") == 0) return (0);
                val0 = ctx.ExprLablPtr.Page;
                break;
            /* VRAM */
            case OperatorType.OP_VRAM:
                if (CheckFuncArgs("VRAM") == 0) return (0);
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    if (ctx.ExprLablPtr.Vram == -1)
                    {
                        outPr.Error("No VRAM address for this symbol!");
                    }
                }
                val0 = ctx.ExprLablPtr.Vram;
                break;
            /* PAL */
            case OperatorType.OP_PAL:
                if (CheckFuncArgs("PAL") == 0) return (0);
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    if (ctx.ExprLablPtr.Pal == -1)
                    {
                        outPr.Error("No palette index for this symbol!");
                    }
                }
                val0 = ctx.ExprLablPtr.Pal;
                break;
            /* DEFINED */
            case OperatorType.OP_DEFINED:
                if (CheckFuncArgs("DEFINED") == 0) return (0);
                if ((ctx.ExprLablPtr.Type != SymbolFlag.IFUNDEF) && (ctx.ExprLablPtr.Type != SymbolFlag.UNDEF))
                {
                    val0 = 1;
                }
                else
                {
                    val0 = 0;
                    ctx.Undef--;
                }
                break;
            /* SIZEOF */
            case OperatorType.OP_SIZEOF:
                if (CheckFuncArgs("SIZEOF") == 0) return (0);
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    if (ctx.ExprLablPtr.DataType == AsmDirective.P_UNDEFINED)
                    {
                        outPr.Error("No size attributes for this symbol!");
                        return (0);
                    }
                }
                val0 = ctx.ExprLablPtr.DataSize;
                break;
            case OperatorType.OP_REGIONSIZE:
                string str = null;
                NesAsmRegion region = null;
                if (0 <= val0 && val0 < ctx.StringTbl.Count)
                {
                    str = ctx.StringTbl[val0];
                    region = ctx.RegionTbl.GetValueOrDefault(str);
                }
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    if (str == null)
                    {
                        outPr.Error($"Invalid argument!");
                    }
                    else if (region == null)
                    {
                        outPr.Error($"Region '{str}' not found!");
                    }
                    else if (region.BeginBank < 0 || region.BeginLocCnt < 0 || region.EndBank < 0 || region.EndLocCnt < 0)
                    {
                        outPr.Error($"Region '{str}' invalid!");
                    }
                }
                val0 = region?.RegionSize ?? 0;
                break;
            /* HIGH */
            case OperatorType.OP_HIGH:
                val0 = (val0 & 0xFF00) >> 8;
                break;
            /* LOW */
            case OperatorType.OP_LOW:
                val0 = val0 & 0xFF;
                break;
            case OperatorType.OP_ADD:
                val0 = val1 + val0;
                break;
            case OperatorType.OP_SUB:
                val0 = val1 - val0;
                break;
            case OperatorType.OP_MUL:
                val0 = val1 * val0;
                break;
            case OperatorType.OP_DIV:
                if (val0 == 0)
                {
                    outPr.Error("Divide by zero!");
                    return (0);
                }
                val0 = val1 / val0;
                break;
            case OperatorType.OP_MOD:
                if (val0 == 0)
                {
                    outPr.Error("Divide by zero!");
                    return (0);
                }
                val0 = val1 % val0;
                break;
            case OperatorType.OP_NEG:
                val0 = -val0;
                break;
            case OperatorType.OP_SHL:
                val0 = val1 << (val0 & 0x1F);
                break;
            case OperatorType.OP_SHR:
                val0 = val1 >> (val0 & 0x1f);
                break;
            case OperatorType.OP_OR:
                val0 = val1 | val0;
                break;
            case OperatorType.OP_XOR:
                val0 = val1 ^ val0;
                break;
            case OperatorType.OP_AND:
                val0 = val1 & val0;
                break;
            case OperatorType.OP_COM:
                val0 = ~val0;
                break;
            case OperatorType.OP_NOT:
                val0 = (val0 == 0) ? 1 : 0;
                break;
            case OperatorType.OP_EQUAL:
                val0 = (val1 == val0) ? 1 : 0;
                break;
            case OperatorType.OP_NOT_EQUAL:
                val0 = (val1 != val0) ? 1 : 0;
                break;
            case OperatorType.OP_LOWER:
                val0 = (val1 < val0) ? 1 : 0;
                break;
            case OperatorType.OP_LOWER_EQUAL:
                val0 = (val1 <= val0) ? 1 : 0;
                break;
            case OperatorType.OP_HIGHER:
                val0 = (val1 > val0) ? 1 : 0;
                break;
            case OperatorType.OP_HIGHER_EQUAL:
                val0 = (val1 >= val0) ? 1 : 0;
                break;
            default:
                outPr.Error("Invalid operator in expression!");
                return (0);
            }

            /* result */
            ctx.ValStack.Push((uint)val0);
            return 1;
        }

        /// <summary>
        /// register a string to StringTbl
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns>string id</returns>
        public int RegisterString(ArrayPointer<char> expr)
        {
            char c;
            int i;
            char[] buf = new char[Definition.STRING_LEN_MAX + 1];

            /* skip spaces */
            while (CharUtil.IsSpace(expr.Value)) expr++;

            /* string must be enclosed */
            if (expr.Read() != '\"')
            {
                outPr.Error("Incorrect string syntax!");
                return (-1);
            }

            /* get string */
            i = 0;
            for (;;)
            {
                c = expr.Read();
                if (c == '\"') break;
                if (i >= Definition.STRING_LEN_MAX)
                {
                    outPr.Error("String too long!");
                    return (-1);
                }
                buf[i++] = c;
            }

            /* end the string */
            buf[i] = '\0';

            /* skip spaces */
            while (CharUtil.IsSpace(expr.Value)) expr++;

            /* ok */
            var str = buf.ToStringFromNullTerminated();
            var val = ctx.StringTbl.IndexOf(str);
            if (val < 0)
            {
                ctx.StringTbl.Add(str);
                val = ctx.StringTbl.Count - 1;
            }
            return val;
        }

        /// <summary>
        /// check BANK/PAGE/VRAM/PAL function arguments
        /// </summary>
        /// <param name="func_name"></param>
        /// <returns></returns>
        public int CheckFuncArgs(string func_name)
        {
            string str;

            if (ctx.ExprLablCnt == 1)
                return (1);
            else if (ctx.ExprLablCnt == 0)
                str = string.Format(@"No symbol in function {0}!", func_name);
            else
            {
                str = string.Format(@"Too many symbols in function {0}!", func_name);
            }

            /* output message */
            outPr.Error(str);
            return (0);
        }
    }
}
