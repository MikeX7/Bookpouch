using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Libropouch
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
        
    public partial class Settings
    {
        public Settings()
        {            
            InitializeComponent();
        }

        private void Language_OnLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var langList = new List<Language>
            {
                new Language("English", "en", "US"),
                new Language("Česky", "cs", "CZ")
            };
         
            comboBox.ItemsSource = langList;

        }

        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var language = (Language) comboBox.SelectedItem;
            var languageCode = String.Format("{0}-{1}", language.Code, language.CountryCode);

            Properties.Settings.Default.Language = languageCode;            
            Properties.Settings.Default.Save();

            Thread.CurrentThread.CurrentUICulture = new CultureInfo(languageCode);
            //MainWindow.Lang.GetString();
        }

        internal new sealed class Language //Class representing items in the language selection drop down menu
        {
            public string Name { private set; get; }
            public string FlagPath { private set; get; }

            public readonly string Code;
            public readonly string CountryCode;

            public Language(string name, string code, string coutnryCode)
            {
                Name = name;
                Code = code;
                CountryCode = coutnryCode;
                FlagPath = "flags/" + coutnryCode + ".png";
            }

        }
    }
    

}
