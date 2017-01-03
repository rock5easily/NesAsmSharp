using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class OutputProcessor : ProcessorBase
    {
        public OutputProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        /// <summary>
        /// prints the contents of prlnbuf
        /// </summary>
        public void PrintLn()
        {
            int nb, cnt;
            int i;
            string str;

            /* check if output possible */
            if (opt.ListLevel == 0) return;
            if (!opt.XListOpt || !opt.AsmOpt[AssemblerOption.OPT_LIST] ||
                (ctx.IsExpandMacro && !opt.AsmOpt[AssemblerOption.OPT_MACRO])) return;


            /* update line buffer if necessary */
            if (ctx.ContinuedLine)
            {
                ctx.PrLnBuf.CopyAsNullTerminated(ctx.TmpLnBuf);
            }

            /* output */
            if (ctx.DataLocCnt == -1)
            {
                /* line buffer */
                str = ctx.PrLnBuf.ToStringFromNullTerminated();
                ctx.LstFp.WriteLine(str);
            }
            else
            {
                /* line buffer + data bytes */
                LoadLc(ctx.DataLocCnt, 0);

                /* number of bytes */
                nb = ctx.LocCnt - ctx.DataLocCnt;

                /* check level */
                if ((ctx.DataLevel > opt.ListLevel) && (nb > 3))
                {
                    /* doesn't match */
                    str = ctx.PrLnBuf.ToStringFromNullTerminated();
                    ctx.LstFp.WriteLine(str);
                }
                else
                {
                    /* ok */
                    cnt = 0;
                    for (i = 0; i < nb; i++)
                    {
                        if (ctx.Bank >= Definition.RESERVED_BANK)
                        {
                            ctx.PrLnBuf[16 + (3 * cnt)] = '-';
                            ctx.PrLnBuf[17 + (3 * cnt)] = '-';
                        }
                        else
                        {
                            var hex = ctx.Rom[ctx.Bank, ctx.DataLocCnt].ToString("X2");
                            ctx.PrLnBuf[16 + (3 * cnt)] = hex[0];
                            ctx.PrLnBuf[17 + (3 * cnt)] = hex[1];
                        }
                        ctx.DataLocCnt++;
                        cnt++;
                        if (cnt == ctx.DataSize)
                        {
                            cnt = 0;
                            str = ctx.PrLnBuf.ToStringFromNullTerminated();
                            ctx.LstFp.WriteLine(str);
                            ClearLn();
                            LoadLc(ctx.DataLocCnt, 0);
                        }
                    }
                    if (cnt != 0)
                    {
                        str = ctx.PrLnBuf.ToStringFromNullTerminated();
                        ctx.LstFp.WriteLine(str);
                    }
                }
            }
        }

        /// <summary>
        /// clear prlnbuf
        /// </summary>
        public void ClearLn()
        {
            int i;

            for (i = 0; i < Definition.SFIELD; i++)
            {
                ctx.PrLnBuf[i] = ' ';
            }
            ctx.PrLnBuf[i] = '\0';
        }

        /// <summary>
        /// load 16 bit value in printable form into prlnbuf
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="pos"></param>
        public void LoadLc(int offset, int pos)
        {
            var i = (pos != 0) ? 16 : 7;
            string hex;

            if (pos == 0)
            {
                if (ctx.Bank >= Definition.RESERVED_BANK)
                {
                    ctx.PrLnBuf[i++] = '-';
                    ctx.PrLnBuf[i++] = '-';
                }
                else
                {
                    hex = ctx.Bank.ToString("X2");
                    ctx.PrLnBuf[i++] = hex[0];
                    ctx.PrLnBuf[i++] = hex[1];
                }
                ctx.PrLnBuf[i++] = ':';
                offset += ctx.Page << 13;
            }
            hex = offset.ToString("X4");
            ctx.PrLnBuf[i++] = hex[0];
            ctx.PrLnBuf[i++] = hex[1];
            ctx.PrLnBuf[i++] = hex[2];
            ctx.PrLnBuf[i] = hex[3];
        }

        /// <summary>
        /// store a byte in the rom
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void PutByte(int offset, int data)
        {
            if (ctx.Bank >= Definition.RESERVED_BANK) return;

            if (offset < 0x2000)
            {
                ctx.Rom[ctx.Bank, offset] = (byte)(data & 0xFF);
                ctx.Map[ctx.Bank, offset] = (byte)(ctx.Section + (ctx.Page << 5));

                /* update rom size */
                if (ctx.Bank > ctx.MaxBank)
                {
                    ctx.MaxBank = ctx.Bank;
                }
            }
            else
            {
                var newBank = ctx.Bank + offset / 0x2000;
                var newOffset = offset & 0x1FFF;
                var overcnt = offset / 0x2000;
                var newPage = ctx.Page;
                for (var i = 0; i < overcnt; i++)
                {
                    newPage = (newPage + 1) & 0x07;
                    if (newPage == 0) newPage = 4; // reset page: 8000-9FFF
                }

                if (newBank >= Definition.RESERVED_BANK)
                {
                    outPr.FatalError("ROM overflow!(PutOverBank)");
                    return;
                }
                ctx.Rom[newBank, newOffset] = (byte)((data) & 0xFF);
                ctx.Map[newBank, newOffset] = (byte)(ctx.Section + (newPage << 5));

                /* update rom size */
                if (newBank > ctx.MaxBank)
                {
                    ctx.MaxBank = newBank;
                }
            }
        }

        /// <summary>
        /// store a word in the rom
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void PutWord(int offset, int data)
        {
            if (ctx.Bank >= Definition.RESERVED_BANK) return;

            if (offset < 0x1FFF)
            {
                /* low byte */
                ctx.Rom[ctx.Bank, offset] = (byte)(data & 0xFF);
                ctx.Map[ctx.Bank, offset] = (byte)(ctx.Section + (ctx.Page << 5));

                /* high byte */
                ctx.Rom[ctx.Bank, offset + 1] = (byte)((data >> 8) & 0xFF);
                ctx.Map[ctx.Bank, offset + 1] = (byte)(ctx.Section + (ctx.Page << 5));

                /* update rom size */
                if (ctx.Bank > ctx.MaxBank)
                {
                    ctx.MaxBank = ctx.Bank;
                }
            }
            else
            {
                PutByte(offset, (data) & 0xFF);
                PutByte(offset + 1, (data >> 8) & 0xFF);
            }
        }

        /// <summary>
        /// copy a buffer at the current location
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        public void PutBuffer(byte[] data, int size)
        {
            int addr;

            /* check size */
            if (size == 0) return;

            /* check if the buffer will fit in the rom */
            if (ctx.Bank >= Definition.RESERVED_BANK)
            {
                addr = ctx.LocCnt + size;

                if (addr > 0x1FFF)
                {
                    FatalError("PROC overflow!");
                    return;
                }
            }
            else
            {
                addr = ctx.LocCnt + size + (ctx.Bank << 13);

                if (addr > ctx.RomLimit)
                {
                    FatalError("ROM overflow!");
                    return;
                }

                /* copy the buffer */
                if (ctx.Pass == PassFlag.LAST_PASS)
                {
                    var bank = ctx.Bank;
                    var locCnt = ctx.LocCnt;
                    var page = ctx.Page;
                    if (data != null)
                    {
                        for (var i = 0; i < size; i++)
                        {
                            ctx.Rom[bank, locCnt] = data[i];
                            ctx.Map[bank, locCnt] = (byte)(ctx.Section + (page << 5));
                            locCnt++;
                            if (locCnt == 0x2000)
                            {
                                locCnt = 0;
                                bank++;
                                page = (page + 1) & 0x07;
                                if (page == 0) page = 4; // reset page: 8000-9FFF
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < size; i++)
                        {
                            ctx.Rom[bank, locCnt] = 0;
                            ctx.Map[bank, locCnt] = (byte)(ctx.Section + (page << 5));
                            locCnt++;
                            if (locCnt == 0x2000)
                            {
                                locCnt = 0;
                                bank++;
                                page = (page + 1) & 0x07;
                                if (page == 0) page = 4; // reset page: 8000-9FFF
                            }
                        }
                    }
                }
            }

            /* update the location counter */
            var overcnt = (ctx.LocCnt + size) / 0x2000;
            for (var i = 0; i < overcnt; i++)
            {
                ctx.Page = (ctx.Page + 1) & 0x07;
                if (ctx.Page == 0) ctx.Page = 4; // reset page: 8000-9FFF
            }
            ctx.Bank += (ctx.LocCnt + size) >> 13;
            ctx.LocCnt = (ctx.LocCnt + size) & 0x1FFF;

            /* update rom size */
            if (ctx.Bank < Definition.RESERVED_BANK)
            {
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
            }
        }

        /// <summary>
        /// write_srec()
        /// </summary>
        /// <param name="file"></param>
        /// <param name="ext"></param>
        /// <param name="base_addr"></param>
        public void WriteSrec(string file, string ext, int base_addr)
        {
            byte data, chksum;
            int addr, dump, cnt, pos, i, j;
            StreamWriter fp;

            /* status message */
            if (ext == "mx")
            {
                opt.StdOut.Write(@"writing mx file... ");
            }
            else
            {
                opt.StdOut.Write(@"writing s-record file... ");
            }

            /* flush output */
            opt.StdOut.Flush();

            /* add the file extension */
            var fname = file + "." + ext;

            /* open the file */
            try
            {
                using (fp = new StreamWriter(fname, false, opt.Encoding))
                {
                    /* dump the rom */
                    dump = 0;
                    cnt = 0;
                    pos = 0;

                    for (i = 0; i <= ctx.MaxBank; i++)
                    {
                        for (j = 0; j < 0x2000; j++)
                        {
                            if (ctx.Map[i, j] != 0xFF)
                            {
                                /* data byte */
                                if (cnt == 0)
                                {
                                    pos = j;
                                }
                                cnt++;
                                if (cnt == 32)
                                {
                                    dump = 1;
                                }
                            }
                            else
                            {
                                /* free byte */
                                if (cnt != 0)
                                {
                                    dump = 1;
                                }
                            }

                            if (j == 0x1FFF)
                            {
                                if (cnt != 0)
                                {
                                    dump = 1;
                                }
                            }

                            /* dump */
                            if (dump != 0)
                            {
                                dump = 0;
                                addr = base_addr + (i << 13) + pos;
                                chksum = (byte)(cnt + ((addr >> 16) & 0xFF) + ((addr >> 8) & 0xFF) + ((addr) & 0xFF) + 4);

                                /* number, address */
                                fp.Write($"S2{(cnt + 4).ToString("X2")}{addr.ToString("X6")}");

                                /* code */
                                while (cnt != 0)
                                {
                                    data = ctx.Rom[i, pos++];
                                    chksum += data;
                                    fp.Write($"{data.ToString("X2")}");
                                    cnt--;
                                }

                                /* chksum */
                                fp.WriteLine($"{((~chksum) & 0xFF).ToString("X2")}");
                            }
                        }
                    }
                    /* starting address */
                    addr = ((ctx.Map[0, 0] >> 5) << 13);
                    chksum = (byte)(((addr >> 8) & 0xFF) + (addr & 0xFF) + 4);
                    fp.Write($"S804{addr.ToString("X6")}{((~chksum) & 0xFF).ToString("X2")}");
                }

            }
            catch (Exception e)
            {
                opt.StdOut.WriteLine("can not open file '{0}'!", fname);
                return;
            }

            /* ok */
            opt.StdOut.WriteLine("OK");
        }

        /// <summary>
        /// stop compilation
        /// </summary>
        /// <param name="str"></param>
        public void FatalError(string str)
        {
            Error(str);
            ctx.StopPass = true;
        }

        /// <summary>
        /// error printing routine
        /// </summary>
        /// <param name="str"></param>
        public void Error(string str)
        {
            Warning(str);
            ctx.ErrCnt++;
        }

        /// <summary>
        /// warning printing routine
        /// </summary>
        /// <param name="str"></param>
        public void Warning(string str)
        {
            int i, temp;

            /* put the source line number into prlnbuf */
            i = 4;
            temp = ctx.SrcLineNum;
            while (temp != 0)
            {
                ctx.PrLnBuf[i--] = (char)(temp % 10 + '0');
                temp /= 10;
            }

            /* update the current file name */
            if (ctx.InFileError != ctx.InFileNum)
            {
                ctx.InFileError = ctx.InFileNum;
                opt.StdOut.WriteLine($"#[{ctx.InFileNum}]   {ctx.InputFile[ctx.InFileNum].Name}");
            }

            /* output the line and the error message */
            LoadLc(ctx.LocCnt, 0);
            opt.StdOut.WriteLine(ctx.PrLnBuf.ToStringFromNullTerminated());
            opt.StdOut.WriteLine("       {0}", str);
        }
    }
}
