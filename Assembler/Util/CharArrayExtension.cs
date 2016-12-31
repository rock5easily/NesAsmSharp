using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Util
{
    public static class CharArrayExtension
    {
        /// <summary>
        /// ヌル終端文字を考慮してchar配列を文字列に変換する
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ToStringFromNullTerminated(this char[] array)
        {
            int i;
            for (i = 0; i < array.Length; i++)
            {
                if (array[i] == '\0') break;
            }

            if (i == 0) return "";
            return new string(array, 0, i);
        }

        public static string ToStringFromNullTerminated(this char[] array, int startIndex)
        {
            int i;
            for (i = startIndex; i < array.Length; i++)
            {
                if (array[i] == '\0') break;
            }

            if (i == startIndex) return "";
            return new string(array, startIndex, i - startIndex);
        }

        /// <summary>
        /// ヌル終端文字を考慮してsrcを自身にコピーする
        /// </summary>
        /// <param name="array"></param>
        /// <param name="src"></param>
        public static void CopyAsNullTerminated(this char[] array, char[] src)
        {
            var i = 0;
            while (src[i] != '\0')
            {
                array[i] = src[i];
                i++;
            }
            array[i] = '\0';
        }

        /// <summary>
        /// ヌル終端文字を考慮してsrcを最大length文字だけ自身にコピーする
        /// </summary>
        /// <param name="array"></param>
        /// <param name="src"></param>
        /// <param name="length"></param>
        public static void CopyAsNullTerminated(this char[] array, char[] src, int length)
        {
            var i = 0;
            while (src[i] != '\0' && i < length)
            {
                array[i] = src[i];
                i++;
            }
            array[i] = '\0';
        }

        /// <summary>
        /// ヌル終端文字を考慮してsrcをstartIndexの位置から最大length文字だけ自身にコピーする
        /// lengthに負数を指定した場合はsrcの最後までコピーする
        /// </summary>
        /// <param name="array"></param>
        /// <param name="src"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        public static void CopyAsNullTerminated(this char[] array, char[] src, int startIndex, int length)
        {
            var i = 0;
            if (length < 0) length = src.Length;

            while (src[startIndex + i] != '\0' && i < length)
            {
                array[i] = src[startIndex + i];
                i++;
            }
            array[i] = '\0';
        }

        /// <summary>
        /// ヌル終端文字を考慮してsrcを自身にコピーする
        /// </summary>
        /// <param name="array"></param>
        /// <param name="src"></param>
        public static void CopyAsNullTerminated(this char[] array, string src)
        {
            var i = 0;
            while (i < src.Length)
            {
                array[i] = src[i];
                i++;
            }
            array[i] = '\0';
        }

        /// <summary>
        /// ヌル終端文字を考慮して自身のdstStartIndexの位置からsrcをコピーする
        /// </summary>
        /// <param name="array"></param>
        /// <param name="dstStartIndex"></param>
        /// <param name="src"></param>
        public static void CopyAsNullTerminated(this char[] array, int dstStartIndex, string src)
        {
            var i = 0;
            while (i < src.Length)
            {
                array[dstStartIndex + i] = src[i];
                i++;
            }
            array[dstStartIndex + i] = '\0';
        }


        /// <summary>
        /// ヌル終端文字を考慮してsrcを最大length文字だけ自身にコピーする
        /// </summary>
        /// <param name="array"></param>
        /// <param name="src"></param>
        /// <param name="length"></param>
        public static void CopyAsNullTerminated(this char[] array, string src, int length)
        {
            var i = 0;
            while (i < src.Length && i < length)
            {
                array[i] = src[i];
                i++;
            }
            array[i] = '\0';
        }

        /// <summary>
        /// 文字列をヌル終端文字列形式のchar配列に変換する
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static char[] ToNullTerminatedCharArray(this string str)
        {
            var len = str.Length;
            var array = new char[len + 1];
            var i = 0;
            while (i < len)
            {
                array[i] = str[i];
                i++;
            }
            array[i] = '\0';
            return array;
        }

        /// <summary>
        /// ヌル終端文字列とみなして文字列長を取得する
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static int GetLengthAsNullTerminated(this char[] array)
        {
            var i = 0;
            while (array[i] != '\0') i++;

            return i;
        }
    }
}
