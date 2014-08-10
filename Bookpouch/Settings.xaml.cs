using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ShadoLib;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;


namespace Bookpouch
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>

    public partial class Settings
    {
        private readonly string[] _supportedLanguages = {"en-US", "cs-CZ"}; //List of languages for which we have translation files
        public Settings()
        {            
            InitializeComponent();
        }

        public static void SelectBooksDir()
        {
            var booksDirDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = String.Format(UiLang.Get("SelectBookDirPrompt"), Properties.Settings.Default.BooksDir)
            };

            if (Directory.Exists(Properties.Settings.Default.BooksDir))
                booksDirDialog.SelectedPath = Properties.Settings.Default.BooksDir;

            var result = booksDirDialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK) 
                return;

            try
            {
                File.GetAccessControl(booksDirDialog.SelectedPath);

                Properties.Settings.Default.BooksDir = booksDirDialog.SelectedPath;
                Properties.Settings.Default.BooksDirSelectionOffered = true;
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
                MessageBox.Show(UiLang.Get("DirAccessDenied"));
            }                       
        }

        private void Language_OnLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var langList = new List<LanguageOption>
            {
                //new LanguageOption(CultureInfo.CurrentUICulture),
                new LanguageOption(CultureInfo.GetCultureInfo("en-US")),
                new LanguageOption(CultureInfo.GetCultureInfo("cs-CZ"))
            };

            var position = Array.IndexOf(_supportedLanguages, Properties.Settings.Default.Language);
            comboBox.ItemsSource = langList;
            comboBox.SelectedIndex = position;

        }

        //Save language change and also start using the new language
        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var language = (LanguageOption) comboBox.SelectedItem;

            Properties.Settings.Default.Language = language.CultureInfo.Name;            
            Properties.Settings.Default.Save();

            Thread.CurrentThread.CurrentUICulture = language.CultureInfo;
        }

        //Populating reader drop down list
        private void Reader_OnLoaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var langList = new List<ReaderOption>
            {                    
                new ReaderOption("kindle", "KINDLE", "Documents"),
                new ReaderOption("nook", "NOOK", "my documents"),                
                new ReaderOption("- other -", "", ""),        
            };

            var position = Array.IndexOf(new[] { "kindle", "nook"}, Properties.Settings.Default.DeviceModel);
            comboBox.ItemsSource = langList;            
            comboBox.SelectedIndex = (position != -1 ? position : 2);

        }

        //Saving change from the reader drop down list
        private void Reader_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox) sender;
            var readerOption = (ReaderOption) comboBox.SelectedItem;

            if (readerOption.PnpDeviceId == "") //If user selects unknown device, don't save anything and display form for manual device info input
            {
                UnknownDeviceForm.Visibility = Visibility.Visible;
                return;
            }
            
            UnknownDeviceForm.Visibility = Visibility.Collapsed;

            Properties.Settings.Default.DeviceModel = readerOption.Model;
            Properties.Settings.Default.DevicePnpId = readerOption.PnpDeviceId;
            Properties.Settings.Default.DeviceBooksDir = readerOption.RootDir;
            Properties.Settings.Default.Save();

        }

        private void UnknownDeviceHint_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(UiLang.Get("UnknownDeviceHint"));
        }

        private void AutoStart_OnLoaded(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;

            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);

            if (key.GetValue("Bookpouch") != null)
                checkBox.IsChecked = true;
        }

        //Add or remove this applications exe from the registry for the purposes of automatically starting during the system boot
        private void AutoStart_OnChecked(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (box.IsChecked == true)            
                key.SetValue("Bookpouch", Assembly.GetExecutingAssembly().Location + " -tray");            
            else
                key.DeleteValue("Bookpouch", false);
        }

        //Handle loading  values for all settings checkboxes
        private void CheckBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox) sender;
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

        //Handle loading values for all settings text fields
        private void TextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var box = (TextBox) sender;
            var prop = Properties.Settings.Default.GetType().GetProperty(box.Name);

            if (prop != null)
                box.Text = (string) prop.GetValue(Properties.Settings.Default);        
        }

        //Handle saving values for all settings text fields
        private void TextBox_OnLostFocus(object sender, RoutedEventArgs e)
        { 
            var box = (TextBox) sender;
            var prop = Properties.Settings.Default.GetType().GetProperty(box.Name);

            if (prop != null)
                prop.SetValue(Properties.Settings.Default, box.Text);

            

            Properties.Settings.Default.Save();            
        }

        private void BooksDir_OnLoaded(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;            
            button.Content = Properties.Settings.Default.BooksDir;
        }

        private void BooksDir_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            SelectBooksDir();

            button.Content = Properties.Settings.Default.BooksDir;
        }

        private void Debug_OnClick(object sender, RoutedEventArgs e)
        {            
             DebugConsole.Open();
        }

        internal sealed class LanguageOption //Class representing items in the language selection drop down menu
        {
            public string Name { private set; get; }
            public string NativeName { private set; get; }
            public string FlagPath { private set; get; }

            public readonly CultureInfo CultureInfo;

            public LanguageOption(CultureInfo cultureInfo)
            {
                this.CultureInfo = cultureInfo;
                var countryCode = (cultureInfo.Name != "" ? new RegionInfo(cultureInfo.Name).TwoLetterISORegionName : "_unknown");
                Name = cultureInfo.DisplayName;
                NativeName = cultureInfo.NativeName;
                FlagPath = "flags/"+ countryCode +".png";
                FlagPath = (Tools.ResourceExists(FlagPath, Assembly.GetExecutingAssembly()) ? FlagPath : "flags/_unknown.png");
            }

        }

        internal sealed class ReaderOption //Class representing items in the reader selection drop down menu
        {            
            public string Model { private set; get; }

            public string PnpDeviceId;
            public string RootDir;
            private readonly String[] _readerImgs = {"kindle", "nook"}; //List of model names for which we have pictures present in the readers folder

            public string ImagePath
            {
                get //Return image path for the reader model name specified in Model or default picture if unkwnon model name is supplied
                {
                    return "Readers/" + (Array.IndexOf(_readerImgs, Model) != -1 ? Model : "_unknown") + ".png";
                }
            }
                     

            public ReaderOption(string model, string pnpDeviceId, string rootDir)
            {                
                Model = model;
                PnpDeviceId = pnpDeviceId;
                RootDir = rootDir;
            }

        }
    }
    
}
