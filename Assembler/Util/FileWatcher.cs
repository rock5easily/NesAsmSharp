using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NesAsmSharp.Assembler.Util
{
    /// <summary>
    /// ファイル監視を行うクラス
    /// </summary>
    public class FileWatcher
    {
        private readonly List<string> _targetFileList;
        private readonly List<FileSystemWatcher> _watcherList;
        private readonly NotifyFilters _notifyFilter;
        private readonly object _eventHandlerLock = new object();

        /// <summary>
        /// 監視するファイルの一覧、ウォッチする変更を指定してインスタンスを生成する
        /// </summary>
        /// <param name="filePaths">監視するファイルの一覧</param>
        /// <param name="notifyFilter">ウォッチする変更を表す列挙体</param>
        public FileWatcher(IEnumerable<string> filePaths, NotifyFilters notifyFilter = NotifyFilters.LastWrite)
        {
            _targetFileList = filePaths.Where(p => !string.IsNullOrEmpty(p)).Select(p => Path.GetFullPath(p).ToLower()).ToList();
            var watchDirectoryList = _targetFileList.Select(p => Path.GetDirectoryName(p)).Distinct();
            _notifyFilter = notifyFilter;

            _watcherList = new List<FileSystemWatcher>();
            foreach (var dir in watchDirectoryList)
            {
                var watcher = CreateFileSystemWatcher(dir);
                _watcherList.Add(watcher);
            }
        }

        /// <summary>
        /// ウォッチする変更を指定してインスタンスを生成する
        /// </summary>
        /// <param name="notifyFilter">ウォッチする変更を表す列挙体</param>
        public FileWatcher(NotifyFilters notifyFilter = NotifyFilters.LastWrite)
        {
            _targetFileList = new List<string>();
            _watcherList = new List<FileSystemWatcher>();
            this._notifyFilter = notifyFilter;
        }

        /// <summary>
        /// FileSystemWatcherオブジェクトを生成する
        /// </summary>
        /// <param name="dir">監視ディレクトリ</param>
        /// <returns></returns>
        private FileSystemWatcher CreateFileSystemWatcher(string dir)
        {
            var watcher = new FileSystemWatcher(dir)
            {
                NotifyFilter = _notifyFilter,
                IncludeSubdirectories = false,
            };
            watcher.Changed += this.ChangedEventHandler;
            watcher.Created += this.CreatedEventHandler;
            watcher.Deleted += this.DeletedEventHandler;
            watcher.Renamed += this.RenamedEventHandler;

            return watcher;
        }

        private bool enableRaisingEvents = false;
        /// <summary>
        /// 変更を検知した時にイベントを発生するかどうかを示す値を取得または設定する
        /// </summary>
        public bool EnableRaisingEvents
        {
            get
            {
                lock (_watcherList)
                {
                    return enableRaisingEvents;
                }
            }
            set
            {
                lock (_watcherList)
                {
                    enableRaisingEvents = value;
                    _watcherList.ForEach(watcher => watcher.EnableRaisingEvents = value);
                }
            }
        }

        /// <summary>
        /// 監視対象ファイル一覧を更新する
        /// </summary>
        /// <param name="filePaths"></param>
        public void UpdateTargetList(IEnumerable<string> filePaths)
        {
            lock (_watcherList)
            {
                var enable = EnableRaisingEvents;
                _watcherList.ForEach(watcher => watcher.EnableRaisingEvents = false);

                _targetFileList.Clear();
                _targetFileList.AddRange(filePaths.Where(p => !string.IsNullOrEmpty(p)).Select(p => Path.GetFullPath(p).ToLower()));
                var watchDirList = _targetFileList.Select(p => Path.GetDirectoryName(p)).Distinct();
                var diffWatchDirList = new List<string>(watchDirList);
                var newWatcherList = new List<FileSystemWatcher>();

                foreach (var w in _watcherList)
                {
                    if (watchDirList.Contains(w.Path))
                    {
                        newWatcherList.Add(w);
                        diffWatchDirList.Remove(w.Path);
                    }
                    else
                    {
                        w.Dispose();
                    }
                }

                foreach (var d in diffWatchDirList)
                {
                    newWatcherList.Add(CreateFileSystemWatcher(d));
                }

                _watcherList.Clear();
                _watcherList.AddRange(newWatcherList);
                if (enable == true)
                {
                    _watcherList.ForEach(watcher => watcher.EnableRaisingEvents = enable);
                }
            }
        }

        /// <summary>
        /// 監視インスタンスの状態を取得
        /// </summary>
        /// <returns></returns>
        public string GetWatcherInfo()
        {
            var sb = new StringBuilder();
            lock (_watcherList)
            {
                sb.AppendLine($"FileSystemWatcher Instances: {_watcherList.Count}");
                sb.AppendLine("Watching path list:");
                var i = 1;
                foreach (var w in _watcherList)
                {
                    sb.AppendLine($"#[{i++}] {w.Path}");
                }
            }
            return sb.ToString();
        }

        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event FileSystemEventHandler Deleted;
        public event RenamedEventHandler Renamed;

        private void ChangedEventHandler(object sender, FileSystemEventArgs e)
        {
            var fullPath = e.FullPath.ToLower();
            lock (_watcherList)
            {
                if (!_targetFileList.Contains(fullPath)) return;
            }
            lock (_eventHandlerLock) Changed?.Invoke(this, e);
        }

        private void CreatedEventHandler(object sender, FileSystemEventArgs e)
        {
            var fullPath = e.FullPath.ToLower();
            lock (_watcherList)
            {
                if (!_targetFileList.Contains(fullPath)) return;
            }
            lock (_eventHandlerLock) Created?.Invoke(this, e);
        }

        private void DeletedEventHandler(object sender, FileSystemEventArgs e)
        {
            var fullPath = e.FullPath.ToLower();
            lock (_watcherList)
            {
                if (!_targetFileList.Contains(fullPath)) return;
            }
            lock (_eventHandlerLock) Deleted?.Invoke(this, e);
        }

        private void RenamedEventHandler(object sender, RenamedEventArgs e)
        {
            var fullPath = e.FullPath.ToLower();
            var oldFullPath = e.OldFullPath.ToLower();
            lock (_watcherList)
            {
                if (!_targetFileList.Contains(fullPath) && !_targetFileList.Contains(oldFullPath)) return;
            }
            lock (_eventHandlerLock) Renamed?.Invoke(this, e);
        }

        public void Dispose()
        {
            lock (_watcherList)
            {
                _watcherList.ForEach(watcher => watcher.Dispose());
                _watcherList.Clear();
                _targetFileList.Clear();
            }
        }
    }
}
