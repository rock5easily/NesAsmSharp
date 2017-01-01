using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class InputProcessor : ProcessorBase
    {
        public InputProcessor(NesAsmContext ctx) : base(ctx)
        {
            InitPath();
        }

        /// <summary>
        /// init the include path
        /// </summary>
        private void InitPath()
        {
            var path = Environment.GetEnvironmentVariable(ctx.Machine.IncludeEnv);

            if (path == null) return;

            var pl = path.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))
                    .Take(ctx.IncPath.Length).ToArray();

            for (var i = 0; i < pl.Length; i++)
            {
                var p = pl[i];

                if (p.Last() != Path.DirectorySeparatorChar)
                {
                    p += Path.DirectorySeparatorChar;
                }
                ctx.IncPath[i] = p;
            }
        }

        /// <summary>
        /// read and format an input line.
        /// </summary>
        /// <returns></returns>
        public int ReadLine()
        {
            int data_ptr;
            string arg, num;
            int j, n;
            int i;      /* pointer into prlnbuf */
            int c;      /* current character		*/
            int temp;   /* temp used for line number conversion */

        start:
            for (i = 0; i < Definition.LAST_CH_POS; i++)
            {
                ctx.PrLnBuf[i] = ' ';
            }

            /* if 'expand_macro' is set get a line from macro buffer instead */
            if (ctx.IsExpandMacro)
            {
                if (ctx.MLPtr == null)
                {
                    while (ctx.MLPtr == null)
                    {
                        ctx.MIdx--;
                        ctx.MLPtr = ctx.MStack.Pop();
                        ctx.MCounter = ctx.MCntStack.Pop();
                        if (ctx.MIdx == 0)
                        {
                            ctx.MLPtr = null;
                            ctx.IsExpandMacro = false;
                            break;
                        }
                    }
                }

                /* expand line */
                if (ctx.MLPtr != null)
                {
                    i = Definition.SFIELD;
                    var data = ctx.MLPtr.Data.ToNullTerminatedCharArray();
                    data_ptr = 0;
                    for (;;)
                    {
                        c = data[data_ptr++];
                        if (c == '\0') break;
                        if (c != '\\')
                        {
                            ctx.PrLnBuf[i++] = (char)c;
                        }
                        else
                        {
                            c = data[data_ptr++];
                            ctx.PrLnBuf[i] = '\0';

                            /* \@ */
                            if (c == '@')
                            {
                                n = 5;
                                arg = ctx.MCounter.ToString("D5");
                            }
                            /* \# */
                            else if (c == '#')
                            {
                                for (j = 9; j > 0; j--)
                                {
                                    if ((ctx.MArg[ctx.MIdx, j - 1][0] == 0)) break;
                                }
                                n = 1;
                                arg = j.ToString();
                            }

                            /* \?1 - \?9 */
                            else if (c == '?')
                            {
                                c = data[data_ptr++];
                                if (c >= '1' && c <= '9')
                                {
                                    n = 1;
                                    num = ((int)macroPr.MacroGetArgType(ctx.MArg[ctx.MIdx, c - '1'].ToStringFromNullTerminated())).ToString();
                                    arg = num;
                                }
                                else
                                {
                                    outPr.Error("Invalid macro argument index!");
                                    return (-1);
                                }
                            }

                            /* \1 - \9 */
                            else if (c >= '1' && c <= '9')
                            {
                                j = c - '1';
                                arg = ctx.MArg[ctx.MIdx, j].ToStringFromNullTerminated();
                                n = arg.Length;
                            }

                            /* unknown macro special command */
                            else
                            {
                                outPr.Error("Invalid macro argument index!");
                                return (-1);
                            }

                            /* check for line overflow */
                            if ((i + n) >= Definition.LAST_CH_POS - 1)
                            {
                                outPr.Error("Invalid line length!");
                                return (-1);
                            }

                            /* copy macro string */
                            for (var k = 0; k < n; k++)
                            {
                                ctx.PrLnBuf[i + k] = arg[k];
                            }
                            i += n;
                        }
                        if (i >= Definition.LAST_CH_POS - 1)
                        {
                            i = Definition.LAST_CH_POS - 1;
                        }
                    }
                    ctx.PrLnBuf[i] = '\0';
                    ctx.MLPtr = ctx.MLPtr.Next;
                    return 0;
                }
            }

            /* put source line number into prlnbuf */
            i = 4;
            temp = ++ctx.SLNum;
            while (temp != 0)
            {
                ctx.PrLnBuf[i--] = (char)(temp % 10 + '0');
                temp /= 10;
            }

            /* get a line */
            i = Definition.SFIELD;

            c = opt.InFp.Read();
            if (c == -1)
            {
                if (CloseInputFile() != 0) return (-1);
                goto start;
            }
            for (;;)
            {
                /* check for the end of line */
                if (c == '\r')
                {
                    c = opt.InFp.Peek();
                    if (c == '\n' || c == -1)
                    {
                        opt.InFp.Read();
                        break;
                    }
                    break;
                }
                if (c == '\n' || c == -1) break;

                /* store char in the line buffer */
                ctx.PrLnBuf[i] = (char)c;
                i += (i < Definition.LAST_CH_POS) ? 1 : 0;

                /* expand tab char to space */
                if (c == '\t')
                {
                    ctx.PrLnBuf[--i] = ' ';
                    i += (8 - ((i - Definition.SFIELD) % 8));
                }

                /* get next char */
                c = opt.InFp.Read();
            }
            ctx.PrLnBuf[i] = '\0';
            return 0;
        }

        /// <summary>
        /// open input files - up to 7 levels.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int OpenInputFile(string name)
        {
            StreamReader sr;
            int p;
            int i;

            /* only 7 nested input files */
            if (ctx.InFileNum == 7)
            {
                outPr.Error("Too many include levels, max. 7!");
                return 1;
            }

            /* backup current input file infos */
            if (ctx.InFileNum != 0)
            {
                ctx.InputFile[ctx.InFileNum].LNum = ctx.SLNum;
                ctx.InputFile[ctx.InFileNum].Fp = opt.InFp;
            }

            /* get a copy of the file name */
            var temp = name;

            /* auto add the .asm file extension */
            if ((p = temp.LastIndexOf('.')) >= 0)
            {
                if (temp.IndexOf(Path.DirectorySeparatorChar, p) >= 0)
                {
                    temp += ".asm";
                }
            }
            else
            {
                temp += ".asm";
            }

            /* check if this file is already opened */
            if (ctx.InFileNum != 0)
            {
                for (i = 1; i < ctx.InFileNum; i++)
                {
                    if (ctx.InputFile[i] != null && ctx.InputFile[i].Name == temp)
                    {
                        outPr.Error("Repeated include file!");
                        return 1;
                    }
                }
            }

            if (!File.Exists(temp))
            {
                outPr.Error($"'{temp}' not found!");
                return 1;
            }

            /* open the file */
            try
            {
                sr = new StreamReader(temp, opt.Encoding);
            }
            catch (Exception e)
            {
                return -1;
            }

            /* update input file infos */
            opt.InFp = sr;
            ctx.SLNum = 0;
            ctx.InFileNum++;
            var inputInfo = new NesAsmInputInfo();
            inputInfo.Fp = sr;
            inputInfo.IfLevel = ctx.IfLevel;
            inputInfo.Name = temp;
            ctx.InputFile[ctx.InFileNum] = inputInfo;

            if ((ctx.Pass == PassFlag.LAST_PASS) && (opt.XListOpt) && (opt.ListLevel > 0))
            {
                opt.LstFp.WriteLine("#[{0}]   {1}", ctx.InFileNum, ctx.InputFile[ctx.InFileNum].Name);
            }

            /* ok */
            return 0;
        }

        /// <summary>
        /// close an input file, return -1 if no more files in the stack.
        /// </summary>
        /// <returns></returns>
        public int CloseInputFile()
        {
            if (ctx.ProcPtr != null)
            {
                outPr.FatalError("Incomplete PROC!");
                return (-1);
            }
            if (ctx.InMacro)
            {
                outPr.FatalError("Incomplete MACRO definition!");
                return (-1);
            }
            if (ctx.InputFile[ctx.InFileNum].IfLevel != ctx.IfLevel)
            {
                outPr.FatalError("Incomplete IF/ENDIF statement!");
                return (-1);
            }
            if (ctx.InFileNum <= 1) return (-1);

            opt.InFp.Close();
            ctx.InputFile[ctx.InFileNum--] = null;
            ctx.InFileError = -1;
            ctx.SLNum = ctx.InputFile[ctx.InFileNum].LNum;
            opt.InFp = ctx.InputFile[ctx.InFileNum].Fp;
            if ((ctx.Pass == PassFlag.LAST_PASS) && (opt.XListOpt) && (opt.ListLevel > 0))
            {
                opt.LstFp.WriteLine("#[{0}]   {1}", ctx.InFileNum, ctx.InputFile[ctx.InFileNum].Name);
            }

            /* ok */
            return 0;
        }

        /// <summary>
        /// カレントディレクトリ、IncPathで指定されたディレクトリの順にファイルを探す
        /// </summary>
        /// <param name="name"></param>
        /// <returns>見つかったファイルのパス</returns>
        public string FindFileByIncPath(string name, List<string> checkedPathHistory = null)
        {
            var fullPath = Path.GetFullPath(name);
            checkedPathHistory?.Add(fullPath);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            if (fullPath == name) return null;

            foreach (var path in ctx.IncPath)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    fullPath = Path.GetFullPath(Path.Combine(path, name));

                    checkedPathHistory?.Add(fullPath);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
            return null;
        }
    }
}
