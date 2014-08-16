using System;

namespace Bookpouch
{
    /// <summary>
    /// Class for storing data available about a book
    /// </summary>
    public class BookData
    {       
        public string Title;
        public string Author;
        public string Publisher;
        public string Language;
        public DateTime? Published;
        public string MobiType;
        public string Description;
        public string Series;
        public DateTime Created;
        public ulong Size;
        public bool Favorite = false;
        public bool Sync = false;
        public string Category;
        public byte[] Cover;
        public string Path;
    }
}
