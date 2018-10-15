using System;

namespace FileSystemVisitor
{
    public class FileSystemVisitorEventArgs : EventArgs
    {
        public FileSystemVisitorEventArgs(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public bool StopSearch { get; set; }

        public bool ExcludeFileSystemEntry { get; set; }
    }
}
