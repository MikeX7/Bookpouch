using System;

namespace Libropouch
{
    //BookData is used to store serialized data about books in info.xml
    public class BookData
    {        
        public String Title;
        public String Author;
        public String Publisher;
        public String Language;
        public DateTime? Published;
        public String MobiType;

        public String Description;
        public DateTime Created;
        public ulong Size;
        public bool Favorite = false;
        public bool Sync = false;
        public int Category;
    }
}
