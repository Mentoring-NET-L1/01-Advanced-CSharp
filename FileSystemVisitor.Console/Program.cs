using System.IO;

namespace FileSystemVisitor.Console
{
    class Program
    {
        private static void Log(string action, FileSystemVisitorEventArgs args)
        {
            System.Console.WriteLine($"{action}: {args.Path}");
        }

        static void Main(string[] args)
        {
            var fsv = new FileSystemVisitor((path) => Path.GetExtension(path) == ".cs");

            int counter = 0;

            fsv.Start += delegate { System.Console.WriteLine("Start"); };
            fsv.Finish += delegate { System.Console.WriteLine("Finish"); };
            //fsv.FileFinded += (sender, e) => { if (counter++ == 3) e.StopSearch = true; Log("FileFinded", e); };
            //fsv.FileFinded += (sender, e) => { e.StopSearch = true; Log("FileFinded", e); };
            fsv.DirectoryFinded += (sender, e) => { Log("DirectoryFinded", e); };
            fsv.FilteredFileFinded += (sender, e) => { Log("FilteredFileFinded", e); };
            fsv.FilteredDirectoryFinded += (sender, e) => { Log("FilteredDirectoryFinded", e); };

            //foreach (var file in fsv.Search(@"D:\Epam\"))
            foreach (var file in fsv.Search(@"c:\Windows\System32\"))
            {
                System.Console.WriteLine(file);
            }

            //foreach(var file in fsv.Search(@"D:\Epam\"))
            //{
            //    System.Console.WriteLine(file);
            //}

            System.Console.ReadLine();
        }
    }
}
