using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler
{
    public interface IAssembler
    {
        /// <summary>
        /// Assemble()の結果が成功しているかを返す
        /// </summary>
        bool AssembleSuccess { get; }
        /// <summary>
        /// Assemble()を実行済みかを返す
        /// </summary>
        bool Executed { get; }
        /// <summary>
        /// アセンブル結果のbyte配列
        /// </summary>
        byte[] ResultBinary { get; }
        /// <summary>
        /// アセンブル結果のMap情報のbyte配列
        /// </summary>
        byte[] ResultMap { get; }
        /// <summary>
        /// アセンブルを実行する
        /// </summary>
        /// <returns></returns>
        int Assemble();
        /// <summary>
        /// アセンブルに使用されたファイル一覧を返す
        /// </summary>
        IList<string> AssembledFileList { get; }
    }
}
