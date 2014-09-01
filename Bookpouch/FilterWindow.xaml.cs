#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            languageOptions.Insert(0, new Settings.LanguageOption(CultureInfo.InvariantCulture){Name = "- - -"}); //Empty language, to remove the language parameter from the filter
            comboBox.ItemsSource = languageOptions;   
        }

        /// <summary>
        /// Save the language in which the book is written
        /// </summary>        
        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var language = (Settings.LanguageOption)comboBox.SelectedItem;            

            FilterSet("Language", (language.Name == "- - -" ? String.Empty : language.CultureInfo.Name));
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

        private void TextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox) sender;

            try
            {
                var whisperer = EditBook.GetWhispererForColumn(textBox.Name);
                whisperer.TextBox = textBox;
            }
            catch (RowNotInTableException exception)
            {
                DebugConsole.WriteLine("FilterWindow: Not possible to load Whisperer for " + textBox.Name + ": " + exception);
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

        private void ApplyFilter_OnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.MW.BookGridReload();
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

            type.GetField(key).SetValue(MainWindow.MW.Filter, value);            
        }      
    }
}
