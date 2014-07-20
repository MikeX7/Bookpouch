using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Libropouch
{    
    static class BookKeeper
    {
        /// <summary>
        /// Add a new book into the library
        /// </summary>
        /// <param name="file">Path to the file which is being added</param>
        public static void Add(String file) //Add a new book into the library
        {
            var finfo = new FileInfo(file);
            var supportedExtensions = Properties.Settings.Default.FileExtensions.Split(';');

            if (!supportedExtensions.Contains(finfo.Extension.Substring(1), StringComparer.CurrentCultureIgnoreCase))
                //Only allow files with supported extensions
                return;

            var dirName = finfo.Name.Substring(0, (finfo.Name.Length - finfo.Extension.Length));
                //Dir to store all book related files in, name derived from the file name
            var dirPath = Properties.Settings.Default.FilesDir + "/" + dirName;
            var newDirPath = dirPath;

            int copyNumber;

            for(copyNumber = 1; Directory.Exists(newDirPath); copyNumber++) //If the folder already exists append a number to the new folder's name
                newDirPath = dirPath + " (" + copyNumber + ")";

            dirPath = newDirPath;

            Directory.CreateDirectory(dirPath); //Create the dir in the default book folder, specified in the settings        
            var fileName = Path.GetFileNameWithoutExtension(finfo.Name)  + (copyNumber > 0 ? " (" + (copyNumber - 1) + ")" : "") + finfo.Extension; //If the parent directory got a number added to its name, add it to the book file name as well
            Debug.WriteLine(copyNumber);

            finfo.CopyTo(dirPath + "/" + fileName, true);

            GenerateInfo(dirPath + "/" + fileName); //Generate info.xml file for this book file
        }        

        /// <summary>
        /// Generate info.dat file containing information about a book (mostly extracted from the book file) and save it into the book's folder.
        /// </summary>
        /// <param name="file">Path to the book file</param>
        public static void GenerateInfo(string file) //Add a new book into the library
        {            
            if (!File.Exists(file))
                return;

            var finfo = new FileInfo(file);
            var bookPeek = new BookPeek(finfo);

            var bookData = new Dictionary<string, object>
            {
                {"title", bookPeek.List["title"]},
                {"author", bookPeek.List.ContainsKey("author") ? bookPeek.List["author"] : ""},
                {"publisher", bookPeek.List.ContainsKey("publisher") ? bookPeek.List["publisher"] : ""},
                {"language", bookPeek.List.ContainsKey("language") ? bookPeek.List["language"] : ""},
                {"published", (DateTime?) (bookPeek.List.ContainsKey("published") ? bookPeek.List["published"] : null)},
                {"description", ""},
                {"series", ""},
                {"category", ""},
                {"mobiType", bookPeek.List.ContainsKey("type") ? bookPeek.List["type"] : ""},
                {"size", (ulong) finfo.Length},
                {"favorite", false},
                {"sync", false},                                
                {"created", DateTime.Now},
                
            };
            
            using (var fs = new FileStream(finfo.Directory + "/info.dat", FileMode.Create))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(fs, bookData);
            }           
        }

        /// <summary>
        /// Remove a book from the library
        /// </summary>
        /// <param name="dirName">Path to the folder which contains the book files</param>
        public static void Discard(string dirName) //Pernamently remove a book from the library
        {
            if (!Directory.Exists(dirName))
                return;

            Directory.Delete(dirName, true);
            MainWindow.MW.BookGrid_OnLoaded(MainWindow.MW.BookGrid, null); //Reload grid in the main window
        }
    }
    
}
