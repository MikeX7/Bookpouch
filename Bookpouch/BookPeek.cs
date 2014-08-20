using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Bookpouch
{
    /// <summary>
    /// Tools for extracting informations from the eBook files
    /// </summary>
    class BookPeek 
    {
        public Dictionary<string, object> List = new Dictionary<string, object>();
        public readonly string DirName;

        public BookPeek(FileInfo file)
        {
            DirName = file.DirectoryName;

            switch (file.Extension.ToLower())
            {
                case ".mobi":
                    Mobi(file);
                    break;
                case ".epub":
                    Epub(file);
                    break;
            }            

            if(!List.ContainsKey("title"))
                List.Add("title", file.Name.Substring(0, (file.Name.Length - file.Extension.Length)));
        }

        private void Epub(FileSystemInfo file)
        {
            try
            {          
                using (var zip = ZipFile.Open(file.FullName, ZipArchiveMode.Read))
                {   
                    //Get location of the content file 
                    var container = zip.GetEntry("META-INF/container.xml");
                    var containerXml = XDocument.Load(container.Open());
                
                    XNamespace ns = containerXml.Root.Attribute("xmlns").Value;

                    var rootFile = containerXml
                        .Root
                        .Descendants(ns + "rootfile")
                        .FirstOrDefault()
                        .Attribute("full-path")
                        .Value;

                    //Get book info from the content file
                    var content = zip.GetEntry(rootFile);
                    var contentXml = XDocument.Load(content.Open());                    
                    XNamespace contentNs = contentXml.Root.Attribute("xmlns").Value;
                    var customNs = XNamespace.Get("http://purl.org/dc/elements/1.1/");                    
                    var metaData = contentXml.Root.Descendants(contentNs + "metadata").FirstOrDefault();
                    var manifest = contentXml.Root.Descendants(contentNs + "manifest").FirstOrDefault();

                    XElement cover = manifest.Elements(contentNs + "item")
                        .FirstOrDefault(
                            x =>
                                (x.Attribute("id").Value.ToLower().Contains("cover") &&
                                 (x.Attribute("href").Value.EndsWith(".jpg") ||
                                  x.Attribute("href").Value.EndsWith(".jpeg"))));

                    if (cover != null)
                    {
                        var dir = new DirectoryInfo(rootFile);

                        var coverFullPath = ((rootFile.Contains("/") || rootFile.Contains("\\"))
                            ? dir.Parent + "/"
                            : "") + cover.Attribute("href").Value;
                        
                        //zip.GetEntry(coverFullPath).ExtractToFile(DirName + "/cover.jpg", true);                            

                        using (var ms = new MemoryStream())
                        {
                            zip.GetEntry(coverFullPath).Open().CopyTo(ms);
                            List.Add("cover", ms.ToArray());
                        }
                    }

                    var meta = from el in metaData.Descendants() where el.Name.Namespace == customNs select el;

                    var author = metaData.Descendants(customNs + "creator").FirstOrDefault();
                    var title = metaData.Descendants(customNs + "title").FirstOrDefault();
                    var publisher = metaData.Descendants(customNs + "publisher").FirstOrDefault();
                    var language = metaData.Descendants(customNs + "language").FirstOrDefault();
                    var published = metaData.Descendants(customNs + "date").FirstOrDefault();                                       

                    if (author != null)
                        List.Add("author", author.Value);

                    if (title != null)
                        List.Add("title", title.Value);

                    if (publisher != null)
                        List.Add("publisher", publisher.Value);

                    if (language != null)
                        List.Add("language", language.Value);

                    if (published != null)
                        List.Add("published", DateTime.Parse(published.Value));
                }
            }
            catch (Exception e)
            {
                DebugConsole.WriteLine("Problem occurred during info extraction from an epub file: " + e.Message);
                MainWindow.Info(UiLang.Get("EpubCorrupted"), 1);
            }             
        }

        private void Mobi(FileSystemInfo file) //Find and read MOBI header       
        {
                 
            var types = new Dictionary<UInt32, String>
                {
                    {2, "Mobipocket Book"},
                    {3, "PalmDoc Book"},
                    {4, "Audio"},
                    {232, "mobipocket? generated by kindlegen1.2"},
                    {248, "KF8: generated by kindlegen2"},
                    {257, "News"},
                    {258, "News Feed"},
                    {259, "News Magazine"},
                    {513, "Pics"},
                    {514, "Word"},
                    {515, "XLS"},
                    {516, "PPT"},
                    {517, "Text"},
                    {518, "HTML"}                    
                };

            using (var fs = File.OpenRead(@file.FullName))
            {
                var headerIdent = new byte[4];
                var headerFound = false;
                var headerLength = new byte[4];
                var type = new byte[4];                
                var titleOffset = new byte[4];
                var titleLength = new byte[4];
                var language = new byte[4];
                var imageIndex = new byte[4];
                var exthFlags = new byte[4];
                long headerPos = 0;

                fs.Seek(300, SeekOrigin.Current);

                while (fs.Read(headerIdent, 0, headerIdent.Length) > 0)
                {
                    if (Encoding.UTF8.GetBytes("MOBI").SequenceEqual(headerIdent)) //Search the stream until the beggining of the MOBI header is found or until end of the stream is reached
                    {
                        headerPos = fs.Position;
                        headerFound = true;
                        break;
                    }
                }

                //Debug.WriteLine("Header position: " + fs.Position);

                if (!headerFound) 
                {
                    MainWindow.Info(string.Format(UiLang.Get("MobiHeaderMissing"), file.Name), 1);
                    return;
                }

                fs.Read(headerLength, 0, headerLength.Length);
                fs.Read(type, 0, type.Length);

                if (types.ContainsKey(ByteToUInt32(type)))
                    List.Add("type", types[ByteToUInt32(type)]);

                fs.Seek((14 * 4), SeekOrigin.Current); //Skip some of the following fields
                fs.Read(titleOffset, 0, titleOffset.Length);
                fs.Read(titleLength, 0, titleLength.Length);
                fs.Read(language, 0, language.Length);
                fs.Seek((3 * 4), SeekOrigin.Current); //Skip some of the following fields
                fs.Read(imageIndex, 0, imageIndex.Length);
                fs.Seek((4 * 4), SeekOrigin.Current); //Skip some of the following fields
                fs.Read(exthFlags, 0, exthFlags.Length);
                
                var langCode = ByteToUInt32(language);

                if (langCode > 100) //Only continue if the language code looks valid and isn't too low 
                {
                    var cultureInfo = CultureInfo.GetCultureInfo((int) langCode);
                    List.Add("language", cultureInfo.Name);
                }

                if ((ByteToUInt32(exthFlags) & 0x40) != 0) //exthFlags tells us if the EXTH header exists in this file
                    MobiExth(fs, file);

                //Attempt to get the cover image from the mobi file and save it
                var img = GetJpegFromStream(fs);

                if (img.Length > 0)
                {
                    List.Add("cover", img.ToArray());
                    
                    /*using (var fileStream = File.Create(DirName + "/cover.jpg")) //Save cover image
                    {
                        img.Seek(0, SeekOrigin.Begin);
                        img.CopyTo(fileStream);
                    }*/
                }

                if (List.ContainsKey("title")) //If exth contained the book title, no need to continue
                    return;

                //Get book title from the mobi header, the title itself is located after the exth header (if it exists)
                fs.Seek(headerPos + ByteToUInt32(titleOffset) - 4 - 16, SeekOrigin.Begin);
                
                var title = new byte[ByteToUInt32(titleLength)];
                fs.Read(title, 0, title.Length);

                if (ByteToUInt32(titleLength) > 0)
                {
                    List.Add("title", Encoding.UTF8.GetString(title));                    
                }    
            }
        }

        private void MobiExth(FileStream fs, FileSystemInfo file) //Attempt to find and process the EXTH header from the mobi file stream
        {
            var records = new Dictionary<UInt32, string>();
            var headerIdent = new byte[4];
            var headerLength = new byte[4];
            var recordCount = new byte[4];

            while (fs.Read(headerIdent, 0, headerIdent.Length) > 0)
            {
                if (Encoding.UTF8.GetBytes("EXTH").SequenceEqual(headerIdent)) //Keep checking the file until we find the EXTH header beginning
                    break;
            }

            if (fs.Length == fs.Position) //If we reach the end of the stream without finding the EXTH header, then the file doesn't contain it
            {
                MainWindow.Info(String.Format(UiLang.Get("ExthHeaderMissing"), file.Name), 1);
                return;
            }

            fs.Read(headerLength, 0, headerLength.Length);
            fs.Read(recordCount, 0, recordCount.Length);

            try
            {
                for (var i = 0; i < ByteToUInt32(recordCount); i++)
                {
                    var recordType = new byte[4];
                    var recordLength = new byte[4];

                    fs.Read(recordType, 0, recordType.Length);
                    fs.Read(recordLength, 0, recordLength.Length);

                    //Debug.WriteLine(ByteToUInt32(recordType));
                    var recordData = new byte[ByteToUInt32(recordLength) - 8];
                    fs.Read(recordData, 0, recordData.Length);

                    if (!records.ContainsKey(ByteToUInt32(recordType)))
                        records.Add(ByteToUInt32(recordType), Encoding.UTF8.GetString(recordData));
                }
                
                if (records.ContainsKey(100))
                    List.Add("author", records[100]);

                if (records.ContainsKey(503))
                    List.Add("title", records[503]);

                if (records.ContainsKey(101))
                    List.Add("publisher", records[101]);

                if (records.ContainsKey(524) && !List.ContainsKey("language")) //Only add language from the exth header, if it wasn't found in the mobi header
                {
                    var cultureInfo = CultureInfo.GetCultureInfo("en");                                        
                    List.Add("language", cultureInfo.Name);
                }

                if (records.ContainsKey(106))
                    List.Add("published", DateTime.Parse(records[106]));
            }
            catch (Exception e)
            {
                MainWindow.Info(String.Format(UiLang.Get("ExthProblemFetching"), file.Name, e), 1);
            }
        }

        private static MemoryStream GetJpegFromStream(Stream fs) //Fetch the first jpg image from the file stream
        {
            var img = new MemoryStream();
            var fsc = new MemoryStream();
            var saving = false;
            var chunk = new Byte[4];
            Byte[] endMagic = null;
            //Byte sequences marking beginning and end of image files
            var jpegMagic = new Tuple<byte[], byte[]>(new byte[] {0xff, 0xd8}, new byte[] { 0xff, 0xd9 }); 
            var gifMagic = new Tuple<byte[], byte[]>(new byte[] {0x47, 0x49, 0x46, 0x38}, new byte[] {0x00, 0x3b});

            fs.Seek(0, SeekOrigin.Begin);

            fs.CopyTo(fsc);
            var bytes = fsc.ToArray();

            for (var index = 0; index < bytes.Length; index++)
            {
                var b = bytes[index];
                chunk[0] = b;
                
                for (var chunkIndex = 1; chunkIndex < chunk.Length; chunkIndex++) //Read several bytes ahead so we can compare whole sequences and not just single bytes
                    chunk[chunkIndex] = ((chunkIndex + index) < bytes.Length ? bytes[index + chunkIndex] : default(byte));

                if (!chunk.Take(2).SequenceEqual(jpegMagic.Item1) && !chunk.SequenceEqual(gifMagic.Item1) && !saving)
                    continue;

                if(endMagic == null)
                    endMagic = (chunk.Take(2).SequenceEqual(jpegMagic.Item1) ? jpegMagic.Item2 : gifMagic.Item2);

                saving = true;

                img.WriteByte(b);

                if (!chunk.Take(2).SequenceEqual(endMagic)) 
                    continue;

                img.WriteByte(chunk[1]);
                break;
            }

            img.Seek(0, SeekOrigin.Begin);
            
            //Check if the image file we extracted is actually valid image
            if (img.Length > 0)
            {
                try
                {
                    using (var imgCheck = Image.FromStream(img))
                    {
                        if (imgCheck.RawFormat.Equals(ImageFormat.Gif))
                            Debug.WriteLine("YIFF");

                        if (imgCheck.RawFormat.Equals(ImageFormat.Jpeg))
                            Debug.WriteLine("JAYPEG");

                        if (!imgCheck.RawFormat.Equals(ImageFormat.Jpeg) && !imgCheck.RawFormat.Equals(ImageFormat.Gif))
                            throw new FormatException();
                    }
                }
                catch (Exception)
                {
                    img.SetLength(0);
                }
            }

            return img;
        }

        private static UInt32 ByteToUInt32(byte[] bytesToConvert)
        {
            //Make copy so we're not permanently reversing the order of the bytes in the actual field
            var buffer = (byte[])bytesToConvert.Clone();

            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            return BitConverter.ToUInt32(buffer, 0);
        }
    }
        
}
