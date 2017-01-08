using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NesAsmSharp.Assembler;
using System.IO;
using System.Text;

namespace NesAsmSharp.Tests
{
    public abstract class TestBase
    {
        protected string TestDataDirectory;

        public TestBase()
        {
            TestDataDirectory = Path.GetFullPath(Environment.CurrentDirectory + "\\..\\..\\TestData");
        }

        public string CaptureConsole(Action f, string chdir = null)
        {
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);

            var stdOut = Console.Out;
            Console.SetOut(writer);

            var pwd = Environment.CurrentDirectory;
            if (chdir != null) Environment.CurrentDirectory = chdir;

            f();

            if (chdir != null) Environment.CurrentDirectory = pwd;

            Console.SetOut(stdOut);
            writer.Close();
            var msg = sb.ToString();
            Console.Out.Write(msg);

            return msg;
        }

        /// <summary>
        /// Map情報とインデックスからNES上のアドレスを取得する
        /// </summary>
        /// <param name="map"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int GetNesAddr(byte[] map, int idx)
        {
            return ((map[idx] >> 5) << 13) + (idx & 0x1FFF);
        }
    }

    public static class StringExtension
    {
        /// <summary>
        /// 文字列中に指定した部分文字列が何回出現しているかを返す
        /// </summary>
        /// <param name="str"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static int CountPattern(this string str, string pattern)
        {
            var split = str.Split(new string[] { pattern }, StringSplitOptions.None);
            return split.Length - 1;
        }
    }
}
