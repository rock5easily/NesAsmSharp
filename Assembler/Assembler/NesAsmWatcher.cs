using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NesAsmSharp.Assembler
{
    public class NesAsmWatcher
    {
        private IAssembler currentAssembler;
        private IList<string> targetList;
        private readonly FileWatcher watcher;
        private readonly MachineType macType;
        private readonly NesAsmOption opt;
        private int latestResult;
        private readonly object lockObject = new object();
        private DateTime lastAssembleDateTime;
        /// <summary>
        /// アセンブルが完了してから次のアセンブルを行うまでに必要な最低待ち時間(ミリ秒)
        /// この時間より短い間隔で行われたアセンブル要求はスキップされる
        /// </summary>
        private readonly int intervalMiliseconds = 500;

        public NesAsmWatcher(MachineType macType, NesAsmOption opt)
        {
            this.macType = macType;
            this.opt = opt;

            // Initialize watcher
            watcher = new FileWatcher();
            watcher.Changed += FileChangedEventHandler;
        }

        public int Watch()
        {
            // first assmeble
            ReassembleAndUpdateTargetList();
            lastAssembleDateTime = DateTime.Now;

            // start watching
            watcher.EnableRaisingEvents = true;
            while (true)
            {
                var keyInfo = Console.ReadKey(true);

                switch (keyInfo.Key)
                {
                case ConsoleKey.Q:
                    goto breakwhile;
                case ConsoleKey.L:
                    ShowTargetFileList();
                    break;
                case ConsoleKey.H:
                case ConsoleKey.Enter:
                    ShowHelp();
                    break;
                case ConsoleKey.A:
                    ForceReassemble();
                    break;
                case ConsoleKey.W:
                    ShowWatcherInfo();
                    break;
                default:
                    break;
                }
            }
        breakwhile:
            // stop watching
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();

            lock (lockObject)
            {
                Console.Out.WriteLine("Quit");
                return latestResult;
            }
        }

        private void ShowTargetFileList()
        {
            lock (lockObject)
            {
                Console.Out.WriteLine("");
                var i = 1;
                foreach (var target in targetList)
                {
                    Console.Out.WriteLine($"#[{i++}] {target}");
                }
                Console.Out.WriteLine("");
                Console.Out.WriteLine("Waiting for source file change...");
            }
        }

        [Conditional("DEBUG")]
        private void ShowWatcherInfo()
        {
            var info = watcher.GetWatcherInfo();
            lock (lockObject)
            {
                Console.Out.WriteLine("");
                Console.Out.WriteLine("----------------------");
                Console.Out.WriteLine("FileSystemWatcher info");
                Console.Out.WriteLine("----------------------");
                Console.Out.Write(info);
                Console.Out.WriteLine("");
                Console.Out.WriteLine("Waiting for source file change...");
            }
        }

        private void ShowHelp()
        {
            lock (lockObject)
            {
                Console.Out.WriteLine("");
                Console.Out.WriteLine("-----------------------------");
                Console.Out.WriteLine("NesAsmSharp file watcher menu");
                Console.Out.WriteLine("-----------------------------");
                Console.Out.WriteLine("  H: Show this help");
                Console.Out.WriteLine("  L: Show target file list");
#if DEBUG
                Console.Out.WriteLine("  W: Show watching info");
#endif
                Console.Out.WriteLine("  A: Force reassemble");
                Console.Out.WriteLine("  Q: Quit");
                Console.Out.WriteLine("");
                Console.Out.WriteLine("Waiting for source file change...");
            }
        }

        private void FileChangedEventHandler(object sender, FileSystemEventArgs e)
        {
            lock (lockObject)
            {
                var span = DateTime.Now - lastAssembleDateTime;
                if (span.TotalMilliseconds < intervalMiliseconds)
                {
                    // Console.Out.WriteLine("Too short interval, skip reassemble!");
                }
                else
                {
                    var changedFile = e.Name;
                    Console.Out.WriteLine($"[{DateTime.Now.ToString()}] '{Path.GetFileName(changedFile)}' update detected! Reassemble!\n");
                    ReassembleAndUpdateTargetList();
                }
            }
        }

        private void ForceReassemble()
        {
            lock (lockObject)
            {
                var span = DateTime.Now - lastAssembleDateTime;
                if (span.TotalMilliseconds < intervalMiliseconds)
                {
                    // Console.Out.WriteLine("Too short interval, skip reassemble!");
                }
                else
                {
                    Console.Out.WriteLine($"Reassemble forcibly!\n");
                    ReassembleAndUpdateTargetList();
                }
            }
        }

        private void ReassembleAndUpdateTargetList()
        {
            // reassemble
            currentAssembler = AssemblerFactory.CreateAssembler(macType, opt);
            latestResult = currentAssembler.Assemble();
            // update target list
            targetList = currentAssembler.AssembledFileList;
            watcher.UpdateTargetList(targetList);
            // update last assembled datetime
            lastAssembleDateTime = DateTime.Now;
            Console.Out.WriteLine("");
            Console.Out.WriteLine("Waiting for source file change...");
        }
    }
}
