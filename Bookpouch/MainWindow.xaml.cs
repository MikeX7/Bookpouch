using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ShadoLib;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using DataGrid = System.Windows.Controls.DataGrid;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;
using Timer = System.Timers.Timer;

namespace Bookpouch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow MW;
        private static NotifyIcon TrayIcon;        

        public MainWindow()
        {
            if (Properties.Settings.Default.OnlyOne)
                if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Count() > 1) Process.GetCurrentProcess().Kill(); //Only allow one instance of Bookpouch

            MW = this;
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); 
            
            if (path != null) 
                Environment.CurrentDirectory = path; //Make sure the app's directory is correct, in case we launched via registry entry during boot

            InitializeComponent();            
            
            TrayIcon = new NotifyIcon
            {
                Icon = new Icon(Application.GetResourceStream(new Uri("Img/kindle.ico", UriKind.Relative)).Stream),
                Visible = false,
                Text = "Bookpouch"
            };

            TrayIcon.Click += (o, args) => 
                {
                    Show();
                    TrayIcon.Visible = false;
                };
            
            if (Environment.GetCommandLineArgs().Contains("-tray")) 
            {
                this.Hide();
                TrayIcon.Visible = true;  
            }

            if (Properties.Settings.Default.UsbAutoSync)
                new ReaderDetector(this);
                    //Start reader detection which automatically triggers UsbSync when the reader is connected to the pc                                  
        }

        private static readonly Queue<Tuple<string, byte>> InfoQueue = new Queue<Tuple<string, byte>>();
        private static bool _infoBoxVisible;

        public static void Info(string text, byte type = 0)
        {
            if (text != "")
            {
                DebugConsole.WriteLine((type == 0 ? "Info: " : "Error: ") + text);

                if (TrayIcon.Visible)
                {
                    TrayIcon.BalloonTipText = text;
                    TrayIcon.ShowBalloonTip(5000);
                    return;
                }                
            }

            if (Properties.Settings.Default.UseInfoBanner == false)
            {
                MessageBox.Show(text);
                return;
            }

            if (text != "")
                InfoQueue.Enqueue(Tuple.Create(text, type));
                    //Add info into the info queue                                                              

            if (_infoBoxVisible || InfoQueue.Count == 0)
                //Proceed only if infobox isn't currently displayed and if the info queue isn't empty
                return;

            var info = InfoQueue.Dequeue(); //item1 = text, item2 = type
            text = info.Item1;
            type = info.Item2;
            var delay = (int) (text.Length*0.09);
                //How long will be the infobox displayed, based in the text length, 1 letter = 0.09 sec

            MW.InfoBox.Text = text;

            var border = (Border) LogicalTreeHelper.GetParent(MW.InfoBox);

            if (type == 0)
                //Change colors of the infobox depending on the type of info being displayed (normal info, or error)
                border.Style = (Style) MW.FindResource("InfoBoxOkBg");
            else
                border.Style = (Style) MW.FindResource("InfoBoxErrorBg");

            _infoBoxVisible = true;

            var sb = (Storyboard) MW.FindResource("InfoDissolve");

            foreach (
                var animation in
                    sb.Children.OfType<DoubleAnimation>().Where(animation => animation.Name == "OutOpacity"))
                animation.BeginTime = new TimeSpan(0, 0, delay);

            foreach (
                var keyFrame in
                    sb.Children.OfType<ObjectAnimationUsingKeyFrames>()
                        .Where(oaukf => oaukf.Name == "OutVisibility")
                        .SelectMany(oaukf => oaukf.KeyFrames.Cast<DiscreteObjectKeyFrame>()))
                keyFrame.KeyTime = new TimeSpan(0, 0, delay + 2);

            Storyboard.SetTarget(sb, border);
            sb.Begin();
        }

        private Dictionary<string, string> filter = new Dictionary<string, string>();

        public void BookGrid_OnLoaded(object sender, RoutedEventArgs e)
        {            
            if (!Directory.Exists(Properties.Settings.Default.FilesDir))
            {
                Info(
                    String.Format(UiLang.Get("DirNotFound"),
                        Properties.Settings.Default.FilesDir), 1);
                return;
            }

            var grid = (DataGrid) sender;
            var extensions = Properties.Settings.Default.FileExtensions.Split(';');
            var dirs = Directory.EnumerateDirectories(Properties.Settings.Default.FilesDir).ToList();
            var bookList = new List<Book>();

            foreach (var dir in dirs)
            {
                var dinfo = new FileInfo(dir);
                var bookFilePath =
                    Directory.EnumerateFiles(dinfo.FullName)
                        .First(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));


                if (!File.Exists(dinfo.FullName + "\\info.dat")) //If info file is missing, attempt to generate new one
                    BookKeeper.GenerateInfo(bookFilePath);

                if (!File.Exists(dinfo.FullName + "\\info.dat"))
                    continue;

                using (var infoFile = new FileStream(dinfo.FullName + "\\info.dat", FileMode.Open))
                {
                    var bf = new BinaryFormatter();
                    var bookInfo = (Dictionary<string, object>) bf.Deserialize(infoFile);
                    var countryCode = "_unknown";
                    //If we can't get proper country code, this fall-back flag image name will be used
                    
                    if (filter.ContainsKey("title") && !((string)bookInfo["title"]).ToLower().Contains(filter["title"].ToLower()))
                        continue;

                    if (filter.ContainsKey("category") && (string) bookInfo["category"] != filter["category"])
                        continue;

                    if (filter.ContainsKey("series") && (string) bookInfo["series"] != filter["series"])
                        continue;

                    if ((string) bookInfo["language"] != "" &&
                        CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                            .FirstOrDefault(x => x.Name == (string) bookInfo["language"]) != null)
                        //Make sure the book language is not neutral (ex: en instead of en-US), or invalid. This will make sure we don't display for example US flag for british english.                         
                    {
                        var cultureInfo = new CultureInfo((string) bookInfo["language"]);
                        countryCode = new RegionInfo(cultureInfo.Name).TwoLetterISORegionName;
                    }

                    bookList.Add(new Book()
                    {
                        Title = (string) bookInfo["title"],
                        Author = (string) bookInfo["author"],
                        Publisher = (string) bookInfo["publisher"],
                        CountryCode = countryCode,
                        Published = (DateTime?) bookInfo["published"],
                        Description = (string) bookInfo["description"],
                        Series = (string) bookInfo["series"],
                        Category = (string) bookInfo["category"],
                        MobiType = (string) bookInfo["mobiType"],
                        Size = Tools.BytesFormat((ulong) bookInfo["size"]),
                        Favorite = (bool) bookInfo["favorite"],
                        Sync = (bool) bookInfo["sync"],
                        DirName = dinfo.FullName
                    });
                }
            }

            grid.ItemsSource = bookList;

            DebugConsole.WriteLine("Reloaded book grid. Book count: " + bookList.Count);
        }

        /// <summary>
        /// Reload the book grid to reflect any change made to it
        /// FilterList textblock will be also reloaded
        /// </summary>
        public void BookGridReload()
        {
            BookGrid_OnLoaded(BookGrid, null);   //Reload the book grid

            //Regenerate the filter list
            if (filter.Count == 0)
            {
                Filter.Visibility = Visibility.Collapsed;                
                return;
            }

            var keyNames = new Dictionary<string, string>()
            {
                {"title", UiLang.Get("BookGridHeaderTitle")},
                {"category", UiLang.Get("BookGridHeaderCategory")},
                {"series", UiLang.Get("BookGridSeries")},
                
            };

            FilterList.Children.Clear();

            foreach (var item in filter)
            {
                var value = new TextBlock()
                {
                    Foreground = Brushes.DodgerBlue,
                    Text = item.Value
                };

                var name = new TextBlock()
                {
                    Foreground = Brushes.Gray,
                    Text = keyNames[item.Key] + ": "
                };

                var separator = new TextBlock()
                {
                    Foreground = Brushes.Gray,
                    Text = "; "
                };

                FilterList.Children.Add(name);
                FilterList.Children.Add(value);
                FilterList.Children.Add(separator);                
                

            }

            Filter.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Filter the books displayed in the grid based on the string from the text field
        /// </summary>
        private void FilterName_OnkeyUp(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox) sender;

            filter["title"] = textBox.Text; //Add selected category name into the filter so only books in that category are displayed

            if (textBox.Text == "" && e.Key == Key.Back)
            {
                filter.Remove("title"); //Remove category from the filter so all categories are displayed
                textBox.Visibility = Visibility.Collapsed;
            }

            BookGridReload();
        }

        /// <summary>
        /// Filter the books displayed in the grid based on the selected category
        /// </summary>        
        private void FilterCategory_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox) sender;            

            filter["category"] = comboBox.SelectedItem.ToString(); //Add selected category name into the filter so only books in that category are displayed

            if (comboBox.SelectedIndex == 0)
            {
                filter.Remove("category"); //Remove category from the filter so all categories are displayed
                comboBox.Visibility = Visibility.Collapsed;
            }

            BookGridReload();
        }


        private void BookGrid_OnKeyUp(object sender, KeyEventArgs e)
        {
            var dataGrid = (DataGrid) sender;
            var forcedSettingValue = (Keyboard.IsKeyDown(Key.LeftShift)
                ? true
                : (Keyboard.IsKeyDown(Key.LeftCtrl) ? false : (bool?) null));

            if (e.Key == Key.Delete)
            {

                if (
                    MessageBox.Show(
                        String.Format(UiLang.Get("DeleteBooksConfirm"),
                            dataGrid.SelectedItems.Count), UiLang.Get("DiscardBook"), MessageBoxButton.YesNo) !=
                    MessageBoxResult.Yes)
                    return;

                foreach (var book in dataGrid.SelectedItems.Cast<Book>().ToList())
                {
                    BookKeeper.Discard(book.DirName);
                }
            }
            else if (e.Key == Key.F)
            {
                foreach (var book in dataGrid.SelectedItems.Cast<Book>().ToList())
                {
                    BookInfoSet("favorite", (forcedSettingValue ?? (!book.Favorite)), book.DirName);
                }

                BookGridReload(); //Reload grid in the main window
            }
            else if (e.Key == Key.S)
            {
                foreach (var book in dataGrid.SelectedItems.Cast<Book>().ToList())
                {
                    BookInfoSet("sync", (forcedSettingValue ?? (!book.Sync)), book.DirName);
                }

                BookGridReload(); //Reload grid in the main window
            }
            else if (e.Key == Key.D)
            {
                foreach (var book in dataGrid.SelectedItems.Cast<Book>().ToList())
                {
                    BookInfoSet("sync", (forcedSettingValue ?? (!book.Sync)), book.DirName);
                    BookInfoSet("favorite", (forcedSettingValue ?? (!book.Favorite)), book.DirName);
                }

                BookGridReload(); //Reload grid in the main window
            }
        }
        
        //If name column header gets right clicked display text for live-filtering the book list
        private void BookGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var obj = e.OriginalSource as TextBlock;            

            //If the category column header gets right clicked display combobox for filtering categories
            if (obj != null && obj.Text == UiLang.Get("BookGridHeaderCategory"))
            {
                var bookList = BookKeeper.List();
                var categoryList = new HashSet<string>{"- - -"};

                foreach (var info in bookList)
                {
                    if (!categoryList.Contains((string)info["category"]) && (string)info["category"] != "")
                        categoryList.Add((string) info["category"]);
                }

                FilterCategory.ItemsSource = categoryList;
                FilterCategory.Visibility = Visibility.Visible;
            }

            if (obj != null && obj.Text == UiLang.Get("BookGridHeaderTitle"))
            {
                FilterName.Visibility = Visibility.Visible;
                FilterName.Focus();
            }
        }

        //Filter book list with only books from the selected series
        private void FilterSeries_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var textBlock = (TextBlock) sender;

            filter["series"] = textBlock.Text;

            BookGridReload();
        }

        private void ClearFilter_OnMouseLeftButtonUp(object sender, RoutedEventArgs routedEventArgs)
        {
            filter.Clear();
            FilterCategory.SelectedIndex = 0;
            FilterCategory.Visibility = Visibility.Collapsed;
            FilterName.Text = String.Empty;
            FilterName.Visibility = Visibility.Collapsed;
            BookGridReload();
        }

        private void Sync_OnClick(object sender, RoutedEventArgs e)
        {
            Info(String.Format(UiLang.Get("SyncDeviceSearch"), Properties.Settings.Default.DeviceModel));

            UsbSync.Sync();
        }

        private void InfoBox_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
            //Upon closing (hiding) the infobox, trigger the info method, to check if there is another info waiting in the queue
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

            Info(UiLang.Get("SyncFilesAdded"));

            var selectedFiles = openFileDialog.FileNames;

            //Fancy animated loading icon in the window title
            var timer = new Timer(300);

            timer.Elapsed += delegate
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.Title = this.Title.Substring(0, 2) == "▣•" ? "•▣" : "▣•";
                    this.Title += " Bookpouch - " + UiLang.Get("SyncAddingBooks");
                });
            };

            timer.Start();

            Task.Factory.StartNew(() =>
            {
                foreach (var file in selectedFiles)
                {
                    BookKeeper.Add(file);

                    this.Dispatcher.Invoke(() => BookGrid_OnLoaded(BookGrid, null));
                        //Refresh the data grid displaying info about books, so we can see any newly added books 
                }

                this.Dispatcher.Invoke(() =>
                {
                    timer.Stop();
                    timer.Close();
                    this.Title = "Bookpouch";
                });
            });





        }

        //Execute the button click event handling method manually from here and then cancel the click, since we need to prevent  showing of the row detail and therefore  cannot wait for full click to be performed
        private void Sync_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SyncToDeviceToggle_OnClick(sender, e);
            e.Handled = true;
        }

        private void SyncToDeviceToggle_OnClick(object sender, RoutedEventArgs e)
        {

            var button = (Button) sender;
            var icon = (Image) VisualTreeHelper.GetChild(button, 0);
            icon.Opacity = (icon.Opacity <= 0.12 ? 1 : 0.12);

            BookInfoSet("sync", (icon.Opacity > 0.9), (string) button.Tag);
        }

        //Execute the button click event handling method manually from here and then cancel the click, since we need to prevent  showing of the row detail and therefore  cannot wait for full click to be performed
        private void Favorite_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            FavoriteToggle_OnClick(sender, e);
            e.Handled = true;
        }

        private void FavoriteToggle_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            var icon = (Image) VisualTreeHelper.GetChild(button, 0);
            icon.Opacity = (icon.Opacity <= 0.12 ? 1 : 0.12);

            BookInfoSet("favorite", (icon.Opacity > 0.9), (string) button.Tag);
        }

        private void EditBook_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            this.IsEnabled = false;
            var editBook = new EditBook {Owner = this};

            editBook.DirName = button.DataContext.ToString();
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
            var about = new AboutBox {Owner = this};
            about.Closed += delegate { this.IsEnabled = true; };
            about.Show();
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e) //Allow user to refresh the book list with F5
        {
            if (e.Key == Key.F5)
                BookGridReload(); //Reload grid
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e) //If the option is set, instead of closing the application minimize it into system tray
        {
            if (!Properties.Settings.Default.CloseIntoTray)
                return;            

            this.Hide();
            TrayIcon.Visible = true;            

            DebugConsole.WriteLine("Minimizing Bookpouch into tray.");
            e.Cancel = true;
        }


      


        //Change value in existing info.dat file for a book
        private void BookInfoSet(string key, object value, string infoFilePath)
        {
            infoFilePath += "/info.dat";

            if (!File.Exists(infoFilePath))
                return;

            Dictionary<string, object> bookInfo;

            using (var infoFile = new FileStream(infoFilePath, FileMode.Open))
            {
                var bf = new BinaryFormatter();
                bookInfo = (Dictionary<string, object>) bf.Deserialize(infoFile);
            }

            if (!bookInfo.ContainsKey(key))
                return;

            bookInfo[key.ToLower()] = value;

            using (var infoFile = new FileStream(infoFilePath, FileMode.Create))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(infoFile, bookInfo);
            }
        }

        internal sealed class Book
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
            public string Category { set; get; }
            public bool Favorite { set; get; }
            public bool Sync { set; get; }
            public string DirName { set; get; }

            public BitmapImage CoverImage
            {
                get
                {
                    var file = Directory.GetFiles(DirName, "cover.*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    BitmapImage cover;

                    try
                    {
                        cover = new BitmapImage();

                        cover.BeginInit();
                        cover.CreateOptions = BitmapCreateOptions.PreservePixelFormat |
                                              BitmapCreateOptions.IgnoreColorProfile;
                        cover.CacheOption = BitmapCacheOption.OnLoad;
                        cover.UriSource = new Uri(@file ?? "img/book.png", UriKind.RelativeOrAbsolute);
                        cover.EndInit();
                    }
                    catch (NotSupportedException)
                    {
                        cover = new BitmapImage(new Uri("img/book.png", UriKind.Relative));
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
