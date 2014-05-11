using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libropouch
{
    public class BookData
    {
        public String Title;
        public String Author;
        public String Publisher;
        public String Language;
        public DateTime? Published;
        public String MobiType;
        public DateTime Created;
        public bool Favorite = false;
    }
}
