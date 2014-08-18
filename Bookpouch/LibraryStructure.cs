using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
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
        /// Go through all database records along with all book files in the library root folder and remove any database entries which point to non-existing files, 
        /// then attempt to generate database entries for all book files, which don't have them.
        /// </summary>
        public static void SyncDbWithFileTree()
        {           
            MainWindow.Busy(true);

            Task.Factory.StartNew(() =>
            {
                GenerateFileTree();
                Tools.RemoveEmptyDirectories(Properties.Settings.Default.BooksDir);
                var fileTree = GetFileTree();
                const string sql = "SELECT Path FROM books";
                const string sqlDelete = "DELETE FROM books WHERE Path = @Path";
                var query = Db.Query(sql);
                var pathList = new List<string>();

                while (query.Read())
                {
                    if (File.Exists(BookKeeper.GetAbsoluteBookFilePath(query["Path"].ToString())))
                        pathList.Add(query["Path"].ToString());
                    else
                        Db.NonQuery(sqlDelete, new[] {new SQLiteParameter("Path", query["Path"].ToString())});
                }

                foreach (
                    var bookFile in
                        fileTree.Where(bookFile => !pathList.Contains(BookKeeper.GetRelativeBookFilePath(bookFile))))
                {
                    try
                    {
                        BookKeeper.GetData(bookFile);
                    }
                    catch (Exception e)
                    {
                        DebugConsole.WriteLine(
                            "Library structure: I found a book file without any entry in the database (" + bookFile +
                            "), but an error occurred during attempted adding: " + e);
                    }
                }

                MainWindow.MW.Dispatcher.Invoke(() =>
                {
                    MainWindow.MW.BookGridReload();                    
                });

                MainWindow.Busy(false);
            });
        }

        /// <summary>
        /// Generate a list of BookData objects, where each of them contains information about a book
        /// </summary>
        public static List<BookData> List()
        {
            var bookData = new List<BookData>();
            const string sql = "SELECT * FROM books";
            var query = Db.Query(sql);
                        
            while (query.Read())
            {
                if (!File.Exists(BookKeeper.GetAbsoluteBookFilePath(query["Path"].ToString())))
                    continue;
                

                bookData.Add(BookKeeper.CastSqlBookRowToBookData(query));                                
            }
            
            query.Dispose();
            
            return bookData;
        }
    }
}
