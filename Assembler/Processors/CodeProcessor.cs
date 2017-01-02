using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NesAsmSharp.Assembler.OpCodeFlag;

namespace NesAsmSharp.Assembler.Processors
{

    public class CodeProcessor : ProcessorBase
    {
        public CodeProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        private NesAsmOpecode[] baseInst;
        public NesAsmOpecode[] BaseInst
        {
            get
            {
                if (baseInst == null)
                {
                    baseInst = new NesAsmOpecode[]
                    {
                        new NesAsmOpecode("ADC", Class4, IMM|ZP|ZP_X|ZP_IND|ZP_IND_X|ZP_IND_Y|ABS|ABS_X|ABS_Y, 0x61, 0),
                        new NesAsmOpecode("AND", Class4, IMM|ZP|ZP_X|ZP_IND|ZP_IND_X|ZP_IND_Y|ABS|ABS_X|ABS_Y, 0x21, 0),
                        new NesAsmOpecode("ASL", Class4, ACC|ZP|ZP_X|ABS|ABS_X, 0x02, 0),
                        new NesAsmOpecode("BCC", Class2, 0, 0x90, 0),
                        new NesAsmOpecode("BCS", Class2, 0, 0xB0, 0),
                        new NesAsmOpecode("BEQ", Class2, 0, 0xF0, 0),
                        new NesAsmOpecode("BIT", Class4, IMM|ZP|ZP_X|ABS|ABS_X, 0x00, 2),
                        new NesAsmOpecode("BMI", Class2, 0, 0x30, 0),
                        new NesAsmOpecode("BNE", Class2, 0, 0xD0, 0),
                        new NesAsmOpecode("BPL", Class2, 0, 0x10, 0),
                        new NesAsmOpecode("BRK", Class1, 0, 0x00, 0),
                        new NesAsmOpecode("BVC", Class2, 0, 0x50, 0),
                        new NesAsmOpecode("BVS", Class2, 0, 0x70, 0),
                        new NesAsmOpecode("CLC", Class1, 0, 0x18, 0),
                        new NesAsmOpecode("CLD", Class1, 0, 0xD8, 0),
                        new NesAsmOpecode("CLI", Class1, 0, 0x58, 0),
                        new NesAsmOpecode("CLV", Class1, 0, 0xB8, 0),
                        new NesAsmOpecode("CMP", Class4, IMM|ZP|ZP_X|ZP_IND|ZP_IND_X|ZP_IND_Y|ABS|ABS_X|ABS_Y, 0xC1, 0),
                        new NesAsmOpecode("CPX", Class4, IMM|ZP|ABS, 0xE0, 1),
                        new NesAsmOpecode("CPY", Class4, IMM|ZP|ABS, 0xC0, 1),
                        new NesAsmOpecode("DEC", Class4, ACC|ZP|ZP_X|ABS|ABS_X, 0x00, 3),
                        new NesAsmOpecode("DEX", Class1, 0, 0xCA, 0),
                        new NesAsmOpecode("DEY", Class1, 0, 0x88, 0),
                        new NesAsmOpecode("EOR", Class4, IMM|ZP|ZP_X|ZP_IND|ZP_IND_X|ZP_IND_Y|ABS|ABS_X|ABS_Y, 0x41, 0),
                        new NesAsmOpecode("INC", Class4, ACC|ZP|ZP_X|ABS|ABS_X, 0x00, 4),
                        new NesAsmOpecode("INX", Class1, 0, 0xE8, 0),
                        new NesAsmOpecode("INY", Class1, 0, 0xC8, 0),
                        new NesAsmOpecode("JMP", Class4, ABS|ABS_IND|ABS_IND_X, 0x40, 0),
                        new NesAsmOpecode("JSR", Class4, ABS, 0x14, 0),
                        new NesAsmOpecode("LDA", Class4, IMM|ZP|ZP_X|ZP_IND|ZP_IND_X|ZP_IND_Y|ABS|ABS_X|ABS_Y, 0xA1, 0),
                        new NesAsmOpecode("LDX", Class4, IMM|ZP|ZP_Y|ABS|ABS_Y, 0xA2, 1),
                        new NesAsmOpecode("LDY", Class4, IMM|ZP|ZP_X|ABS|ABS_X, 0xA0, 1),
                        new NesAsmOpecode("LSR", Class4, ACC|ZP|ZP_X|ABS|ABS_X, 0x42, 0),
                        new NesAsmOpecode("NOP", Class1, 0, 0xEA, 0),
                        new NesAsmOpecode("ORA", Class4, IMM|ZP|ZP_X|ZP_IND|ZP_IND_X|ZP_IND_Y|ABS|ABS_X|ABS_Y, 0x01, 0),
                        new NesAsmOpecode("PHA", Class1, 0, 0x48, 0),
                        new NesAsmOpecode("PHP", Class1, 0, 0x08, 0),
                        new NesAsmOpecode("PLA", Class1, 0, 0x68, 0),
                        new NesAsmOpecode("PLP", Class1, 0, 0x28, 0),
                        new NesAsmOpecode("ROL", Class4, ACC|ZP|ZP_X|ABS|ABS_X, 0x22, 0),
                        new NesAsmOpecode("ROR", Class4, ACC|ZP|ZP_X|ABS|ABS_X, 0x62, 0),
                        new NesAsmOpecode("RTI", Class1, 0, 0x40, 0),
                        new NesAsmOpecode("RTS", Class1, 0, 0x60, 0),
                        new NesAsmOpecode("SBC", Class4, IMM|ZP|ZP_X|ZP_IND|ZP_IND_X|ZP_IND_Y|ABS|ABS_X|ABS_Y, 0xE1, 0),
                        new NesAsmOpecode("SEC", Class1, 0, 0x38, 0),
                        new NesAsmOpecode("SED", Class1, 0, 0xF8, 0),
                        new NesAsmOpecode("SEI", Class1, 0, 0x78, 0),
                        new NesAsmOpecode("STA", Class4, ZP|ZP_X|ZP_IND|ZP_IND_X|ZP_IND_Y|ABS|ABS_X|ABS_Y, 0x81, 0),
                        new NesAsmOpecode("STX", Class4, ZP|ZP_Y|ABS, 0x82, 0),
                        new NesAsmOpecode("STY", Class4, ZP|ZP_X|ABS, 0x80, 0),
                        new NesAsmOpecode("TAX", Class1, 0, 0xAA, 0),
                        new NesAsmOpecode("TAY", Class1, 0, 0xA8, 0),
                        new NesAsmOpecode("TSX", Class1, 0, 0xBA, 0),
                        new NesAsmOpecode("TXA", Class1, 0, 0x8A, 0),
                        new NesAsmOpecode("TXS", Class1, 0, 0x9A, 0),
                        new NesAsmOpecode("TYA", Class1, 0, 0x98, 0)
                    };
                }
                return baseInst;
            }
        }

        private int[,] opValTab;
        public int[,] OpValTab
        {
            get
            {
                if (opValTab == null)
                {
                    opValTab = new int[6, 16]
                    {
                        {
                            0x08, 0x08, 0x04, 0x14, 0x14, 0x11, 0x00, 0x10,  // CPX CPY LDX LDY
                            0x0C, 0x1C, 0x18, 0x2C, 0x3C, 0x00, 0x00, 0x00
                        },
                        {
                            0x00, 0x00, 0x04, 0x14, 0x14, 0x00, 0x00, 0x00,  // ST0 ST1 ST2 TAM TMA
                            0x0C, 0x1C, 0x1C, 0x00, 0x00, 0x00, 0x00, 0x00
                        },
                        {
                            0x00, 0x89, 0x24, 0x34, 0x00, 0x00, 0x00, 0x00,  // BIT
                            0x2C, 0x3C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                        },
                        {
                            0x3A, 0x00, 0xC6, 0xD6, 0x00, 0x00, 0x00, 0x00,  // DEC
                            0xCE, 0xDE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                        },
                        {
                            0x1A, 0x00, 0xE6, 0xF6, 0x00, 0x00, 0x00, 0x00,  // INC
                            0xEE, 0xFE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                        },
                        {
                            0x00, 0x00, 0x64, 0x74, 0x00, 0x00, 0x00, 0x00,  // STZ
                            0x9C, 0x9E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                        }
                    };
                }
                return opValTab;
            }
        }

        /// <summary>
        /// 1 byte, no operand field
        /// </summary>
        /// <param name="ip"></param>
        public void Class1(ref int ip)
        {
            asmPr.CheckEOL(ref ip);

            /* update location counter */
            ctx.LocCnt++;

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* opcode */
                outPr.PutByte(ctx.DataLocCnt, (int)ctx.OpVal);

                /* output line */
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// 2 bytes, relative addressing
        /// </summary>
        /// <param name="ip"></param>
        public void Class2(ref int ip)
        {
            uint addr;

            /* update location counter */
            ctx.LocCnt += 2;

            /* get destination address */
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* opcode */
                outPr.PutByte(ctx.DataLocCnt, (int)ctx.OpVal);

                /* calculate branch offset */
                addr = (uint)(ctx.Value - (ctx.LocCnt + (ctx.Page << 13)));

                /* check range */
                if (addr > 0x7F && addr < 0xFFFFFF80)
                {
                    outPr.Error("Branch address out of range!");
                    return;
                }

                /* offset */
                outPr.PutByte(ctx.DataLocCnt + 1, (int)addr);

                /* output line */
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// 2 bytes, inherent addressing
        /// </summary>
        /// <param name="ip"></param>
        public void Class3(ref int ip)
        {
            asmPr.CheckEOL(ref ip);

            /* update location counter */
            ctx.LocCnt += 2;

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* opcodes */
                outPr.PutByte(ctx.DataLocCnt, (int)ctx.OpVal);
                outPr.PutByte(ctx.DataLocCnt + 1, ctx.OpType);

                /* output line */
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// various addressing modes
        /// </summary>
        public void Class4(ref int ip)
        {
            char[] buffer = new char[32];
            char c;
            int len, mode;
            int i;

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

            /* low/high byte prefix string */
            if (CharUtil.IsAlpha(ctx.PrLnBuf[ip]))
            {
                len = 0;
                i = ip;

                /* extract string */
                for (;;)
                {
                    c = ctx.PrLnBuf[i];
                    if (c == '\0' || c == ' ' || c == '\t' || c == ';') break;
                    if ((!CharUtil.IsAlpha(c) && c != '_') || (len == buffer.Length - 1))
                    {
                        len = 0;
                        break;
                    }
                    buffer[len++] = c;
                    i++;
                }

                /* check */
                if (len != 0)
                {
                    buffer[len] = '\0';
                    var buffer_str = buffer.ToStringFromNullTerminated().ToLower();
                    if (buffer_str == @"low_byte")
                    {
                        ctx.OpExt = 'L';
                        ip = i;
                    }
                    else if (buffer_str == @"high_byte")
                    {
                        ctx.OpExt = 'H';
                        ip = i;
                    }
                }
            }

            /* get operand */
            mode = GetOperand(ref ip, (int)ctx.OpFlg, ';');
            if (mode == 0) return;

            /* make opcode */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                for (i = 0; i < 32; i++)
                {
                    if ((mode & (1 << i)) != 0) break;
                }
                ctx.OpVal += OpValTab[ctx.OpType, i];
            }

            /* auto-tag */
            if (ctx.AutoTag != 0)
            {
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PutByte(ctx.LocCnt, 0xA0);
                    outPr.PutByte(ctx.LocCnt + 1, (int)ctx.AutoTagValue);
                }
                ctx.LocCnt += 2;
            }

            /* generate code */
            switch ((OpCodeFlag)mode)
            {
            case ACC:
                /* one byte */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PutByte(ctx.LocCnt, (int)ctx.OpVal);
                }
                ctx.LocCnt++;
                break;
            case IMM:
            case ZP:
            case ZP_X:
            case ZP_Y:
            case ZP_IND:
            case ZP_IND_X:
            case ZP_IND_Y:
                /* two bytes */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PutByte(ctx.LocCnt, (int)ctx.OpVal);
                    outPr.PutByte(ctx.LocCnt + 1, (int)ctx.Value);
                }
                ctx.LocCnt += 2;
                break;
            case ABS:
            case ABS_X:
            case ABS_Y:
            case ABS_IND:
            case ABS_IND_X:
                /* three bytes */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PutByte(ctx.LocCnt, (int)ctx.OpVal);
                    outPr.PutWord(ctx.LocCnt + 1, (int)ctx.Value);
                }
                ctx.LocCnt += 3;
                break;
            }

            /* auto-increment */
            if (ctx.AutoInc != 0)
            {
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PutByte(ctx.LocCnt, ctx.AutoInc);
                }
                ctx.LocCnt += 1;
            }

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// 3 bytes, zp/relative addressing
        /// </summary>
        /// <param name="ip"></param>
        public void Class5(ref int ip)
        {
            int zp;
            uint addr;
            int mode;

            /* update location counter */
            ctx.LocCnt += 3;

            /* get first operand */
            mode = GetOperand(ref ip, (int)ZP, ',');
            zp = (int)ctx.Value;
            if (mode == 0) return;

            /* get second operand */
            mode = GetOperand(ref ip, (int)ABS, ';');
            if (mode == 0) return;

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* opcodes */
                outPr.PutByte(ctx.DataLocCnt, (int)ctx.OpVal);
                outPr.PutByte(ctx.DataLocCnt + 1, zp);

                /* calculate branch offset */
                addr = (uint)(ctx.Value - (ctx.LocCnt + (ctx.Page << 13)));

                /* check range */
                if (addr > 0x7F && addr < 0xFFFFFF80)
                {
                    outPr.Error("Branch address out of range!");
                    return;
                }

                /* offset */
                outPr.PutByte(ctx.DataLocCnt + 2, (int)addr);

                /* output line */
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// 7 bytes, src/dest/length
        /// </summary>
        /// <param name="ip"></param>
        public void Class6(ref int ip)
        {
            int i;
            int[] addr = new int[3];

            /* update location counter */
            ctx.LocCnt += 7;

            /* get operands */
            for (i = 0; i < 3; i++)
            {
                if (exprPr.Evaluate(ref ip, (i < 2) ? ',' : ';') == 0) return;
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    if ((ctx.Value & 0xFFFF0000) != 0)
                    {
                        outPr.Error("Operand size error!");
                        return;
                    }
                }
                addr[i] = (int)ctx.Value;
            }

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* opcodes */
                outPr.PutByte(ctx.DataLocCnt, (int)ctx.OpVal);
                outPr.PutWord(ctx.DataLocCnt + 1, addr[0]);
                outPr.PutWord(ctx.DataLocCnt + 3, addr[1]);
                outPr.PutWord(ctx.DataLocCnt + 5, addr[2]);

                /* output line */
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// TST instruction
        /// </summary>
        /// <param name="ip"></param>
        public void Class7(ref int ip)
        {
            int mode;
            int addr, imm;

            /* get first operand */
            mode = GetOperand(ref ip, (int)IMM, ',');
            imm = (int)ctx.Value;
            if (mode == 0) return;

            /* get second operand */
            mode = GetOperand(ref ip, (int)(ZP | ZP_X | ABS | ABS_X), ';');
            addr = (int)ctx.Value;
            if (mode == 0) return;

            /* make opcode */
            if ((mode & (int)(ZP | ZP_X)) != 0) ctx.OpVal = (AsmDirective)0x83;
            if ((mode & (int)(ABS | ABS_X)) != 0) ctx.OpVal = (AsmDirective)0x93;
            if ((mode & (int)(ZP_X | ABS_X)) != 0) ctx.OpVal += 0x20;

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* opcodes */
                outPr.PutByte(ctx.LocCnt, (int)ctx.OpVal);
                outPr.PutByte(ctx.LocCnt + 1, imm);

                if ((mode & (int)(ZP | ZP_X)) != 0)
                {
                    /* zero page */
                    outPr.PutByte(ctx.LocCnt + 2, addr);
                }
                else
                {
                    /* absolute */
                    outPr.PutWord(ctx.LocCnt + 2, addr);
                }
            }

            /* update location counter */
            if ((mode & (int)(ZP | ZP_X)) != 0)
            {
                ctx.LocCnt += 3;
            }
            else
            {
                ctx.LocCnt += 4;
            }

            /* auto-increment */
            if (ctx.AutoInc != 0)
            {
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.PutByte(ctx.LocCnt, ctx.AutoInc);
                }
                ctx.LocCnt += 1;
            }

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
                outPr.PrintLn();
        }

        /// <summary>
        /// TAM/TMA instruction
        /// </summary>
        /// <param name="ip"></param>
        public void Class8(ref int ip)
        {
            int mode;

            /* update location counter */
            ctx.LocCnt += 2;

            /* get operand */
            mode = GetOperand(ref ip, (int)IMM, ';');
            if (mode == 0) return;

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* check page index */
                if ((ctx.Value & 0xF8) != 0)
                {
                    outPr.Error("Incorrect page index!");
                    return;
                }

                /* opcodes */
                outPr.PutByte(ctx.DataLocCnt, (int)ctx.OpVal);
                outPr.PutByte(ctx.DataLocCnt + 1, (1 << (int)ctx.Value));

                /* output line */
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// RMB/SMB instructions
        /// </summary>
        /// <param name="ip"></param>
        public void Class9(ref int ip)
        {
            int bit;
            int mode;

            /* update location counter */
            ctx.LocCnt += 2;

            /* get the bit index */
            mode = GetOperand(ref ip, (int)IMM, ',');
            bit = (int)ctx.Value;
            if (mode == 0) return;

            /* get the zero page address */
            mode = GetOperand(ref ip, (int)ZP, ';');
            if (mode == 0) return;

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* check bit number */
                if (bit > 7)
                {
                    outPr.Error("Incorrect bit number!");
                    return;
                }

                /* opcodes */
                outPr.PutByte(ctx.DataLocCnt, (int)ctx.OpVal + (bit << 4));
                outPr.PutByte(ctx.DataLocCnt + 1, (int)ctx.Value);

                /* output line */
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// BBR/BBS instructions
        /// </summary>
        /// <param name="ip"></param>
        public void Class10(ref int ip)
        {
            int bit;
            int zp;
            int mode;
            uint addr;

            /* update location counter */
            ctx.LocCnt += 3;

            /* get the bit index */
            mode = GetOperand(ref ip, (int)IMM, ',');
            bit = (int)ctx.Value;
            if (mode == 0) return;

            /* get the zero page address */
            mode = GetOperand(ref ip, (int)ZP, ',');
            zp = (int)ctx.Value;
            if (mode == 0) return;

            /* get the jump address */
            mode = GetOperand(ref ip, (int)ABS, ';');
            if (mode == 0) return;

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* check bit number */
                if (bit > 7)
                {
                    outPr.Error("Incorrect bit number!");
                    return;
                }

                /* opcodes */
                outPr.PutByte(ctx.DataLocCnt, (int)ctx.OpVal + (bit << 4));
                outPr.PutByte(ctx.DataLocCnt + 1, zp);

                /* calculate branch offset */
                addr = (uint)(ctx.Value - (ctx.LocCnt + (ctx.Page << 13)));

                /* check range */
                if (addr > 0x7F && addr < 0xFFFFFF80)
                {
                    outPr.Error("Branch address out of range!");
                    return;
                }

                /* offset */
                outPr.PutByte(ctx.DataLocCnt + 2, (int)addr);

                /* output line */
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// getoperand()
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="flag"></param>
        /// <param name="last_char"></param>
        /// <returns></returns>
        public int GetOperand(ref int ip, int flag, int last_char)
        {
            uint tmp;
            char c;
            int code;
            int mode;
            int pos;
            int end;
            // 2010/12/30 minachun add
            int flag_preind = 0;
            // 2010/12/30 minachun end

            /* init */
            ctx.AutoInc = 0;
            ctx.AutoTag = 0;

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

            /* check addressing mode */
            switch (ctx.PrLnBuf[ip])
            {
            case '\0':
            case ';':
                /* no operand */
                outPr.Error("Operand missing!");
                return (0);
            case 'A':
            case 'a':
                /* accumulator */
                c = ctx.PrLnBuf[ip + 1];
                if (CharUtil.IsSpace(c) || c == '\0' || c == ';' || c == ',')
                {
                    mode = (int)ACC;
                    ip++;
                    break;
                }
                goto default;
            default:
                /* other */
                switch (ctx.PrLnBuf[ip])
                {
                case '#':
                    /* immediate */
                    mode = (int)IMM;
                    ip++;
                    break;
                case '<':
                    /* zero page */
                    mode = (int)(ZP | ZP_X | ZP_Y);
                    ip++;
                    break;
                case '[':
                    /* indirect */
                    mode = (int)(ABS_IND | ABS_IND_X | ZP_IND | ZP_IND_X | ZP_IND_Y);
                    ip++;
                    break;
                // 2010/12/30 minachun add
                case '(':
                    /* special case for Pre(Post)-indexed indirect */
                    if (opt.AutoZPOpt)
                    {
                        flag_preind = CheckPreindexed(ip);
                        if (flag_preind < 0) return (0);
                        if (flag_preind == 1)
                        {
                            mode = (int)(ZP_IND_Y | ABS_Y);
                        }
                        else if (flag_preind > 0)
                        {
                            ip++;            /* skip '(' */
                            ctx.PrLnBuf[flag_preind] = ';';
                            mode = (int)ZP_IND_X;
                        }
                        else
                        {
                            mode = (int)(ZP_X | ZP_Y | ABS_X | ABS_Y | ABS | ZP | ZP_IND_Y);
                        }
                    }
                    else
                    {
                        /* same default */
                        // mode = ZP | ZP_X | ZP_Y | ABS | ABS_X | ABS_Y;
                        mode = (int)(ABS | ABS_X | ABS_Y);
                    }
                    break;
                // 2010/12/30 minachun end

                default:
                    /* absolute */
                    // 2010/12/30 minachun changed.
                    if (opt.AutoZPOpt)
                    {
                        mode = (int)(ZP | ZP_X | ZP_Y | ABS | ABS_X | ABS_Y);
                    }
                    else
                    {
                        mode = (int)(ABS | ABS_X | ABS_Y);
                    }
                    break;
                }

                /* get value */
                if (exprPr.Evaluate(ref ip, '\0') == 0) return (0);

                // 2010/12/30 minachun add
                /* restore preindexed */
                if (flag_preind > 1)
                {
                    ctx.PrLnBuf[flag_preind] = ',';
                }
                // 2010/12/30 minachun end

                /* check addressing mode */
                code = 0;
                end = 0;
                pos = 0;

                while (end == 0)
                {
                    c = ctx.PrLnBuf[ip];
                    if (c == ';' || c == '\0') break;

                    switch (char.ToUpper(c))
                    {
                    case ',':       /* , = 5 */
                        if (pos == 0)
                        {
                            pos = ip;
                        }
                        else
                        {
                            end = 1;
                            break;
                        }
                        code++;
                        goto case '+';
                    case '+':       /* + = 4 */
                        code++;
                        goto case ')';
                    // 2010/12/30 minachun add
                    case ')':       /* ) = 3 */
                        if (c == ')' && flag_preind == 0)
                        {
                            // same default
                            code = 0xFFFFFF;
                            end = 1;
                            break;
                        }
                        goto case ']';
                    // 2010/12/30 minachun end
                    case ']':       /* ] = 3 */
                        code++;
                        if (ctx.PrLnBuf[ip + 1] == '.')
                        {
                            end = 1;
                            break;
                        }
                        goto case 'X';
                    case 'X':       /* X = 2 */
                        code++;
                        goto case 'Y';
                    case 'Y':       /* Y = 1 */
                        code++;
                        code <<= 4;
                        goto case ' ';
                    case ' ':
                    case '\t':
                        ip++;
                        break;
                    default:
                        code = 0xFFFFFF;
                        end = 1;
                        break;
                    }
                }

                /* absolute, zp, or immediate */
                if (code == 0x000000)
                {
                    // 2011/01/01 minachun add
                    if ((opt.AutoZPOpt))
                    {
                        //printf("**");
                        if ((int)ctx.OpVal != 0x14 && (int)ctx.OpVal != 0x40 && (ctx.Value & 0xFFFFFF00) == 0)
                        {
                            //printf("**");
                            mode &= (int)(ZP | IMM);
                        }
                        else
                        {
                            //printf("*");
                            mode &= (int)(ABS | IMM);
                        }
                    }
                    else
                    {
                        // 2011/01/01 minachun end
                        mode &= (int)(ABS | ZP | IMM);
                    }
                }

                /* indirect */
                else if (code == 0x000030)
                    mode &= (int)(ZP_IND | ABS_IND);     // ]

                /* indexed modes */
                else if (code == 0x000510)
                {
                    // 2011/01/01 minachun add
                    if ((flag_preind == 1) && (ctx.Value & 0xFFFFFF00) == 0)
                    {
                        mode &= (int)ZP_IND_Y;
                    }
                    else
                    {
                        // 2011/01/10 minachun fixed.
                        // if ( ( autozp_opt == 1 ) && (pass == LAST_PASS) ) {
                        //				if ( ( autozp_opt == 1 ) ) {
                        //					if ( ( value & 0xFFFFFF00 ) == 0 ) {
                        //						mode &= ZP_Y;
                        //					} else {
                        //						mode &= ABS_Y;			// ,Y
                        //					}
                        //				} else {
                        mode &= (int)(ABS_Y | ZP_Y);         // ,Y
                                                             //				}
                    }
                    // 2011/01/01 minachun end
                }
                else if (code == 0x000520)
                {
                    // 2011/01/01 minachun add
                    //printf("==");
                    // 2011/01/10 minachun fixed.
                    //			if ( ( autozp_opt == 1 ) && (pass == LAST_PASS) ) {
                    if ((opt.AutoZPOpt))
                    {
                        if ((ctx.Value & 0xFFFFFF00) == 0)
                        {
                            //printf("==");
                            mode &= (int)ZP_X;           // ,X
                        }
                        else
                        {
                            //printf("=+");
                            mode &= (int)ABS_X;          // ,X
                        }
                    }
                    else
                    {
                        // 2011/01/01 minachun end
                        //printf("= autozp_opt=%d value=%08x ", autozp_opt,value);
                        mode &= (int)(ABS_X | ZP_X);         // ,X
                    }
                }
                else if (code == 0x005230)
                {
                    mode &= (int)(ZP_IND_X | ABS_IND_X); // ,X]
                }
                else if (code == 0x003510)
                {
                    mode &= (int)(ZP_IND_Y);             // ],Y
                }
                else if (code == 0x000001)
                {
                    mode &= (int)(ZP_IND_Y);             // ].tag
                    ip += 2;

                    /* get tag */
                    tmp = ctx.Value;

                    if (exprPr.Evaluate(ref ip, '\0') == 0) return (0);

                    /* ok */
                    ctx.AutoTag = 1;
                    ctx.AutoTagValue = ctx.Value;
                    ctx.Value = tmp;
                }
                /* indexed modes with post-increment */
                else if (code == 0x051440)
                {
                    mode &= (int)(ABS_Y | ZP_Y);         // ,Y++
                    ctx.AutoInc = 0xC8;
                }
                else if (code == 0x052440)
                {
                    mode &= (int)(ABS_X | ZP_X);         // ,X++
                    ctx.AutoInc = 0xE8;
                }
                else if (code == 0x351440)
                {
                    mode &= (int)(ZP_IND_Y);             // ],Y++
                    ctx.AutoInc = 0xC8;
                }
                /* absolute, zp, or immediate (or error) */
                else
                {
                    mode &= (int)(ABS | ZP | IMM);
                    if (pos != 0) ip = pos;
                }

                // 2010/12/30 minachun add


                // ToDo: ここの判定がそもそもうまくいけてないよ。
                /*
                        printf("Line:%s  value=%08x opval=%02x opext=%02x code=%08x preind=%d pass=%d flag=%08x mode=%08x(%s%s%s%s%s%s%s%s%s%s)\n",
                            prlnbuf,value,opval,opext,code,flag_preind,pass,flag,mode,
                            (mode&IMM)?"IMM ":"",
                            (mode&ZP)?"ZP ":"",
                            (mode&ZP_X)?"ZP_X ":"",
                            (mode&ZP_Y)?"ZP_Y ":"",
                            (mode&ZP_IND)?"ZP_IND ":"",
                            (mode&ZP_IND_X)?"ZP_IND_X ":"",
                            (mode&ZP_IND_Y)?"ZP_IND_Y ":"",
                            (mode&ABS)?"ABS ":"",
                            (mode&ABS_X)?"ABS_X ":"",
                            (mode&ABS_Y)?"ABS_Y ":""
                            );
                */
                // 2011/01/10 minachun fixed.
                if (opt.AutoZPOpt && ctx.Pass == PassFlag.LAST_PASS && (int)ctx.OpVal != 0x14 && (int)ctx.OpVal != 0x40 && (mode & (int)IMM) == 0 && (ctx.Value & 0xFFFFFF00) == 0x00)
                {
                    mode &= (int)(ZP | ZP_X | ZP_Y | ZP_IND | ZP_IND_X | ZP_IND_Y);
                }
                // 2010/12/30 minachun end

                /* check value on last pass */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    /* zp modes */
                    if ((mode & (int)(ZP | ZP_X | ZP_Y | ZP_IND | ZP_IND_X | ZP_IND_Y) & flag) != 0)
                    {
                        /* extension stuff */
                        if (ctx.OpExt != 0 && ctx.AutoInc == 0)
                        {
                            if ((mode & (int)(ZP_IND | ZP_IND_X | ZP_IND_Y)) != 0)
                            {
                                outPr.Error("Instruction extension not supported in indirect modes!");
                            }
                            if (ctx.OpExt == 'H') ctx.Value++;
                        }
                        /* check address validity */
                        if (((ctx.Value & 0xFFFFFF00) != 0) && ((ctx.Value & 0xFFFFFF00) != ctx.Machine.RamBase))
                        {
                            outPr.Error("Incorrect zero page address!");
                        }
                    }
                    /* immediate mode */
                    else if ((mode & (int)(IMM) & flag) != 0)
                    {
                        /* extension stuff */
                        if (ctx.OpExt == 'L')
                        {
                            ctx.Value = (ctx.Value & 0xFF);
                        }
                        else if (ctx.OpExt == 'H')
                        {
                            ctx.Value = (ctx.Value & 0xFF00) >> 8;
                        }
                        else
                        {
                            /* check value validity */
                            if ((ctx.Value > 0xFF) && (ctx.Value < 0xFFFFFF00))
                            {
                                outPr.Error("Incorrect immediate value!");
                            }
                        }
                    }
                    /* absolute modes */
                    else if ((mode & (int)(ABS | ABS_X | ABS_Y | ABS_IND | ABS_IND_X) & flag) != 0)
                    {
                        /* extension stuff */
                        if (ctx.OpExt != 0 && ctx.AutoInc == 0)
                        {
                            if ((mode & (int)(ABS_IND | ABS_IND_X)) != 0)
                            {
                                outPr.Error("Instruction extension not supported in indirect modes!");
                            }
                            if (ctx.OpExt == 'H') ctx.Value++;
                        }
                        /* check address validity */
                        if ((ctx.Value & 0xFFFF0000) != 0)
                        {
                            outPr.Error("Incorrect absolute address!");
                        }
                    }
                }
                break;
            }

            /* compare addressing mode */
            mode &= flag;
            if (mode == 0)
            {
                outPr.Error("Incorrect addressing mode!");
                return (0);
            }

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

            /* get last char */
            c = ctx.PrLnBuf[ip];

            /* check if it's what the user asked for */
            switch (last_char)
            {
            case ';':
                /* last operand */
                if (c != ';' && c != '\0')
                {
                    outPr.Error("Syntax error!");
                    return (0);
                }
                ip++;
                break;
            case ',':
                /* need more operands */
                if (c != ',')
                {
                    outPr.Error("Operand missing!");
                    return (0);
                }
                ip++;
                break;
            }

            /* ok */
            return (mode);
        }

        /// <summary>
        /// get a string from prlnbuf
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public int GetString(ref int ip, out string result, int size)
        {
            char c;
            int i;
            char[] buf = new char[size + 1];

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

            /* string must be enclosed */
            if (ctx.PrLnBuf[ip++] != '\"')
            {
                outPr.Error("Incorrect string syntax!");
                result = null;
                return (0);
            }

            /* get string */
            i = 0;
            for (;;)
            {
                c = ctx.PrLnBuf[ip++];
                if (c == '\"') break;
                if (i >= size)
                {
                    outPr.Error("String too long!");
                    result = null;
                    return (0);
                }
                buf[i++] = c;
            }

            /* end the string */
            buf[i] = '\0';

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

            /* ok */
            result = buf.ToStringFromNullTerminated();
            return (1);
        }

        // 2010/12/30 minachun add

        /// <summary>
        /// check PreIndexed(opt ($nn,x)) from prlnbuf
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int CheckPreindexed(int idx)
        {
            /* temporaly replace ',' to ')' for "opt ($nn,X)" */
            int idx_comma = 0;
            int level_rnd = 0;
            int idx_x = 0;
            int idx_y = 0;
            int end;
            char c;

            /* parse */
            end = 0;
            while (end == 0)
            {
                c = ctx.PrLnBuf[idx];
                if (c == ';' || c == '\0')
                {
                    break;
                }
                switch (char.ToUpper(c))
                {
                case ',':
                    idx_comma = idx;
                    idx_x = idx_y = 0;      /* reset */
                    break;
                case 'X':
                    idx_x = idx;
                    break;
                case 'Y':
                    idx_y = idx;
                    break;
                case '(':
                    level_rnd++;
                    idx_comma = 0;      /* reset */
                    break;
                case ')':
                    level_rnd--;
                    if (level_rnd < 0)
                    {
                        end = 1;
                    }
                    break;
                default:
                    if (!CharUtil.IsSpace(c))
                    {
                        /* reset by not Xreg. */
                        idx_x = idx_y = 0;
                    }
                    else
                    {
                        /* space */
                    }
                    break;
                }
                idx++;
            }
            /* check error */
            if (end == 0 && level_rnd > 0)
            {
                /* Syntax Error  unbalanced () */
                outPr.Error("Syntax error in expression!");
                return (-1);
            }
            /* check pre-indexed indirect : ( , X ) */
            if (idx_comma > 0 && idx_x > idx_comma)
            {
                return (idx_comma);
            }
            if (idx_comma > 0 && idx_y > idx_comma)
            {
                return (1);
            }
            return (0);
        }

        // 2010/12/30 minachun end
    }
}
