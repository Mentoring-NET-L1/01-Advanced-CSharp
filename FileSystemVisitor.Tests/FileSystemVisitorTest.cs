using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DirectoryHelper = System.IO.Directory;

namespace FileSystemVisitor.Tests
{
    [TestClass]
    public class FileSystemVisitorTest
    {
        private const string TestRootDirectoryName = "test-root-directory";

        private static readonly Comparison<string> StringComparison = (s1, s2) => string.Compare(s1, s2, false);

        private static string[] AllTestFileSystemEntries;

        [ClassInitialize]
        public static void CreateTestFilesAndDirectories(TestContext context)
        {

            var rootDirectory = new Directory
            {
                Name = TestRootDirectoryName,
                Files = Array.Empty<string>(),
                Directories = new Directory[]
                {
                    new Directory
                    {
                        Name = "first-dir",
                        Files = new string[]
                        {
                            "file1.txt",
                            "file2.cs",
                            "file3.exe",
                        },
                        Directories = new Directory[]
                        {
                            new Directory
                            {
                                Name = "sub-dir1.cs",
                                Files = new string[]
                                {
                                    "sub-file1.pdb",
                                    "sub-file2.pdb",
                                    "sub-file3.pdb",
                                },
                                Directories = Array.Empty<Directory>(),
                            },
                            new Directory
                            {
                                Name = "sub-dir2",
                                Files = new string[]
                                {
                                    "sub2-file1.pdb",
                                    "sub2-file2.xlsx",
                                    "sub2-file3.xlsx",
                                },
                                Directories = Array.Empty<Directory>(),
                            },
                            new Directory
                            {
                                Name = "sub-dir3.pdb",
                                Files = Array.Empty<string>(),
                                Directories = Array.Empty<Directory>(),
                            },
                        }
                    },
                }
            };

            CreateDirectory(rootDirectory);

            AllTestFileSystemEntries = DirectoryHelper
                .EnumerateFileSystemEntries(TestRootDirectoryName, "*", SearchOption.AllDirectories)
                .ToArray();
        }

        [ClassCleanup]
        public static void DeleteTestFilesAndDirectories()
        {
            DirectoryHelper.Delete(TestRootDirectoryName, true);
        }

        private static void CreateDirectory(Directory directory, string path = ".")
        {
            var directoryPath = Path.Combine(path, directory.Name);
            var directoryInfo = DirectoryHelper.CreateDirectory(directoryPath);

            foreach (var file in directory.Files)
            {
                File.Create(Path.Combine(directoryPath, file))?.Dispose();
            }

            foreach (var subDirectory in directory.Directories)
            {
                CreateDirectory(subDirectory, directoryPath);
            }
        }

        #region Invalid Input

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Search_NullDirectory_Exception()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            // Act
            fsv.Search(null);

            // Assert
            // throw ArgumentNullException
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("+sadadaw4/sad/as/ds")]
        [DataRow("C:/sad//as/ds")]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void Search_InvalidDirectory_Exception(string rootDirectory)
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            // Act
            fsv.Search(rootDirectory);

            // Assert
            // throw DirectoryNotFoundException
        }

        [TestMethod]
        public void Search_NullFilter_FindAllFilesAndDirectories()
        {
            // Arrange
            var fsv = new FileSystemVisitor(null);

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(ArrayHelper.Compare(
                AllTestFileSystemEntries,
                findedEntries,
                StringComparison)
            );
        }

        #endregion

        #region Search Result

        [TestMethod]
        public void Search_NoFilter_FindAllFilesAndDirectories()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(ArrayHelper.Compare(
                AllTestFileSystemEntries,
                findedEntries,
                StringComparison)
            );
        }

        [DataTestMethod]
        [DataRow(".cs")]    // Existing exctension
        [DataRow(".xxx")]   // Missing exctension
        public void Search_ExistingFileExtensionFilter_FindAllFilesWithExtension(string extension)
        {
            // Arrange
            Predicate<string> extensionFilter = (path) => Path.GetExtension(path) == extension;
            var fsv = new FileSystemVisitor(extensionFilter);

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(ArrayHelper.Compare(
                AllTestFileSystemEntries.Where((path) => extensionFilter(path)).ToArray(),
                findedEntries,
                StringComparison)
            );
        }

        [DataTestMethod]
        [DataRow("sub")]    // Existing substring
        [DataRow("12")]     // Missing substring
        public void Search_ContainsSubstringFilter_FindAllEntitiesWithSubstring(string substring)
        {
            // Arrange
            Predicate<string> extensionFilter = (path) => path.Contains(substring);
            var fsv = new FileSystemVisitor(extensionFilter);

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(ArrayHelper.Compare(
                AllTestFileSystemEntries.Where((path) => extensionFilter(path)).ToArray(),
                findedEntries,
                StringComparison)
            );
        }

        #endregion

        #region Event Rising

        [TestMethod]
        public void Search_StartEvent_RiseOneEvent()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var countOfStartEventRise = 0;
            fsv.Start += (sender, args) => { countOfStartEventRise++; };

            // Act
            fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.AreEqual(1, countOfStartEventRise);
        }

        [TestMethod]
        public void Search_StartEvent_RiseStartEventBeforOthers()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var risedEvents = new StringBuilder(8);
            fsv.Start += (sender, args) => { risedEvents.Append('1'); };

            EventHandler<FileSystemVisitorEventArgs> notStartEvent = (sender, args) => { risedEvents.Append('0'); };
            fsv.Finish += (sender, args) => { risedEvents.Append('0'); };
            fsv.FileFinded += notStartEvent;
            fsv.DirectoryFinded += notStartEvent;
            fsv.FilteredFileFinded += notStartEvent;
            fsv.FilteredDirectoryFinded += notStartEvent;

            // Act
            fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.AreEqual('1', risedEvents.ToString().First());
        }

        [TestMethod]
        public void Search_FinishEvent_RiseOneEvent()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var countOfFinishEventRise = 0;
            fsv.Finish += (sender, args) => { countOfFinishEventRise++; };

            // Act
            fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.AreEqual(1, countOfFinishEventRise);
        }

        [TestMethod]
        public void Search_FinishEvent_RiseStartEventBeforOthers()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var risedEvents = new StringBuilder(8);
            fsv.Finish += (sender, args) => { risedEvents.Append('1'); };

            EventHandler<FileSystemVisitorEventArgs> notStartEvent = (sender, args) => { risedEvents.Append('0'); };
            fsv.Start += (sender, args) => { risedEvents.Append('0'); };
            fsv.FileFinded += notStartEvent;
            fsv.DirectoryFinded += notStartEvent;
            fsv.FilteredFileFinded += notStartEvent;
            fsv.FilteredDirectoryFinded += notStartEvent;

            // Act
            fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.AreEqual('1', risedEvents.ToString().Last());
        }

        [TestMethod]
        public void Search_FileFindedWithFalseFilter_RiseEventForEachFileInDirectory()
        {
            // Arrange
            var fsv = new FileSystemVisitor((path) => false);

            var findedFiles = new List<string>();
            fsv.FileFinded += (sender, args) => { findedFiles.Add(args.Path); };

            // Act
            fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            var expectedEntries = DirectoryHelper.EnumerateFiles(TestRootDirectoryName, "*", SearchOption.AllDirectories).ToArray();
            Assert.IsTrue(ArrayHelper.Compare(
                expectedEntries,
                findedFiles.ToArray(),
                StringComparison)
            );
        }

        [TestMethod]
        public void Search_DirectoryFindedWithFalseFilter_RiseEventForEachSubdirectoryInDirectory()
        {
            // Arrange
            var fsv = new FileSystemVisitor((path) => false);

            var findedDirectories = new List<string>();
            fsv.DirectoryFinded += (sender, args) => { findedDirectories.Add(args.Path); };

            // Act
            fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            var expectedEntries = DirectoryHelper.EnumerateDirectories(TestRootDirectoryName, "*", SearchOption.AllDirectories).ToArray();
            Assert.IsTrue(ArrayHelper.Compare(
                expectedEntries,
                findedDirectories.ToArray(),
                StringComparison)
            );
        }

        [DataTestMethod]
        [DataRow(".pdb")]    // Existing exctension
        [DataRow(".yy")]     // Missing exctension
        public void Search_FilteredFileFindedWithExtensionFilter_RiseEventForEachFilteredFileInDirectory(string extension)
        {
            // Arrange
            var fsv = new FileSystemVisitor((path) => Path.GetExtension(path) == extension);

            var filteredFiles = new List<string>();
            fsv.FilteredFileFinded += (sender, args) => { filteredFiles.Add(args.Path); };

            // Act
            fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            var expectedEntries = DirectoryHelper.EnumerateFiles(TestRootDirectoryName, $"*{extension}", SearchOption.AllDirectories).ToArray();
            Assert.IsTrue(ArrayHelper.Compare(
                expectedEntries,
                filteredFiles.ToArray(),
                StringComparison)
            );
        }

        [DataTestMethod]
        [DataRow("pdb")]    // Existing substring which matches file extension
        [DataRow("sub")]    // Existing substring
        [DataRow("iii")]    // Missing substring
        public void Search_FilteredDirectoryFindedWithSubstringFilter_RiseEventForEachFilteredSubdirectoryInDirectory(string substring)
        {
            // Arrange
            var fsv = new FileSystemVisitor((path) => path.Contains(substring));

            var filteredDirectories = new List<string>();
            fsv.FilteredFileFinded += (sender, args) => { filteredDirectories.Add(args.Path); };

            // Act
            fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            var expectedEntries = DirectoryHelper.EnumerateFiles(TestRootDirectoryName, $"*{substring}*", SearchOption.AllDirectories).ToArray();
            Assert.IsTrue(ArrayHelper.Compare(
                expectedEntries,
                filteredDirectories.ToArray(),
                StringComparison)
            );
        }

        #endregion

        #region Stop Search

        [TestMethod]
        public void Search_FileFindedStopSearch_RiseStartAndFinishEventsFindOnlyDirectories()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var isStartEventRised = false;
            var isFinishEventRised = false;
            fsv.Start += (sender, args) => { isStartEventRised = true; };
            fsv.Finish += (sender, args) => { isFinishEventRised = true; };
            fsv.FileFinded += (sender, args) => { args.StopSearch = true; };

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(isStartEventRised);
            Assert.IsTrue(isFinishEventRised);
            Assert.IsTrue(FileSystemHelper.IsAllDirectories(findedEntries));
        }

        [TestMethod]
        public void Search_DirectoryFindedStopSearch_RiseStartAndFinishEventsFindOnlyFiles()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var isStartEventRised = false;
            var isFinishEventRised = false;
            fsv.Start += (sender, args) => { isStartEventRised = true; };
            fsv.Finish += (sender, args) => { isFinishEventRised = true; };
            fsv.DirectoryFinded += (sender, args) => { args.StopSearch = true; };

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(isStartEventRised);
            Assert.IsTrue(isFinishEventRised);
            Assert.IsTrue(FileSystemHelper.IsAllFiles(findedEntries));
        }

        [TestMethod]
        public void Search_FilteredFileFindedStopSearch_RiseStartAndFinishEventsFindOnlyDirectories()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var isStartEventRised = false;
            var isFinishEventRised = false;
            fsv.Start += (sender, args) => { isStartEventRised = true; };
            fsv.Finish += (sender, args) => { isFinishEventRised = true; };
            fsv.FilteredFileFinded += (sender, args) => { args.StopSearch = true; };

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(isStartEventRised);
            Assert.IsTrue(isFinishEventRised);
            Assert.IsTrue(FileSystemHelper.IsAllDirectories(findedEntries));
        }

        [TestMethod]
        public void Search_FilteredDirectoryFindedStopSearch_RiseStartAndFinishEventsFindOnlyFiles()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var isStartEventRised = false;
            var isFinishEventRised = false;
            fsv.Start += (sender, args) => { isStartEventRised = true; };
            fsv.Finish += (sender, args) => { isFinishEventRised = true; };
            fsv.FilteredDirectoryFinded += (sender, args) => { args.StopSearch = true; };

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(isStartEventRised);
            Assert.IsTrue(isFinishEventRised);
            Assert.IsTrue(FileSystemHelper.IsAllFiles(findedEntries));
        }

        #endregion

        #region Exclude File System Entry

        [TestMethod]
        public void Search_FileFindedExcludeFileSystemEntry_RiseStartAndFinishEventsFindOnlyDirectories()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var isStartEventRised = false;
            var isFinishEventRised = false;
            fsv.Start += (sender, args) => { isStartEventRised = true; };
            fsv.Finish += (sender, args) => { isFinishEventRised = true; };
            fsv.FileFinded += (sender, args) => { args.ExcludeFileSystemEntry = true; };

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(isStartEventRised);
            Assert.IsTrue(isFinishEventRised);

            var expectedEntries = DirectoryHelper.EnumerateDirectories(TestRootDirectoryName, "*", SearchOption.AllDirectories).ToArray();
            Assert.IsTrue(ArrayHelper.Compare(
                expectedEntries,
                findedEntries,
                StringComparison)
            );
        }

        [TestMethod]
        public void Search_DirectoryFindedExcludeFileSystemEntry_RiseStartAndFinishEventsFindOnlyFiles()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var isStartEventRised = false;
            var isFinishEventRised = false;
            fsv.Start += (sender, args) => { isStartEventRised = true; };
            fsv.Finish += (sender, args) => { isFinishEventRised = true; };
            fsv.DirectoryFinded += (sender, args) => { args.ExcludeFileSystemEntry = true; };

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(isStartEventRised);
            Assert.IsTrue(isFinishEventRised);

            var expectedEntries = DirectoryHelper.EnumerateFiles(TestRootDirectoryName, "*", SearchOption.AllDirectories).ToArray();
            Assert.IsTrue(ArrayHelper.Compare(
                expectedEntries,
                findedEntries,
                StringComparison)
            );
        }

        [TestMethod]
        public void Search_FilteredFileFindedExcludeFileSystemEntry_RiseStartAndFinishEventsFindOnlyDirectories()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var isStartEventRised = false;
            var isFinishEventRised = false;
            fsv.Start += (sender, args) => { isStartEventRised = true; };
            fsv.Finish += (sender, args) => { isFinishEventRised = true; };
            fsv.FilteredFileFinded += (sender, args) => { args.ExcludeFileSystemEntry = true; };

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(isStartEventRised);
            Assert.IsTrue(isFinishEventRised);

            var expectedEntries = DirectoryHelper.EnumerateDirectories(TestRootDirectoryName, "*", SearchOption.AllDirectories).ToArray();
            Assert.IsTrue(ArrayHelper.Compare(
                expectedEntries,
                findedEntries,
                StringComparison)
            );
        }

        [TestMethod]
        public void Search_FilteredDirectoryFindedExcludeFileSystemEntry_RiseStartAndFinishEventsFindOnlyFiles()
        {
            // Arrange
            var fsv = new FileSystemVisitor();

            var isStartEventRised = false;
            var isFinishEventRised = false;
            fsv.Start += (sender, args) => { isStartEventRised = true; };
            fsv.Finish += (sender, args) => { isFinishEventRised = true; };
            fsv.FilteredDirectoryFinded += (sender, args) => { args.ExcludeFileSystemEntry = true; };

            // Act
            var findedEntries = fsv.Search(TestRootDirectoryName).ToArray();

            // Assert
            Assert.IsTrue(isStartEventRised);
            Assert.IsTrue(isFinishEventRised);

            var expectedEntries = DirectoryHelper.EnumerateFiles(TestRootDirectoryName, "*", SearchOption.AllDirectories).ToArray();
            Assert.IsTrue(ArrayHelper.Compare(
                expectedEntries,
                findedEntries,
                StringComparison)
            );
        }

        #endregion

        private struct Directory
        {
            public string Name;
            public string[] Files;
            public Directory[] Directories;
        }
    }
}
