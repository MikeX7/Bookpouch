using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Libropouch
{
    /// <summary>
    /// Interaction logic for EditBook.xaml
    /// </summary>
    public partial class EditBook
    {
        public EditBook()
        {
            InitializeComponent();
        }

        private void Language_OnLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var langList = new List<Settings.LanguageOption>
            {
                new Settings.LanguageOption("- Automatic -", "", ""),
                new Settings.LanguageOption("English", "en", "US"),
                new Settings.LanguageOption("Česky", "cs", "CZ")
            };

            var position = Array.IndexOf(new[] { "", "en-US", "cs-CZ" }, Properties.Settings.Default.Language);
            comboBox.ItemsSource = langList;
            comboBox.SelectedIndex = (position > 0 ? position : 0);

        }

        //Save language change and also start using the new language
        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var language = (Settings.LanguageOption)comboBox.SelectedItem;
            var languageCode = (language.CountryCode != "" ? String.Format("{0}-{1}", language.Code, language.CountryCode) : "");

            Properties.Settings.Default.Language = languageCode;
            Properties.Settings.Default.Save();

            Thread.CurrentThread.CurrentUICulture = new CultureInfo((languageCode != "" ? languageCode : CultureInfo.CurrentCulture.Name));
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
    }
}
