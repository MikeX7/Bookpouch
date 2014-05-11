using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

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

            Directory.CreateDirectory(dirPath); //Create the dir in the default book folder, specified in the settings

            if (!Directory.Exists(dirPath) || 1 == 1)
            {

                finfo.CopyTo(dirPath + "/" + finfo.Name, true);

                var bookInfo = new BookPeek(finfo);

                foreach (var l in bookInfo.List)
                {
                    Debug.WriteLine(l);
                }

                //Debug.WriteLine(bookInfo.List["Author"]);

                //Debug.WriteLine(finfo.Name.Substring(0, (finfo.Name.Length - finfo.Extension.Length)));
            }
        }
    }

    
}
