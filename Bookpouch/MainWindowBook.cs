using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Bookpouch
{
    partial class MainWindow
    {
        public class Book
        {
            public byte[] Cover;
            public string Title { set; get; }
            public string Author { set; get; }
            public string Series { set; get; }
            public string Publisher { set; get; }
            public DateTime? Published;

            public string PublishedDate
            {
                get { return (Published.HasValue ? Published.Value.ToShortDateString() : null); }
            }

            public string CountryCode;

            private string _description;
            
            public string Description
            {
                set { _description = value; }
                get
                {
                    var description = Regex.Replace(_description.Replace("<p", "\n<p"), "<[^>]+>", ""); //Remove html tags and replace block tags with new line marks

                    if (Properties.Settings.Default.DescriptionMaxLength > 0 && description.Length > Properties.Settings.Default.DescriptionMaxLength)
                        description = description.Substring(0, Properties.Settings.Default.DescriptionMaxLength) + "...";

                    return description;
                } 
            }

            public string MobiType { set; get; }
            public string Size { set; get; }

            public List<string> Categories;

            public string CategoriesString
            {
                get { return String.Join(", ", Categories); }
            }

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

            public Visibility CategoriesVisibility
            {
                get { return (CategoriesString != "" ? Visibility.Visible : Visibility.Collapsed); }
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
