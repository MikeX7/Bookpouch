using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace Bookpouch
{
    /// <summary>
    /// Interaction logic for EditBook.xaml
    /// </summary>
    public partial class EditBook
    {        
        public string DirName; //Name or name and path of the direcotry in which all the files related to the edited book are stored        
        private Dictionary<string, object> _bookInfo; //Dictionary containing the data about the book         
        

        public EditBook()
        {
            InitializeComponent();
            Debug.WriteLine("UI language: " + Thread.CurrentThread.CurrentUICulture);
        }

        private void CoverImage_OnLoaded(object sender, RoutedEventArgs e)
        {
            var image = (Image) sender;
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
                cover = new BitmapImage(new Uri("img/book.png", UriKind.Relative)); //Provide default image in case the book cover image exists but is faulty
            }
            

            image.Source = cover;                      
        }

        private void CoverImage_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) //Change the book cover picture
        {
            var image = (Image) sender;
            var openFileDialog = new OpenFileDialog {Filter = "Images|*.png; *.jpg; *.gif; *.tif; *.bmp"};

            if (openFileDialog.ShowDialog() != true) 
                return;            

            image.Source = null;

            var oldCover = Directory.GetFiles(DirName, "cover.*", SearchOption.TopDirectoryOnly).FirstOrDefault();  

            if(oldCover != null)
                File.Delete(oldCover);
 
            var file = File.Create(DirName + "/cover" + Path.GetExtension(openFileDialog.FileName));            
            var newFile = openFileDialog.OpenFile();

            newFile.CopyTo(file);
            newFile.Close();
            file.Close();

            var cover = new BitmapImage();
            cover.BeginInit();
            cover.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
            cover.CacheOption = BitmapCacheOption.OnLoad;
            cover.UriSource = new Uri(openFileDialog.FileName, UriKind.Absolute);
            cover.EndInit();

            image.Source = cover;
        }

        private void CoverImage_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) //Remove existing book cover picture
        {
            var image = (Image)sender;            

            var oldCover = Directory.GetFiles(DirName, "cover.*", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (oldCover == null)
                return;
            
            File.Delete(oldCover);

            var cover = new BitmapImage(new Uri("img/book.png", UriKind.Relative));

            image.Source = cover;
        }

        private void Language_OnLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var cultureList = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            var languageOptions = cultureList.Select(culture => new Settings.LanguageOption(culture)).ToList();
            var language = (string) BookInfoGet("language");               
         
            comboBox.ItemsSource = languageOptions;
            comboBox.SelectedIndex = cultureList.Select(culture => culture.Name).ToList().IndexOf(language); //Set combobox position to the book's language            
        }

        //Save book language change
        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var language = (Settings.LanguageOption) comboBox.SelectedItem;

            BookInfoSet("language", language.CultureInfo.Name);                        
        }


        //Handle loading  values for all checkboxes
        private void CheckBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            box.IsChecked = (bool) BookInfoGet(box.Name);
        }

        //Handle saving  values for all checkboxes
        private void CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;            
            BookInfoSet(box.Name, box.IsChecked);
        }

        //Handle loading  values for all  textboxes
        private void TextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox) sender;
            textBox.Text = (string) BookInfoGet(textBox.Name);

            if (textBox.Name == "Title")
                base.Title = textBox.Text; //Set the window title to the name of the book
        }

        //Handle saving  values for all  textboxes
        private void TextBox_OnChanged(object sender, RoutedEventArgs e)
        {            
            var textBox = (TextBox)sender;
            BookInfoSet(textBox.Name, textBox.Text);
        }

        //Handle loading  values for all  dateboxes
        private void DatePicker_OnLoaded(object sender, RoutedEventArgs e)
        {
            var datePicker = (DatePicker) sender;
            datePicker.SelectedDate = (DateTime?) BookInfoGet(datePicker.Name);
        }

        //Handle saving values for all  dateboxes
        private void DatePicker_OnChanged(object sender, RoutedEventArgs e)
        {            
            var datePicker = (DatePicker) sender;                       
            BookInfoSet(datePicker.Name, datePicker.SelectedDate);
        }

        private void Series_OnLoaded(object sender, RoutedEventArgs e)
        {
            TextBox_OnLoaded(sender, e);

            var bookInfo = BookKeeper.List();
            var hintSet = new HashSet<string>();

            foreach (var info in bookInfo.Where(info => (string) info["series"] != ""))
                hintSet.Add((string) info["series"]);

            var hintList = hintSet.ToList();
            hintList.Sort();
            
            new Whisperer
            {
                TextBox = (TextBox) sender,
                HintList = hintList   
            };            
        }

        private void Category_OnLoaded(object sender, RoutedEventArgs e)
        {
            TextBox_OnLoaded(sender, e);

            var bookInfo = BookKeeper.List();
            var defaultCategories = Properties.Settings.Default.DefaultCategories.Split(';');
            var hintSet = new HashSet<string>(defaultCategories);

            foreach (var info in bookInfo.Where(info => (string)info["category"] != ""))
                hintSet.Add((string)info["category"]);

            var hintList = hintSet.ToList();
            hintList.Sort();

            new Whisperer
            {
                TextBox = (TextBox)sender,
                HintList = hintList
            };
        }
        private void Discard_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(String.Format("Do you really want to pernamently delete {0}?", BookInfoGet("title")), "Discard book?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            BookKeeper.Discard(DirName);
            Close();
        }

        private object BookInfoGet(string key) //Fetch data to fill the form fields, from the bookinfo dictionary based on the key
        {
            if (_bookInfo == null) //Singleton, so we don't have to reopen the file with saved info, after every form field loads and its load event handler calls BookInfoGet
            {
                using (var infoFile = new FileStream(DirName + "/info.dat", FileMode.Open))
                {
                    var bf = new BinaryFormatter();
                    _bookInfo = (Dictionary<string, object>) bf.Deserialize(infoFile);                    
                }
            }

            return _bookInfo.ContainsKey(key.ToLower()) ? _bookInfo[key.ToLower()] : null;
        }

        private void BookInfoSet(string key, object value)
        {
            if (!_bookInfo.ContainsKey(key.ToLower()))
                return;

            _bookInfo[key.ToLower()] = value;

            using (var infoFile = new FileStream(DirName + "/info.dat", FileMode.Create))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(infoFile, _bookInfo);
            }            

        }
    }
}
