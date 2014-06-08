using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Libropouch
{
    /// <summary>
    /// Interaction logic for EditBook.xaml
    /// </summary>
    public partial class EditBook
    {
        public string InfoFile; //Path to the info.dat file for the edited book
        private Dictionary<string, object> _bookInfo; //Dictionary containing the data about the book
        

        public EditBook()
        {
            InitializeComponent();
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
        private void Series_OnKeyUp(object sender, KeyEventArgs e)
        {
            //Whisperer
            var textBox = (TextBox) sender;
            Whisperer.TextBox = textBox;
            Whisperer.HintList = new List<string>{"lol", "that feel when no gf", "feels bad man :(", "tfw no gf", "tfw no qt3.14","pppppppppppppppppppppppp"};
            Whisperer.Pop();   

            if (e.Key == Key.Down)            
                Whisperer.Focus();
        }

        private object BookInfoGet(string key) //Fetch data to fill the form fields, from the bookinfo dictionary based on the key
        {
            if (_bookInfo == null) //Singleton, so we don't have to reopen the file with saved info, after every form field loads and its load event handler calls BookInfoGet
            {
                using (var infoFile = new FileStream(InfoFile, FileMode.Open))
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

            using (var infoFile = new FileStream(InfoFile, FileMode.Create))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(infoFile, _bookInfo);
            }            

        }


     
    }
}
