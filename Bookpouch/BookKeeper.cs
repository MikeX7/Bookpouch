using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Bookpouch
{    
    /// <summary>
    /// Tools for editing and reading the existing library of books
    /// </summary>
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
            var fileName = Path.GetFileNameWithoutExtension(finfo.Name)  + (copyNumber > 1 ? " (" + (copyNumber - 1) + ")" : "") + finfo.Extension; //If the parent directory got a number added to its name, add it to the book file name as well

            finfo.CopyTo(dirPath + "/" + fileName, true);

            GenerateInfo(dirPath + "/" + fileName); //Generate .dat file for this book 
        }        

        /// <summary>
        /// Generate *.dat file containing information about a book (mostly extracted from the book file) and save it into the book's folder.
        /// </summary>
        /// <param name="bookFile">Path to the book file</param>
        public static void GenerateInfo(string bookFile) 
        {            
            if (!File.Exists(bookFile))
                return;

            var finfo = new FileInfo(bookFile);
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
                {"cover", bookPeek.List.ContainsKey("cover") ? bookPeek.List["cover"] : null},                
            };

            var infoFile = bookFile + ".dat";

            using (var fs = new FileStream(infoFile, FileMode.Create))
            {
                File.SetAttributes(infoFile, File.GetAttributes(infoFile) | FileAttributes.Hidden);
                var bf = new BinaryFormatter();
                bf.Serialize(fs, bookData);
            }           
        }

        /// <summary>
        /// Read the info file associated with the supplied book and return the information
        /// </summary>
        /// <param name="bookFile">Path to the book file</param>
        /// <returns>Dictionary containing saved information about the file</returns>
        /// <exception cref="FileNotFoundException">Book file associated with the .dat file doesn't exist or vice versa</exception>
        public static Dictionary<string, object> GetInfo(string bookFile)
        {
            if (!File.Exists(bookFile))
                throw new FileNotFoundException();

            var infoFile = bookFile + ".dat";
            
            if(!File.Exists(infoFile)) //If the .dat file is missing, attempt to generate it 
                GenerateInfo(bookFile);
            
            if (!File.Exists(infoFile))
                throw new FileNotFoundException();

            try
            {
                using (var fs = new FileStream(infoFile, FileMode.Open))
                {
                    var bf = new BinaryFormatter();                
                    var bookInfo = (Dictionary<string, object>)bf.Deserialize(fs);
                    bookInfo.Add("path", bookFile);          

                    return bookInfo;
                }
            }
            catch (Exception e)
            {
                DebugConsole.WriteLine("Book keeper: Problem with reading the " + infoFile + " file: " + e.Message);

                try
                {
                    File.Delete(infoFile);
                }
                catch (Exception) { }

                throw;
            }
            
        }

        /// <summary>
        /// Save dictionary with book info back into a file
        /// </summary>
        /// <param name="bookFile">Path to the .dat file into which the dictionary will be saved</param>
        /// <param name="bookInfo">The dictionary object containing the book info</param>
        /// <exception cref="FileNotFoundException">It was not possible to access the info file belonging to the given book</exception>
        public static void SaveInfo(string bookFile, Dictionary<string, object> bookInfo)
        {
            var infoFile = bookFile + ".dat";
            

            if (!File.Exists(infoFile))
                GenerateInfo(bookFile);

            if(!File.Exists(infoFile))
                throw new FileNotFoundException();

            bookInfo.Remove("path");

            try
            {
                using (var fs = new FileStream(infoFile, FileMode.OpenOrCreate))
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(fs, bookInfo);
                }
            }
            catch (Exception e)
            {                
                DebugConsole.WriteLine("Book keeper: Saving info failed: " + e.Message);
                throw;
            }
        }

        /// <summary>
        /// Remove a book from the library
        /// </summary>
        /// <param name="dirName">Path to the folder which contains the book files</param>
        public static void Discard(string dirName) //Permanently remove a book from the library
        {
            if (!Directory.Exists(dirName))
                return;

            Directory.Delete(dirName, true);
            MainWindow.MW.BookGrid_OnLoaded(MainWindow.MW.BookGrid, null); //Reload grid in the main window
        }        
    }
    
}
