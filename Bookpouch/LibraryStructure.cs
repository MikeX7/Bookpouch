using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using ShadoLib;

namespace Bookpouch
{
    static class LibraryStructure
    {
        /// <summary>
        /// Scan the given root directory, along with subfolders and save paths of all files with supported extensions into a LibraryTree.dat file
        /// </summary>
        public static void GenerateFileTree()
        {
            DebugConsole.WriteLine("Regenerating the file tree for the library books...");
            //Info(      String.Format(UiLang.Get("DirNotFound"),    Properties.Settings.Default.BooksDir), 1);
            if(!Directory.Exists(Properties.Settings.Default.BooksDir))
                Settings.SelectBooksDir();

            var dirList = Tools.GetDirectoryList(Properties.Settings.Default.BooksDir);
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
          
            DebugConsole.WriteLine("File tree regeneration finished.");
        }

        /// <summary>
        /// Return list of all book files (obtained from the LibraryTree.dat file) in the library.
        /// </summary>
        /// <returns>List of all book files in the library, including their paths</returns>
        /// <exception cref="FileNotFoundException">It was not possible to find, or re-generate the LibraryTree.dat file. </exception>
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
        /// Generate a list of BookData objects, where each of them contains information about a book
        /// </summary>
        public static List<BookData> List()
        {
            List<string> books;
            var bookData = new List<BookData>();
            var fileTreeIsOutdated = false;

            try
            {
                books = GetFileTree();
            }
            catch (FileNotFoundException e)
            {
                DebugConsole.WriteLine("Library structure: An error occurred while fetching the file tree: " + e);
                return new List<BookData>();
            }

            foreach (var book in books)
            {
                try
                {                    
                    var bookInfo = BookKeeper.GetData(book);
                    bookData.Add(bookInfo);
                }
                catch (FileNotFoundException)
                {                    
                    fileTreeIsOutdated = true;
                }
                catch (Exception e)
                {
                    DebugConsole.WriteLine("Library structure: An error occurred while fetching the book info for " + book + ": " + e);
                }
            }

            if (fileTreeIsOutdated) //If some of the book files saved in the file tree were not found, the user (or some other app) probably manually changed the file structure, so regenerate the file tree
                GenerateFileTree();

            return bookData;
        }
    }
}
