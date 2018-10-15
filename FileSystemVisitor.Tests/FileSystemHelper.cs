using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSystemVisitor.Tests
{
    internal static class FileSystemHelper
    {
        public static bool IsAllFiles(IEnumerable<string> pathes)
        {
            if (pathes == null)
                throw new ArgumentNullException(nameof(pathes));

            return pathes.All(File.Exists);
        }

        public static bool IsAllDirectories(IEnumerable<string> pathes)
        {
            if (pathes == null)
                throw new ArgumentNullException(nameof(pathes));

            return pathes.All(Directory.Exists);
        }
    }
}
