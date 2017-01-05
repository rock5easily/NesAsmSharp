using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class CommandProcessor : ProcessorBase
    {
        /* pseudo instructions section flag */
        /*
         * 1 << S_ZP   = 1,
         * 1 << S_BSS  = 2,
         * 1 << S_CODE = 4,
         * 1 << S_DATA = 8,
         */
        public static readonly int[] PseudoFlag = {
            0x0C, 0x0C, 0x0F, 0x0F, 0x0F, 0x0C, 0x0C, 0x0C, 0x0F, 0x0C,
            0x0C, 0x0C, 0x0C, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F,
            0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0C,
            0x0F, 0x0F, 0x0F, 0x0C, 0x0C, 0x0C, 0x0C, 0x0F, 0x0F, 0x0F,
            0x0F, 0x0F, 0x0C, 0x0C, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x0F
        };

        public CommandProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        /// <summary>
        /// pseudo instruction processor
        /// </summary>
        /// <param name="ip"></param>
        public void DoPseudo(ref int ip)
        {
            string str;
            int old_bank;
            int size;

            /* check if the directive is allowed in the current section */
            if ((PseudoFlag[(int)ctx.OpVal] & (1 << (int)ctx.Section)) == 0)
            {
                outPr.FatalError("Directive not allowed in the current section!");
            }

            /* save current location */
            old_bank = ctx.Bank;

            /* execute directive */
            ctx.OpProc(ref ip);

            /* reset last label pointer */
            switch (ctx.OpVal)
            {
            case AsmDirective.P_VRAM:
            case AsmDirective.P_PAL:
                break;
            case AsmDirective.P_DB:
            case AsmDirective.P_DW:
                if (ctx.LastLabl != null)
                {
                    if (ctx.LastLabl.DataType != AsmDirective.P_DB) ctx.LastLabl = null;
                }
                break;
            default:
                if (ctx.LastLabl != null)
                {
                    if (ctx.LastLabl.DataType != ctx.OpVal) ctx.LastLabl = null;
                }
                break;
            }

            /* bank overflow warning */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                if (opt.AsmOpt[AssemblerOption.OPT_WARNING])
                {
                    switch (ctx.OpVal)
                    {
                    case AsmDirective.P_INCBIN:
                    case AsmDirective.P_INCCHR:
                    case AsmDirective.P_INCSPR:
                    case AsmDirective.P_INCPAL:
                    case AsmDirective.P_INCBAT:
                    case AsmDirective.P_INCTILE:
                    case AsmDirective.P_INCMAP:
                        if (ctx.Bank != old_bank)
                        {
                            size = ((ctx.Bank - old_bank - 1) * 8192) + ctx.LocCnt;
                            if (size != 0)
                            {
                                str = string.Format("Warning, bank overflow by {0} bytes!\n", size);
                                outPr.Warning(str);
                            }
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// .list pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoList(ref int ip)
        {
            /* check end of line */
            if (asmPr.CheckEOL(ref ip) == 0) return;

            opt.AsmOpt[AssemblerOption.OPT_LIST] = true;
            opt.XListOpt = true;
        }

        /// <summary>
        /// .mlist pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoMlist(ref int ip)
        {
            /* check end of line */
            if (asmPr.CheckEOL(ref ip) == 0) return;

            opt.AsmOpt[AssemblerOption.OPT_MACRO] = true;
        }

        /// <summary>
        /// .nolist pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoNolist(ref int ip)
        {
            /* check end of line */
            if (asmPr.CheckEOL(ref ip) == 0) return;

            opt.AsmOpt[AssemblerOption.OPT_LIST] = false;
        }

        /// <summary>
        /// .nomlist pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoNomlist(ref int ip)
        {
            /* check end of line */
            if (asmPr.CheckEOL(ref ip) == 0) return;

            opt.AsmOpt[AssemblerOption.OPT_MACRO] = opt.MListOpt;
        }

        /// <summary>
        /// .db pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoDb(ref int ip)
        {
            char c;

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* output infos */
            ctx.DataLocCnt = ctx.LocCnt;
            ctx.DataLevel = 2;

            /* skip spaces */
            while (CharUtil.IsSpace(ctx.PrLnBuf[++ip])) ;

            /* get bytes */
            for (;;)
            {
                /* ASCII string */
                if (ctx.PrLnBuf[ip] == '\"')
                {
                    for (;;)
                    {
                        c = ctx.PrLnBuf[++ip];
                        if (c == '\"') break;

                        if (c == '\0')
                        {
                            outPr.Error("Unterminated ASCII string!");
                            return;
                        }
                        if (c == '\\')
                        {
                            c = ctx.PrLnBuf[++ip];
                            switch (c)
                            {
                            case 'r':
                                c = '\r';
                                break;
                            case 'n':
                                c = '\n';
                                break;
                            case 't':
                                c = '\t';
                                break;
                            }
                        }
                        /* store char on last pass */
                        if (ctx.Pass == PassFlag.LAST_PASS)
                        {
                            outPr.PutByte(ctx.LocCnt, c);
                        }
                        /* update location counter */
                        ctx.LocCnt++;
                    }
                    ip++;
                }
                /* bytes */
                else
                {
                    /* get a byte */
                    if (exprPr.Evaluate(ref ip, (char)0) == 0) return;

                    /* update location counter */
                    ctx.LocCnt++;

                    /* store byte on last pass */
                    if (ctx.Pass == PassFlag.LAST_PASS)
                    {
                        /* check for overflow */
                        if ((ctx.Value > 0xFF) && (ctx.Value < 0xFFFFFF80))
                        {
                            outPr.Error("Overflow error!");
                            return;
                        }

                        /* store byte */
                        outPr.PutByte(ctx.LocCnt - 1, (int)ctx.Value);
                    }
                }

                /* check if there's another byte */
                c = ctx.PrLnBuf[ip++];

                if (c != ',') break;
            }

            /* check error */
            if (c != ';' && c != '\0')
            {
                outPr.Error("Syntax error!");
                return;
            }

            /* size */
            if (ctx.LablPtr != null)
            {
                ctx.LablPtr.DataType = AsmDirective.P_DB;
                ctx.LablPtr.DataSize = ctx.LocCnt - ctx.DataLocCnt;
            }
            else
            {
                if (ctx.LastLabl != null)
                {
                    if (ctx.LastLabl.DataType == AsmDirective.P_DB)
                    {
                        ctx.LastLabl.DataSize += ctx.LocCnt - ctx.DataLocCnt;
                    }
                }
            }

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .dw pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoDw(ref int ip)
        {
            char c;

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* output infos */
            ctx.DataLocCnt = ctx.LocCnt;
            ctx.DataSize = 2;
            ctx.DataLevel = 2;

            /* get data */
            for (;;)
            {
                /* get a word */
                if (exprPr.Evaluate(ref ip, (char)0) == 0)
                    return;

                /* update location counter */
                ctx.LocCnt += 2;

                /* store word on last pass */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    /* check for overflow */
                    if ((ctx.Value > 0xFFFF) && (ctx.Value < 0xFFFF8000))
                    {
                        outPr.Error("Overflow error!");
                        return;
                    }

                    /* store word */
                    outPr.PutWord(ctx.LocCnt - 2, (int)ctx.Value);
                }

                /* check if there's another word */
                c = ctx.PrLnBuf[ip++];

                if (c != ',') break;
            }

            /* check error */
            if (c != ';' && c != '\0')
            {
                outPr.Error("Syntax error!");
                return;
            }

            /* size */
            if (ctx.LablPtr != null)
            {
                ctx.LablPtr.DataType = AsmDirective.P_DB;
                ctx.LablPtr.DataSize = ctx.LocCnt - ctx.DataLocCnt;
            }
            else
            {
                if (ctx.LastLabl != null)
                {
                    if (ctx.LastLabl.DataType == AsmDirective.P_DB)
                    {
                        ctx.LastLabl.DataSize += ctx.LocCnt - ctx.DataLocCnt;
                    }
                }
            }

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .equ pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoEqu(ref int ip)
        {
            /* get value */
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            /* assign value to the label */
            symPr.AssignValueToLablPtr((int)ctx.Value, false);

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc((int)ctx.Value, 1);
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .page pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoPage(ref int ip)
        {
            /* not allowed in procs */
            if (ctx.ProcPtr != null)
            {
                outPr.FatalError("PAGE can not be changed in procs!");
                return;
            }

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* get page index */
            if (exprPr.Evaluate(ref ip, ';') == 0) return;
            if (ctx.Value > 7)
            {
                outPr.Error("Invalid page index!");
                return;
            }
            ctx.Page = (int)ctx.Value;

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc((int)ctx.Value << 13, 1);
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .org pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoOrg(ref int ip)
        {
            /* get the .org value */
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            /* check for undefined symbol - they are not allowed in .org */
            if (ctx.Undef != 0)
            {
                outPr.Error("Undefined symbol in operand field!");
                return;
            }

            /* section switch */
            switch (ctx.Section)
            {
            case SectionType.S_ZP:
                /* zero page section */
                if (((ctx.Value & 0xFFFFFF00) != 0) && ((ctx.Value & 0xFFFFFF00) != ctx.Machine.RamBase))
                {
                    outPr.Error("Invalid address!");
                    return;
                }
                break;
            case SectionType.S_BSS:
                /* ram section */
                if ((ctx.Value < ctx.Machine.RamBase) || (ctx.Value >= (ctx.Machine.RamBase + ctx.Machine.RamLimit)))
                {
                    outPr.Error("Invalid address!");
                    return;
                }
                break;
            case SectionType.S_CODE:
            case SectionType.S_DATA:
                /* not allowed in procs */
                if (ctx.ProcPtr != null)
                {
                    outPr.FatalError("ORG can not be changed in procs!");
                    return;
                }

                /* code and data section */
                if ((ctx.Value & 0xFFFF0000) != 0)
                {
                    outPr.Error("Invalid address!");
                    return;
                }
                ctx.Page = ((int)ctx.Value >> 13) & 0x07;
                break;
            }

            /* set location counter */
            ctx.LocCnt = ((int)ctx.Value & 0x1FFF);

            /* set label value if there was one */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* output line on last pass */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc((int)ctx.Value, 1);
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .bank pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoBank(ref int ip)
        {
            string name;

            /* not allowed in procs */
            if (ctx.ProcPtr != null)
            {
                outPr.FatalError("Bank can not be changed in procs!");
                return;
            }

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* get bank index */
            if (exprPr.Evaluate(ref ip, (char)0) == 0) return;
            if (ctx.Value > ctx.BankLimit)
            {
                outPr.Error("Bank index out of range!");
                return;
            }

            /* check if there's a bank name */
            switch (ctx.PrLnBuf[ip])
            {
            case ';':
            case '\0':
                break;
            case ',':
                /* get name */
                ip++;
                if (codePr.GetString(ref ip, out name, 63) == 0) return;

                /* check name validity */
                if (ctx.BankName[ctx.Value].Length > 0)
                {
                    if (ctx.BankName[ctx.Value].ToLower() != name.ToLower())
                    {
                        outPr.Error("Different bank names not allowed!");
                        return;
                    }
                }
                /* copy name */
                ctx.BankName[ctx.Value] = name;

                /* check end of line */
                if (asmPr.CheckEOL(ref ip) == 0) return;
                /* ok */
                break;
            default:
                outPr.Error("Syntax error!");
                return;
            }

            /* backup current bank infos */
            ctx.BankGLabl[(int)ctx.Section, ctx.Bank] = ctx.GLablPtr;
            ctx.BankLocCnt[(int)ctx.Section, ctx.Bank] = ctx.LocCnt;
            ctx.BankPage[(int)ctx.Section, ctx.Bank] = ctx.Page;

            /* get new bank infos */
            ctx.Bank = (int)ctx.Value;
            ctx.Page = ctx.BankPage[(int)ctx.Section, ctx.Bank];
            ctx.LocCnt = ctx.BankLocCnt[(int)ctx.Section, ctx.Bank];
            ctx.GLablPtr = ctx.BankGLabl[(int)ctx.Section, ctx.Bank];

            /* update the max bank counter */
            if (ctx.MaxBank < ctx.Bank) ctx.MaxBank = ctx.Bank;

            /* output on last pass */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc(ctx.Bank, 1);
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .incbin pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoIncbin(ref int ip)
        {
            FileStream fsBin;
            int p;
            string fname;
            long longsize;

            /* get file name */
            if (codePr.GetString(ref ip, out fname, 127) == 0) return;

            /* get file extension */
            if ((p = fname.LastIndexOf('.')) >= 0)
            {
                if (fname.IndexOf(Path.DirectorySeparatorChar, p) < 0)
                {
                    /* check if it's a mx file */
                    if (fname.Substring(p).ToLower() == ".mx")
                    {
                        DoMx(fname);
                        return;
                    }
                    /* check if it's a map file */
                    if (fname.Substring(p).ToLower() == ".fmp")
                    {
                        // if (pce_load_map(fname_str, 0))
                        //     return;
                        throw new PCENotImplementedException();
                    }
                }
            }

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* output */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc(ctx.LocCnt, 0);
            }

            /* open file */
            var targetName = inPr.FindFileByIncPath(fname);
            if (targetName == null)
            {
                outPr.FatalError("file not found!");
                return;
            }

            try
            {
                fsBin = new FileStream(targetName, FileMode.Open, FileAccess.Read);
            }
            catch(Exception e)
            {
                outPr.FatalError($"Can not open file '{targetName}'!");
                return;
            }

            int size = 0;
            using (fsBin)
            {
                try
                {
                    /* get file size */
                    longsize = fsBin.Length;

                    /* check if it will fit in the rom */
                    if (((ctx.Bank << 13) + ctx.LocCnt + longsize) > ctx.RomLimit)
                    {
                        fsBin.Close();
                        outPr.Error("ROM overflow!");
                        return;
                    }

                    size = (int)longsize;
                    /* load data on last pass */
                    if (ctx.Pass == PassFlag.LAST_PASS)
                    {
                        var buf = new byte[size];
                        fsBin.Read(buf, 0, size);

                        var _bank = ctx.Bank;
                        var _loc = ctx.LocCnt;
                        var m = (byte)(ctx.Section + (ctx.Page << 5));
                        for (var i = 0; i < size; i++)
                        {
                            ctx.Rom[_bank, _loc] = buf[i];
                            ctx.Map[_bank, _loc] = m;
                            if (++_loc == 0x2000)
                            {
                                _loc = 0;
                                _bank++;
                            }
                        }

                        /* output line */
                        outPr.PrintLn();
                    }
                }
                catch(Exception e)
                {
                    outPr.FatalError($"file '{targetName}' read error!");
                    return;
                }
            }

            /* update bank and location counters */
            ctx.Bank += (ctx.LocCnt + size) >> 13;
            ctx.LocCnt = (ctx.LocCnt + size) & 0x1FFF;
            if (ctx.Bank > ctx.MaxBank)
            {
                if (ctx.LocCnt != 0)
                {
                    ctx.MaxBank = ctx.Bank;
                }
                else
                {
                    ctx.MaxBank = ctx.Bank - 1;
                }
            }

            /* size */
            if (ctx.LablPtr != null)
            {
                ctx.LablPtr.DataType = AsmDirective.P_INCBIN;
                ctx.LablPtr.DataSize = size;
            }
            else
            {
                if (ctx.LastLabl != null)
                {
                    if (ctx.LastLabl.DataType == AsmDirective.P_INCBIN)
                    {
                        ctx.LastLabl.DataSize += size;
                    }
                }
            }
        }

        /// <summary>
        /// load a mx file
        /// </summary>
        /// <param name="fname"></param>
        public void DoMx(string fname)
        {
            StreamReader fp;
            int ptr;
            char type;
            string line_str;
            byte[] buffer = new byte[128];
            int data;
            int flag = 0;
            int size = 0;
            int cnt, addr, chksum;
            int i;

            /* open the file */
            try
            {
                fp = new StreamReader(fname, opt.Encoding);
            }
            catch (Exception e)
            {
                outPr.FatalError($"Can not open file '{fname}'!");
                return;
            }

            using (fp)
            {
                try
                {
                    /* read loop */
                    while ((line_str = fp.ReadLine()) != null)
                    {
                        var line = line_str.ToNullTerminatedCharArray();

                        if (line[0] == 'S')
                        {
                            /* get record type */
                            type = line[1];

                            /* error on unsupported records */
                            if ((type != '2') && (type != '8'))
                            {
                                outPr.Error("Unsupported S-record type!");
                                return;
                            }

                            /* get count and address */
                            cnt = HexToInt(line, 2, 2);
                            addr = HexToInt(line, 4, 6);

                            if ((line.Length < 12) || (cnt < 4) || (addr == -1))
                            {
                                outPr.Error("Incorrect S-record line!");
                                return;
                            }

                            /* adjust count */
                            cnt -= 4;

                            /* checksum */
                            chksum = cnt + ((addr >> 16) & 0xFF) +
                                           ((addr >> 8) & 0xFF) +
                                           ((addr) & 0xFF) + 4;

                            /* get data */
                            ptr = 10;

                            for (i = 0; i < cnt; i++)
                            {
                                data = HexToInt(line, ptr, 2);
                                buffer[i] = (byte)data;
                                chksum += data;
                                ptr += 2;

                                if (data == -1)
                                {
                                    outPr.Error("Syntax error in a S-record line!");
                                    return;
                                }
                            }

                            /* checksum test */
                            data = HexToInt(line, ptr, 2);
                            chksum = (~chksum) & 0xFF;

                            if (data != chksum)
                            {
                                outPr.Error("Checksum error!");
                                return;
                            }

                            /* end record */
                            if (type == '8') break;

                            /* data record */
                            if (type == '2')
                            {
                                /* set the location counter */
                                if ((addr & 0xFFFF0000) != 0)
                                {
                                    outPr.Error("Invalid address!");
                                    return;
                                }
                                ctx.Page = (addr >> 13) & 0x07;
                                ctx.LocCnt = (addr & 0x1FFF);

                                /* define label */
                                if (flag == 0)
                                {
                                    flag = 1;
                                    symPr.AssignValueToLablPtr(ctx.LocCnt, true);

                                    /* output */
                                    if (ctx.Pass == PassFlag.LAST_PASS)
                                    {
                                        outPr.LoadLc(ctx.LocCnt, 0);
                                    }
                                }

                                /* copy data */
                                if (ctx.Pass == PassFlag.LAST_PASS)
                                {
                                    for (i = 0; i < cnt; i++)
                                    {
                                        outPr.PutByte(ctx.LocCnt + i, buffer[i]);
                                    }
                                }

                                /* update location counter */
                                ctx.LocCnt += cnt;
                                size += cnt;
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    outPr.FatalError($"file '{fname}' read error!");
                    return;
                }
            }

            /* define label */
            if (flag == 0)
            {
                symPr.AssignValueToLablPtr(ctx.LocCnt, true);

                /* output */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    outPr.LoadLc(ctx.LocCnt, 0);
                }
            }

            /* size */
            if (ctx.LablPtr != null)
            {
                ctx.LablPtr.DataType = AsmDirective.P_INCBIN;
                ctx.LablPtr.DataSize = size;
            }
            else
            {
                if (ctx.LastLabl != null)
                {
                    if (ctx.LastLabl.DataType == AsmDirective.P_INCBIN)
                    {
                        ctx.LastLabl.DataSize += size;
                    }
                }
            }

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .include pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoInclude(ref int ip)
        {
            string fname;

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* get file name */
            if (codePr.GetString(ref ip, out fname, 127) == 0) return;

            /* open file */
            if (inPr.OpenInputFile(fname) == -1)
            {
                outPr.FatalError("Can not open file!");
                return;
            }

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .rsset pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoRsset(ref int ip)
        {
            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* get value */
            if (exprPr.Evaluate(ref ip, ';') == 0) return;
            if ((ctx.Value & 0xFFFF0000) != 0)
            {
                outPr.Error("Address out of range!");
                return;
            }

            /* set 'rs' base */
            ctx.RSBase = (int)ctx.Value;

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc(ctx.RSBase, 1);
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .rs pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoRs(ref int ip)
        {
            /* define label */
            symPr.AssignValueToLablPtr(ctx.RSBase, false);

            /* get the number of bytes to reserve */
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            /* ouput line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc(ctx.RSBase, 1);
                outPr.PrintLn();
            }

            /* update 'rs' base */
            ctx.RSBase += (int)ctx.Value;
            if ((ctx.RSBase & 0xFFFF0000) != 0)
            {
                outPr.Error("Address out of range!");
            }
        }

        /// <summary>
        /// .ds pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoDs(ref int ip)
        {
            int limit = 0;
            int addr;

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* get the number of bytes to reserve */
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            /* section switch */
            switch (ctx.Section)
            {
            case SectionType.S_ZP:
                /* zero page section */
                limit = (int)ctx.Machine.ZPLimit;
                break;

            case SectionType.S_BSS:
                /* ram section */
                limit = (int)ctx.Machine.RamLimit;
                break;

            case SectionType.S_CODE:
            case SectionType.S_DATA:
                /* code and data sections */
                limit = 0x2000;
                break;
            }

            /* check range */
            if ((ctx.LocCnt + ctx.Value) > limit)
            {
                outPr.Error("Out of range!");
                return;
            }

            /* update max counter for zp and bss sections */
            addr = ctx.LocCnt + (int)ctx.Value;

            switch (ctx.Section)
            {
            case SectionType.S_ZP:
                /* zero page */
                if (addr > ctx.MaxZP)
                    ctx.MaxZP = addr;
                break;

            case SectionType.S_BSS:
                /* ram page */
                if (addr > ctx.MaxBSS)
                    ctx.MaxBSS = addr;
                break;
            }

            /* output line on last pass */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                switch (ctx.Section)
                {
                case SectionType.S_CODE:
                case SectionType.S_DATA:
                    for (var i = 0; i < ctx.Value; i++)
                    {
                        ctx.Rom[ctx.Bank, ctx.LocCnt + i] = 0;
                        ctx.Map[ctx.Bank, ctx.LocCnt + i] = (byte)((int)ctx.Section + (ctx.Page << 5));
                    }
                    if (ctx.Bank > ctx.MaxBank) ctx.MaxBank = ctx.Bank;
                    break;
                }
                outPr.LoadLc(ctx.LocCnt, 0);
                outPr.PrintLn();
            }

            /* update location counter */
            ctx.LocCnt += (int)ctx.Value;
        }

        /// <summary>
        /// .fail pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoFail(ref int ip)
        {
            outPr.FatalError("Compilation failed!");
        }

        /// <summary>
        /// .zp/.bss/.code/.data pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoSection(ref int ip)
        {
            if (ctx.ProcPtr != null)
            {
                if (ctx.OpType == (int)SectionType.S_DATA)
                {
                    outPr.FatalError("No data segment in procs!");
                    return;
                }
            }

            if ((int)ctx.Section != ctx.OpType)
            {
                /* backup current section data */
                ctx.SectionBank[(int)ctx.Section] = ctx.Bank;
                ctx.BankGLabl[(int)ctx.Section, ctx.Bank] = ctx.GLablPtr;
                ctx.BankLocCnt[(int)ctx.Section, ctx.Bank] = ctx.LocCnt;
                ctx.BankPage[(int)ctx.Section, ctx.Bank] = ctx.Page;

                /* change section */
                ctx.Section = (SectionType)ctx.OpType;

                /* switch to the new section */
                ctx.Bank = ctx.SectionBank[(int)ctx.Section];
                ctx.Page = ctx.BankPage[(int)ctx.Section, ctx.Bank];
                ctx.LocCnt = ctx.BankLocCnt[(int)ctx.Section, ctx.Bank];
                ctx.GLablPtr = ctx.BankGLabl[(int)ctx.Section, ctx.Bank];
            }

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc(ctx.LocCnt + (ctx.Page << 13), 1);
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .inchr pseudo - convert a PCX to 8x8 character tiles
        /// </summary>
        /// <param name="ip"></param>
        public void DoIncchr(ref int ip)
        {
            byte[] buffer = new byte[32];
            int i, j;
            int x, y, w, h;
            int tx, ty;
            int total = 0;
            int size;

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* output */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc(ctx.LocCnt, 0);
            }

            /* get args */
            if (pcxPr.GetPcxArgs(ref ip) == 0) return;
            if (pcxPr.ParsePcxArgs(0, ctx.PcxNbArgs, out x, out y, out w, out h, 8) == 0) return;

            /* pack data */
            for (i = 0; i < h; i++)
            {
                for (j = 0; j < w; j++)
                {
                    /* tile coordinates */
                    tx = x + (j << 3);
                    ty = y + (i << 3);

                    /* get tile */
                    size = pcxPr.PackPcx8x8Tile(buffer, tx, ty);
                    total += size;

                    /* store tile */
                    outPr.PutBuffer(buffer, size);
                }
            }

            /* size */
            if (ctx.LablPtr != null)
            {
                ctx.LablPtr.DataType = AsmDirective.P_INCCHR;
                ctx.LablPtr.DataSize = total;
            }
            else
            {
                if (ctx.LastLabl != null)
                {
                    if (ctx.LastLabl.DataType == AsmDirective.P_INCCHR)
                    {
                        ctx.LastLabl.DataSize += total;
                    }
                }
            }

            /* output */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .opt pseudo - compilation options
        /// </summary>
        /// <param name="ip"></param>
        public void DoOpt(ref int ip)
        {
            char c;
            char flag;
            char[] name = new char[32];
            AssemblerOption option;
            int i;

            for (;;)
            {
                /* skip spaces */
                while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

                /* get char */
                c = ctx.PrLnBuf[ip++];

                /* no option */
                if (c == ',') continue;

                /* end of line */
                if (c == ';' || c == '\0') break;

                /* extract option */
                i = 0;
                for (;;)
                {
                    if (c == ' ') continue;
                    if (c == ',' || c == ';' || c == '\0') break;
                    if (i >= 31)
                    {
                        outPr.Error("Syntax error!");
                        return;
                    }
                    name[i++] = c;
                    c = ctx.PrLnBuf[ip++];
                }

                /* get option flag */
                name[i] = '\0';
                flag = name[--i];
                name[i] = '\0';

                /* search option */
                var name_str = name.ToStringFromNullTerminated().ToLower();
                switch (name_str)
                {
                case "l":
                    option = AssemblerOption.OPT_LIST;
                    break;
                case "m":
                    option = AssemblerOption.OPT_MACRO;
                    break;
                case "w":
                    option = AssemblerOption.OPT_WARNING;
                    break;
                case "o":
                    option = AssemblerOption.OPT_OPTIMIZE;
                    break;
                default:
                    outPr.Error("Unknown option!");
                    return;
                }

                /* set option */
                if (flag == '+')
                {
                    opt.AsmOpt[option] = true;
                }
                else if (flag == '-')
                {
                    opt.AsmOpt[option] = false;
                }
            }

            /* output */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// hex-style string to int
        /// </summary>
        /// <param name="str"></param>
        /// <param name="startIndex"></param>
        /// <param name="nb"></param>
        /// <returns></returns>
        public int HexToInt(char[] str, int startIndex, int nb)
        {
            char c;
            int val;
            int i;

            val = 0;

            for (i = 0; i < nb; i++)
            {
                c = char.ToUpper(str[startIndex + i]);

                if ((c >= '0') && (c <= '9'))
                {
                    val = (val << 4) + (c - '0');
                }
                else if ((c >= 'A') && (c <= 'F'))
                {
                    val = (val << 4) + (c - 'A' + 10);
                }
                else
                {
                    return (-1);
                }
            }

            /* ok */
            return val;
        }

        /* pseudo instruction table */
        private NesAsmOpecode[] basePseudo;
        public NesAsmOpecode[] BasePseudo
        {
            get
            {
                if (basePseudo == null)
                {
                    basePseudo = new NesAsmOpecode[]
                    {
                        new NesAsmOpecode("=",            DoEqu,            OpCodeFlag.PSEUDO, AsmDirective.P_EQU,     0),

                        new NesAsmOpecode("BANK",         DoBank,           OpCodeFlag.PSEUDO, AsmDirective.P_BANK,    0),
                        new NesAsmOpecode("BSS",          DoSection,        OpCodeFlag.PSEUDO, AsmDirective.P_BSS,     (int)SectionType.S_BSS),
                        new NesAsmOpecode("BYTE",         DoDb,             OpCodeFlag.PSEUDO, AsmDirective.P_DB,      0),
                        new NesAsmOpecode("CALL",         procPr.DoCall,    OpCodeFlag.PSEUDO, AsmDirective.P_CALL,    0),
                        new NesAsmOpecode("CODE",         DoSection,        OpCodeFlag.PSEUDO, AsmDirective.P_CODE,    (int)SectionType.S_CODE),
                        new NesAsmOpecode("DATA",         DoSection,        OpCodeFlag.PSEUDO, AsmDirective.P_DATA,    (int)SectionType.S_DATA),
                        new NesAsmOpecode("DB",           DoDb,             OpCodeFlag.PSEUDO, AsmDirective.P_DB,      0),
                        new NesAsmOpecode("DW",           DoDw,             OpCodeFlag.PSEUDO, AsmDirective.P_DW,      0),
                        new NesAsmOpecode("DS",           DoDs,             OpCodeFlag.PSEUDO, AsmDirective.P_DS,      0),
                        new NesAsmOpecode("ELSE",         asmPr.DoElse,     OpCodeFlag.PSEUDO, AsmDirective.P_ELSE,    0),
                        new NesAsmOpecode("ENDIF",        asmPr.DoEndif,    OpCodeFlag.PSEUDO, AsmDirective.P_ENDIF,   0),
                        new NesAsmOpecode("ENDM",         macroPr.DoEndm,  OpCodeFlag.PSEUDO, AsmDirective.P_ENDM,    0),
                        new NesAsmOpecode("ENDP",         procPr.DoEndp,    OpCodeFlag.PSEUDO, AsmDirective.P_ENDP,    (int)AsmDirective.P_PROC),
                        new NesAsmOpecode("ENDPROCGROUP", procPr.DoEndp,    OpCodeFlag.PSEUDO, AsmDirective.P_ENDPG,   (int)AsmDirective.P_PGROUP),
                        new NesAsmOpecode("EQU",          DoEqu,            OpCodeFlag.PSEUDO, AsmDirective.P_EQU,     0),
                        new NesAsmOpecode("FAIL",         DoFail,           OpCodeFlag.PSEUDO, AsmDirective.P_FAIL,    0),
                        new NesAsmOpecode("FUNC",         funcPr.DoFunc,    OpCodeFlag.PSEUDO, AsmDirective.P_FUNC,    0),
                        new NesAsmOpecode("IF",           asmPr.DoIf,       OpCodeFlag.PSEUDO, AsmDirective.P_IF,      0),
                        new NesAsmOpecode("IFDEF",        asmPr.DoIfdef,    OpCodeFlag.PSEUDO, AsmDirective.P_IFDEF,   1),
                        new NesAsmOpecode("IFNDEF",       asmPr.DoIfdef,    OpCodeFlag.PSEUDO, AsmDirective.P_IFNDEF,  0),
                        new NesAsmOpecode("INCBIN",       DoIncbin,         OpCodeFlag.PSEUDO, AsmDirective.P_INCBIN,  0),
                        new NesAsmOpecode("INCLUDE",      DoInclude,        OpCodeFlag.PSEUDO, AsmDirective.P_INCLUDE, 0),
                        new NesAsmOpecode("INCCHR",       DoIncchr,         OpCodeFlag.PSEUDO, AsmDirective.P_INCCHR,  0xEA),
                        new NesAsmOpecode("LIST",         DoList,           OpCodeFlag.PSEUDO, AsmDirective.P_LIST,    0),
                        new NesAsmOpecode("MAC",          macroPr.DoMacro,  OpCodeFlag.PSEUDO, AsmDirective.P_MACRO,   0),
                        new NesAsmOpecode("MACRO",        macroPr.DoMacro,  OpCodeFlag.PSEUDO, AsmDirective.P_MACRO,   0),
                        new NesAsmOpecode("MLIST",        DoMlist,          OpCodeFlag.PSEUDO, AsmDirective.P_MLIST,   0),
                        new NesAsmOpecode("NOLIST",       DoNolist,         OpCodeFlag.PSEUDO, AsmDirective.P_NOLIST,  0),
                        new NesAsmOpecode("NOMLIST",      DoNomlist,        OpCodeFlag.PSEUDO, AsmDirective.P_NOMLIST, 0),
                        new NesAsmOpecode("OPT",          DoOpt,            OpCodeFlag.PSEUDO, AsmDirective.P_OPT,     0),
                        new NesAsmOpecode("ORG",          DoOrg,            OpCodeFlag.PSEUDO, AsmDirective.P_ORG,     0),
                        new NesAsmOpecode("PAGE",         DoPage,           OpCodeFlag.PSEUDO, AsmDirective.P_PAGE,    0),
                        new NesAsmOpecode("PROC",         procPr.DoProc,    OpCodeFlag.PSEUDO, AsmDirective.P_PROC,    (int)AsmDirective.P_PROC),
                        new NesAsmOpecode("PROCGROUP",    procPr.DoProc,    OpCodeFlag.PSEUDO, AsmDirective.P_PGROUP,  (int)AsmDirective.P_PGROUP),
                        new NesAsmOpecode("RSSET",        DoRsset,          OpCodeFlag.PSEUDO, AsmDirective.P_RSSET,   0),
                        new NesAsmOpecode("RS",           DoRs,             OpCodeFlag.PSEUDO, AsmDirective.P_RS,      0),
                        new NesAsmOpecode("WORD",         DoDw,             OpCodeFlag.PSEUDO, AsmDirective.P_DW,      0),
                        new NesAsmOpecode("ZP",           DoSection,        OpCodeFlag.PSEUDO, AsmDirective.P_ZP,      (int)SectionType.S_ZP),

                        new NesAsmOpecode(".BANK",         DoBank,          OpCodeFlag.PSEUDO, AsmDirective.P_BANK,    0),
                        new NesAsmOpecode(".BSS",          DoSection,       OpCodeFlag.PSEUDO, AsmDirective.P_BSS,     (int)SectionType.S_BSS),
                        new NesAsmOpecode(".BYTE",         DoDb,            OpCodeFlag.PSEUDO, AsmDirective.P_DB,      0),
                        new NesAsmOpecode(".CODE",         DoSection,       OpCodeFlag.PSEUDO, AsmDirective.P_CODE,    (int)SectionType.S_CODE),
                        new NesAsmOpecode(".DATA",         DoSection,       OpCodeFlag.PSEUDO, AsmDirective.P_DATA,    (int)SectionType.S_DATA),
                        new NesAsmOpecode(".DB",           DoDb,            OpCodeFlag.PSEUDO, AsmDirective.P_DB,      0),
                        new NesAsmOpecode(".DW",           DoDw,            OpCodeFlag.PSEUDO, AsmDirective.P_DW,      0),
                        new NesAsmOpecode(".DS",           DoDs,            OpCodeFlag.PSEUDO, AsmDirective.P_DS,      0),
                        new NesAsmOpecode(".ELSE",         asmPr.DoElse,    OpCodeFlag.PSEUDO, AsmDirective.P_ELSE,    0),
                        new NesAsmOpecode(".ENDIF",        asmPr.DoEndif,   OpCodeFlag.PSEUDO, AsmDirective.P_ENDIF,   0),
                        new NesAsmOpecode(".ENDM",         macroPr.DoEndm, OpCodeFlag.PSEUDO, AsmDirective.P_ENDM,    0),
                        new NesAsmOpecode(".ENDP",         procPr.DoEndp,   OpCodeFlag.PSEUDO, AsmDirective.P_ENDP,    (int)AsmDirective.P_PROC),
                        new NesAsmOpecode(".ENDPROCGROUP", procPr.DoEndp,   OpCodeFlag.PSEUDO, AsmDirective.P_ENDPG,   (int)AsmDirective.P_PGROUP),
                        new NesAsmOpecode(".EQU",          DoEqu,           OpCodeFlag.PSEUDO, AsmDirective.P_EQU,     0),
                        new NesAsmOpecode(".FAIL",         DoFail,          OpCodeFlag.PSEUDO, AsmDirective.P_FAIL,    0),
                        new NesAsmOpecode(".FUNC",         funcPr.DoFunc,   OpCodeFlag.PSEUDO, AsmDirective.P_FUNC,    0),
                        new NesAsmOpecode(".IF",           asmPr.DoIf,      OpCodeFlag.PSEUDO, AsmDirective.P_IF,      0),
                        new NesAsmOpecode(".IFDEF",        asmPr.DoIfdef,   OpCodeFlag.PSEUDO, AsmDirective.P_IFDEF,   1),
                        new NesAsmOpecode(".IFNDEF",       asmPr.DoIfdef,   OpCodeFlag.PSEUDO, AsmDirective.P_IFNDEF,  0),
                        new NesAsmOpecode(".INCBIN",       DoIncbin,        OpCodeFlag.PSEUDO, AsmDirective.P_INCBIN,  0),
                        new NesAsmOpecode(".INCLUDE",      DoInclude,       OpCodeFlag.PSEUDO, AsmDirective.P_INCLUDE, 0),
                        new NesAsmOpecode(".INCCHR",       DoIncchr,        OpCodeFlag.PSEUDO, AsmDirective.P_INCCHR,  0xEA),
                        new NesAsmOpecode(".LIST",         DoList,          OpCodeFlag.PSEUDO, AsmDirective.P_LIST,    0),
                        new NesAsmOpecode(".MAC",          macroPr.DoMacro, OpCodeFlag.PSEUDO, AsmDirective.P_MACRO,   0),
                        new NesAsmOpecode(".MACRO",        macroPr.DoMacro, OpCodeFlag.PSEUDO, AsmDirective.P_MACRO,   0),
                        new NesAsmOpecode(".MLIST",        DoMlist,         OpCodeFlag.PSEUDO, AsmDirective.P_MLIST,   0),
                        new NesAsmOpecode(".NOLIST",       DoNolist,        OpCodeFlag.PSEUDO, AsmDirective.P_NOLIST,  0),
                        new NesAsmOpecode(".NOMLIST",      DoNomlist,       OpCodeFlag.PSEUDO, AsmDirective.P_NOMLIST, 0),
                        new NesAsmOpecode(".OPT",          DoOpt,           OpCodeFlag.PSEUDO, AsmDirective.P_OPT,     0),
                        new NesAsmOpecode(".ORG",          DoOrg,           OpCodeFlag.PSEUDO, AsmDirective.P_ORG,     0),
                        new NesAsmOpecode(".PAGE",         DoPage,          OpCodeFlag.PSEUDO, AsmDirective.P_PAGE,    0),
                        new NesAsmOpecode(".PROC",         procPr.DoProc,   OpCodeFlag.PSEUDO, AsmDirective.P_PROC,    (int)AsmDirective.P_PROC),
                        new NesAsmOpecode(".PROCGROUP",    procPr.DoProc,   OpCodeFlag.PSEUDO, AsmDirective.P_PGROUP,  (int)AsmDirective.P_PGROUP),
                        new NesAsmOpecode(".RSSET",        DoRsset,         OpCodeFlag.PSEUDO, AsmDirective.P_RSSET,   0),
                        new NesAsmOpecode(".RS",           DoRs,            OpCodeFlag.PSEUDO, AsmDirective.P_RS,      0),
                        new NesAsmOpecode(".WORD",         DoDw,            OpCodeFlag.PSEUDO, AsmDirective.P_DW,      0),
                        new NesAsmOpecode(".ZP",           DoSection,       OpCodeFlag.PSEUDO, AsmDirective.P_ZP,      (int)SectionType.S_ZP)
                    };
                }
                return basePseudo;
            }
        }
    }
}
