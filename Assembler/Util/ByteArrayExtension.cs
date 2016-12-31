using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Util
{
    public static class ByteArrayExtension
    {
        /// <summary>
        /// UInt32型の配列をbyte型の配列に変換する
        /// 1個のUInt32型の値につき4個のbyte型の値にする
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this UInt32[] array)
        {
            var len = array.Length;
            var bytearray = new byte[len * 4];

            for (var i = 0; i < len; i++)
            {
                UInt32 v = array[i];
                bytearray[i * 4] = (byte)(v & 0xFF);
                bytearray[i * 4 + 1] = (byte)((v >> 8) & 0xFF);
                bytearray[i * 4 + 2] = (byte)((v >> 16) & 0xFF);
                bytearray[i * 4 + 3] = (byte)((v >> 24) & 0xFF);
            }

            return bytearray;
        }

        /// <summary>
        /// ランク2のbyte型の配列をbyte型の配列に変換する
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this byte[,] array)
        {
            var len = array.Length;
            var bytearray = new byte[len];

            var dim0Len = array.GetLength(0);
            var dim1Len = array.GetLength(1);

            var p = 0;
            for (var i = 0; i < dim0Len; i++)
            {
                for (var j = 0; j < dim1Len; j++)
                {
                    bytearray[p++] = array[i, j];
                }
            }

            return bytearray;
        }

        /// <summary>
        /// byte型の配列をランク2のbyte型の配列に変換する
        /// byte型の配列の要素数とdim0Len*dim1Lenが一致しない場合は例外がスローされる
        /// </summary>
        /// <param name="array"></param>
        /// <param name="dim0Len">ランク2配列の0次元目の要素数</param>
        /// <param name="dim1Len">ランク2配列の1次元目の要素数</param>
        /// <returns></returns>
        public static byte[,] ToRank2ByteArray(this byte[] array, int dim0Len, int dim1Len)
        {
            var len = array.Length;

            if (len != dim0Len * dim1Len)
            {
                throw new ArgumentException();
            }

            var rank2array = new byte[dim0Len, dim1Len];

            var p = 0;
            for (var i = 0; i < dim0Len; i++)
            {
                for (var j = 0; j < dim1Len; j++)
                {
                    rank2array[i, j] = array[p++];
                }
            }

            return rank2array;
        }
    }

}
