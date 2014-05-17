using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
            var langList = new List<LanguageOption>
            {
                new LanguageOption("- Automatic -", "", ""),
                new LanguageOption("English", "en", "US"),
                new LanguageOption("Česky", "cs", "CZ")
            };

            var position = Array.IndexOf(new[] {"", "en-US", "cs-CZ"}, Properties.Settings.Default.Language);
            comboBox.ItemsSource = langList;
            comboBox.SelectedIndex = (position > 0 ? position : 0);
            
        }

        //Save language change and also start using the new language
        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var language = (LanguageOption) comboBox.SelectedItem;
            var languageCode = (language.CountryCode != "" ? String.Format("{0}-{1}", language.Code, language.CountryCode) : "");

            Properties.Settings.Default.Language = languageCode;            
            Properties.Settings.Default.Save();

            Thread.CurrentThread.CurrentUICulture = new CultureInfo((languageCode != "" ? languageCode : CultureInfo.CurrentCulture.Name));        
        }

        //Populating reader drop down list
        private void Reader_OnLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var langList = new List<ReaderOption>
            {
                new ReaderOption("kindle", "KINDLE", "Documents"),
                new ReaderOption("nook", "NOOK", "my documents"),                
            };

            var position = Array.IndexOf(new[] { "kindle", "Nook"}, Properties.Settings.Default.UsbModel);
            comboBox.ItemsSource = langList;
            comboBox.SelectedIndex = (position > 0 ? position : 0);

        }

        //Saving change from the reader drop down list
        private void Reader_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var readerOption = (ReaderOption) comboBox.SelectedItem;

            Properties.Settings.Default.UsbModel = readerOption.Model;
            Properties.Settings.Default.UsbPnpDeviceId = readerOption.PnpDeviceId;
            Properties.Settings.Default.Save();
        }

        //Handle loading  values for all settings checkboxes
        private void CheckBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            var prop = Properties.Settings.Default.GetType().GetProperty(box.Name);

            if(prop != null)
                box.IsChecked = (bool) prop.GetValue(Properties.Settings.Default);                       

        }

        //Handle saving  values for all settings checkboxes
        private void CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox) sender;
            var prop = Properties.Settings.Default.GetType().GetProperty(box.Name);             

            if(prop != null)
                prop.SetValue(Properties.Settings.Default, box.IsChecked);                      

            Properties.Settings.Default.Save();            
        }

        //Handle loading for all settings text fields
        private void TextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var box = (TextBox) sender;
            var prop = Properties.Settings.Default.GetType().GetProperty(box.Name);

            if (prop != null)
                box.Text = (string) prop.GetValue(Properties.Settings.Default);

        }

        //Handle saving for all settings text fields
        private void TextBox_OnChecked(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            var prop = Properties.Settings.Default.GetType().GetProperty(box.Name);

            if (prop != null)
                prop.SetValue(Properties.Settings.Default, box.IsChecked);

            Properties.Settings.Default.Save();
        }

        internal sealed class LanguageOption //Class representing items in the language selection drop down menu
        {
            public string Name { private set; get; }
            public string FlagPath { private set; get; }

            public readonly string Code;
            public readonly string CountryCode;

            public LanguageOption(string name, string code, string coutnryCode)
            {
                Name = name;
                Code = code;
                CountryCode = coutnryCode;                
                FlagPath = "Flags/" + (coutnryCode != "" ? coutnryCode : "_unknown") + ".png";                
            }

        }

        internal sealed class ReaderOption //Class representing items in the reader selection drop down menu
        {            
            public string Model { private set; get; }

            public string PnpDeviceId;
            public string RootDir;
            public string ImagePath { get { return "Img/" + Model.ToLower() + ".png"; } }            

            public ReaderOption(string model, string pnpDeviceId, string rootDir)
            {                
                Model = model;
                PnpDeviceId = pnpDeviceId;
                RootDir = rootDir;
            }

        }

     
    }
    

}
