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
        public string Contributor;        
        public string Publisher;
        public string Categories;
        public string Language;
        public DateTime? Published;        
        public string Description;
        public string Series;
        public string Coverage;
        public DateTime Created;
        public ulong Size;
        public bool Favorite = false;
        public bool Sync = false;        
        public byte[] Cover;
        public string Path;
    }
}
