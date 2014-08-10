using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ShadoLib;

namespace Bookpouch
{
    static class LibraryStructure
    {
        /// <summary>
        /// Regenerate the library book file tree and save it into a file
        /// </summary>
        public static void GenerateFileTree()
        {
            DebugConsole.WriteLine("Regenerating the file tree for the library books...");
            //Info(      String.Format(UiLang.Get("DirNotFound"),    Properties.Settings.Default.FilesDir), 1);
            if(!Directory.Exists(Properties.Settings.Default.FilesDir))
                throw new DirectoryNotFoundException(); //Note to self: Change this to a folder selection dialog later

            var dirList = Tools.GetDirectoryList(Properties.Settings.Default.FilesDir);
            var bookFileList = new List<string>();
            var extensions = Properties.Settings.Default.FileExtensions.Split(';');
            
            foreach (var bookFiles in dirList.Select(dir => Directory.EnumerateFiles(dir)
                .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))))            
                {bookFileList.AddRange(bookFiles);}

            using (var treeFile = new FileStream("LibraryTree.dat", FileMode.Create))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(treeFile, bookFileList);
            }

            //Clean up dead .dat files
            foreach (var datFile in dirList.Select(dir => Directory.EnumerateFiles(dir)
                .Where(f => extensions.Any(ext => f.EndsWith(ext + ".dat", StringComparison.OrdinalIgnoreCase))))
                .SelectMany(
                    datFiles => datFiles.Where(datFile => !File.Exists(Path.GetDirectoryName(datFile) + "/" + Path.GetFileNameWithoutExtension(datFile)))))
            {
                Debug.WriteLine(Path.GetFileNameWithoutExtension(datFile));
                try
                {
                    File.Delete(datFile);
                }
                catch (Exception e)
                {
                    DebugConsole.WriteLine("I was not able to delete a dead .dat file: " + e.Message);
                }

                DebugConsole.WriteLine("Removed a dead .dat file: " + datFile);
            }

            DebugConsole.WriteLine("File tree regeneration finished.");
        }

        /// <summary>
        /// Return list of all book files (obtained from the LibraryTree.dat file) in the library.
        /// </summary>
        /// <returns>List of all book files in the library, including their paths</returns>
        /// <exception cref="FileNotFoundException">It was not possible to access or find the LibraryTree.dat file.</exception>
        public static List<string> GetFileTree()
        {
            if (!File.Exists("LibraryTree.dat"))
                GenerateFileTree();

            if (!File.Exists("LibraryTree.dat"))
                throw new FileNotFoundException();

            using (var fs = new FileStream("LibraryTree.dat", FileMode.Open))
            {
                var bf = new BinaryFormatter();
                var fileTree = (List<string>) bf.Deserialize(fs);

                return fileTree;
            }
        }

        /// <summary>
        /// Generate a list of all books (including their full info) in the library
        /// </summary>
        public static List<Dictionary<string, object>> List()
        {
            List<string> books;
            var bookData = new List<Dictionary<string, object>>();
            var someBooksMissing = false;

            try
            {
                books = GetFileTree();
            }
            catch (FileNotFoundException)
            {
                return new List<Dictionary<string, object>>();
            }

            foreach (var book in books)
            {
                try
                {
                    var bookInfo = BookKeeper.GetInfo(book);
                    bookData.Add(bookInfo);
                }
                catch (FileNotFoundException)
                {
                    someBooksMissing = true;
                }
                catch (Exception) { }
            }

            if(someBooksMissing)
                GenerateFileTree();

            return bookData;
        }
    }
}
