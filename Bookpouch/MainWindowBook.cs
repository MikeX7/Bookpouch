using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Bookpouch
{
    partial class MainWindow
    {
        internal sealed class Book
        {
            public byte[] Cover;
            public string Title { set; get; }
            public string Author { set; get; }
            public string Series { set; get; }
            public string Publisher { set; get; }
            public DateTime? Published { set; get; }
            public string CountryCode;
            public string Description { set; get; }
            public string MobiType { set; get; }
            public string Size { set; get; }
            public string Category { set; get; }
            public bool Favorite { set; get; }
            public bool Sync { set; get; }
            public string BookFile { set; get; }

            public BitmapImage CoverImage
            {
                get
                {                    
                    BitmapImage cover;

                    try
                    {
                        cover = new BitmapImage();

                        cover.BeginInit();
                        cover.CreateOptions = BitmapCreateOptions.PreservePixelFormat |
                                              BitmapCreateOptions.IgnoreColorProfile;
                        cover.CacheOption = BitmapCacheOption.OnLoad;

                        if(Cover == null)
                            cover.UriSource = new Uri("pack://application:,,,/Bookpouch;component/Img/book.png");
                        else
                            cover.StreamSource = new MemoryStream(Cover);    
                        
                        
                        cover.EndInit();
                    }
                    catch (NotSupportedException)
                    {
                        cover = new BitmapImage(new Uri("pack://application:,,,/Bookpouch;component/Img/book.png"));
                        //Provide default image in case the book cover image exists but is faulty
                    }

                    return cover;
                }
            }

            public Visibility SeriesVisibility
            {
                get { return (Series != "" ? Visibility.Visible : Visibility.Collapsed); }
            }

            public Visibility AuthorVisibility
            {
                get { return (Author != "" ? Visibility.Visible : Visibility.Collapsed); }
            }

            public double FavoriteOpacity
            {
                get { return (Favorite ? 1 : 0.12); }
            }

            public double SyncOpacity
            {
                get { return (Sync ? 1 : 0.12); }
            }

            public string CountryFlagPath
            {
                get { return "flags/" + CountryCode.Trim() + ".png"; }
            }

        }
    }

}
