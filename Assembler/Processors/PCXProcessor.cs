using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class PCXProcessor : ProcessorBase
    {
        public PCXProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        public ushort GET_SHORT(byte[] a)
        {
            return (ushort)((a[1] << 8) + a[0]);
        }

        /// <summary>
        /// pcx_pack_8x8_tile()
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int PackPcx8x8Tile(byte[] buffer, int x, int y)
        {
            /* tile address */
            var ptr = new ArrayPointer<byte>(ctx.PcxBuf, x + (y * ctx.PcxW));

            /* encode the tile */
            return (ctx.Machine.Pack8x8Tile(buffer, ptr, ctx.PcxW, TileFormat.CHUNKY_TILE));

        }

        /// <summary>
        /// pcx_pack_16x16_tile()
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int PackPcx16x16Tile(byte[] buffer, int x, int y)
        {
            // tile address
            var ptr = new ArrayPointer<byte>(ctx.PcxBuf, x + (y * ctx.PcxW));

            // encode the tile
            return (ctx.Machine.Pack16x16Tile(buffer, ptr, ctx.PcxW, TileFormat.CHUNKY_TILE));
        }

        /// <summary>
        /// pcx_pack_16x16_sprite()
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int PackPcx16x16Sprite(byte[] buffer, int x, int y)
        {
            // sprite address
            var ptr = new ArrayPointer<byte>(ctx.PcxBuf, x + (y * ctx.PcxW));

            // encode the sprite
            return (ctx.Machine.Pack16x16Sprite(buffer, ptr, ctx.PcxW, TileFormat.CHUNKY_TILE));
        }

        /// <summary>
        /// pcx_set_tile()
        /// </summary>
        /// <param name="symref"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public int SetPcxTile(NesAsmSymbol symref, uint offset)
        {
            int i;
            int hash;
            int size, start;
            uint crc;
            int nb;

            // do nothing in first passes
            if (ctx.Pass != PassFlag.LAST_PASS) return (1);

            // same tile set?
            if (symref == null) return (1);
            if ((symref == ctx.TileLablPtr) && (offset == ctx.TileOffset)) return (1);

            // check symbol
            if (symref.Nb == 0)
            {
                if ((symref.Type == SymbolFlag.IFUNDEF) || (symref.Type == SymbolFlag.UNDEF))
                {
                    outPr.Error("Tile table undefined!");
                }
                else
                {
                    outPr.Error("Incorrect tile table reference!");
                }
                // no tile table
                ctx.TileLablPtr = null;
                return (1);
            }
            if (symref.Size == 0)
            {
                outPr.Error("Tile table has not been compiled yet!");
                ctx.TileLablPtr = null;
                return (1);
            }

            // adjust offset
            start = (int)(offset - symref.Value);

            if ((start < 0)) goto err;
            if ((start % symref.Size) != 0) goto err;
            if ((start / symref.Size) >= symref.Nb) goto err;

            // reset tile hash table
            for (i = 0; i < ctx.TileTbl.Length; i++)
            {
                ctx.TileTbl[i] = null;
            }

            // get infos
            nb = symref.Nb - (start / symref.Size);
            size = symref.Size;
            var data = new Rank2ArrayPointer<byte>(ctx.Rom, symref.Bank - ctx.BankBase, symref.Value & 0x1FFF);
            data += start;

            // 256 tiles max
            if (nb > 256) nb = 256;

            // parse tiles
            for (i = 0; i < nb; i++)
            {
                // calculate tile crc
                crc = crcPr.CalcCRC(data, size);
                hash = (int)(crc & 0xFF);

                // insert the tile in the tile table
                ctx.Tile[i].Next = ctx.TileTbl[hash];
                ctx.Tile[i].Index = i;
                ctx.Tile[i].Data = data;
                ctx.Tile[i].Crc = crc;
                ctx.TileTbl[hash] = ctx.Tile[i];

                // next
                data += size;
            }

            // ok
            ctx.TileLablPtr = symref;
            ctx.TileOffset = offset;
            return (1);

        // error
        err:
            ctx.TileLablPtr = null;

            outPr.Error("Incorrect tile table reference!");
            return (1);
        }


        /* ----
         * pcx_search_tile()
         * ----
         */

        public int SearchPcxTile(IArrayPointer<byte> data, int size)
        {
            NesAsmTile tile;
            uint crc;
            int i;

            /* do nothing in first passes */
            if (ctx.Pass != PassFlag.LAST_PASS) return (0);

            /* quick check */
            if (ctx.TileLablPtr == null) return (-1);
            if (ctx.TileLablPtr.Size != size) return (-1);

            /* calculate tile crc */
            crc = crcPr.CalcCRC(data, size);
            tile = ctx.TileTbl[crc & 0xFF];

            /* search tile */
            while (tile != null)
            {
                if (tile.Crc == crc)
                {
                    for (i = 0; i < size; i++)
                    {
                        if (tile.Data[i] != data[i]) break;
                    }
                    if (i == size) return (tile.Index);
                }
                tile = tile.Next;
            }

            /* not found */
            return (-1);
        }


        /* ----
         * pcx_get_args()
         * ----
         * get arguments in pcx pseudo instructions (.incchr/spr/tile/pal/bat)
         */

        public int GetPcxArgs(ref int ip)
        {
            string name;
            char c;

            /* get pcx file name */
            if (codePr.GetString(ref ip, out name, 127) == 0) return (0);

            /* reset args counter */
            ctx.PcxNbArgs = 0;

            /* get args */
            for (;;)
            {
                /* skip spaces */
                while (CharUtil.IsSpace(c = ctx.PrLnBuf[ip++])) ;

                /* check syntax */
                if ((c != ',') && (c != ';') && (c != 0))
                {
                    outPr.Error("Syntax error!");
                    return (0);
                }

                if (c != ',') break;

                /* get arg */
                if (exprPr.Evaluate(ref ip, (char)0) == 0) return (0);

                /* store arg */
                ctx.PcxArg[ctx.PcxNbArgs++] = ctx.Value;

                /* check number of args */
                if (ctx.PcxNbArgs == 7) break;
            }

            /* check number of args */
            if ((ctx.OpType & (1 << ctx.PcxNbArgs)) != 0)
            {
                outPr.Error("Invalid number of arguments!");
                return (0);
            }

            /* load and unpack the pcx */
            if (LoadPcx(name) == 0) return (0);

            /* parse tiles */
            if (ctx.OpVal == AsmDirective.P_INCMAP)
            {
                if (ctx.ExprLablCnt == 0)
                {
                    outPr.Error("No tile table reference!");
                }

                if (ctx.ExprLablCnt > 1)
                {
                    // fixed. null.0
                    ctx.ExprLablCnt = 0;
                    outPr.Error("Too many tile table references!");
                }

                if (SetPcxTile(ctx.ExprLablPtr, ctx.Value) == 0) return (0);
            }

            /* ok */
            return (1);
        }


        /* ----
         * pcx_parse_args()
         * ----
         * parse arguments of pcx pseudo directive
         */

        public int ParsePcxArgs(int i, int nb, out int a, out int b, out int c, out int d, int size)
        {
            int x, y, w, h;

            x = 0;
            y = 0;

            /* get coordinates */
            if (nb == 0)
            {           /* no arg */
                w = (ctx.PcxW / size);
                h = (ctx.PcxH / size);
            }
            else if (nb == 2)
            {       /* 2 args */
                w = (int)ctx.PcxArg[i];
                h = (int)ctx.PcxArg[i + 1];
            }
            else
            {                   /* 4 args */
                x = (int)ctx.PcxArg[i];
                y = (int)ctx.PcxArg[i + 1];
                w = (int)ctx.PcxArg[i + 2];
                h = (int)ctx.PcxArg[i + 3];
            }

            /* check */
            if (((x + w * size) > ctx.PcxW) || ((y + h * size) > ctx.PcxH))
            {
                outPr.Error("Coordinates out of range!");
                a = b = c = d = 0;
                return (0);
            }

            /* write back the value */
            a = x;
            b = y;
            c = w;
            d = h;

            /* ok */
            return (1);
        }


        /* ----
         * pcx_load()
         * ----
         * load a PCX file and unpack it
         */

        public int LoadPcx(string name)
        {
            FileStream fs;

            /* check if the file is the same as the previously loaded one;
             * if this is the case do not reload it
             */
            // fixed. null.0
            if (!string.IsNullOrEmpty(name) && ctx.PcxName.ToLower() == name.ToLower())
            {
                return 1;
            }
            else
            {
                /* no it's a new file - ok let's prepare loading */
                ctx.PcxBuf = null;
                ctx.PcxName = null;
            }

            try
            {
                /* open the file */
                fs = File.OpenRead(name);
            }
            catch (Exception e)
            {
                outPr.Error($"Can not open file '{name}'!");
                return 0;
            }

            using (fs)
            {
                try
                {
                    /* get the picture size */
                    var barray = new byte[128];
                    fs.Read(barray, 0, 128);
                    ctx.Pcx.SetValues(barray);
                    ctx.PcxW = (GET_SHORT(ctx.Pcx.xmax) - GET_SHORT(ctx.Pcx.xmin) + 1);
                    ctx.PcxH = (GET_SHORT(ctx.Pcx.ymax) - GET_SHORT(ctx.Pcx.ymin) + 1);

                    /* adjust picture width */
                    if ((ctx.PcxW & 0x01) != 0) ctx.PcxW++;

                    /* check size range */
                    if ((ctx.PcxW > 1024) || (ctx.PcxH > 768))
                    {
                        outPr.Error("Picture size too big, max. 1024x768!");
                        return (0);
                    }
                    if ((ctx.PcxW < 16) || (ctx.PcxH < 16))
                    {
                        outPr.Error("Picture size too small, min. 16x16!");
                        return (0);
                    }

                    /* malloc a buffer */
                    ctx.PcxBuf = new byte[ctx.PcxW * ctx.PcxH];

                    /* decode the picture */
                    if ((ctx.Pcx.bpp == 8) && (ctx.Pcx.np == 1))
                    {
                        Decode256(fs, ctx.PcxW, ctx.PcxH);
                    }
                    else if ((ctx.Pcx.bpp == 1) && (ctx.Pcx.np <= 4))
                    {
                        Decode16(fs, ctx.PcxW, ctx.PcxH);
                    }
                    else
                    {
                        outPr.Error("Unsupported or invalid PCX format!");
                        return 0;
                    }
                }
                catch(Exception e)
                {
                    outPr.Error($"file '{name}' read error!");
                    return 0;

                }
            }

            ctx.PcxName = name;
            return 1;
        }


        /* ----
         * decode_256()
         * ----
         * decode a 256 colors PCX file
         */

        public void Decode256(FileStream fs, int w, int h)
        {
            uint i, j, x, y;
            int c;
            int ptr = 0;
            x = 0;
            y = 0;

            /* decode */
            switch (ctx.Pcx.encoding)
            {
            case 0:
                /* raw */
                fs.Read(ctx.PcxBuf, 0, w * h);
                c = fs.ReadByte();
                return;

            case 1:
                /* simple run-length encoding */
                do
                {
                    c = fs.ReadByte();
                    if (c == -1) break;
                    if ((c & 0xC0) != 0xC0)
                    {
                        i = 1;
                    }
                    else
                    {
                        i = (uint)(c & 0x3F);
                        c = fs.ReadByte();
                    }

                    do
                    {
                        x++;
                        ctx.PcxBuf[ptr++] = (byte)c;
                        if (x == w)
                        {
                            x = 0;
                            y++;
                        }
                    } while ((--i) != 0);
                } while (y < h);
                break;
            default:
                outPr.Error("Unsupported PCX encoding scheme!");
                return;
            }

            /* get the palette */
            if (c != -1)
            {
                c = fs.ReadByte();
            }
            while ((c != 12) && (c != -1))
            {
                c = fs.ReadByte();
            }
            if (c == 12)
            {
                var pal = new byte[768];
                fs.Read(pal, 0, 768);
                for (i = 0; i < 256; i++)
                {
                    for (j = 0; j < 3; j++)
                    {
                        ctx.PcxPal[i, j] = pal[j + 3 * i];
                    }
                }
            }

            /* number of colors */
            ctx.PcxNbColors = 256;
        }


        /* ----
         * decode_16()
         * ----
         * decode a 16 (or less) colors PCX file
         */

        public void Decode16(FileStream fs, int w, int h)
        {
            int i, j, k, n;
            int x, y, p;
            uint pix;
            int c;

            int ptr = 0;
            x = 0;
            y = 0;
            p = 0;

            /* decode */
            switch (ctx.Pcx.encoding)
            {
            case 0:
                /* raw */
                outPr.Error("Unsupported PCX encoding scheme!");
                break;

            case 1:
                /* simple run-length encoding */
                do
                {
                    /* get a char */
                    c = fs.ReadByte();
                    if (c == -1) break;

                    /* check if it's a repeat command */
                    if ((c & 0xC0) != 0xC0)
                    {
                        i = 1;
                    }
                    else
                    {
                        i = (c & 0x3F);
                        c = fs.ReadByte();
                    }

                    /* unpack */
                    do
                    {
                        ctx.PcxPlane[x >> 3, p] = (byte)c;
                        x += 8;

                        /* end of line */
                        if (x >= w)
                        {
                            x = 0;
                            p++;

                            /* plane to chunky conversion */
                            if (p == ctx.Pcx.np)
                            {
                                p = 0;
                                n = (w + 7) >> 3;
                                y++;

                                /* loop */
                                for (j = 0; j < n; j++)
                                {
                                    for (k = 7; k >= 0; k--)
                                    {
                                        /* get pixel index */
                                        pix = 0;

                                        switch (ctx.Pcx.np)
                                        {
                                        case 4:
                                            pix |= (uint)(((ctx.PcxPlane[j, 3] >> k) & 0x01) << 3);
                                            goto case 3;
                                        case 3:
                                            pix |= (uint)(((ctx.PcxPlane[j, 2] >> k) & 0x01) << 2);
                                            goto case 2;
                                        case 2:
                                            pix |= (uint)(((ctx.PcxPlane[j, 1] >> k) & 0x01) << 1);
                                            goto case 1;
                                        case 1:
                                            pix |= (uint)(((ctx.PcxPlane[j, 0] >> k) & 0x01));
                                            break;
                                        }

                                        /* store pixel */
                                        if (x < w) ctx.PcxBuf[ptr++] = (byte)pix;
                                        x++;
                                    }
                                }
                                x = 0;
                            }
                        }
                    }
                    while ((--i) != 0);
                }
                while (y < h);
                break;
            default:
                outPr.Error("Unsupported PCX encoding scheme!");
                return;
            }

            /* get the palette */
            Array.Clear(ctx.PcxPal, 0, 768);
            Array.Copy(ctx.Pcx.colormap, 0, ctx.PcxPal, 0, (1 << ctx.Pcx.np) * 3);

            /* number of colors */
            ctx.PcxNbColors = 16;
        }
    }
}
