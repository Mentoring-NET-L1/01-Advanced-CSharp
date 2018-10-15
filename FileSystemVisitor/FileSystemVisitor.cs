using System;
using System.Collections.Generic;
using System.IO;

namespace FileSystemVisitor
{
    public class FileSystemVisitor
    {
        private static readonly Predicate<string> DefaultFilter = (path) => true;

        private bool _stopSearch;
        private bool _excludeFileSystemEntry;
        private Predicate<string> _filter;

        public FileSystemVisitor()
            : this(DefaultFilter)
        {
        }

        public FileSystemVisitor(Predicate<string> filter)
        {
            _filter = filter ?? DefaultFilter;
        }

        public event EventHandler<EventArgs> Start;

        public event EventHandler<EventArgs> Finish;

        public event EventHandler<FileSystemVisitorEventArgs> FileFinded;

        public event EventHandler<FileSystemVisitorEventArgs> FilteredFileFinded;

        public event EventHandler<FileSystemVisitorEventArgs> DirectoryFinded;

        public event EventHandler<FileSystemVisitorEventArgs> FilteredDirectoryFinded;

        public IEnumerable<string> Search(string rootDirectory)
        {
            if (rootDirectory == null)
                throw new ArgumentNullException(nameof(rootDirectory));
            if (!Directory.Exists(rootDirectory))
                throw new DirectoryNotFoundException($"Can't find \"{rootDirectory}\" directory.");

            _stopSearch = false;
            _excludeFileSystemEntry = false;

            return Find(rootDirectory);
        }

        protected virtual void OnStart(EventArgs args)
        {
            Start?.Invoke(this, args);
        }

        protected virtual void OnFinish(EventArgs args)
        {
            Finish?.Invoke(this, args);
        }

        protected virtual void OnFileFinded(FileSystemVisitorEventArgs args)
        {
            TriggerEvent(FileFinded, args);
        }

        protected virtual void OnDirectoryFinded(FileSystemVisitorEventArgs args)
        {
            TriggerEvent(DirectoryFinded, args);
        }

        protected virtual void OnFilteredFileFinded(FileSystemVisitorEventArgs args)
        {
            TriggerEvent(FilteredFileFinded, args);
        }

        protected virtual void OnFilteredDirectoryFinded(FileSystemVisitorEventArgs args)
        {
            TriggerEvent(FilteredDirectoryFinded, args);
        }

        private void TriggerEvent(EventHandler<FileSystemVisitorEventArgs> eventHandler, FileSystemVisitorEventArgs args)
        {
            eventHandler?.Invoke(this, args);
            _stopSearch = args.StopSearch;
            _excludeFileSystemEntry = args.ExcludeFileSystemEntry;
        }

        private IEnumerable<string> Find(string directoryPath, int depth = 0)
        {
            if (depth == 0)
                OnStart(EventArgs.Empty);

            foreach (var file in Directory.EnumerateFiles(directoryPath))
            {
                OnFileFinded(new FileSystemVisitorEventArgs(file));
                if (_stopSearch) goto Finish;
                if (_excludeFileSystemEntry) continue;

                if (_filter(file))
                {
                    OnFilteredFileFinded(new FileSystemVisitorEventArgs(file));
                    if (_stopSearch) goto Finish;
                    if (_excludeFileSystemEntry) continue;

                    yield return file;
                }
            }

            foreach (var directory in Directory.EnumerateDirectories(directoryPath))
            {
                OnDirectoryFinded(new FileSystemVisitorEventArgs(directory));
                if (_stopSearch) goto Finish;

                if (!_excludeFileSystemEntry && _filter(directory))
                {
                    OnFilteredDirectoryFinded(new FileSystemVisitorEventArgs(directory));
                    if (_stopSearch) goto Finish;
                    if (!_excludeFileSystemEntry) yield return directory;
                }

                foreach (var file in Find(directory, depth + 1))
                {
                    yield return file;
                }
            }

            Finish:
            if (depth == 0)
                OnFinish(EventArgs.Empty);
        }
    }
}
