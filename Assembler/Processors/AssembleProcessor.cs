﻿using System;
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
            string name;
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
                if (!string.IsNullOrEmpty((name = symPr.ReadSymbolNameFromPrLnBuf(ref i))))
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
                    if (ctx.MacroLinePtr != null)
                    {
                        ctx.MacroLinePtr.Next = ptr;
                    }
                    else
                    {
                        ctx.MacroPtr.Line = ptr;
                    }
                    ctx.MacroLinePtr = ptr;
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
            // Label declaration line
            else
            {
                if (!string.IsNullOrEmpty((name = symPr.ReadSymbolNameFromPrLnBuf(ref i))))
                {
                    if ((ctx.LablPtr = symPr.LookUpSymbolTable(name, true)) == null) return;
                }
                else
                {
                    return;
                }
                if ((ctx.LablPtr != null) && (ctx.PrLnBuf[i] == ':')) i++;
            }

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[i])) i++;

            /* is it a macro? */
            ip = i;
            ctx.MacroPtr = macroPr.LookUpMacroTable(ref ip);
            if (ctx.MacroPtr != null)
            {
                /* define label */
                symPr.AssignValueToLablPtr(ctx.LocCnt, true);

                /* output location counter */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    if (!ctx.AsmOpt[AssemblerOption.OPT_MACRO])
                    {
                        outPr.LoadLc((ctx.Page << 13) + ctx.LocCnt, 0);
                    }
                }

                /* get macro args */
                if (macroPr.GetMacroArgs(ip) == 0) return;

                /* output line */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PrintLn();
                }

                /* ok */
                ctx.MacroCntMax++;
                ctx.MacroCounter = ctx.MacroCntMax;
                ctx.IsExpandMacro = true;
                ctx.MacroLinePtr = ctx.MacroPtr.Line;
                return;
            }

            /* an instruction then */
            ip = i;
            flag = OpLook(ref ip);
            if (flag < 0)
            {
                symPr.AssignValueToLablPtr(ctx.LocCnt, true);
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
                cmdPr.DoPseudo(ref ip);
            }
            else if (symPr.AssignValueToLablPtr(ctx.LocCnt, true) == -1)
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
            int i;

            /* get instruction name */
            i = 0;
            ctx.OpExt = (char)0;
            flag = false;

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

            var namestr = name.ToStringFromNullTerminated();

            /* search the instruction in the hash table */
            ptr = ctx.InstTbl.GetValueOrDefault(namestr);

            if (ptr != null)
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
            if (optbl == null) return;

            /* parse list */
            foreach (var inst in optbl)
            {
                /* calculate instruction hash value */
                var name = inst.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    /* insert the instruction in the hash table */
                    ctx.InstTbl[name] = inst;
                }
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
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* get expression */
            if (exprPr.Evaluate(ref ip, ';') == 0)
            {
                return;
            }

            /* check for '.if' stack overflow */
            if (ctx.IfLevel == ctx.IfState.Length - 1)
            {
                outPr.FatalError("Too many nested IF/ENDIF!");
                return;
            }

            if (ctx.Undef > 0)
            {
                outPr.Warning("Warning: Undefined label in IF condition.");
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
            string name;
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

            /* get symbol */
            if (string.IsNullOrEmpty((name = symPr.ReadSymbolNameFromPrLnBuf(ref ip, true))))
            {
                outPr.Error("Syntax error!");
                return;
            }
            if (CheckEOL(ref ip) == 0) return;

            ctx.IfExpr = true;
            ctx.LablPtr = symPr.LookUpSymbolTable(name, true);
            ctx.IfExpr = false;
            if (ctx.LablPtr != null && (ctx.LablPtr.Type == SymbolFlag.IFUNDEF || ctx.LablPtr.Type == SymbolFlag.UNDEF))
            {
                ctx.LablPtr.Type = SymbolFlag.IFUNDEF;
                ctx.LablPtr = null;
            }

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
