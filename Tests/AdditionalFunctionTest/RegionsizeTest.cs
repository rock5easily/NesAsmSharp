using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NesAsmSharp.Assembler;

namespace NesAsmSharp.Tests
{
    [TestClass]
    public class RegionsizeTest : TestBase
    {
        [TestMethod]
        public void No_Defined()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "no_defined_regionsize_sample.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"Region 'dummy_region' not found!");
        }

        [TestMethod]
        public void Invalid_Argument1()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "invalid_argument_regionsize_sample1.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"Syntax error in expression!");
        }

        [TestMethod]
        public void Invalid_Argument2()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "invalid_argument_regionsize_sample2.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"Invalid argument!");
            StringAssert.Contains(consoleMsg, @"Region '' not found!");
        }

        [TestMethod]
        public void Insufficient()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "insufficient_regionsize_sample.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"Region 'insufficent_region1' invalid!");
            StringAssert.Contains(consoleMsg, @"Region 'insufficent_region2' invalid!");
        }

        [TestMethod]
        public void Various()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "various_regionsize_sample.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsTrue(assembler.AssembleSuccess);
            var result = assembler.ResultBinary;
            Assert.AreEqual(0xFF, result[0x00]);
            Assert.AreEqual(0x1F, result[0x01]);
            Assert.AreEqual(0x00, result[0x02]);
            Assert.AreEqual(0x00, result[0x03]);

            Assert.AreEqual(0x00, result[0x04]);
            Assert.AreEqual(0x20, result[0x05]);
            Assert.AreEqual(0x00, result[0x06]);
            Assert.AreEqual(0x00, result[0x07]);

            Assert.AreEqual(0x88, result[0x08]);
            Assert.AreEqual(0x09, result[0x09]);
            Assert.AreEqual(0x00, result[0x0A]);
            Assert.AreEqual(0x00, result[0x0B]);

            Assert.AreEqual(0x08, result[0x0C]);
            Assert.AreEqual(0x00, result[0x0D]);
            Assert.AreEqual(0x00, result[0x0E]);
            Assert.AreEqual(0x00, result[0x0F]);

            Assert.AreEqual(0xFF, result[0x10]);
            Assert.AreEqual(0xFF, result[0x11]);
            Assert.AreEqual(0x07, result[0x12]);
            Assert.AreEqual(0x00, result[0x13]);

            Assert.AreEqual(0xFF, result[0x14]);
            Assert.AreEqual(0xFF, result[0x15]);
            Assert.AreEqual(0xFF, result[0x16]);
            Assert.AreEqual(0xFF, result[0x17]);
        }
    }
}
