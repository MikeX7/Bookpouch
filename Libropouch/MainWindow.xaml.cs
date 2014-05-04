using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Libropouch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            new UsbSync();

            if (Properties.Settings.Default.UsbAutoDetect)
                new ReaderDetector(this); //Start reader detection which automatically triggers UsbSync when reader is connected to the pc

           
            
        }

        public static void Info(String text, byte type = 0)
        {
            MessageBox.Show(text);
        }


        private void BookGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
               
        }
    }
}
