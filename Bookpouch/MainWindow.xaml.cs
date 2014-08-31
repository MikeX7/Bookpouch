using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using ShadoLib;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using DataGrid = System.Windows.Controls.DataGrid;
using DataObject = System.Windows.DataObject;
using DragEventArgs = System.Windows.DragEventArgs;
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
        /// <summary>
        /// Instance of the main window object, containing the GUI
        /// </summary>      
        // ReSharper disable once InconsistentNaming
        public static MainWindow MW;
        private static NotifyIcon _trayIcon;

        public MainWindow()
        {
            if (Properties.Settings.Default.OnlyOne)
                if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Count() > 1) Process.GetCurrentProcess().Kill(); //Only allow one instance of Bookpouch

            MW = this;
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); 
            
            if (path != null) 
                Environment.CurrentDirectory = path; //Make sure the app's directory is correct, in case we launched via registry entry during boot
            
            InitializeComponent();
            GenerateFilterPresetList();
            AllowDrop = true;
            Drop += Add_OnDrop;

            if(Properties.Settings.Default.DebugOnStart)
                DebugConsole.Open();
            
            _trayIcon = new NotifyIcon
            {
                Icon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Bookpouch;component/Img/BookPouch1.ico")).Stream),
                Visible = false,
                Text = "Bookpouch"
            };

            _trayIcon.Click += (o, args) => 
                {
                    Show();
                    _trayIcon.Visible = false;
                };
            
            if (Environment.GetCommandLineArgs().Contains("-tray")) 
            {
                Hide();
                _trayIcon.Visible = true;  
            }

            if (Properties.Settings.Default.UsbAutoSync)
                new ReaderDetector(this);
                    //Start reader detection which automatically triggers UsbSync when the reader is connected to the pc         
            
        }        

        private static void Add_OnDrop(object sender, DragEventArgs e)
        {
            if (!(e.Data is DataObject) || !((DataObject) e.Data).ContainsFileDropList()) 
                return;

            var list = ((DataObject) e.Data).GetFileDropList().Cast<string>().ToList();

            AddBooksFromList(list);
        }

        private void BookGrid_OnLoaded(object sender, RoutedEventArgs e)
        {            
            DebugConsole.WriteLine("Loading the book grid...");
            var stopwatch = new Stopwatch();
            stopwatch.Start();            
            var grid = (DataGrid) sender;
            var sql = AssembleQuery();                        
            var categoryList = new Dictionary<string, List<string>>();
            var bookDataList = new List<BookData>();
            var bookList = new List<Book>();
            var cultureList = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            //Turn rows form the db into BookData objects
            using (var query = Db.Query(sql.Item1, sql.Item2))
            {
                //Add list of attached categories to each BookData object
                using (var queryAllCategories = Db.Query("SELECT Path, Name FROM categories")) //Pull all categories from database, so that we don't have to do a separate query for each book
                {
                    //Turn category rows into a single dictionary, where each key is a Path, whose value is a List of category Names associated with said Path
                    while (queryAllCategories.Read())
                    {
                        var key = queryAllCategories["Path"].ToString();
                        var value = queryAllCategories["Name"].ToString();

                        if (categoryList.ContainsKey(key))
                            categoryList[key].Add(value);
                        else
                            categoryList.Add(key, new List<string> {value});
                    }
                }

                //Cast book rows into BookData objects and assign them their category lists
                while (query.Read())
                {
                    var bookData = BookKeeper.CastSqlBookRowToBookData(query);

                    if (categoryList.ContainsKey(query["Path"].ToString()))
                        bookData.Categories = categoryList[query["Path"].ToString()];

                    bookDataList.Add(bookData);
                }

            }

            foreach (var bookData in bookDataList)
            {               
                var countryCode = "_unknown";

                if (bookData.Language != "" &&
                    cultureList.FirstOrDefault(x => x.Name == bookData.Language) != null)
                    //Make sure the book language is not neutral (ex: en instead of en-US), or invalid. This will make sure we don't display for example US flag for british english.                         
                {                    
                    countryCode = new RegionInfo((new CultureInfo(bookData.Language)).Name).TwoLetterISORegionName;
                }
                
                bookList.Add(new Book
                {
                    Title = bookData.Title,
                    Author = bookData.Author,
                    Publisher = bookData.Publisher,
                    CountryCode = countryCode,
                    Published = bookData.Published,
                    Description = bookData.Description,
                    Series = bookData.Series,
                    Categories = bookData.Categories,                    
                    Size = Tools.BytesFormat(bookData.Size),
                    Favorite = bookData.Favorite,
                    Sync = bookData.Sync,
                    Cover = bookData.Cover,
                    BookFile = bookData.Path,                    
                });
                
            }
                        
            grid.ItemsSource = bookList.OrderBy(x => x.Title); //By default, the list is sorted by book titles. Note: this is much faster than ORDER BY Title directly in the sqlite query

            stopwatch.Stop();
            DebugConsole.WriteLine("Book grid loaded. Book count: " + bookList.Count + ", load time: " + stopwatch.Elapsed);                        
        }

        /// <summary>
        /// Reload the book grid to reflect any change made to it
        /// FilterList textblock will be also reloaded
        /// </summary>
        public void BookGridReload()
        {
            BookGrid_OnLoaded(BookGrid, null);   //Reload the book grid
            GenerateFilterView();
            GenerateFilterPresetList();
        }

        private readonly Timer _searchStartTimer = new Timer(1000);   

        /// <summary>
        /// Trigger a filtered in titles, after the user stops typing into the field
        /// </summary>        
        private void FilterName_OnkeyUp(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox)sender;

            if (textBox.Text == "" && e.Key == Key.Back) //Remove the title search parameter from the filter
            {
                Filter.Title = null; 
                textBox.Visibility = Visibility.Collapsed;
                _searchStartTimer.Stop();
                BookGridReload();
                return;
            }            

            if (_searchStartTimer.Enabled)
            {
                _searchStartTimer.Stop();
                _searchStartTimer.Start();
                return;
            }

            _searchStartTimer.Elapsed -= FilterNameSearch;
            _searchStartTimer.Elapsed += FilterNameSearch;
            _searchStartTimer.AutoReset = false;
            _searchStartTimer.Start();
        }

        /// <summary>
        /// Add a title search parameter into the filter
        /// </summary>
        private void FilterNameSearch(object sender, ElapsedEventArgs e)
        {                       
            Dispatcher.Invoke(() =>
            {
                if (String.IsNullOrEmpty(FilterName.Text))
                    return;

                Filter.Title = FilterName.Text;
                //Add selected category name into the filter so only books in that category are displayed

                BookGridReload();
            });
        }

        /// <summary>
        /// FilterWrap the books displayed in the grid based on the selected category
        /// </summary>        
        private void FilterCategory_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
     /*       var comboBox = (ComboBox) sender;            

            Filter.Category = comboBox.SelectedItem.ToString(); //Add selected category name into the filter so only books in that category are displayed

            if (comboBox.SelectedIndex == 0)
            {
                Filter.Category = null; //Remove category from the filter so all categories are displayed
                comboBox.Visibility = Visibility.Collapsed;
            }

            BookGridReload();*/
        }


        private void BookGrid_OnKeyUp(object sender, KeyEventArgs e)
        {
            var dataGrid = (DataGrid) sender;
            var forcedSettingValue = (Keyboard.IsKeyDown(Key.LeftShift)
                ? true
                : (Keyboard.IsKeyDown(Key.LeftCtrl) ? false : (bool?) null));

            switch (e.Key)
            {
                case Key.Delete:
                    if (
                        MessageBox.Show(
                            String.Format(UiLang.Get("DeleteBooksConfirm"),
                                dataGrid.SelectedItems.Count), UiLang.Get("DiscardBook"), MessageBoxButton.YesNo) !=
                        MessageBoxResult.Yes)
                        return;

                    foreach (var book in dataGrid.SelectedItems.Cast<Book>().ToList())
                        BookKeeper.Discard(book.BookFile);

                    break;
                case Key.F:
                    foreach (var book in dataGrid.SelectedItems.Cast<Book>().ToList())
                        BookInfoSet("Favorite", (forcedSettingValue ?? (!book.Favorite)), book.BookFile);

                    BookGridReload(); //Reload grid in the main window
                    break;
                case Key.S:
                    foreach (var book in dataGrid.SelectedItems.Cast<Book>().ToList())
                        BookInfoSet("Sync", (forcedSettingValue ?? (!book.Sync)), book.BookFile);

                    BookGridReload(); //Reload grid in the main window
                    break;
                case Key.D:
                    foreach (var book in dataGrid.SelectedItems.Cast<Book>().ToList())
                    {
                        BookInfoSet("Sync", (forcedSettingValue ?? (!book.Sync)), book.BookFile);
                        BookInfoSet("Favorite", (forcedSettingValue ?? (!book.Favorite)), book.BookFile);
                    }
                    BookGridReload(); //Reload grid in the main window
                    break;
            }
        }
        
        //If name column header gets right clicked display text for live-filtering the book list
        private void BookGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var obj = e.OriginalSource as TextBlock;            

            if (obj == null || obj.Text != UiLang.Get("FieldTitle")) 
                return;

            FilterName.Visibility = Visibility.Visible;
            FilterName.Focus();
        }

        //Filter book list with only books from the selected series
        private void FilterSeries_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var textBlock = (TextBlock) sender;
            Filter.Series = textBlock.Text;

            BookGridReload();
        }

        //Filter book list with only books from the selected author
        private void FilterAuthor_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var textBlock = (TextBlock)sender;
            Filter.Author = textBlock.Text;

            BookGridReload();
        }

        //Filter book list with only books from the selected publisher
        private void FilterPublisher_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var textBlock = (TextBlock)sender;
            Filter.Publisher = textBlock.Text;

            BookGridReload();
        }

        private void ClearFilter_OnMouseLeftButtonUp(object sender, RoutedEventArgs routedEventArgs)
        {
            Filter = new BookFilter();
            FilterName.Text = String.Empty;
            FilterName.Visibility = Visibility.Collapsed;
            BookGridReload();
        }

        private void Sync_OnClick(object sender, RoutedEventArgs e)
        {
            Info(String.Format(UiLang.Get("SyncDeviceSearch"), Properties.Settings.Default.DeviceModel));
            UsbSync.ManualSync = true;
            UsbSync.Sync();
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
           
            AddBooksFromList(selectedFiles);
        }

        private static void AddBooksFromList(IEnumerable<string> list)
        {
            Busy(true);

            Task.Factory.StartNew(() =>
            {
                foreach (var file in list)
                    BookKeeper.Add(file);

                MW.Dispatcher.Invoke(() =>
                {
                    LibraryStructure.GenerateFileTree();
                    MW.BookGridReload();
                });

                Busy(false);
            });
        }

        private void DataStructureSync_OnClick(object sender, RoutedEventArgs e)
        {
            Info(UiLang.Get("SyncingDataStructure"));
            LibraryStructure.SyncDbWithFileTree();           
        }

        private void Filter_OnClick(object sender, RoutedEventArgs e)
        {
            var filterWindow = new FilterWindow
            {
                Owner = this
            };

            OpenFilter.IsEnabled = false;

            filterWindow.Closed += (o, args) => { OpenFilter.IsEnabled = true; };

            filterWindow.Show();
        }

        //Execute the button click event handling method manually from here and then cancel the click, since we need to prevent  showing of the row detail and therefore  cannot wait for full click to be performed
        private void Sync_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var button = (Button)sender;
            var icon = (Image)VisualTreeHelper.GetChild(button, 0);
            icon.Opacity = (icon.Opacity <= 0.12 ? 1 : 0.12);

            BookInfoSet("Sync", (icon.Opacity > 0.9), (string)button.Tag);
            e.Handled = true;
        }
        

        //Execute the button click event handling method manually from here and then cancel the click, since we need to prevent  showing of the row detail and therefore  cannot wait for full click to be performed
        private void Favorite_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var button = (Button)sender;
            var icon = (Image)VisualTreeHelper.GetChild(button, 0);
            icon.Opacity = (icon.Opacity <= 0.12 ? 1 : 0.12);
                        
            BookInfoSet("Favorite", (icon.Opacity > 0.9), (string)button.Tag);
            e.Handled = true;
        }        

        private void EditBook_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            var bookFile = button.Tag.ToString();             

            IsEnabled = false;
            var editBook = new EditBook(bookFile)
            {
                Owner = this, 
            };
            
            editBook.Closed += delegate { IsEnabled = true; };
            editBook.Show();
        }

        private void BookDir_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var bookFile = button.Tag.ToString();
            var path = Path.GetDirectoryName(bookFile);

            if (path != null && Directory.Exists(path))
                Process.Start(@path);
        }

        private void RootDir_OnClick(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Properties.Settings.Default.BooksDir))
                Process.Start(Properties.Settings.Default.BooksDir);            
        }

        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            var settings = new Settings {Owner = this};
            settings.Closed += delegate { IsEnabled = true; };
            settings.Show();
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            var about = new AboutBox {Owner = this};
            about.Closed += delegate { IsEnabled = true; };
            about.Show();
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e) //Allow user to refresh the book list with F5
        {
            switch (e.Key)
            {
                case Key.F5:
                    BookGridReload(); //Reload grid
                    break;
                case Key.F12:
                    //Clear the info stack
                    InfoQueue.Clear();
                    Info(UiLang.Get("InfoQueueDeleted"));
                    break;
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e) //If the option is set, instead of closing the application minimize it into system tray
        {
            if (!Properties.Settings.Default.CloseIntoTray)
                return;            

            Hide();
            _trayIcon.Visible = true;            

            DebugConsole.WriteLine("Minimizing Bookpouch into tray.");
            e.Cancel = true;
        }     

        //Change value in existing .dat file for a book
        private static void BookInfoSet(string key, object value, string bookFile)
        {                                
            try
            {                
                var bookData = BookKeeper.GetData(bookFile);

                if ((typeof (BookData)).GetField("Sync") == null)
                    return;                

                typeof(BookData).GetField(key).SetValue(bookData, value);
                BookKeeper.SaveData(bookData);
            }
            catch (Exception e)
            {
                DebugConsole.WriteLine("Edit book: It was not possible to save the provided book data: " + e.Message);  
            }    
        }


    }    

    /// <summary>
    /// Get maximum width for the detail bubble in the datagrid, calculated from the datagrid width, to make surethe detail bubble won't be wider than the datagrid
    /// </summary>
    public class BookGridMaxRowDetailWidth : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
                return System.Convert.ToInt32(value) - 35;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value) + 35;
        }
    }
}
