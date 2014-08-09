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

            var dirList = Tools.GetDirectoryList("books");
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
    }
}
