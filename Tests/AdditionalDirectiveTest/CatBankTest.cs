using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NesAsmSharp.Assembler;
using System.IO;
using System.Text;

namespace NesAsmSharp.Tests
{
    [TestClass]
    public class CatbankTest : TestBase
    {
        [TestMethod]
        public void No_Catbank()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "no_catbank_sample.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"Bank overflow, offset > $1FFF!");
        }

        [TestMethod]
        public void Catbank()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "catbank_sample.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsTrue(assembler.AssembleSuccess);

            var result = assembler.ResultBinary;
            Assert.AreNotEqual(null, result);
            Assert.AreEqual(8 * 0x2000, result.Length);

            var map = assembler.ResultMap;
            Assert.AreNotEqual(null, map);
            Assert.AreEqual(8 * 0x2000, map.Length);

            Assert.AreEqual(1, result[0x1FFC]);
            Assert.AreEqual(0x9FFC, GetNesAddr(map, 0x1FFC));
            Assert.AreEqual(2, result[0x1FFD]);
            Assert.AreEqual(0x9FFD, GetNesAddr(map, 0x1FFD));
            Assert.AreEqual(3, result[0x1FFE]);
            Assert.AreEqual(0x9FFE, GetNesAddr(map, 0x1FFE));
            Assert.AreEqual(4, result[0x1FFF]);
            Assert.AreEqual(0x9FFF, GetNesAddr(map, 0x1FFF));
            Assert.AreEqual(5, result[0x2000]);
            Assert.AreEqual(0xA000, GetNesAddr(map, 0x2000));
            Assert.AreEqual(6, result[0x2001]);
            Assert.AreEqual(0xA001, GetNesAddr(map, 0x2001));
            Assert.AreEqual(7, result[0x2002]);
            Assert.AreEqual(0xA002, GetNesAddr(map, 0x2002));
            Assert.AreEqual(8, result[0x2003]);
            Assert.AreEqual(0xA003, GetNesAddr(map, 0x2003));

            Assert.AreEqual(8, result[0x5FFB]);
            Assert.AreEqual(0xDFFB, GetNesAddr(map, 0x5FFB));
            Assert.AreEqual(7, result[0x5FFC]);
            Assert.AreEqual(0xDFFC, GetNesAddr(map, 0x5FFC));
            Assert.AreEqual(6, result[0x5FFD]);
            Assert.AreEqual(0xDFFD, GetNesAddr(map, 0x5FFD));
            Assert.AreEqual(5, result[0x5FFE]);
            Assert.AreEqual(0xDFFE, GetNesAddr(map, 0x5FFE));
            Assert.AreEqual(4, result[0x5FFF]);
            Assert.AreEqual(0xDFFF, GetNesAddr(map, 0x5FFF));
            Assert.AreEqual(3, result[0x6000]);
            Assert.AreEqual(0xE000, GetNesAddr(map, 0x6000));
            Assert.AreEqual(2, result[0x6001]);
            Assert.AreEqual(0xE001, GetNesAddr(map, 0x6001));
            Assert.AreEqual(1, result[0x6002]);
            Assert.AreEqual(0xE002, GetNesAddr(map, 0x6002));

            Assert.AreEqual(0x23, result[0x9FFA]);
            Assert.AreEqual(0x9FFA, GetNesAddr(map, 0x9FFA));
            Assert.AreEqual(0x01, result[0x9FFB]);
            Assert.AreEqual(0x9FFB, GetNesAddr(map, 0x9FFB));
            Assert.AreEqual(0x67, result[0x9FFC]);
            Assert.AreEqual(0x9FFC, GetNesAddr(map, 0x9FFC));
            Assert.AreEqual(0x45, result[0x9FFD]);
            Assert.AreEqual(0x9FFD, GetNesAddr(map, 0x9FFD));
            Assert.AreEqual(0xAB, result[0x9FFE]);
            Assert.AreEqual(0x9FFE, GetNesAddr(map, 0x9FFE));
            Assert.AreEqual(0x89, result[0x9FFF]);
            Assert.AreEqual(0x9FFF, GetNesAddr(map, 0x9FFF));
            Assert.AreEqual(0xEF, result[0xA000]);
            Assert.AreEqual(0xA000, GetNesAddr(map, 0xA000));
            Assert.AreEqual(0xCD, result[0xA001]);
            Assert.AreEqual(0xA001, GetNesAddr(map, 0xA001));

            Assert.AreEqual(0xDC, result[0xDFFB]);
            Assert.AreEqual(0xDFFB, GetNesAddr(map, 0xDFFB));
            Assert.AreEqual(0xFE, result[0xDFFC]);
            Assert.AreEqual(0xDFFC, GetNesAddr(map, 0xDFFC));
            Assert.AreEqual(0x98, result[0xDFFD]);
            Assert.AreEqual(0xDFFD, GetNesAddr(map, 0xDFFD));
            Assert.AreEqual(0xBA, result[0xDFFE]);
            Assert.AreEqual(0xDFFE, GetNesAddr(map, 0xDFFE));
            Assert.AreEqual(0x54, result[0xDFFF]);
            Assert.AreEqual(0xDFFF, GetNesAddr(map, 0xDFFF));
            Assert.AreEqual(0x76, result[0xE000]);
            Assert.AreEqual(0xE000, GetNesAddr(map, 0xE000));
            Assert.AreEqual(0x10, result[0xE001]);
            Assert.AreEqual(0xE001, GetNesAddr(map, 0xE001));
            Assert.AreEqual(0x32, result[0xE002]);
            Assert.AreEqual(0xE002, GetNesAddr(map, 0xE002));
        }

        [TestMethod]
        public void Incbin_Catbank()
        {
            IAssembler assembler = null;
            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "incbin_catbank_sample.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsTrue(assembler.AssembleSuccess);

            var result = assembler.ResultBinary;
            Assert.AreNotEqual(null, result);
            Assert.AreEqual(11 * 0x2000, result.Length);

            var map = assembler.ResultMap;
            Assert.AreNotEqual(null, map);
            Assert.AreEqual(11 * 0x2000, map.Length);

            Assert.AreEqual(0x9FFF, GetNesAddr(map, 0x1FFF));
            Assert.AreEqual(0xA000, GetNesAddr(map, 0x2000));

            Assert.AreEqual(0xBFFF, GetNesAddr(map, 0x3FFF));
            Assert.AreEqual(0xC000, GetNesAddr(map, 0x4000));

            Assert.AreEqual(0xDFFF, GetNesAddr(map, 0x5FFF));
            Assert.AreEqual(0xE000, GetNesAddr(map, 0x6000));

            Assert.AreEqual(0xFFFF, GetNesAddr(map, 0x7FFF));
            Assert.AreEqual(0x8000, GetNesAddr(map, 0x8000));

            Assert.AreEqual(0x8800, GetNesAddr(map, 0x10800));
            Assert.AreEqual(0x9FFF, GetNesAddr(map, 0x11FFF));
            Assert.AreEqual(0xA000, GetNesAddr(map, 0x12000));

            Assert.AreEqual(0xBFFF, GetNesAddr(map, 0x13FFF));
            Assert.AreEqual(0xC000, GetNesAddr(map, 0x14000));
            Assert.AreEqual(0xC7FF, GetNesAddr(map, 0x147FF));
        }

    }
}
