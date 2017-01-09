using NesAsmSharp.Assembler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NesAsmSharp.Main
{
    public class NesAsmSharp
    {
        private static string programName;

        /// <summary>
        /// show assembler usage
        /// </summary>
        public static void ShowHelp()
        {
            /* display help */
            Console.Out.WriteLine("{0} [-options] [-? (for help)] infile\n", programName);
            Console.Out.WriteLine("-s/S   : show segment usage");
            Console.Out.WriteLine("-l #   : listing file output level (0-3)");
            Console.Out.WriteLine("-m     : force macro expansion in listing");
            Console.Out.WriteLine("-raw   : prevent adding a ROM header");
            Console.Out.WriteLine("-autozp: assemble auto zeropage memory access. Ex) lda ($nn),y");
            Console.Out.WriteLine("-srec  : create a Motorola S-record file");
            Console.Out.WriteLine("-e ENC : specify infile text encoding (SJIS|UTF8)");
            Console.Out.WriteLine("-watch : watch source file change and reassemble");
            Console.Out.WriteLine("-wd    : disable assembler warning");
            Console.Out.WriteLine("infile : file to be assembled");
        }

        /// <summary>
        /// デフォルトのオプションを返す
        /// </summary>
        /// <returns></returns>
        public static NesAsmOption GetDefaultOption()
        {
            /* init assembler options */
            return new NesAsmOption
            {
                ListLevel = 2,
                HeaderOpt = true,
                DeveloOpt = false,
                MListOpt = false,
                SrecOpt = false,
                RunOpt = 0,
                ScdOpt = false,
                CdOpt = false,
                MxOpt = false,
                AutoZPOpt = false
            };
        }

        /// <summary>
        /// コマンドライン引数のパース
        /// </summary>
        /// <param name="args"></param>
        public static NesAsmOption ParseOption(string[] args)
        {
            var opt = GetDefaultOption();

            // option0
            var DicOpt0 = new Dictionary<string, Action>();
            DicOpt0["-s"] = () => opt.DumpSeg = 1;
            DicOpt0["-S"] = () => opt.DumpSeg = 2;
            DicOpt0["-m"] = () => opt.MListOpt = true;
            DicOpt0["-raw"] = () => opt.HeaderOpt = false;
            DicOpt0["-srec"] = () => opt.SrecOpt = true;
            DicOpt0["-autozp"] = () => opt.AutoZPOpt = true;
            DicOpt0["-l0"] = () => opt.ListLevel = 0;
            DicOpt0["-l1"] = () => opt.ListLevel = 1;
            DicOpt0["-l2"] = () => opt.ListLevel = 2;
            DicOpt0["-l3"] = () => opt.ListLevel = 3;
            DicOpt0["-watch"] = () => opt.WatchOpt = true;
            DicOpt0["-wd"] = () => opt.WarningDisabled = true;
            DicOpt0["-?"] = () =>
            {
                ShowHelp();
                Environment.Exit(0);
            };
            // option1
            var DicOpt1 = new Dictionary<string, Func<string, bool>>();
            DicOpt1["-l"] = arg =>
            {
                int level;
                if (int.TryParse(arg, out level))
                {
                    if (level < 0) level = 0;
                    else if (level > 3) level = 3;
                    opt.ListLevel = level;
                    return true;
                }
                else
                {
                    Console.Out.WriteLine("'-l' option error");
                    return false;
                }
            };
            DicOpt1["-e"] = arg =>
            {
                var encstr = arg.ToUpper();
                switch (encstr)
                {
                case "SJIS":
                    opt.Encoding = Encoding.GetEncoding(932);
                    return true;
                case "UTF8":
                    opt.Encoding = new UTF8Encoding(false);
                    return true;
                }
                Console.Out.WriteLine("'-e' option error");
                return false;
            };

            // parse args
            var i = 0;
            while (i < args.Length)
            {
                if (DicOpt0.ContainsKey(args[i]))
                {
                    DicOpt0[args[i]]();
                }
                else if (DicOpt1.ContainsKey(args[i]))
                {
                    if (i < args.Length - 1)
                    {
                        if (!DicOpt1[args[i]](args[i + 1]))
                        {
                            ShowHelp();
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        Console.Out.WriteLine($"'{args[i]}' option error");
                        ShowHelp();
                        Environment.Exit(1);
                    }
                    i++;
                }
                else if (args[i].StartsWith("-"))
                {
                    Console.Out.WriteLine($"unknown option '{args[i]}'");
                    ShowHelp();
                    Environment.Exit(1);
                }
                else
                {
                    if (opt.InFName != null)
                    {
                        Console.Out.WriteLine("infile error");
                        ShowHelp();
                        Environment.Exit(1);
                    }
                    opt.InFName = args[i];
                }
                i++;
            }

            if (opt.InFName == null)
            {
                Console.Out.WriteLine("need infile name");
                ShowHelp();
                Environment.Exit(1);
            }

            /* auto-add file extensions */
            if (Path.GetExtension(opt.InFName).ToLower() != ".asm")
            {
                opt.InFName = opt.InFName + ".asm";
            }

            return opt;
        }

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            programName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

            var opt = ParseOption(args);
            var result = 0;

            if (opt.WatchOpt)
            {
                var watcher = new NesAsmWatcher(MachineType.MACHINE_NES, opt);
                result = watcher.Watch();
            }
            else
            {
                var assembler = AssemblerFactory.CreateAssembler(MachineType.MACHINE_NES, opt);

                result = assembler.Assemble();
            }

            Environment.Exit(result);
        }

    }
}
