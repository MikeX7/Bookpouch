using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace Libropouch
{
    /// <summary>
    /// Interaction logic for EditBook.xaml
    /// </summary>
    public partial class EditBook
    {
        public string InfoFile;

        public EditBook()
        {
            InitializeComponent();

            
        }

        private void Language_OnLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var cultureList = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            var languageOptions = cultureList.Select(culture => new Settings.LanguageOption(culture)).ToList();

            var position = Array.IndexOf(new[] { "", "en-US", "cs-CZ" }, Properties.Settings.Default.Language);
            comboBox.ItemsSource = languageOptions;
            comboBox.SelectedIndex = (position > 0 ? position : 0);
        }

        //Save language change and also start using the new language
        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var language = (Settings.LanguageOption) comboBox.SelectedItem;
            

            Properties.Settings.Default.Language = language.cultureInfo.Name;
            Properties.Settings.Default.Save();

            Thread.CurrentThread.CurrentUICulture = language.cultureInfo;
        }


        //Handle loading  values for all settings checkboxes
        private void CheckBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            var prop = Properties.Settings.Default.GetType().GetProperty(box.Name);

            if (prop != null)
                box.IsChecked = (bool)prop.GetValue(Properties.Settings.Default);

        }

        //Handle saving  values for all settings checkboxes
        private void CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            var prop = Properties.Settings.Default.GetType().GetProperty(box.Name);

            if (prop != null)
                prop.SetValue(Properties.Settings.Default, box.IsChecked);

            Properties.Settings.Default.Save();
        }

        //Handle loading  values for all settings textboxes
        private void TextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox) sender;

            using (var infoFile = new FileStream(InfoFile, FileMode.Open))
            {
                var serializer = new XmlSerializer(typeof (BookData));
                var bookInfo = (BookData) serializer.Deserialize(infoFile);

                var prop = bookInfo.GetType().GetProperty(textBox.Name);
                
                Debug.WriteLine(textBox.Name + prop);
                Debug.WriteLine(bookInfo.Title);

                

                
            }
        }
    }
}
