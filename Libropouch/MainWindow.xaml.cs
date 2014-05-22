using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Serialization;
using ShadoLib;
using Button = System.Windows.Controls.Button;
using DataGrid = System.Windows.Controls.DataGrid;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Libropouch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow MW;        

        public MainWindow()
        {
            MW = this;            

            InitializeComponent();            

            if (Properties.Settings.Default.UsbAutoSync)
                new ReaderDetector(this); //Start reader detection which automatically triggers UsbSync when reader is connected to the pc                      
        }

        private static readonly Queue<Tuple<string, byte>> InfoQueue = new Queue<Tuple<string, byte>>();        
        private static bool _infoBoxVisible;

        public static void Info(string text, byte type = 0)
        {
            if (Properties.Settings.Default.UseInfoBanner == false)
            {
                MessageBox.Show(text);
                return;
            }

            if (text != "")            
                InfoQueue.Enqueue(Tuple.Create(text, type)); //Add info into the info queue                                                              

            if (_infoBoxVisible || InfoQueue.Count == 0) //Proceed only if infobox isn't currently displayed and if the info queue isn't empty
                return;                        
            
            var info = InfoQueue.Dequeue(); //item1 = text, item2 = type
            text = info.Item1;
            type = info.Item2;
            var delay = (int) (text.Length * 0.09); //How long will be the infobox displayed, based in the text length, 1 letter = 0.09 sec

            MW.InfoBox.Text = text;
            
            var border = (Border)LogicalTreeHelper.GetParent(MW.InfoBox);

            if (type == 0) //Change colors of the infobox depending on the type of info being displayed (normal info, or error)
                border.Style = (Style)MW.FindResource("InfoBoxOkBg");
            else
                border.Style = (Style)MW.FindResource("InfoBoxErrorBg");
            
            _infoBoxVisible = true;
                                
            var sb = (Storyboard) MW.FindResource("InfoDissolve");

            foreach (var animation in sb.Children.OfType<DoubleAnimation>().Where(animation => animation.Name == "OutOpacity"))
                animation.BeginTime = new TimeSpan(0, 0, delay);

            foreach (var keyFrame in sb.Children.OfType<ObjectAnimationUsingKeyFrames>().Where(oaukf => oaukf.Name == "OutVisibility").SelectMany(oaukf => oaukf.KeyFrames.Cast<DiscreteObjectKeyFrame>()))            
                keyFrame.KeyTime = new TimeSpan(0, 0, delay + 2);            

            Storyboard.SetTarget(sb, border);
            sb.Begin();                                     
        }

        private void BookGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Properties.Settings.Default.FilesDir))
            {
                Info(String.Format("I wasn't able to find the specified directory ({0}) for storing books.", Properties.Settings.Default.FilesDir), 1);
                return;
            }

            var categories = new List<string> {"Sci-fi", "Action", "Fantasy", "Horror", "Poetry", "Biography", "Comic"};
                

            var grid = (DataGrid) sender;
            var extensions = Properties.Settings.Default.FileExtensions.Split(';');            
            var dirs = Directory.EnumerateDirectories(Properties.Settings.Default.FilesDir).ToList();
            var bookList = new List<Book>();
            
            foreach (var dir in dirs)
            {
                var dinfo = new FileInfo(dir);
                var bookFilePath = Directory.EnumerateFiles(dinfo.FullName).First(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
                

                if (!File.Exists(dinfo.FullName + "\\info.xml")) //If info file is missing, attempt to generate new one
                    BookKeeper.GenerateInfo(bookFilePath);                

                using (var infoFile = new FileStream(dinfo.FullName + "\\info.xml", FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof (BookData));
                    var bookInfo = (BookData) serializer.Deserialize(infoFile);
                    var countryCode = "_unknown"; //If we can't get proper country code, this fallback flag image name will be used

                    if (bookInfo.Language != "" && CultureInfo.GetCultures(CultureTypes.SpecificCultures).FirstOrDefault(x => x.Name == bookInfo.Language) != null) //Make sure the book language is not neutral (ex: en instead of en-US), or invalid. This will make sure we don't display for example US flag for british english.                         
                    {         
                        var cultureInfo = new CultureInfo(bookInfo.Language);
                        countryCode = new RegionInfo(cultureInfo.Name).TwoLetterISORegionName;                                                        
                    }

                    bookList.Add(new Book()
                    {
                        Title = bookInfo.Title,
                        Author = bookInfo.Author,
                        Publisher = bookInfo.Publisher,
                        CountryCode = countryCode,
                        Published = bookInfo.Published,
                        Description = bookInfo.Description,
                        MobiType = bookInfo.MobiType,
                        Size = Tools.BytesFormat(bookInfo.Size),
                        Favorite = bookInfo.Favorite,
                        Sync = bookInfo.Sync,
                        Category = bookInfo.Category,
                        InfoFile = dinfo.FullName + "\\info.xml"
                    });
                }
            }

            grid.ItemsSource = bookList;

        }        

        private void Sync_OnClick(object sender, RoutedEventArgs e)
        {
            Info(String.Format(UiLang.Get("InfoSyncDeviceSearch"), Properties.Settings.Default.DeviceModel));  

            new UsbSync();
        }

        private void InfoBox_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) //Upon closing (hiding) the infobox, trigger the info method, to check if there is another info waiting in the queue
        {
            var border = (Border) sender;

            if (border.Visibility == Visibility.Hidden)
            {
                _infoBoxVisible = false;
                Info("");                
            }
            else
            {
                _infoBoxVisible = true;
            }
        }

        private void Add_OnClick(object sender, RoutedEventArgs e)
        {
            var supportedFiles = "*." + Properties.Settings.Default.FileExtensions.Replace(";", ";*.");            
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "eBook files|" + supportedFiles + "|All Files|*.*",                
            };

            var filesSelected = openFileDialog.ShowDialog();

            if (filesSelected != true)            
                return;

            Info("Selected supported files are being added into the library...");

            var selectedFiles = openFileDialog.FileNames;

            foreach (var file in selectedFiles)            
                BookKeeper.Add(file);

            BookGrid_OnLoaded(BookGrid, null); //Refresh the data grid displaying info about books, so we can see any newly added books 
        }

        private void SyncToDeviceToggle_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var icon = (Image) VisualTreeHelper.GetChild(button, 0);            
            icon.Opacity = (icon.Opacity <= 0.12 ? 1 : 0.12);
            

        }

        private void FavoriteToggle_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var icon = (Image)VisualTreeHelper.GetChild(button, 0);
            icon.Opacity = (icon.Opacity <= 0.12 ? 1 : 0.12);
        }

        private void EditBook_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            Debug.WriteLine(button.DataContext);
            this.IsEnabled = false;
            var editBook = new EditBook { Owner = this };
            editBook.Closed += delegate { this.IsEnabled = true; };
            editBook.Show();
        }

        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            var settings = new Settings {Owner = this};
            settings.Closed += delegate { this.IsEnabled = true; };
            settings.Show();
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            var about = new AboutBox { Owner = this };
            about.Closed += delegate { this.IsEnabled = true; };
            about.Show();
        }
    }

    internal class Book
    {
        public string Title { set; get; }
        public string Author { set; get; }
        public string Series { set; get; }
        public string Publisher { set; get; }
        public DateTime? Published { set; get; }
        public string CountryCode;
        public string Description { set; get; }
        public string MobiType { set; get; }
        public string Size { set; get; }
        public int Category { set; get; }
        public bool Favorite { set; get; }
        public bool Sync { set; get; }
        public string InfoFile { set; get; }
        
        public Visibility SeriesVisibility 
        { 
            get { return (Series != null ? Visibility.Visible : Visibility.Collapsed); } 
        }

        public Visibility AuthorVisibility
        {
            get { return (Author != null ? Visibility.Visible : Visibility.Collapsed); }
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

        public Book()
        {

        }
        
    }
}
