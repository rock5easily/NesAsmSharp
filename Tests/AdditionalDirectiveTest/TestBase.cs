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

        public string CaptureConsole(Action f)
        {
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);

            var stdOut = Console.Out;
            Console.SetOut(writer);

            f();

            Console.SetOut(stdOut);
            writer.Close();
            var msg = sb.ToString();
            Console.Out.Write(msg);

            return msg;
        }
    }
}
