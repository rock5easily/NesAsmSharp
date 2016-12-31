using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Util
{
    public static class CharUtil
    {
        public static bool IsSpace(char c)
        {
            return (c == ' ' || c == '\t');
        }

        public static bool IsAlNum(char c)
        {
            return ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z') || char.IsDigit(c);
        }

        public static bool IsAlpha(char c)
        {
            return ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z');
        }
    }

}
