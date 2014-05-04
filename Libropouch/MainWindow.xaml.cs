using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Libropouch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow MW;

        public MainWindow()
        {
            MW = this;            
            InitializeComponent();

            new UsbSync();

            if (Properties.Settings.Default.UsbAutoDetect)
                new ReaderDetector(this); //Start reader detection which automatically triggers UsbSync when reader is connected to the pc

           
            
        }

        public static void Info(String text, byte type = 0)
        {
            if (Properties.Settings.Default.UseInfoBanner == false)
            {
                MessageBox.Show(text);
                return;
            }

            MW.InfoBox.Text = text;
            var parent = (System.Windows.Controls.Border) LogicalTreeHelper.GetParent(MW.InfoBox);            
            parent.Visibility = Visibility.Visible;
        }


        private void BookGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
               
        }        

        private void InfoBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void Sync_OnClick(object sender, RoutedEventArgs e)
        {
            Info("Hello");
            Info("Hello\n sdddsds \n ssdds");
        }
    }
}
