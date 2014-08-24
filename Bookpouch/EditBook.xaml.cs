using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace Bookpouch
{
    /// <summary>
    /// Interaction logic for EditBook.xaml, containing the form for editing book details
    /// </summary>
    public partial class EditBook
    {
        private readonly string _bookFile; //Name or name and path of the directory in which all the files related to the edited book are stored        
        private BookData _bookData; //Dictionary containing the data about the book         
        

        public EditBook(string bookFile)
        {
            _bookFile = bookFile;      
                    
            InitializeComponent();           

            if (Properties.Settings.Default.AutoSavedEditsPopupShown) 
                return;

            MessageBox.Show(UiLang.Get("EditBookAutoSavePopup"));
            Properties.Settings.Default.AutoSavedEditsPopupShown = true;               
            Properties.Settings.Default.Save();
        }

        private void CoverImage_OnLoaded(object sender, RoutedEventArgs e)
        {
            var image = (Image) sender;
            var coverArray = (byte[]) BookInfoGet("Cover");

            BitmapImage cover;

            try
            {
                cover = new BitmapImage();

                cover.BeginInit();
                cover.CreateOptions = BitmapCreateOptions.PreservePixelFormat |
                                      BitmapCreateOptions.IgnoreColorProfile;
                cover.CacheOption = BitmapCacheOption.OnLoad;

                if (coverArray == null)
                    cover.UriSource = new Uri("pack://application:,,,/Bookpouch;component/Img/book.png");
                else
                    cover.StreamSource = new MemoryStream(coverArray);    

                cover.EndInit();
            }
            catch (NotSupportedException)
            {
                cover = new BitmapImage(new Uri("pack://application:,,,/Bookpouch;component/img/book.png")); //Provide default image in case the book cover image exists but is faulty
            }
            

            image.Source = cover;                      
        }

        private void CoverImage_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) //Change the book cover picture
        {
            var image = (Image) sender;
            var openFileDialog = new OpenFileDialog {Filter = "Images|*.png; *.jpg; *.gif; *.tif; *.bmp"};

            if (openFileDialog.ShowDialog() != true) 
                return;            
          
            var cover = new BitmapImage();
            cover.BeginInit();
            cover.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
            cover.CacheOption = BitmapCacheOption.OnLoad;
            cover.StreamSource = openFileDialog.OpenFile();
            cover.EndInit();

            image.Source = cover;

            BookInfoSet("Cover", File.ReadAllBytes(openFileDialog.FileName));
        }

        private void CoverImage_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) //Remove existing book cover picture
        {
            var image = (Image)sender;            
            var cover = new BitmapImage(new Uri("pack://application:,,,/Bookpouch;component/img/book.png"));

            image.Source = cover;

            BookInfoSet("Cover", null);
        }

        private void Language_OnLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var cultureList = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            var languageOptions = cultureList.Select(culture => new Settings.LanguageOption(culture)).ToList();
            var language = (string) BookInfoGet("Language");               
         
            comboBox.ItemsSource = languageOptions;
            comboBox.SelectedIndex = cultureList.Select(culture => culture.Name).ToList().IndexOf(language); //Set combobox position to the book's language            
        }
        

        /// <summary>
        /// Save the language in which the book is written
        /// </summary>        
        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var language = (Settings.LanguageOption) comboBox.SelectedItem;

            BookInfoSet("Language", language.CultureInfo.Name);                        
        }


        /// <summary>
        /// Handle loading  values for all checkboxes
        /// </summary>        
        private void CheckBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            box.IsChecked = (bool) BookInfoGet(box.Name);
        }

        /// <summary>
        /// Handle saving  values for all checkboxes
        /// </summary>        
        private void CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;            
            BookInfoSet(box.Name, box.IsChecked);
        }

        /// <summary>
        /// Handle loading  values for all  textboxes
        /// </summary>        
        private void TextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox) sender;
            textBox.Text = (string) BookInfoGet(textBox.Name);

            if (textBox.Name == "Title")
                base.Title = textBox.Text; //Set the window title to the name of the book
        }

        /// <summary>
        /// Handle saving  values for all  textboxes
        /// </summary>        
        private void TextBox_OnChanged(object sender, RoutedEventArgs e)
        {            
            var textBox = (TextBox)sender;
            BookInfoSet(textBox.Name, textBox.Text);
        }

        /// <summary>
        /// Handle loading  values for all  dateboxes
        /// </summary>        
        private void DatePicker_OnLoaded(object sender, RoutedEventArgs e)
        {
            var datePicker = (DatePicker) sender;
            datePicker.SelectedDate = (DateTime?) BookInfoGet(datePicker.Name);
        }

        /// <summary>
        /// Handle saving values for all  dateboxes
        /// </summary>        
        private void DatePicker_OnChanged(object sender, RoutedEventArgs e)
        {            
            var datePicker = (DatePicker) sender;                       
            BookInfoSet(datePicker.Name, datePicker.SelectedDate);
        }

        private void Series_OnLoaded(object sender, RoutedEventArgs e)
        {
            TextBox_OnLoaded(sender, e);

            var bookData = LibraryStructure.List();
            var hintSet = new HashSet<string>();

            foreach (var info in bookData.Where(info => info.Series != ""))
                hintSet.Add(info.Series);

            var hintList = hintSet.ToList();
            hintList.Sort();
            
            new Whisperer
            {
                TextBox = (TextBox) sender,
                HintList = hintList   
            };            
        }

        ObservableCollection<CategoryTag> categoryTagList = new ObservableCollection<CategoryTag>();

        private void Category_OnLoaded(object sender, RoutedEventArgs e)
        {                        
            var query = Db.Query("SELECT Name, FromFile FROM categories WHERE Path = @Path", new []{new SQLiteParameter("Path", BookKeeper.GetRelativeBookFilePath(_bookFile)), });            

            while (query.Read())
            {                
                var category = new CategoryTag()
                {
                    Name = query["Name"].ToString(),
                    FromFile = SQLiteConvert.ToBoolean(query["FromFile"])
                };
                
                categoryTagList.Add(category);            
                
            }

            if(categoryTagList.Count > 0)
                CategoryTagsBorder.Visibility = Visibility.Visible;

            Categories.ItemsSource = categoryTagList;

            var defaultCategories = Properties.Settings.Default.DefaultCategories.Split(';');
            var hintList = new List<string>(defaultCategories);
            hintList.AddRange(LibraryStructure.CategoryList());                        
            hintList.Sort();

            new Whisperer
            {
                TextBox = (TextBox)sender,
                HintList = hintList
            };
        }    

        /// <summary>
        /// Remove category from the book
        /// </summary>
        private void CategoryTag_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {            
            var categoryTag = (CategoryTag) (((Button) sender).DataContext);

            categoryTagList.Remove(categoryTag);

            if (categoryTagList.Count == 0)
                CategoryTagsBorder.Visibility = Visibility.Collapsed;

            Db.NonQuery("DELETE FROM categories WHERE Name = @Name AND Path = @Path", new []
                {
                    new SQLiteParameter("Name", categoryTag.Name),
                    new SQLiteParameter("Path", BookKeeper.GetRelativeBookFilePath(_bookFile)),
                });
        }

        /// <summary>
        /// Mark automatically added category as user added category
        /// </summary>
        private void CategoryTag_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var categoryTag = (CategoryTag)(((Button)sender).DataContext);

            if (categoryTagList.Count == 0)
                CategoryTagsBorder.Visibility = Visibility.Collapsed;

            Db.NonQuery("UPDATE categories SET FromFile = 0 WHERE Name = @Name AND Path = @Path", new[]
                {
                    new SQLiteParameter("Name", categoryTag.Name),
                    new SQLiteParameter("Path", BookKeeper.GetRelativeBookFilePath(_bookFile)),
                });

            categoryTag.FromFile = false;
            ((Button) sender).BorderBrush = Brushes.DarkRed;
        }

        /// <summary>
        /// Add category to the book
        /// </summary>        
        private void Category_OnKeyUp(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox) sender;
            
            if(textBox.Text.Length <= 1 || (textBox.Text.Substring(textBox.Text.Length - 1) != ";" && textBox.Text.Substring(textBox.Text.Length - 1) != ","))
                return;

            var category = textBox.Text.Substring(0, textBox.Text.Length - 1);
            textBox.Text = String.Empty;

            if (categoryTagList.Any(categoryTag => categoryTag.Name == category))
                return;

            CategoryTagsBorder.Visibility = Visibility.Visible;
            categoryTagList.Add(new CategoryTag { Name = category});            

            Db.NonQuery("INSERT OR IGNORE INTO categories VALUES(@Path, @Name, 0)", new []
            {
                new SQLiteParameter("Path", BookKeeper.GetRelativeBookFilePath(_bookFile)), 
                new SQLiteParameter("Name", category)
            });

        }

        private void Category_OnLostFocus(object sender, RoutedEventArgs e)
        {            
        /*    var textBox = (TextBox)sender;

            if (textBox.Text == String.Empty)
                return;
            textBox.Text += ";";

            Category_OnKeyUp(sender, null);*/
        }

        private class CategoryTag
        {
            public bool FromFile;
            public string Name { set; get; }

            public string Color
            {
                get { return (FromFile ? "RoyalBlue" : "DarkRed");} 
            }
            
        }

        private void Discard_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(String.Format(UiLang.Get("BookDeleteConfirm"), BookInfoGet("Title")), UiLang.Get("BookDeleteConfirmTitle"), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            BookKeeper.Discard(_bookFile);
            Close();
        }

        /// <summary>
        /// Fetch data to fill the form fields, from the BookInfo object based on the key
        /// </summary>
        /// <param name="key">Name of the BookData field from which to get the data</param>
        /// <returns>Value from the BookData field specified by the given key</returns>
        private object BookInfoGet(string key)
        { 
            if (_bookData == null) //Singleton, so we don't have to reopen the DB with saved info, after every form field loads and its load event handler calls BookInfoGet
            {
                try
                {
                    _bookData = BookKeeper.GetData(_bookFile);
                }
                catch (Exception)
                {
                    MainWindow.Info(UiLang.Get("BookInfoNotAvailable"), 1);
                    IsEnabled = false;
                    Close();
                    _bookData = new BookData();
                    return null;
                }
            }
            
            return (typeof(BookData).GetField(key) != null ? typeof(BookData).GetField(key).GetValue(_bookData) : null);
        }

        /// <summary>
        /// Save data from the EditBook fields into the database
        /// </summary>
        /// <param name="key">Name of the BookData field to which to save the data</param>
        /// <param name="value">Value to be saved into the specified field</param>
        private void BookInfoSet(string key, object value)
        {
            if (_bookData == null)
                return;

            typeof(BookData).GetField(key).SetValue(_bookData, value);
            
            try
            {
                BookKeeper.SaveData(_bookData);
            }
            catch (Exception e)
            {
                MainWindow.Info(String.Format(UiLang.Get("DatFileNotAvailable"),  _bookData.Title), 1);
                DebugConsole.WriteLine("Edit book: It was not possible to save the provided value into the data file: " + e.Message);
                Close();
            }         

        }

    }
}
