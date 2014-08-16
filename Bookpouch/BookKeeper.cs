using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
        public static void Add(string file) //Add a new book into the library
        {
            DebugConsole.WriteLine("Book keeper: New book is being manually added into the library: " + file);

            if(!Directory.Exists(Properties.Settings.Default.BooksDir))
                throw new DirectoryNotFoundException("Root directory in which the book files are stored was not found.");            

            var finfo = new FileInfo(file);
            var supportedExtensions = Properties.Settings.Default.FileExtensions.Split(';');

            if (!supportedExtensions.Contains(finfo.Extension.Substring(1), StringComparer.CurrentCultureIgnoreCase))
                //Only allow files with supported extensions
                return;

            if (!File.Exists(file))
                throw new FileNotFoundException("The book file supplied for adding into the library doesn't exist.");

            var dirName = Path.GetFileNameWithoutExtension(finfo.Name);
                //Name of the directory, in which the book file will be stored, name of the directory is identical to the file name, except without extension
            var dirPath = Path.Combine(Properties.Settings.Default.BooksDir, dirName);
            var newDirPath = dirPath;            
            int copyNumber;

            for(copyNumber = 1; Directory.Exists(newDirPath); copyNumber++) //If the folder already exists append a number to the new folder's name
                newDirPath = dirPath + " (" + copyNumber + ")";

            dirPath = newDirPath; //We are now sure, the folder name for the book storing folder doesn't already exist
            dirName = Path.GetFileName(dirPath);            

            var fileName = Path.GetFileNameWithoutExtension(finfo.Name)  + (copyNumber > 1 ? " (" + (copyNumber - 1) + ")" : "") + finfo.Extension; //If the parent directory got a number added to its name, add it to the book file name as well
            var path = Path.Combine(dirPath, fileName);

            try
            {
                Directory.CreateDirectory(dirPath); //Create the dir in the default book folder, specified in the settings        
                finfo.CopyTo(path, true);
                GenerateData(path); //Generate data for this book 
            }
            catch (Exception e)
            {
                MainWindow.Info(String.Format(UiLang.Get("BookCopyError"), file));
                DebugConsole.WriteLine("Book keeper: Copying of the book file " + file + " failed because: " + e);

            }            
        }        

        /// <summary>
        /// Generate *.dat file containing information about a book (mostly extracted from the book file) and save it into the book's folder.
        /// </summary>
        /// <param name="bookFile">Path to the book file</param>
        /// <exception cref="FileNotFoundException">The supplied book file was not found</exception>
        public static void GenerateData(string bookFile) 
        {
            if (!File.Exists(bookFile))
            {
                DebugConsole.WriteLine("Book keeper: Generating book data failed, because the supplied book file (" + bookFile + ") doesn't exist.");
                throw new FileNotFoundException();
            }

            DebugConsole.WriteLine("Book keeper: Generating data for " + bookFile);
            var relativePath = bookFile.Replace(Properties.Settings.Default.BooksDir + Path.DirectorySeparatorChar, String.Empty);

            var finfo = new FileInfo(bookFile);
            var bookPeek = new BookPeek(finfo);            
            
            Db.NonQuery("INSERT OR IGNORE INTO books VALUES(@Path, @Title, @Author, @Publisher, @Language, @Published, @Description, @Series, @Category, @MobiType, @Size, @Favorite, @Sync, @Created, @Cover)", 
                new[]
                {
                    new SQLiteParameter("Path", relativePath), 
                    new SQLiteParameter("Title", bookPeek.List["title"].ToString()), 
                    new SQLiteParameter("Author", (bookPeek.List.ContainsKey("author") ? bookPeek.List["author"] : String.Empty).ToString()), 
                    new SQLiteParameter("Publisher", (bookPeek.List.ContainsKey("publisher") ? bookPeek.List["publisher"] : String.Empty).ToString()), 
                    new SQLiteParameter("Language", (bookPeek.List.ContainsKey("language") ? bookPeek.List["language"] : String.Empty).ToString()),
                    new SQLiteParameter("Published", (DateTime?) (bookPeek.List.ContainsKey("published") ? bookPeek.List["published"] : null)),
                    new SQLiteParameter("Description", String.Empty), 
                    new SQLiteParameter("Series", String.Empty),
                    new SQLiteParameter("Category", String.Empty),
                    new SQLiteParameter("MobiType", (bookPeek.List.ContainsKey("type") ? bookPeek.List["type"] : "").ToString()),
                    new SQLiteParameter("Size",  (ulong) finfo.Length), 
                    new SQLiteParameter("Favorite", false),
                    new SQLiteParameter("Sync", false), 
                    new SQLiteParameter("Created", DateTime.Now), 
                    new SQLiteParameter("Cover", (bookPeek.List.ContainsKey("cover") ? bookPeek.List["cover"] : null))
                });            
            
        }

        /// <summary>
        /// Read the info file associated with the supplied book and return the information
        /// </summary>
        /// <param name="bookFile">Path to the book file</param>
        /// <returns>Dictionary containing saved information about the file</returns>
        /// <exception cref="FileNotFoundException">Book file associated with the .dat file doesn't exist or vice versa</exception>
        /// <exception cref="RowNotInTableException">Row containing info about the given book was not found in the table and the attempt at regenerating this row failed.</exception>
        public static BookData GetData(string bookFile)
        {
            if (!File.Exists(bookFile))
                throw new FileNotFoundException();

            const string sql = "SELECT * FROM books WHERE Path = @Path LIMIT 1";
            var relativePath = bookFile.Replace(Properties.Settings.Default.BooksDir + Path.DirectorySeparatorChar, String.Empty);
            var parameters = new[] {new SQLiteParameter("Path", relativePath)};
            var query = Db.Query(sql, parameters);

            if (!query.HasRows) //If the row is missing, attempt to generate it 
            {
                query.Dispose();
                DebugConsole.WriteLine("Book keeper: Nonexistent data for " + bookFile + ". Triggering data generation...");
                GenerateData(bookFile);
                
                query = Db.Query(sql, parameters);

                if (!query.HasRows)
                {
                    query.Dispose();
                    throw new RowNotInTableException(
                        "Row for the specified book file was not found in the database, and the attempted regeneration failed.");
                }
            }                            

            query.Read();
           
           var bookData = new BookData
            {
                Title = (string) query["Title"],
                Author = (string) query["Author"],
                Publisher = (string) query["Publisher"],
                Language = (string) query["Language"],
                Published = (DateTime?) query["Published"],
                Description = (string) query["Description"],
                Series = (string) query["Series"],
                Category = (string) query["Category"],
                MobiType = (string) query["MobiType"],
                Size = Convert.ToUInt64(query["Size"]),
                Favorite = (bool) query["Favorite"],
                Sync = (bool) query["Sync"],
                Created = (DateTime) query["Created"],
                Cover = (byte[]) query["Cover"],
                Path = bookFile
            };         

            query.Dispose();

            return bookData;
        }

        /// <summary>
        /// Save dictionary with book info back into a file
        /// </summary>
        /// <param name="bookFile">Path to the .dat file into which the dictionary will be saved</param>
        /// <param name="bookData">The dictionary object containing the book info</param>
        /// <exception cref="FileNotFoundException">Supplied book file was not found</exception>
        /// <exception cref="RowNotInTableException">Database record for the supplied book file doesn't exists and it was not possible to regenerate it</exception>
        public static void SaveData(string bookFile, BookData bookData)
        {
            const string sql = "SELECT EXISTS( SELECT * FROM books WHERE Path = @Path LIMIT 1)";
            var parameters = new[] { new SQLiteParameter("Path", bookFile) };
            var exists = Db.QueryExists(sql, parameters);

            if (!exists)
            {
                GenerateData(bookFile);

                exists = Db.QueryExists(sql, parameters);

                if(!exists)
                    throw new RowNotInTableException("Row for the specified book file was not found in the database, and the attempted regeneration failed.");
            }

            Db.NonQuery(
                "UPDATE books SET Title = @Title, Author = @Author, Publisher = @Publisher, Language = @Language, Published = @Published, Description = @Description, Series = @series, Category = @Category, MobiType = @MobiType, Size = @Size, Favorite = @Favorite, Sync = @Sync, Create = @Create, Cover = @Cover WHERE Path = @Path LIMIT 1",
                typeof (BookData).GetProperties().Select(property => new SQLiteParameter(property.Name, property.GetValue(bookData))).ToArray());
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
