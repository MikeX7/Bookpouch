using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

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

            var bookData = new BookData
            {
                Title = (string)bookPeek.List["title"],
                Author = (string)(bookPeek.List.ContainsKey("author") ? bookPeek.List["author"] : ""),
                Publisher = (string)(bookPeek.List.ContainsKey("publisher") ? bookPeek.List["publisher"] : ""),
                Language = (string)(bookPeek.List.ContainsKey("language") ? bookPeek.List["language"] : ""),
                Published = (DateTime?)(bookPeek.List.ContainsKey("published") ? bookPeek.List["published"] : null),
                MobiType = (string)(bookPeek.List.ContainsKey("type") ? bookPeek.List["type"] : ""),
                Created = DateTime.Now,
                Size = (ulong)finfo.Length
            };
            Debug.WriteLine(finfo.Directory);
            using (var writer = XmlWriter.Create(finfo.Directory + "/info.xml"))
            {
                var serializer = new XmlSerializer(typeof(BookData));
                serializer.Serialize(writer, bookData);
            }
        }
    }
    
}
