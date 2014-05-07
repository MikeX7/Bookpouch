using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

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

            //new UsbSync();

            if (Properties.Settings.Default.UsbAutoDetect)
                new ReaderDetector(this); //Start reader detection which automatically triggers UsbSync when reader is connected to the pc

           
            
        }

        private static readonly Queue<Tuple<string, byte, ushort>> InfoQueue = new Queue<Tuple<string, byte, ushort>>();
        private static DateTime _infoLastShown;
        private static bool _infoBoxVisible = false;

        public static void Info(string text, byte type = 0, ushort timer = 5)
        {
            
            if (Properties.Settings.Default.UseInfoBanner == false)
            {
                MessageBox.Show(text);
                return;
            }

            if (text != "")            
                InfoQueue.Enqueue(Tuple.Create(text, type, timer)); //Add info into the info queue                            

            

            var border = (Border)LogicalTreeHelper.GetParent(MW.InfoBox);
            Debug.WriteLine(border.Visibility + text);

            if (_infoBoxVisible || InfoQueue.Count == 0) //Proceed only if infobox isn't currently displayed and if the info queue isn't empty
                return;            

            
            
            var info = InfoQueue.Dequeue(); //item1 = text, item2 = type, item3 = timer
      
            MW.InfoBox.Text = info.Item1;                

            if (info.Item2 == 0) //Change colors of the infobox depending on the type of info being displayed (normal info, or error)
                border.Style = (Style)MW.FindResource("InfoBoxOkBg");
            else
                border.Style = (Style)MW.FindResource("InfoBoxErrorBg");
            lock (new object())
            {
                border.Visibility = Visibility.Visible;
                Debug.WriteLine("allowed to continue" + MW.InfoBoxBorder.Visibility + border.Visibility);
            }
            _infoBoxVisible = true;

            
            
            

            var sb = (Storyboard) MW.FindResource("InfoDissolve");
            Storyboard.SetTarget(sb, border);
            sb.Begin();                         

            _infoLastShown = DateTime.Now;
        }

        private void BookGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
               
        }        

        private void Sync_OnClick(object sender, RoutedEventArgs e)
        {                        
            Info(String.Format("Searching for connected {0}...", Properties.Settings.Default.UsbModel));  

            new UsbSync();
        }

        private void InfoBox_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) //Upon closing (hiding) the infobox, trigger the info method, to check if there is another info waiting in the queue
        {
            var border = (Border) sender;

            if (border.Visibility == Visibility.Hidden)
            {
                _infoBoxVisible = false;
                Info("");
                
            }
            else
            {
                _infoBoxVisible = true;
            }

            Debug.WriteLine("Changed vis: " + border.Visibility);
        }
    }
}
