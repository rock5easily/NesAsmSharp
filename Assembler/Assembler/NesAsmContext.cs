using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NesAsmSharp.Assembler.Processors;
using System.IO;

namespace NesAsmSharp.Assembler
{
    public delegate void OpProcAction(ref int ip);

    public class NesAsmContext
    {
        // vars.h
        public byte[,] Rom { get; private set; }
        public byte[,] Map { get; private set; }
        public string[] BankName { get; private set; }
        public int[,] BankLocCnt { get; private set; }
        public int[,] BankPage { get; private set; }
        public int MaxZP { get; set; } // higher used address in zero page
        public int MaxBSS { get; set; } // higher used address in ram
        public int MaxBank { get; set; } // last bank used
        public int DataLocCnt { get; set; } // data location counter
        public int DataSize { get; set; } // size of binary output (in bytes)
        public int DataLevel { get; set; } // data output level, must be <= listlevel to be outputed
        public int LocCnt { get; set; } // location counter
        public int Bank { get; set; } // current bank
        public int BankBase { get; set; } // bank base index
        public int RomLimit { get; set; } // bank limit
        public int BankLimit { get; set; } // rom max. size in bytes
        public int Page { get; set; } // page
        public int RSBase { get; set; } // .rs counter
        public SectionType Section { get; set; } // current section: S_ZP, S_BSS, S_CODE or S_DATA
        public int[] SectionBank { get; private set; } // current bank for each section
        public bool StopPass { get; set; } // stop the program {get; set; } set by fatal_error()
        public int ErrCnt { get; set; } // error counter
        public NesAsmMachine Machine { get; set; }
        public Dictionary<string, NesAsmOpecode> InstTbl { get; private set; } // instructions hash table
        public Dictionary<string, NesAsmSymbol> GLablHashTbl { get; private set; } // label hash table
        public NesAsmSymbol LablPtr { get; set; } // label pointer into symbol table
        public NesAsmSymbol GLablPtr { get; set; } // pointer to the latest defined global label
        public NesAsmSymbol LastLabl { get; set; } // last label we have seen
        public NesAsmSymbol[,] BankGLabl { get; private set; } // latest global symbol for each bank
        public OpProcAction OpProc { get; set; } // instruction gen proc
        public OpCodeFlag OpFlg { get; set; } // instruction flags
        public AsmDirective OpVal { get; set; } // instruction value
        public int OpType { get; set; } // instruction type
        public char OpExt { get; set; } // instruction extension (.l or .h)
        public PassFlag Pass { get; set; } // pass counter
        public char[] PrLnBuf { get; private set; } // input line buffer
        public char[] TmpLnBuf { get; private set; } // temporary line buffer
        public Dictionary<string, NesAsmRegion> RegionTbl { get; private set; } // region table
        public List<string> StringTbl { get; private set; } // registered string table

        /// <summary>
        /// バンク跨ぎ設定フラグ
        /// </summary>
        public bool[] BankCat { get; private set; } // cat bank flag
        /// <summary>
        /// 現在読んでいるソースの読んだ行数
        /// </summary>
        public int SrcLineNum { get; set; } // source line number counter
        public int Undef { get; set; } // undefined symbol in expression flg
        public uint Value { get; set; } // operand field value

        // macro.c
        public bool InMacro { get; set; }
        public bool IsExpandMacro { get; set; }
        public char[,][] MacroArg { get; private set; }
        public int MacroIdx { get; set; }
        public int MacroCounter { get; set; }
        public int MacroCntMax { get; set; }
        public Stack<int> MacroCntStack { get; private set; }
        public Stack<NesAsmLine> MacroStack { get; private set; }
        public NesAsmLine MacroLinePtr { get; set; }
        public Dictionary<string, NesAsmMacro> MacroTbl { get; private set; }
        public NesAsmMacro MacroPtr { get; set; }

        // assemble.c
        public bool InIf { get; set; } // set when we are in an .if statement
        public bool IfExpr { get; set; } // set when parsing an .if expression
        public int IfLevel { get; set; } // level of nested .if's
        public bool[] IfState { get; private set; }  // status when entering the .if
        public bool[] IfFlag { get; private set; } // .if/.else status
        public bool SkipLines { get; set; } // set when lines must be skipped
        public bool ContinuedLine { get; set; } // set when a line is the continuation of another line

        // func.c
        public Dictionary<string, NesAsmFunc> FuncTbl { get; private set; }
        public NesAsmFunc FuncPtr { get; set; }
        public char[] FuncLine { get; private set; }
        public char[,][] FuncArg { get; private set; }
        public int FuncIdx { get; set; }

        // expr.c
        public Stack<OperatorType> OpStack { get; private set; } // operator stack
        public Stack<uint> ValStack { get; private set; } // value stack
        public bool NeedOperator { get; set; } // when set await an operator, else await a value
        public ArrayPointer<char> Expr { get; set; } // pointer to the expression string
        public Stack<ArrayPointer<char>> ExprStack { get; private set; } // expression stack
        public NesAsmSymbol ExprLablPtr { get; set; } // pointer to the lastest label
        public int ExprLablCnt { get; set; } // number of label seen in an expression

        // proc.c
        public Dictionary<string, NesAsmProc> ProcTbl { get; private set; }
        public NesAsmProc ProcPtr { get; set; }
        public NesAsmProc ProcFirst { get; set; }
        public NesAsmProc ProcLast { get; set; }
        public int ProcNb { get; set; }
        public int CallPtr { get; set; }
        public int CallBank { get; set; }

        // input.c
        public int InFileError { get; set; }
        public int InFileNum { get; set; }
        public NesAsmInputInfo[] InputFile { get; private set; }
        /// <summary>
        /// 現在のソース読み込み用StreamReaderオブジェクト
        /// </summary>
        public StringReader InFp { get; set; } // file pointers, input
        /// <summary>
        /// 読み込んだソースファイルのキャッシュ
        /// key = ソースファイルの絶対パス, Value = ソースファイルのテキスト
        /// </summary>
        public Dictionary<string, string> InFileTextCache { get; private set; }
        /// <summary>
        /// .lstファイル書き込み用StreamWriterオブジェクト
        /// </summary>
        public StreamWriter LstFp { get; set; } // listing
        public string[] IncPath { get; private set; }

        // code.c
        public byte AutoInc { get; set; }
        public byte AutoTag { get; set; }
        public uint AutoTagValue { get; set; }

        // pcx.c
        public string PcxName { get; set; } // pcx file name
        public int PcxW { get; set; } //. pcx dimensions
        public int PcxH { get; set; } //. pcx dimensions
        public int PcxNbColors { get; set; } // number of colors in the pcx
        public int PcxNbArgs { get; set; } // number of argument
        public uint[] PcxArg { get; private set; } // pcx args array
        public byte[] PcxBuf { get; set; } // pointer to the pcx buffer
        public byte[,] PcxPal { get; private set; }	// palette
        public byte[,] PcxPlane { get; private set; } // plane buffer
        public uint TileOffset { get; set; } // offset in the tile reference table
        public NesAsmTile[] Tile { get; private set; } // tile info table
        public NesAsmTile[] TileTbl { get; private set; } // tile hash table
        public NesAsmSymbol TileLablPtr { get; set; } // tile symbol reference
        public PCXHeader Pcx { get; set; }

        // Option
        public NesAsmOption Option { get; set; }


        public NesAsmContext()
        {
            Rom = new byte[Definition.BANK_NUM_MAX, Definition.BANK_SIZE];
            Map = new byte[Definition.BANK_NUM_MAX, Definition.BANK_SIZE];
            BankName = new string[Definition.BANK_NUM_MAX];
            BankLocCnt = new int[4, 256];
            BankPage = new int[4, 256];
            SectionBank = new int[4];
            InstTbl = new Dictionary<string, NesAsmOpecode>();
            GLablHashTbl = new Dictionary<string, NesAsmSymbol>();
            BankGLabl = new NesAsmSymbol[4, 256];
            PrLnBuf = new char[Definition.LAST_CH_POS + 4];
            TmpLnBuf = new char[Definition.LAST_CH_POS + 4];
            BankCat = new bool[Definition.BANK_NUM_MAX];
            RegionTbl = new Dictionary<string, NesAsmRegion>();
            StringTbl = new List<string>();

            // macro.c
            MacroArg = new char[8, 10][];
            for (var i = 0; i < 8; i++)
            {
                for (var j = 0; j < 10; j++)
                {
                    MacroArg[i, j] = new char[Definition.MACRO_ARG_MAX_LEN + 1];
                }
            }
            MacroCntStack = new Stack<int>();
            MacroStack = new Stack<NesAsmLine>();
            MacroTbl = new Dictionary<string, NesAsmMacro>();

            // assemble.c
            IfState = new bool[256];
            IfFlag = new bool[256];

            // func.c
            FuncTbl = new Dictionary<string, NesAsmFunc>();
            FuncArg = new char[8, 10][];
            for (var i = 0; i < 8; i++)
            {
                for (var j = 0; j < 10; j++)
                {
                    FuncArg[i, j] = new char[Definition.FUNC_ARG_MAX_LEN + 1];
                }
            }
            FuncLine = new char[128];

            // expr.c
            OpStack = new Stack<OperatorType>();
            ValStack = new Stack<uint>();
            ExprStack = new Stack<ArrayPointer<char>>();

            // proc.c
            ProcTbl = new Dictionary<string, NesAsmProc>();

            // input.c
            InputFile = new NesAsmInputInfo[8];
            IncPath = new string[Definition.INC_PATH_MAX];
            InFileTextCache = new Dictionary<string, string>();

            // pcx.c
            PcxArg = new uint[8];
            PcxPal = new byte[256, 3];
            PcxPlane = new byte[128, 4];
            Tile = new NesAsmTile[256];
            TileTbl = new NesAsmTile[256];

            // CreateInstanceDictionary();
            instanceDictionary = new Dictionary<Type, ProcessorBase>();
        }

        private Dictionary<Type, ProcessorBase> instanceDictionary;

        /// <summary>
        /// Get processor instance
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public T GetProcessor<T>() where T : ProcessorBase
        {
            if (!instanceDictionary.ContainsKey(typeof(T)))
            {
                var instance = (T)Activator.CreateInstance(typeof(T), this);
                instanceDictionary[typeof(T)] = instance;
            }
            return (T)instanceDictionary[typeof(T)];
        }

    }
}
