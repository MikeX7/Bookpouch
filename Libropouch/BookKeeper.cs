using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Libropouch
{    
    static class BookKeeper
    {
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

            for(var i = 1; Directory.Exists(newDirPath); i++) //If the folder already exists append number to the new folder's name
                newDirPath = dirPath + " (" + i + ")";

            dirPath = newDirPath;

            Directory.CreateDirectory(dirPath); //Create the dir in the default book folder, specified in the settings        

            finfo.CopyTo(dirPath + "/" + finfo.Name, true);

            GenerateInfo(dirPath + "/" + finfo.Name); //Generate info.xml file for this book file
        }

        public static void GenerateInfo(String file) //Add a new book into the library
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
    }
    
}
