using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class NesMachineProcessor : ProcessorBase
    {
        /* locals */
        private byte inesPrg;        /* number of prg banks */
        private byte inesChr;        /* number of character banks */
        private byte[] inesMapper = new byte[2];  /* rom mapper type */
        private NesHeader header;

        public NesMachineProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        /// <summary>
        /// generate and write rom header
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="banks"></param>
        public void WriteNesHeader(FileStream fs, int banks)
        {
            /* setup INES header */
            header = new NesHeader(inesPrg, inesChr, inesMapper);

            var array = header.ToByteArray();
            /* write */
            fs.Write(array, 0, array.Length);
        }

        /// <summary>
        /// encode a 8x8 tile for the NES
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="data"></param>
        /// <param name="line_offset"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public int PackNes8x8Tile(byte[] buffer, IArrayPointer<byte> data, int line_offset, TileFormat format)
        {
            int i, j;
            uint pixel;

            /* pack the tile only in the last pass */
            if (ctx.Pass != PassFlag.LAST_PASS) return 16;

            /* clear buffer */
            Array.Clear(buffer, 0, 16);

            var cnt = 0;
            var err = 0;
            /* encode the tile */
            switch (format)
            {
            case TileFormat.CHUNKY_TILE:
                /* 8-bit chunky format */
                cnt = 0;
                var ptr = data;

                for (i = 0; i < 8; i++)
                {
                    for (j = 0; j < 8; j++)
                    {
                        pixel = ptr[j ^ 0x07];
                        buffer[cnt] |= (byte)((pixel & 0x01) != 0 ? (1 << j) : 0);
                        buffer[cnt + 8] |= (byte)((pixel & 0x02) != 0 ? (1 << j) : 0);
                    }
                    ptr.Forward(line_offset);
                    cnt += 1;
                }
                break;
            case TileFormat.PACKED_TILE:
                /* 4-bit packed format */
                var packed = data;

                for (i = 0; i < 8; i++)
                {
                    pixel = packed[i];

                    for (j = 0; j < 8; j++)
                    {
                        /* check for errors */
                        if ((pixel & 0x0C) != 0)
                        {
                            err++;
                        }

                        /* convert the tile */
                        buffer[cnt] |= (byte)((pixel & 0x01) != 0 ? (1 << j) : 0);
                        buffer[cnt + 8] |= (byte)((pixel & 0x02) != 0 ? (1 << j) : 0);
                        pixel >>= 4;
                    }
                    cnt += 1;
                }
                /* error message */
                if (err > 0)
                {
                    outPr.Error("Incorrect pixel color index!");
                }
                break;
            default:
                /* other formats not supported */
                outPr.Error("Internal error: unsupported format passed to 'pack_8x8_tile'!");
                break;
            }
            /* ok */
            return 16;
        }

        /// <summary>
        /// .defchr pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoNesDefchr(ref int ip)
        {
            byte[] buffer = new byte[16];
            uint[] data = new uint[8];
            int size;
            int i;

            /* define label */
            symPr.LablDef(ctx.LocCnt, 1);

            /* output infos */
            ctx.DataLocCnt = ctx.LocCnt;
            ctx.DataSize = 3;
            ctx.DataLevel = 3;

            /* get tile data */
            for (i = 0; i < 8; i++)
            {
                /* get value */
                if (exprPr.Evaluate(ref ip, (i < 7) ? ',' : ';') == 0) return;

                /* store value */
                data[i] = ctx.Value;
            }

            /* encode tile */
            size = PackNes8x8Tile(buffer, new ArrayPointer<byte>(data.ToByteArray()), 0, TileFormat.PACKED_TILE);

            /* store tile */
            outPr.PutBuffer(buffer, size);

            /* output line */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .inesprg pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoNesInesPrg(ref int ip)
        {
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            if ((ctx.Value < 0) || (ctx.Value > 64))
            {
                outPr.Error("Prg bank value out of range!");
                return;
            }

            inesPrg = (byte)ctx.Value;

            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .ineschr pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoNesInesChr(ref int ip)
        {
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            if (((int)ctx.Value < 0) || ((int)ctx.Value > 64))
            {
                outPr.Error("Prg bank value out of range!");
                return;
            }

            inesChr = (byte)ctx.Value;

            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .inesmap pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoNesInesMap(ref int ip)
        {
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            if ((ctx.Value < 0) || (ctx.Value > 255))
            {
                outPr.Error("Mapper value out of range!");
                return;
            }

            inesMapper[0] &= 0x0F;
            inesMapper[0] |= (byte)((ctx.Value & 0x0F) << 4);
            inesMapper[1] = (byte)(ctx.Value & 0xF0);

            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .ines.mirror pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoNesInesMir(ref int ip)
        {
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            if ((ctx.Value < 0) || (ctx.Value > 15))
            {
                outPr.Error("Mirror value out of range!");

                return;
            }

            inesMapper[0] &= 0xF0;
            inesMapper[0] |= (byte)(ctx.Value & 0x0F);

            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// auto zeropage mode pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoNesAutoZP(ref int ip)
        {
            if (exprPr.Evaluate(ref ip, ';') == 0) return;

            if ((ctx.Value < 0) || (ctx.Value > 1))
            {
                outPr.Error("autozp value out of range!  0=off/1=on");

                return;
            }

            opt.AutoZPOpt = (ctx.Value == 1);

            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /* NES specific pseudos */
        private NesAsmOpecode[] nesPseudo;
        public NesAsmOpecode[] NesPseudo
        {
            get
            {
                if (nesPseudo == null)
                {
                    nesPseudo = new NesAsmOpecode[]
                    {
                        new NesAsmOpecode("DEFCHR",   DoNesDefchr,  OpCodeFlag.PSEUDO, AsmDirective.P_DEFCHR,  0),
                        new NesAsmOpecode("INESPRG",  DoNesInesPrg, OpCodeFlag.PSEUDO, AsmDirective.P_INESPRG, 0),
                        new NesAsmOpecode("INESCHR",  DoNesInesChr, OpCodeFlag.PSEUDO, AsmDirective.P_INESCHR, 0),
                        new NesAsmOpecode("INESMAP",  DoNesInesMap, OpCodeFlag.PSEUDO, AsmDirective.P_INESMAP, 0),
                        new NesAsmOpecode("INESMIR",  DoNesInesMir, OpCodeFlag.PSEUDO, AsmDirective.P_INESMIR, 0),
                        new NesAsmOpecode("AUTOZP",   DoNesAutoZP,  OpCodeFlag.PSEUDO, AsmDirective.P_AUTOZP,  0),
                        new NesAsmOpecode(".DEFCHR",  DoNesDefchr,  OpCodeFlag.PSEUDO, AsmDirective.P_DEFCHR,  0),
                        new NesAsmOpecode(".INESPRG", DoNesInesPrg, OpCodeFlag.PSEUDO, AsmDirective.P_INESPRG, 0),
                        new NesAsmOpecode(".INESCHR", DoNesInesChr, OpCodeFlag.PSEUDO, AsmDirective.P_INESCHR, 0),
                        new NesAsmOpecode(".INESMAP", DoNesInesMap, OpCodeFlag.PSEUDO, AsmDirective.P_INESMAP, 0),
                        new NesAsmOpecode(".INESMIR", DoNesInesMir, OpCodeFlag.PSEUDO, AsmDirective.P_INESMIR, 0),
                        new NesAsmOpecode(".AUTOZP",  DoNesAutoZP,  OpCodeFlag.PSEUDO, AsmDirective.P_AUTOZP,  0)
                    };
                }
                return nesPseudo;
            }
        }

        private NesAsmMachine nesMachine;
        public NesAsmMachine NesMachine
        {
            get
            {
                if (nesMachine == null)
                {
                    nesMachine = new NesAsmMachine
                    {
                        Type = MachineType.MACHINE_NES,
                        AsmName = "NesAsmSharp",
                        AsmTitle = "NES Assembler based on v2.51+autozp beta3",
                        RomExt = ".nes",
                        IncludeEnv = "NES_INCLUDE",
                        ZPLimit = 0x100,
                        RamLimit = 0x800,
                        RamBase = 0,
                        RamPage = 0,
                        RamBank = (uint)Definition.RESERVED_BANK,
                        Inst = null,
                        PseudoInst = NesPseudo,
                        Pack8x8Tile = PackNes8x8Tile,
                        Pack16x16Tile = null,
                        Pack16x16Sprite = null,
                        WriteHeader = WriteNesHeader
                    };
                }
                return nesMachine;
            }
        }
    }

    public class NesHeader
    {
        /* INES rom header */
        public byte[] Id { get; private set; }
        public byte Prg { get; set; }
        public byte Chr { get; set; }
        public byte[] Mapper { get; private set; }
        public byte[] Unused { get; private set; }

        public NesHeader()
        {
            Id = new byte[4] { (byte)'N', (byte)'E', (byte)'S', 26 };
            Mapper = new byte[2];
            Unused = new byte[8];
        }

        public NesHeader(byte prg, byte chr, byte[] mapper) : this()
        {
            Prg = prg;
            Chr = chr;
            Mapper[0] = mapper[0];
            Mapper[1] = mapper[1];
        }

        public byte[] ToByteArray()
        {
            var array = new byte[16]
            {
                 Id[0],  Id[1],  Id[2], Id[3], Prg, Chr, Mapper[0], Mapper[1],
                 0, 0, 0, 0, 0, 0, 0, 0
            };

            return array;
        }
    }
}
