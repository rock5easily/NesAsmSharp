using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NesAsmSharp.Assembler;
using System.IO;
using System.Text;

namespace NesAsmSharp.Tests
{
    [TestClass]
    public class RegionTest : TestBase
    {
        [TestMethod]
        public void Invalid_Region1()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "invalid_region_sample1.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"BEGINREGION: region name is required!");
            StringAssert.Contains(consoleMsg, @"ENDREGION: region name is required!");
        }

        [TestMethod]
        public void Invalid_Region2()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "invalid_region_sample2.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"BEGINREGION: region name must be non-empty string");
            StringAssert.Contains(consoleMsg, @"ENDREGION: region name must be non-empty string");
        }

        [TestMethod]
        public void Insufficient_Region()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "insufficient_region_sample.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsTrue(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"Region insufficent_region1: ENDREGION not found");
            StringAssert.Contains(consoleMsg, @"Region insufficent_region2: BEGINREGION not found");
        }

        [TestMethod]
        public void Empty_Region()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "empty_region_sample.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsTrue(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"Region emptyregion1:        0 bytes (0x000000 bytes)");
            StringAssert.Contains(consoleMsg, @"Region emptyregion2:        0 bytes (0x000000 bytes)");
        }

        [TestMethod]
        public void Various_Region()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "various_region_sample.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsTrue(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"Region bank00start-end1:     8191 bytes (0x001FFF bytes)");
            StringAssert.Contains(consoleMsg, @"Region bank00start-end2:     8192 bytes (0x002000 bytes)");
            StringAssert.Contains(consoleMsg, @"Region bank01-02-region:     2440 bytes (0x000988 bytes)");
            StringAssert.Contains(consoleMsg, @"Region subroutine  :        8 bytes (0x000008 bytes)");
            StringAssert.Contains(consoleMsg, @"Region large_region:   524287 bytes (0x07FFFF bytes)");
            StringAssert.Contains(consoleMsg, @"Region minus_region:       -1 bytes (0xFFFFFFFF bytes)");
        }
    }
}
