using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NesAsmSharp.Assembler.Processors;
using NesAsmSharp.Assembler.Util;

namespace NesAsmSharp.Assembler
{
    public class NesAssembler : IAssembler
    {
        private NesAsmContext ctx;
        private NesAsmOption opt;

        public NesAsmContext Context
        {
            get
            {
                return ctx;
            }
        }

        public NesAsmOption Option
        {
            get
            {
                return opt;
            }
        }

        public NesAssembler(NesAsmOption opt)
        {
            this.opt = opt;
            ctx = new NesAsmContext();
            ctx.Option = opt;
            var nesPr = ctx.GetProcessor<NesMachineProcessor>();
            ctx.Machine = nesPr.NesMachine;
        }

        /// <summary>
        /// Run assemble
        /// </summary>
        /// <returns>0 - success</returns>
        public int Assemble()
        {
            var result = 0;

            result = Prepare();
            if (result != 0) return result;

            result = RunPass();
            if (result == 0)
            {
                result = Output();
            }

            Cleanup();

            return result;
        }

        /// <summary>
        /// Prepare phase
        /// </summary>
        /// <returns>0 - success</returns>
        private int Prepare()
        {
            var inPr = ctx.GetProcessor<InputProcessor>();
            /* open the input file */
            if (inPr.OpenInputFile(opt.InFName) != 0)
            {
                opt.StdOut.WriteLine("Can not open input file '{0}'!", opt.InFName);
                return 1;
            }

            var baseDir = Path.GetDirectoryName(opt.InFName);
            var baseFname = Path.GetFileNameWithoutExtension(opt.InFName);
            if (baseDir != "") baseFname = baseDir + "\\" + baseFname;

            if (string.IsNullOrEmpty(opt.LstFName))
            {
                // set default lst name
                opt.LstFName = baseFname + ".lst";
            }

            if (string.IsNullOrEmpty(opt.BinFName))
            {
                // set default bin name
                opt.BinFName = baseFname + ctx.Machine.RomExt;
            }

            if (string.IsNullOrEmpty(opt.OutFName))
            {
                // set default out name
                opt.OutFName = baseFname;
            }

            var asmPr = ctx.GetProcessor<AssembleProcessor>();
            var codePr = ctx.GetProcessor<CodeProcessor>();
            var cmdPr = ctx.GetProcessor<CommandProcessor>();

            /* fill the instruction hash table */
            asmPr.AddInst(codePr.BaseInst);
            asmPr.AddInst(cmdPr.BasePseudo);

            /* add machine specific instructions and pseudos */
            asmPr.AddInst(ctx.Machine.Inst);
            asmPr.AddInst(ctx.Machine.PseudoInst);

            var symPr = ctx.GetProcessor<SymbolProcessor>();
            /* predefined symbols */
            symPr.LablSet("MAGICKIT", 1);
            symPr.LablSet("DEVELO", 0);
            symPr.LablSet("CDROM", 0);
            symPr.LablSet("_bss_end", 0);
            symPr.LablSet("_bank_base", 0);
            symPr.LablSet("_nb_bank", 1);
            symPr.LablSet("_call_bank", 0);

            /* init global variables */
            ctx.MaxZP = 0x01;
            ctx.MaxBSS = 0x0201;
            ctx.MaxBank = 0;
            ctx.RomLimit = 0x100000;       /* 1MB */
            ctx.BankLimit = 0x7F;
            ctx.BankBase = 0;
            ctx.ErrCnt = 0;

            return 0;
        }

        public int RunPass()
        {
            var inPr = ctx.GetProcessor<InputProcessor>();
            var outPr = ctx.GetProcessor<OutputProcessor>();
            var procPr = ctx.GetProcessor<ProcProcessor>();
            var symPr = ctx.GetProcessor<SymbolProcessor>();
            var asmPr = ctx.GetProcessor<AssembleProcessor>();

            PassFlag pass;
            int ram_bank;

            /* assemble */
            for (pass = PassFlag.FIRST_PASS; pass <= PassFlag.LAST_PASS; pass++)
            {
                ctx.Pass = pass;
                ctx.InFileError = -1;
                ctx.Page = 7;
                ctx.Bank = 0;
                ctx.LocCnt = 0;
                ctx.SrcLineNum = 0;
                ctx.MacroCounter = 0;
                ctx.MacroCntMax = 0;
                opt.XListOpt = false;
                ctx.GLablPtr = null;
                ctx.SkipLines = false;
                ctx.RSBase = 0;
                ctx.ProcNb = 0;

                /* reset assembler options */
                opt.AsmOpt[AssemblerOption.OPT_LIST] = false;
                opt.AsmOpt[AssemblerOption.OPT_MACRO] = opt.MListOpt;
                opt.AsmOpt[AssemblerOption.OPT_WARNING] = false;
                opt.AsmOpt[AssemblerOption.OPT_OPTIMIZE] = false;

                /* reset bank arrays */
                Array.Clear(ctx.BankLocCnt, 0, ctx.BankLocCnt.Length);
                Array.Clear(ctx.BankGLabl, 0, ctx.BankGLabl.Length);
                Array.Clear(ctx.BankPage, 0, ctx.BankPage.Length);

                /* reset sections */
                ram_bank = (int)ctx.Machine.RamBank;
                ctx.Section = SectionType.S_CODE;

                /* .zp */
                ctx.SectionBank[(int)SectionType.S_ZP] = ram_bank;
                ctx.BankPage[(int)SectionType.S_ZP, ram_bank] = (int)ctx.Machine.RamPage;
                ctx.BankLocCnt[(int)SectionType.S_ZP, ram_bank] = 0x0000;

                /* .bss */
                ctx.SectionBank[(int)SectionType.S_BSS] = ram_bank;
                ctx.BankPage[(int)SectionType.S_BSS, ram_bank] = (int)ctx.Machine.RamPage;
                ctx.BankLocCnt[(int)SectionType.S_BSS, ram_bank] = 0x0200;

                /* .code */
                ctx.SectionBank[(int)SectionType.S_CODE] = 0x00;
                ctx.BankPage[(int)SectionType.S_CODE, 0x00] = 0x07;
                ctx.BankLocCnt[(int)SectionType.S_CODE, 0x00] = 0x0000;

                /* .data */
                ctx.SectionBank[(int)SectionType.S_DATA] = 0x00;
                ctx.BankPage[(int)SectionType.S_DATA, 0x00] = 0x07;
                ctx.BankLocCnt[(int)SectionType.S_DATA, 0x00] = 0x0000;

                /* pass message */
                opt.StdOut.WriteLine("pass {0}", (int)(pass + 1));

                /* assemble */
                while (inPr.ReadLine() != -1)
                {
                    asmPr.Assemble();
                    if (ctx.LocCnt > 0x2000)
                    {
                        if (ctx.ProcPtr == null)
                        {
                            outPr.FatalError("Bank overflow, offset > $1FFF!");
                        }
                        else
                        {
                            var msg = $"Proc : '{ctx.ProcPtr.Name}' is too large (code > 8KB)!";
                            outPr.FatalError(msg);
                        }
                        break;
                    }
                    if (ctx.StopPass) break;
                }

                /* relocate procs */
                if (pass == PassFlag.FIRST_PASS)
                {
                    procPr.ProcReloc();
                }

                /* abord pass on errors */
                if (ctx.ErrCnt != 0)
                {
                    opt.StdOut.WriteLine("# {0} error(s)\n", ctx.ErrCnt);
                    break;
                }

                /* adjust bank base */
                if (pass == PassFlag.FIRST_PASS)
                {
                    ctx.BankBase = CalcBankBase();
                }

                /* update predefined symbols */
                if (pass == PassFlag.FIRST_PASS)
                {
                    symPr.LablSet("_bss_end", (int)ctx.Machine.RamBase + ctx.MaxBSS);
                    symPr.LablSet("_bank_base", ctx.BankBase);
                    symPr.LablSet("_nb_bank", ctx.MaxBank + 1);
                }

                /* adjust the symbol table for the develo or for cd-roms */
                if (pass == PassFlag.FIRST_PASS)
                {
                    if (opt.DeveloOpt || opt.MxOpt || opt.CdOpt || opt.ScdOpt)
                    {
                        symPr.LablRemap();
                    }
                }

                /* rewind input file */
                ctx.InFp.BaseStream.Seek(0, SeekOrigin.Begin);

                /* open the listing file */
                if (pass == PassFlag.FIRST_PASS)
                {
                    if (opt.XListOpt && opt.ListLevel > 0)
                    {
                        try
                        {
                            ctx.LstFp = new StreamWriter(opt.LstFName, false, opt.Encoding);
                        }
                        catch (Exception e)
                        {
                            opt.StdOut.WriteLine("Can not open listing file '{0}'!", opt.LstFName);
                            Environment.Exit(1);
                        }
                        ctx.LstFp.WriteLine("#[1]   {0}", ctx.InputFile[1].Name);
                    }
                }
            }

            return ctx.ErrCnt;
        }

        private int Output()
        {
            var outpr = ctx.GetProcessor<OutputProcessor>();

            FileStream fsBin = null;
            FileStream fsIpl = null;

            /* rom */
            /* cd-rom */
            if (opt.CdOpt || opt.ScdOpt)
            {
                /* open output file */
                try
                {
                    fsBin = new FileStream(opt.BinFName, FileMode.Create, FileAccess.Write);
                }
                catch (Exception e)
                {
                    opt.StdOut.WriteLine("Can not open output file '{0}'!", opt.BinFName);
                    return 1;
                }

                /* boot code */
                if (opt.HeaderOpt)
                {
                    /* open ipl binary file */
                    try
                    {
                        fsIpl = new FileStream("boot.bin", FileMode.Open, FileAccess.Read);
                    }
                    catch (Exception e)
                    {
                        opt.StdOut.WriteLine("Can not find CD boot file 'boot.bin'!");
                        return 1;
                    }

                    /* load ipl */
                    var iplBuffer = new byte[4096];
                    using (fsIpl)
                    {
                        try
                        {
                            fsIpl.Read(iplBuffer, 0, iplBuffer.Length);
                        }
                        catch (Exception e)
                        {
                            opt.StdOut.WriteLine("CD boot file 'boot.bin' read error!");
                            return 1;
                        }
                    }

                    Array.Clear(iplBuffer, 0x800, 32);

                    /* prg sector base */
                    iplBuffer[0x802] = 2;
                    /* nb sectors */
                    iplBuffer[0x803] = 4;
                    /* loading address */
                    iplBuffer[0x804] = 0x00;
                    iplBuffer[0x805] = 0x40;
                    /* starting address */
                    iplBuffer[0x806] = 0x10;
                    iplBuffer[0x807] = 0x40;
                    /* mpr registers */
                    iplBuffer[0x808] = 0x00;
                    iplBuffer[0x809] = 0x01;
                    iplBuffer[0x80A] = 0x02;
                    iplBuffer[0x80B] = 0x03;
                    iplBuffer[0x80C] = 0x04;
                    /* load mode */
                    iplBuffer[0x80D] = 0x60;

                    /* write boot code */
                    try
                    {
                        fsBin.Write(iplBuffer, 0, 4096);
                    }
                    catch (Exception e)
                    {
                        opt.StdOut.WriteLine("output file '{0}' write error!", opt.BinFName);
                        fsBin.Close();
                        return 1;
                    }
                }

                /* write rom */
                var rom = ctx.Rom.ToByteArray();
                using (fsBin)
                {
                    try
                    {
                        fsBin.Write(rom, 0, 8192 * (ctx.MaxBank + 1));
                    }
                    catch (Exception e)
                    {
                        opt.StdOut.WriteLine("output file '{0}' write error!", opt.BinFName);
                        return 1;
                    }
                }
            }
            /* develo box */
            else if (opt.DeveloOpt || opt.MxOpt)
            {
                ctx.Page = (ctx.Map[0, 0] >> 5);

                /* save mx file */
                if ((ctx.Page + ctx.MaxBank) < 7)
                {
                    /* old format */
                    outpr.WriteSrec(opt.OutFName, "mx", ctx.Page << 13);
                }
                else
                {
                    /* new format */
                    outpr.WriteSrec(opt.OutFName, "mx", 0xD0000);
                }

                /* execute */
                if (opt.DeveloOpt)
                {
                    var cmd = $"perun {opt.OutFName}";
                    var p = System.Diagnostics.Process.Start(cmd);
                    p.WaitForExit();
                }
            }
            /* save */
            else
            {
                /* s-record file */
                if (opt.SrecOpt)
                {
                    outpr.WriteSrec(opt.OutFName, "s28", 0);
                }
                /* binary file */
                else
                {
                    /* open file */
                    try
                    {
                        fsBin = new FileStream(opt.BinFName, FileMode.Create, FileAccess.Write);
                    }
                    catch (Exception e)
                    {
                        opt.StdOut.WriteLine("Can not open binary file '{0}'!\n", opt.BinFName);
                        return 1;
                    }

                    /* write header */
                    if (opt.HeaderOpt)
                    {
                        try
                        {
                            ctx.Machine.WriteHeader(fsBin, ctx.MaxBank + 1);
                        }
                        catch (Exception e)
                        {
                            opt.StdOut.WriteLine("output file '{0}' write error!", opt.BinFName);
                            fsBin.Close();
                            return 1;
                        }
                    }

                    /* write rom */
                    var rom = ctx.Rom.ToByteArray();
                    using (fsBin)
                    {
                        try
                        {
                            fsBin.Write(rom, 0, 8192 * (ctx.MaxBank + 1));
                        }
                        catch (Exception e)
                        {
                            opt.StdOut.WriteLine("output file '{0}' write error!", opt.BinFName);
                            return 1;
                        }
                    }
                }
            }

            return 0;
        }

        private void Cleanup()
        {
            /* close listing file */
            ctx.LstFp?.Close();
            ctx.LstFp = null;

            /* close input file */
            ctx.InFp?.Close();
            ctx.InFp = null;

            /* dump the bank table */
            if (opt.DumpSeg > 0)
            {
                showSegUsage();
            }
        }

        /// <summary>
        /// calculate rom bank base
        /// </summary>
        /// <returns></returns>
        private int CalcBankBase()
        {
            int bank_base;

            /* cd */
            if (opt.CdOpt)
            {
                bank_base = 0x80;
            }
            /* super cd */
            else if (opt.ScdOpt)
            {
                bank_base = 0x68;
            }
            /* develo */
            else if (opt.DeveloOpt || opt.MxOpt)
            {
                if (ctx.MaxBank < 4)
                {
                    bank_base = 0x84;
                }
                else
                {
                    bank_base = 0x68;
                }
            }
            /* default */
            else
            {
                bank_base = 0;
            }

            return bank_base;
        }

        /// <summary>
        /// show_seg_usage()
        /// </summary>
        public void showSegUsage()
        {
            int i, j;
            int addr, start, stop, nb;
            int rom_used;
            int rom_free;
            int ram_base = (int)ctx.Machine.RamBase;

            opt.StdOut.WriteLine("segment usage:\n");

            /* zp usage */
            if (ctx.MaxZP <= 1)
            {
                opt.StdOut.WriteLine("      ZP    -");
            }
            else
            {
                start = ram_base;
                stop = ram_base + (ctx.MaxZP - 1);
                opt.StdOut.WriteLine("      ZP    ${0:X4}-${1:X4}  [{2,4}]", start, stop, stop - start + 1);
            }

            /* bss usage */
            if (ctx.MaxBSS <= 0x201)
            {
                opt.StdOut.WriteLine("     BSS    -");
            }
            else
            {
                start = ram_base + 0x200;
                stop = ram_base + (ctx.MaxBSS - 1);
                opt.StdOut.WriteLine("     BSS    ${0:X4}-${1:X4}  [{2,4}]", start, stop, stop - start + 1);
            }

            /* bank usage */
            rom_used = 0;
            rom_free = 0;

            if (ctx.MaxBank != 0)
            {
                opt.StdOut.WriteLine("\t\t\t\t    USED/FREE");
            }

            /* scan banks */
            for (i = 0; i <= ctx.MaxBank; i++)
            {
                start = 0;
                addr = 0;
                nb = 0;

                /* count used and free bytes */
                for (j = 0; j < 8192; j++)
                {
                    if (ctx.Map[i, j] != 0xFF) nb++;
                }

                /* display bank infos */
                if (nb != 0)
                {
                    opt.StdOut.WriteLine("BANK{0,4}    {1,20}    {2,4}/{3,4}", i, ctx.BankName[i], nb, 8192 - nb);
                }
                else
                {
                    opt.StdOut.WriteLine("BANK{0,4}    {1,20}       0/8192", i, ctx.BankName[i]);
                    continue;
                }

                /* update used/free counters */
                rom_used += nb;
                rom_free += 8192 - nb;

                /* scan */
                if (opt.DumpSeg == 1) continue;

                for (;;)
                {
                    /* search section start */
                    for (; addr < 8192; addr++)
                    {
                        if (ctx.Map[i, addr] != 0xFF) break;
                    }

                    /* check for end of bank */
                    if (addr > 8191) break;

                    /* get section type */
                    ctx.Section = (SectionType)(ctx.Map[i, addr] & 0x0F);
                    ctx.Page = (ctx.Map[i, addr] & 0xE0) << 8;
                    start = addr;

                    /* search section end */
                    for (; addr < 8192; addr++)
                    {
                        if ((ctx.Map[i, addr] & 0x0F) != (byte)ctx.Section) break;
                    }

                    /* display section infos */
                    opt.StdOut.WriteLine("    {0}    ${1:X4}-${2:X4}  [{3,4}]",
                            Definition.SectionName[(int)ctx.Section],  /* section name */
                            start + ctx.Page,           /* starting address */
                            addr + ctx.Page - 1,        /* end address */
                            addr - start);              /* size */
                }
            }

            /* total */
            rom_used = (rom_used + 1023) >> 10;
            rom_free = (rom_free) >> 10;
            opt.StdOut.WriteLine("\t\t\t\t    ---- ----");
            opt.StdOut.WriteLine("\t\t\t\t    {0,4}K{0,4}K", rom_used, rom_free);
        }
    }
}
