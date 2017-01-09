using System;
using System.IO;
using System.Text;

namespace NesAsmSharp.Assembler
{
    public class NesAsmOption
    {
        /* variables */
        public string InFName { get; set; } // file names, input
        public string OutFName { get; set; } // output
        public string BinFName { get; set; } // binary
        public string LstFName { get; set; } // listing
        public string PrgName { get; set; } // program name
        public Encoding Encoding { get; set; } // source file text encoding
        public int DumpSeg { get; set; }
        /// <summary>
        /// Develo option (PCE only)
        /// </summary>
        public bool DeveloOpt { get; set; }
        public bool HeaderOpt { get; set; }
        public bool SrecOpt { get; set; }
        public int RunOpt { get; set; }
        /// <summary>
        /// SCD option (PCE only)
        /// </summary>
        public bool ScdOpt { get; set; }
        /// <summary>
        /// CD option (PCE only)
        /// </summary>
        public bool CdOpt { get; set; }
        /// <summary>
        /// MX option (PCE only)
        /// </summary>
        public bool MxOpt { get; set; }
        public bool MListOpt { get; set; } // macro listing main flag
        public int ListLevel { get; set; } // output level
        public bool AutoZPOpt { get; set; } // auto zeropage mode for NES ONLY
        /// <summary>
        /// 標準出力先を変更する場合に設定する
        /// </summary>
        public TextWriter StdOut { get; set; }
        /// <summary>
        /// 標準エラー出力先を変更する場合に設定する
        /// </summary>
        public TextWriter StdErr { get; set; }

        /// <summary>
        /// バイナリファイル出力を行わない(テスト用オプション)
        /// </summary>
        public bool OutputBinDisabled { get; set; }
        /// <summary>
        /// リストファイル出力を行わない(テスト用オプション)
        /// </summary>
        public bool OutputLstDisabled { get; set; }
        /// <summary>
        /// リスト出力用のStreamWriterを明示的に指定する場合に設定(テスト用オプション)
        /// </summary>
        public StreamWriter LstStreamWriter { get; set; }
        /// <summary>
        /// ソースファイルの変更を監視して再アセンブルをかける
        /// </summary>
        public bool WatchOpt { get; set; }
        /// <summary>
        /// アセンブラの警告を無効にする
        /// </summary>
        public bool WarningDisabled { get; set; }

        public NesAsmOption()
        {
            // set default encoding
            this.Encoding = Encoding.Default;
            this.StdOut = Console.Out;
            this.StdErr = Console.Error;
        }

        public NesAsmOption Clone()
        {
            var cloned = this.MemberwiseClone() as NesAsmOption;
            return cloned;
        }
    }
}
