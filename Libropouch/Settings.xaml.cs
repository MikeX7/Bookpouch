using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            comboBox.ItemsSource = new List<string> { "English", "Česky" }; ;
        }

        private void Language_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            Debug.WriteLine(comboBox.SelectedIndex);
            
        }
    }
}
