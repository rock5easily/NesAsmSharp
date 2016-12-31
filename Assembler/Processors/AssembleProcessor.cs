using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NesAsmSharp.Assembler.Util;

namespace NesAsmSharp.Assembler.Processors
{
    public class AssembleProcessor : ProcessorBase
    {
        public AssembleProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        /// <summary>
        /// translate source line to machine language
        /// </summary>
        public void Assemble()
        {
            NesAsmLine ptr;
            char c;
            int flag;
            int ip, i, j;       /* prlnbuf pointer */

            /* init variables */
            ctx.LablPtr = null;
            ctx.ContinuedLine = false;
            ctx.DataLocCnt = -1;
            ctx.DataSize = 3;
            ctx.DataLevel = 1;

            /* macro definition */
            if (ctx.InMacro)
            {
                i = Definition.SFIELD;
                if (symPr.ColSym(ref i) != 0)
                {
                    if (ctx.PrLnBuf[i] == ':') i++;
                }

                while (CharUtil.IsSpace(ctx.PrLnBuf[i])) i++;

                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PrintLn();
                }
                if (OpLook(ref i) >= 0)
                {
                    if (ctx.OpFlg == OpCodeFlag.PSEUDO)
                    {
                        if (ctx.OpVal == AsmDirective.P_MACRO)
                        {
                            outPr.Error("Can not nest macro definitions!");
                            return;
                        }
                        if (ctx.OpVal == AsmDirective.P_ENDM)
                        {
                            if (CheckEOL(ref i) == 0) return;
                            ctx.InMacro = false;
                            return;
                        }
                    }
                }
                if (ctx.Pass == PassFlag.FIRST_PASS)
                {
                    ptr = new NesAsmLine();
                    ptr.Next = null;
                    ptr.Data = ctx.PrLnBuf.ToStringFromNullTerminated(Definition.SFIELD);
                    if (ctx.MLPtr != null)
                    {
                        ctx.MLPtr.Next = ptr;
                    }
                    else
                    {
                        ctx.MPtr.Line = ptr;
                    }
                    ctx.MLPtr = ptr;
                }
                return;
            }

            /* IF/ELSE section;
             * check for a '.else' or '.endif'
             * to toggle state
             */
            if (ctx.InIf)
            {
                i = Definition.SFIELD;
                while (CharUtil.IsSpace(ctx.PrLnBuf[i])) i++;

                if (OpLook(ref i) >= 0)
                {
                    if (ctx.OpFlg == OpCodeFlag.PSEUDO)
                    {
                        switch (ctx.OpVal)
                        {
                        case AsmDirective.P_IF:          // .if
                        case AsmDirective.P_IFDEF:       // .ifdef
                        case AsmDirective.P_IFNDEF:      // .ifndef
                            if (ctx.SkipLines)
                            {
                                ctx.IfLevel++;
                                ctx.IfState[ctx.IfLevel] = false;
                            }
                            break;
                        case AsmDirective.P_ELSE:        // .else
                            if (CheckEOL(ref i) == 0) return;
                            if (ctx.IfState[ctx.IfLevel])
                            {
                                ctx.SkipLines = !ctx.IfFlag[ctx.IfLevel];
                                if (ctx.Pass == PassFlag.LAST_PASS)
                                {
                                    outPr.PrintLn();
                                }
                            }
                            return;
                        case AsmDirective.P_ENDIF:       // .endif
                            if (CheckEOL(ref i) == 0) return;
                            if (ctx.IfState[ctx.IfLevel] && (ctx.Pass == PassFlag.LAST_PASS))
                            {
                                outPr.PrintLn();
                            }
                            ctx.SkipLines = !ctx.IfState[ctx.IfLevel];
                            ctx.IfLevel--;
                            if (ctx.IfLevel == 0)
                            {
                                ctx.InIf = false;
                            }
                            return;
                        }
                    }
                }
            }

            if (ctx.SkipLines) return;

            /* comment line */
            c = ctx.PrLnBuf[Definition.SFIELD];
            if (c == ';' || c == '*' || c == '\0')
            {
                ctx.LastLabl = null;
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PrintLn();
                }
                return;
            }

            /* search for a label */
            i = Definition.SFIELD;
            j = 0;
            while (CharUtil.IsSpace(ctx.PrLnBuf[i])) i++;
            for (;;)
            {
                c = ctx.PrLnBuf[i + j];
                if (char.IsDigit(c) && (j == 0)) break;
                if (!CharUtil.IsAlNum(c) && (c != '_') && (c != '.')) break;
                j++;
            }
            if ((j == 0) || ((i != Definition.SFIELD) && (c != ':')))
            {
                i = Definition.SFIELD;
            }
            else
            {
                if (symPr.ColSym(ref i) != 0)
                {
                    if ((ctx.LablPtr = symPr.STLook(1)) == null) return;
                }
                if ((ctx.LablPtr != null) && (ctx.PrLnBuf[i] == ':')) i++;
            }

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[i])) i++;

            /* is it a macro? */
            ip = i;
            ctx.MPtr = macroPr.MacroLook(ref ip);
            if (ctx.MPtr != null)
            {
                /* define label */
                symPr.LablDef(ctx.LocCnt, 1);

                /* output location counter */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    if (!opt.AsmOpt[AssemblerOption.OPT_MACRO])
                    {
                        outPr.LoadLc((ctx.Page << 13) + ctx.LocCnt, 0);
                    }
                }

                /* get macro args */
                if (macroPr.MacroGetArgs(ip) == 0) return;

                /* output line */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PrintLn();
                }

                /* ok */
                ctx.MCntMax++;
                ctx.MCounter = ctx.MCntMax;
                ctx.IsExpandMacro = true;
                ctx.MLPtr = ctx.MPtr.Line;
                return;
            }

            /* an instruction then */
            ip = i;
            flag = OpLook(ref ip);
            if (flag < 0)
            {
                symPr.LablDef(ctx.LocCnt, 1);
                if (flag == -1)
                {
                    outPr.Error("Unknown instruction!");
                }

                if ((flag == -2) && (ctx.Pass == PassFlag.LAST_PASS))
                {
                    if (ctx.LablPtr != null)
                    {
                        outPr.LoadLc(ctx.LocCnt, 0);
                    }
                    outPr.PrintLn();
                }
                ctx.LastLabl = null;
                return;
            }

            /* generate code */
            if (ctx.OpFlg == OpCodeFlag.PSEUDO)
            {
                cmdPr.do_pseudo(ref ip);
            }
            else if (symPr.LablDef(ctx.LocCnt, 1) == -1)
            {
                return;
            }
            else
            {
                /* output infos */
                ctx.DataLocCnt = ctx.LocCnt;

                /* check if we are in the CODE section */
                if (ctx.Section != SectionType.S_CODE)
                {
                    outPr.FatalError("Instructions not allowed in this section!");
                }
                /* generate code */
                ctx.OpProc(ref ip);

                /* reset last label pointer */
                ctx.LastLabl = null;
            }
        }

        /// <summary>
        /// operation code table lookup
        /// return symbol length if found
        /// </summary>
        /// <param name="idx"></param>
        /// <returns>
        /// return -1 on syntax error
        /// return -2 if no symbol
        /// </returns>
        public int OpLook(ref int idx)
        {
            NesAsmOpecode ptr;

            char[] name = new char[16];
            char c;
            bool flag;
            int hash;
            int i;

            /* get instruction name */
            i = 0;
            ctx.OpExt = (char)0;
            flag = false;
            hash = 0;

            for (;;)
            {
                c = char.ToUpper(ctx.PrLnBuf[idx]);
                if (c == ' ' || c == '\t' || c == '\0' || c == ';') break;
                if (!CharUtil.IsAlNum(c) && c != '.' && c != '*' && c != '=') return (-1);
                if (i == 15) return (-1);

                /* handle instruction extension */
                if (c == '.' && i != 0)
                {
                    if (flag) return (-1);
                    flag = true;
                    idx++;
                    continue;
                }
                if (flag)
                {
                    if (ctx.OpExt != 0) return (-1);
                    ctx.OpExt = c;
                    idx++;
                    continue;
                }

                /* store char */
                name[i++] = c;
                hash += c;
                hash = (hash << 3) + (hash >> 5) + c;
                idx++;

                /* break if '=' directive */
                if (c == '=') break;
            }

            /* check extension */
            if (flag)
            {
                if ((ctx.OpExt != 'L') && (ctx.OpExt != 'H')) return (-1);
            }

            /* end name string */
            name[i] = '\0';

            /* return if no instruction */
            if (i == 0) return (-2);

            /* search the instruction in the hash table */
            ptr = ctx.InstTbl[hash & 0xFF];

            var namestr = name.ToStringFromNullTerminated();
            while (ptr != null)
            {
                if (namestr == ptr.Name)
                {
                    ctx.OpProc = ptr.Proc;
                    ctx.OpFlg = ptr.Flag;
                    ctx.OpVal = ptr.Value;
                    ctx.OpType = ptr.TypeIdx;

                    if (ctx.OpExt != 0)
                    {
                        /* no extension for pseudos */
                        if (ctx.OpFlg == OpCodeFlag.PSEUDO) return (-1);
                        /* extension valid only for these addressing modes */
                        if ((ctx.OpFlg &
                            (OpCodeFlag.IMM | OpCodeFlag.ZP | OpCodeFlag.ZP_X | OpCodeFlag.ZP_IND_Y | OpCodeFlag.ABS | OpCodeFlag.ABS_X | OpCodeFlag.ABS_Y)) != 0) return (-1);
                    }
                    return (i);
                }
                ptr = ptr.Next;
            }

            /* didn't find this instruction */
            return (-1);
        }

        /// <summary>
        /// add a list of instructions to the instruction
        /// hash table
        /// </summary>
        /// <param name="optbl"></param>
        public void AddInst(NesAsmOpecode[] optbl)
        {
            int hash;
            int len;
            int i;
            string name;
            char c;
            int idx = 0;

            if (optbl == null) return;

            /* parse list */
            while (optbl[idx].Name != null)
            {
                /* calculate instruction hash value */
                hash = 0;
                name = optbl[idx].Name;
                len = name.Length;

                for (i = 0; i < len; i++)
                {
                    c = name[i];
                    hash += c;
                    hash = (hash << 3) + (hash >> 5) + c;
                }

                hash &= 0xFF;

                /* insert the instruction in the hash table */
                optbl[idx].Next = ctx.InstTbl[hash];
                ctx.InstTbl[hash] = optbl[idx];

                /* next instruction */
                idx++;
            }
        }

        /// <summary>
        /// check the end of line for garbage
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public int CheckEOL(ref int ip)
        {
            while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;
            if (ctx.PrLnBuf[ip] == ';' || ctx.PrLnBuf[ip] == '\0')
            {
                return (1);
            }
            else
            {
                outPr.Error("Syntax error!");
                return (0);
            }
        }

        /// <summary>
        /// .if pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoIf(ref int ip)
        {
            symPr.LablDef(ctx.LocCnt, 1);

            /* get expression */
            ctx.IfExpr = true;
            if (exprPr.Evaluate(ref ip, ';') == 0)
            {
                ctx.IfExpr = false;
                return;
            }
            ctx.IfExpr = false;

            /* check for '.if' stack overflow */
            if (ctx.IfLevel == ctx.IfState.Length - 1)
            {
                outPr.FatalError("Too many nested IF/ENDIF!");
                return;
            }
            ctx.InIf = true;
            ctx.IfLevel++;
            ctx.IfState[ctx.IfLevel] = !ctx.SkipLines;
            if (!ctx.SkipLines)
            {
                ctx.IfFlag[ctx.IfLevel] = (ctx.Value == 0);
                ctx.SkipLines = (ctx.Value == 0);
            }

            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc((int)ctx.Value, 1);
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .else pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoElse(ref int ip)
        {
            if (!ctx.InIf)
            {
                outPr.FatalError("Unexpected ELSE!");
            }
        }

        /// <summary>
        /// .endif pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoEndif(ref int ip)
        {
            if (!ctx.InIf)
            {
                outPr.FatalError("Unexpected ENDIF!");
            }
        }

        /// <summary>
        /// .ifdef/.ifndef pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoIfdef(ref int ip)
        {
            symPr.LablDef(ctx.LocCnt, 1);

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

            /* get symbol */
            if (symPr.ColSym(ref ip) == 0)
            {
                outPr.Error("Syntax error!");
                return;
            }
            if (CheckEOL(ref ip) == 0) return;

            ctx.LablPtr = symPr.STLook(0);

            /* check for '.if' stack overflow */
            if (ctx.IfLevel == ctx.IfState.Length - 1)
            {
                outPr.FatalError("Too many nested IF/ENDIF!");
                return;
            }
            ctx.InIf = true;
            ctx.IfLevel++;
            ctx.IfState[ctx.IfLevel] = !ctx.SkipLines;
            if (!ctx.SkipLines)
            {
                if (ctx.OpType != 0)
                {
                    /* .ifdef */
                    ctx.SkipLines = ctx.IfFlag[ctx.IfLevel] = (ctx.LablPtr == null);
                }
                else
                {
                    /* .ifndef */
                    ctx.SkipLines = ctx.IfFlag[ctx.IfLevel] = (ctx.LablPtr != null);
                }
            }

            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                var offset = ctx.SkipLines ? 0 : 1;
                outPr.LoadLc(offset, 1);
                outPr.PrintLn();
            }
        }
    }
}
