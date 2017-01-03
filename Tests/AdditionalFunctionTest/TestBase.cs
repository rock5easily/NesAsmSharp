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
}
