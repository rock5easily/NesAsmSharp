using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Util
{
    public static class CharArrayPointerExtension
    {
        /// <summary>
        /// ヌル終端文字を考慮してchar配列を文字列に変換する
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ToStringFromNullTerminated(this ArrayPointer<char> ptr)
        {
            var array = ptr.Array;
            var startIndex = ptr.Current;
            var i = startIndex;

            while (array[i] != '\0') i++;

            if (i == startIndex) return "";
            return new string(array, startIndex, i - startIndex);
        }

        /// <summary>
        /// ヌル終端文字を考慮してsrcを自身にコピーする
        /// </summary>
        /// <param name="array"></param>
        /// <param name="src"></param>
        public static void CopyAsNullTerminated(this ArrayPointer<char> ptr, char[] src)
        {
            var array = ptr.Array;
            var startIndex = ptr.Current;
            var i = 0;

            while (src[i] != '\0')
            {
                array[startIndex + i] = src[i];
                i++;
            }
            array[startIndex + i] = '\0';
        }

        /// <summary>
        /// ヌル終端文字を考慮してsrcを自身にコピーする
        /// </summary>
        /// <param name="array"></param>
        /// <param name="src"></param>
        public static void CopyAsNullTerminated(this ArrayPointer<char> ptr, string src)
        {
            var array = ptr.Array;
            var startIndex = ptr.Current;
            var len = src.Length;

            int i;
            for (i = 0; i < len; i++)
            {
                array[startIndex + i] = src[i];
            }
            array[startIndex + i] = '\0';
        }

        /// <summary>
        /// ヌル終端文字形式の文字列とみなして文字列を後ろに連結する
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="src"></param>
        public static ArrayPointer<char> AppendAsNullTerminated(this ArrayPointer<char> ptr, char[] src)
        {
            var array = ptr.Array;
            var p = ptr.Current;

            while (array[p] != '\0') p++;

            var i = 0;
            while (src[i] != '\0')
            {
                ptr[p + i] = src[i];
                i++;
            }
            ptr[p + i] = '\0';

            return ptr;
        }

        /// <summary>
        /// ヌル終端文字形式の文字列とみなして文字列を後ろに連結する
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="src"></param>
        public static ArrayPointer<char> AppendAsNullTerminated(this ArrayPointer<char> ptr, string src)
        {
            var array = ptr.Array;
            var p = ptr.Current;
            var len = src.Length;

            while (array[p] != '\0') p++;

            int i;
            for (i = 0; i < len; i++)
            {
                ptr[p + i] = src[i];
            }
            ptr[p + i] = '\0';

            return ptr;
        }

        /// <summary>
        /// ヌル終端文字形式の文字列とみなしたときの長さを返す
        /// </summary>
        /// <param name="arrayptr"></param>
        /// <returns></returns>
        public static int GetLengthAsNullTerminated(this ArrayPointer<char> arrayptr)
        {
            var i = 0;
            while (arrayptr[i] != '\0') i++;
            return i;
        }
    }

}
