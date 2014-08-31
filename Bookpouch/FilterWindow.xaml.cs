#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#endregion

namespace Bookpouch
{
    /// <summary>
    /// Interaction logic for EditBook.xaml, containing the form for editing book details
    /// </summary>
    public partial class FilterWindow
    {
        public FilterWindow()
        {            
            InitializeComponent();

            if (Properties.Settings.Default.FilterPopupHintShown)
                return;

            MessageBox.Show(UiLang.Get("FilterFirstUsePopup"));
            Properties.Settings.Default.FilterPopupHintShown = true;
            Properties.Settings.Default.Save();
        }


        private void Language_OnLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var cultureList = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            var languageOptions = cultureList.Select(culture => new Settings.LanguageOption(culture)).ToList();

            comboBox.ItemsSource = languageOptions;   
        }


        /// <summary>
        /// Save the language in which the book is written
        /// </summary>        
        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var language = (Settings.LanguageOption)comboBox.SelectedItem;

            FilterSet("Language", language.CultureInfo.Name);
        }


        

        /// <summary>
        /// Handle saving  values for all checkboxes
        /// </summary>        
        private void CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            FilterSet(box.Name, box.IsChecked);
        }


        /// <summary>
        /// Handle saving  values for all  textboxes
        /// </summary>        
        private void TextBox_OnChanged(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            FilterSet(textBox.Name, textBox.Text);
        }

        /// <summary>
        /// Handle saving values for all  dateboxes
        /// </summary>        
        private void DatePicker_OnChanged(object sender, RoutedEventArgs e)
        {
            var datePicker = (DatePicker)sender;
            FilterSet(datePicker.Name, datePicker.SelectedDate);
            Debug.WriteLine(datePicker.SelectedDate);
        }

        private void PublishedRange_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;

            switch (button.Content.ToString())
            {
                case "=":
                    button.Content = ">";
                    FilterSet("PublishedRange", 1);
                    break;
                case ">":
                    button.Content = "<";
                    FilterSet("PublishedRange", 2);
                    break;
                case "<":
                    button.Content = "=";
                    FilterSet("PublishedRange", 0);
                    break;
            }
            
        }

        private void CreatedRange_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            switch (button.Content.ToString())
            {
                case "=":
                    button.Content = ">";
                    FilterSet("CreatedRange", 1);
                    break;
                case ">":
                    button.Content = "<";
                    FilterSet("CreatedRange", 2);
                    break;
                case "<":
                    button.Content = "=";
                    FilterSet("CreatedRange", 0);
                    break;
            }
            
        }

        private void Series_OnLoaded(object sender, RoutedEventArgs e)
        {
            var bookData = LibraryStructure.List();
            var hintSet = new HashSet<string>();

            foreach (var info in bookData.Where(info => info.Series != ""))
                hintSet.Add(info.Series);

            var hintList = hintSet.ToList();
            hintList.Sort();

            new Whisperer
            {
                TextBox = (TextBox)sender,
                HintList = hintList
            };
        }        

        private void Author_OnLoaded(object sender, RoutedEventArgs e)
        {
            using (var query = Db.Query("SELECT DISTINCT Author FROM books GROUP BY Author COLLATE NOCASE"))
            {
                var hintList = new List<string>();

                while (query.Read())
                    hintList.Add(query["Author"].ToString());
                
                hintList.Sort();

                new Whisperer
                {
                    TextBox = (TextBox)sender,
                    HintList = hintList
                };
            }
        }

        private void Category_OnLoaded(object sender, RoutedEventArgs e)
        {                              
            var hintList = LibraryStructure.CategoryList();
            hintList.Sort();            

            new Whisperer
            {
                TextBox = (TextBox) sender,
                HintList = hintList
            };
        }

        /// <summary>
        /// Save search parameters into the filter list
        /// </summary>
        /// <param name="key">Name of the filter field to which to save the data</param>
        /// <param name="value">Value to be saved into the specified field</param>
        private static void FilterSet(string key, object value)
        {
            var type = typeof (MainWindow.BookFilter);

            if (key == null || type.GetField(key) == null)
                return;
         Debug.WriteLine("ooo");
            type.GetField(key).SetValue(MainWindow.MW.Filter, value);

            MainWindow.MW.BookGridReload();
        }
  
    }
}
