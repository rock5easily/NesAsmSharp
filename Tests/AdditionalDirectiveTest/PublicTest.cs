using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NesAsmSharp.Assembler;
using System.IO;
using System.Text;
using System.Linq;

namespace NesAsmSharp.Tests
{
    [TestClass]
    public class PublicTest : TestBase
    {
        [TestMethod]
        public void No_Public1()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "no_public_sample1.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            Assert.AreEqual(4, consoleMsg.CountPattern(@"Local label access not allowed!"));
        }

        [TestMethod]
        public void No_Public2()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "no_public_sample2.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            Assert.AreEqual(4, consoleMsg.CountPattern(@"Local label access not allowed!"));
        }

        [TestMethod]
        public void Invalid_Label_Definition1()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "invalid_label_definition_public_sample1.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"00:8002            Invalid.Global:");
            StringAssert.Contains(consoleMsg, @"Invalid symbol name!");
        }

        [TestMethod]
        public void Invalid_Label_Definition2()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "invalid_label_definition_public_sample2.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            StringAssert.Contains(consoleMsg, @"00:8004            .Invalid.Local");
            StringAssert.Contains(consoleMsg, @"Invalid symbol name!");
        }

        [TestMethod]
        public void Undefined_Public1()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "undefined_public_sample1.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            Assert.AreEqual(4, consoleMsg.CountPattern(@"Undefined symbol in operand field!"));
        }

        [TestMethod]
        public void Undefined_Public2()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "undefined_public_sample2.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsFalse(assembler.AssembleSuccess);
            Assert.AreEqual(4, consoleMsg.CountPattern(@"Undefined symbol in operand field!"));
        }

        [TestMethod]
        public void Public1()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "public_sample1.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsTrue(assembler.AssembleSuccess);
            var result = assembler.ResultBinary;
            Assert.IsNotNull(result);
            var i = 0;
            // lda #$01
            Assert.AreEqual(0xA9, result[i++]);
            Assert.AreEqual(0x01, result[i++]);
            // clc
            Assert.AreEqual(0x18, result[i++]);
            // adc <$00
            Assert.AreEqual(0x65, result[i++]);
            Assert.AreEqual(0x00, result[i++]);
            // sta <$00
            Assert.AreEqual(0x85, result[i++]);
            Assert.AreEqual(0x00, result[i++]);
            // rts
            Assert.AreEqual(0x60, result[i++]);

            // jsr $8000
            Assert.AreEqual(0x20, result[i++]);
            Assert.AreEqual(0x00, result[i++]);
            Assert.AreEqual(0x80, result[i++]);
            // lda #$03
            Assert.AreEqual(0xA9, result[i++]);
            Assert.AreEqual(0x03, result[i++]);
            // jsr $8002
            Assert.AreEqual(0x20, result[i++]);
            Assert.AreEqual(0x02, result[i++]);
            Assert.AreEqual(0x80, result[i++]);
            // rts
            Assert.AreEqual(0x60, result[i++]);
            // .dw $8002
            Assert.AreEqual(0x02, result[i++]);
            Assert.AreEqual(0x80, result[i++]);
            // .db LOW($8002)
            Assert.AreEqual(0x02, result[i++]);
            // .db HIGH($8002)
            Assert.AreEqual(0x80, result[i++]);
        }

        [TestMethod]
        public void Public2()
        {
            IAssembler assembler = null;

            var consoleMsg = CaptureConsole(() =>
            {
                var opt = new NesAsmOption()
                {
                    InFName = "public_sample2.asm",
                    OutputBinDisabled = true,
                    OutputLstDisabled = true
                };

                assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);
                assembler.Assemble();
            }, TestDataDirectory);

            Assert.IsTrue(assembler.AssembleSuccess);
            var result = assembler.ResultBinary;
            Assert.IsNotNull(result);
            var i = 0;
            // jsr $800D
            Assert.AreEqual(0x20, result[i++]);
            Assert.AreEqual(0x0D, result[i++]);
            Assert.AreEqual(0x80, result[i++]);
            // lda #$03
            Assert.AreEqual(0xA9, result[i++]);
            Assert.AreEqual(0x03, result[i++]);
            // jsr $800F
            Assert.AreEqual(0x20, result[i++]);
            Assert.AreEqual(0x0F, result[i++]);
            Assert.AreEqual(0x80, result[i++]);
            // rts
            Assert.AreEqual(0x60, result[i++]);
            // .dw $800F
            Assert.AreEqual(0x0F, result[i++]);
            Assert.AreEqual(0x80, result[i++]);
            // .db LOW($800F)
            Assert.AreEqual(0x0F, result[i++]);
            // .db HIGH($800F)
            Assert.AreEqual(0x80, result[i++]);

            // lda #$01
            Assert.AreEqual(0xA9, result[i++]);
            Assert.AreEqual(0x01, result[i++]);
            // clc
            Assert.AreEqual(0x18, result[i++]);
            // adc <$00
            Assert.AreEqual(0x65, result[i++]);
            Assert.AreEqual(0x00, result[i++]);
            // sta <$00
            Assert.AreEqual(0x85, result[i++]);
            Assert.AreEqual(0x00, result[i++]);
            // rts
            Assert.AreEqual(0x60, result[i++]);
        }

    }
}
