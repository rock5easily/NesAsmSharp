using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler
{
    public static class Definition
    {
        /* reserved bank index */
        public static readonly int RESERVED_BANK = 0xF0;
        public static readonly int PROC_BANK = 0xF1;
        public static readonly int GROUP_BANK = 0xF2;

        /* line buffer length */
        /// <summary>
        /// LAST_CH_POS = 158
        /// </summary>
        public static readonly int LAST_CH_POS = 158;
        /// <summary>
        /// SFIELD = 26
        /// </summary>
        public static readonly int SFIELD = 26;
        /// <summary>
        /// ラベル名の長さ(オリジナルの値=32)
        /// SBOLSZ = 64
        /// </summary>
        public static readonly int SBOLSZ = 64;
        /// <summary>
        /// マクロ名の長さ(オリジナルの値=32)
        /// MACROSZ = 64
        /// </summary>
        public static readonly int MACROSZ = 64;

        /// <summary>
        /// Functuionの引数の最大文字数
        /// FUNC_ARG_MAX_LEN = 80
        /// </summary>
        public static readonly int FUNC_ARG_MAX_LEN = 80;
        /// <summary>
        /// Macroの引数の最大文字数
        /// MACRO_ARG_MAX_LEN = 80
        /// </summary>
        public static readonly int MACRO_ARG_MAX_LEN = 80;
        /// <summary>
        /// ValStackのスタックサイズ上限
        /// VALSTACK_MAX = 64
        /// </summary>
        public static readonly int VALSTACK_MAX = 64;
        /// <summary>
        /// OpStackのスタックサイズ上限
        /// OPSTACK_MAX = 64
        /// </summary>
        public static readonly int OPSTACK_MAX = 64;
        /// <summary>
        /// バンク数の上限
        /// BANK_NUM_MAX = 128
        /// </summary>
        public static readonly int BANK_NUM_MAX = 128;
        /// <summary>
        /// バンクサイズ
        /// BANK_SIZE = 8192
        /// </summary>
        public static readonly int BANK_SIZE = 0x2000;

        /// <summary>
        /// インクルードパスの最大数
        /// INC_PATH_MAX = 10
        /// </summary>
        public static readonly int INC_PATH_MAX = 10;

        /// <summary>
        /// セクション名
        /// </summary>
        public static readonly string[] SectionName = { "  ZP", " BSS", "CODE", "DATA" };
    }

    /* tile format for encoder */
    public enum TileFormat
    {
        CHUNKY_TILE = 1,
        PACKED_TILE,
    }

    /* macro argument types */
    public enum MacroArgumentType
    {
        NO_ARG = 0,
        ARG_REG,
        ARG_IMM,
        ARG_ABS,
        ARG_INDIRECT,
        ARG_STRING,
        ARG_LABEL,
    }

    /* section types */
    public enum SectionType
    {
        S_ZP = 0,
        S_BSS,
        S_CODE,
        S_DATA,
    }

    /* assembler options */
    public enum AssemblerOption
    {
        OPT_LIST = 0,
        OPT_MACRO,
        OPT_WARNING,
        OPT_OPTIMIZE,
    }

    /* assembler directives */
    public enum AsmDirective
    {
        P_UNDEFINED = -1,
        P_DB = 0, // .db
        P_DW = 1, // .dw
        P_DS = 2, // .ds
        P_EQU = 3, // .equ
        P_ORG = 4, // .org
        P_PAGE = 5, // .page
        P_BANK = 6, // .bank
        P_INCBIN = 7, // .incbin
        P_INCLUDE = 8, // .include
        P_INCCHR = 9, // .incchr
        P_INCSPR = 10, // .incspr
        P_INCPAL = 11, // .incpal
        P_INCBAT = 12, // .incbat
        P_MACRO = 13, // .macro
        P_ENDM = 14, // .endm
        P_LIST = 15, // .list
        P_MLIST = 16, // .mlist
        P_NOLIST = 17, // .nolist
        P_NOMLIST = 18, // .nomlist
        P_RSSET = 19, // .rsset
        P_RS = 20, // .rs
        P_IF = 21, // .if
        P_ELSE = 22, // .else
        P_ENDIF = 23, // .endif
        P_FAIL = 24, // .fail
        P_ZP = 25, // .zp
        P_BSS = 26, // .bss
        P_CODE = 27, // .code
        P_DATA = 28, // .data
        P_DEFCHR = 29, // .defchr
        P_FUNC = 30, // .func
        P_IFDEF = 31, // .ifdef
        P_IFNDEF = 32, // .ifndef
        P_VRAM = 33, // .vram
        P_PAL = 34, // .pal
        P_DEFPAL = 35, // .defpal
        P_DEFSPR = 36, // .defspr
        P_INESPRG = 37, // .inesprg
        P_INESCHR = 38, // .ineschr
        P_INESMAP = 39, // .inesmap
        P_INESMIR = 40, // .inesmir
        P_OPT = 41, // .opt
        P_INCTILE = 42, // .inctile
        P_INCMAP = 43, // .incmap
        P_MML = 44, // .mml
        P_PROC = 45, // .proc
        P_ENDP = 46, // .endp
        P_PGROUP = 47, // .procgroup
        P_ENDPG = 48, // .endprocgroup
        P_CALL = 49, // .call
        P_AUTOZP = 50, // .autozp
    }

    /* symbol flags */
    public enum SymbolFlag
    {
        UNDEF = 1, // undefined - may be zero page
        IFUNDEF,   // declared in a .if expression
        MDEF,      // multiply defined
        DEFABS,    // defined - two byte address
        MACRO,     // used for a macro name
        FUNC,      // used for a function
    }

    /// <summary>
    /// シンボルのスコープ(GLOBAL/LOCAL)
    /// </summary>
    public enum SymbolScope
    {
        GLOBAL,
        LOCAL
    }

    /* operation code flags */
    public enum OpCodeFlag
    {
        UNDEFINED = 0x0000000,
        ACC = 0x0000001,
        IMM = 0x0000002,
        ZP = 0x0000004,
        ZP_X = 0x0000008,
        ZP_Y = 0x0000010,
        ZP_IND = 0x0000020,
        ZP_IND_X = 0x0000040,
        ZP_IND_Y = 0x0000080,
        ABS = 0x0000100,
        ABS_X = 0x0000200,
        ABS_Y = 0x0000400,
        ABS_IND = 0x0000800,
        ABS_IND_X = 0x0001000,
        PSEUDO = 0x0008000,
        CLASS1 = 0x0010000,
        CLASS2 = 0x0020000,
        CLASS3 = 0x0040000,
        CLASS5 = 0x0080000,
        CLASS6 = 0x0100000,
        CLASS7 = 0x0200000,
        CLASS8 = 0x0400000,
        CLASS9 = 0x0800000,
        CLASS10 = 0x1000000,
    }

    /* value types */
    public enum ValueType
    {
        T_DECIMAL = 0,
        T_HEXA,
        T_BINARY,
        T_CHAR,
        T_SYMBOL,
        T_PC,
    }

    /* operators */
    public enum OperatorType
    {
        OP_START = 0,
        OP_OPEN = 1,
        OP_ADD = 2,
        OP_SUB = 3,
        OP_MUL = 4,
        OP_DIV = 5,
        OP_MOD = 6,
        OP_NEG = 7,
        OP_SHL = 8,
        OP_SHR = 9,
        OP_OR = 10,
        OP_XOR = 11,
        OP_AND = 12,
        OP_COM = 13,
        OP_NOT = 14,
        OP_EQUAL = 15,
        OP_NOT_EQUAL = 16,
        OP_LOWER = 17,
        OP_LOWER_EQUAL = 18,
        OP_HIGHER = 19,
        OP_HIGHER_EQUAL = 20,
        OP_DEFINED = 21,
        OP_HIGH = 22,
        OP_LOW = 23,
        OP_PAGE = 24,
        OP_BANK = 25,
        OP_VRAM = 26,
        OP_PAL = 27,
        OP_SIZEOF = 28,
    }

    /* pass flags */
    public enum PassFlag
    {
        FIRST_PASS = 0,
        LAST_PASS,
    }


    /* machine */
    public enum MachineType
    {
        MACHINE_PCE = 0,
        MACHINE_NES = 1,
    }

    public class NesAsmOpecode
    {
        public NesAsmOpecode Next { get; set; }
        public string Name { get; set; }
        public OpProcAction Proc { get; set; }
        public OpCodeFlag Flag { get; set; }
        public AsmDirective Value { get; set; }
        public int TypeIdx { get; set; }

        public NesAsmOpecode()
        {
        }

        public NesAsmOpecode(NesAsmOpecode next, string name, OpProcAction proc, OpCodeFlag flag, int value, int typeIdx)
        {
            Next = next;
            Name = name;
            Proc = proc;
            Flag = flag;
            Value = (AsmDirective)value;
            TypeIdx = typeIdx;
        }

        public NesAsmOpecode(NesAsmOpecode next, string name, OpProcAction proc, OpCodeFlag flag, AsmDirective value, int typeIdx)
        {
            Next = next;
            Name = name;
            Proc = proc;
            Flag = flag;
            Value = value;
            TypeIdx = typeIdx;
        }

    }

    public class NesAsmInputInfo
    {
        /// <summary>
        /// ソース読み込み用StreamReaderオブジェクト
        /// </summary>
        public StreamReader Fp { get; set; }
        /// <summary>
        /// 読んだ行数
        /// </summary>
        public int LineNum { get; set; } // Line number
        public int IfLevel { get; set; }
        /// <summary>
        /// ソースファイル名
        /// </summary>
        public string Name { get; set; }
    }

    public class NesAsmProc
    {
        public NesAsmProc Next { get; set; }
        public NesAsmProc Link { get; set; }
        public NesAsmProc Group { get; set; }
        public int OldBank { get; set; }
        public int Bank { get; set; }
        public int Org { get; set; }
        public int Base { get; set; }
        public int Size { get; set; }
        public int Call { get; set; }
        public AsmDirective Type { get; set; }
        public int RefCnt { get; set; }
        public string Name { get; set; }
    }

    public class NesAsmSymbol
    {
        public NesAsmSymbol Next { get; set; }
        public NesAsmSymbol Local { get; set; }
        public NesAsmProc Proc { get; set; }
        public SymbolFlag Type { get; set; }
        public int Value { get; set; }
        public int Bank { get; set; }
        public int Page { get; set; }
        public int Nb { get; set; }
        public int Size { get; set; }
        public int Vram { get; set; }
        public int Pal { get; set; }
        public int RefCnt { get; set; }
        public int Reserved { get; set; }
        public AsmDirective DataType { get; set; }
        public int DataSize { get; set; }
        public string Name { get; set; }
    }

    public class NesAsmLine
    {
        public NesAsmLine Next { get; set; }
        public string Data { get; set; }
    }

    public class NesAsmMacro
    {
        public NesAsmMacro Next { get; set; }
        public NesAsmLine Line { get; set; }
        public string Name { get; set; }
    }

    public class NesAsmFunc
    {
        public NesAsmFunc Next { get; set; }
        public char[] Line { get; private set; }
        public string Name { get; set; }

        public NesAsmFunc()
        {
            Line = new char[128];
        }
    }

    public class NesAsmTile
    {
        public NesAsmTile Next { get; set; }
        public IArrayPointer<byte> Data { get; set; }
        public uint Crc { get; set; }
        public int Index { get; set; }
    }

    public class NesAsmMachine
    {
        public MachineType Type { get; set; }
        public string AsmName { get; set; }
        public string AsmTitle { get; set; }
        public string RomExt { get; set; }
        public string IncludeEnv { get; set; }
        public uint ZPLimit { get; set; }
        public uint RamLimit { get; set; }
        public uint RamBase { get; set; }
        public uint RamPage { get; set; }
        public uint RamBank { get; set; }
        public NesAsmOpecode[] Inst { get; set; }
        public NesAsmOpecode[] PseudoInst { get; set; }
        public Func<byte[], ArrayPointer<byte>, int, TileFormat, int> Pack8x8Tile { get; set; }
        public Func<byte[], ArrayPointer<byte>, int, TileFormat, int> Pack16x16Tile { get; set; }
        public Func<byte[], ArrayPointer<byte>, int, TileFormat, int> Pack16x16Sprite { get; set; }
        public Action<FileStream, int> WriteHeader { get; set; }
    }

    public class PCXHeader
    {
        /* pcx file header */
        public byte manufacturer;
        public byte version;
        public byte encoding;
        public byte bpp;
        public byte[] xmin = new byte[2];
        public byte[] ymin = new byte[2];
        public byte[] xmax = new byte[2];
        public byte[] ymax = new byte[2];
        public byte[] xdpi = new byte[2];
        public byte[] ydpi = new byte[2];
        public byte[,] colormap = new byte[16, 3];
        public byte reserved;
        public byte np;
        public byte[] bytes_per_line = new byte[2];
        public byte[] palette_info = new byte[2];
        public byte[] xscreen = new byte[2];
        public byte[] yscreen = new byte[2];
        public byte[] pad = new byte[54];

        public void SetValues(byte[] a)
        {
            manufacturer = a[0];
            version = a[1];
            encoding = a[2];
            bpp = a[3];
            xmin[0] = a[4];
            xmin[1] = a[5];
            ymin[0] = a[6];
            ymin[1] = a[7];
            xmax[0] = a[8];
            xmax[1] = a[9];
            ymax[0] = a[10];
            ymax[1] = a[11];
            xdpi[0] = a[12];
            xdpi[1] = a[13];
            ydpi[0] = a[14];
            ydpi[1] = a[15];
            for (var i = 0; i < 16; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    colormap[i, j] = a[16 + j + 3 * i]; // index 16-63
                }
            }
            reserved = a[64];
            np = a[65];
            bytes_per_line[0] = a[66];
            bytes_per_line[1] = a[67];
            palette_info[0] = a[68];
            palette_info[1] = a[69];
            xscreen[0] = a[70];
            xscreen[1] = a[71];
            yscreen[0] = a[72];
            yscreen[1] = a[73];
            for (var i = 0; i < 54; i++)
            {
                pad[i] = a[74 + i]; // index 74-127
            }
        }
    }

    public class PCENotImplementedException : NotImplementedException
    {

    }
}
