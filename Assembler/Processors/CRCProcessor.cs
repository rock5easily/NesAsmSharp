using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class CRCProcessor : ProcessorBase
    {
        /* locals */
        private uint[] crcTable;

        public CRCProcessor(NesAsmContext ctx) : base(ctx)
        {
            crcTable = new uint[256];
            Init();
        }

        /// <summary>
        /// crc_init()
        /// </summary>
        private void Init()
        {
            int i;
            uint t;
            uint p, q;
            uint poly = 0x864CFB;

            p = 0;
            q = 0;
            crcTable[q++] = 0;
            crcTable[q++] = poly;

            for (i = 1; i < 128; i++)
            {
                t = crcTable[++p];
                if ((t & 0x800000) != 0)
                {
                    t <<= 1;
                    crcTable[q++] = t ^ poly;
                    crcTable[q++] = t;
                }
                else
                {
                    t <<= 1;
                    crcTable[q++] = t;
                    crcTable[q++] = t ^ poly;
                }
            }
        }

        /// <summary>
        /// 24-bit crc
        /// </summary>
        /// <param name="data"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public uint CalcCRC(byte[] data, int len)
        {
            uint crc = 0;

            for (var i = 0; i < len; i++)
            {
                crc = (crc << 8) ^ crcTable[(byte)(crc >> 16) ^ data[i]];
            }

            /* ok */
            return (crc & 0xFFFFFF);
        }

        public uint CalcCRC(IArrayPointer<byte> data, int len)
        {
            uint crc = 0;

            for (var i = 0; i < len; i++)
            {
                crc = (crc << 8) ^ crcTable[(byte)(crc >> 16) ^ data[i]];
            }

            /* ok */
            return (crc & 0xFFFFFF);
        }
    }
}
