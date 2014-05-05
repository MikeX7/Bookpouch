using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Timers;

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

        private static readonly Queue<Tuple<string, byte, ushort>> InfoQueue = new Queue<Tuple<string, byte, ushort>>();
        private static DateTime _infoLastShown;
        private static Timer _infoTimer;

        public static void Info(string text, byte type = 0, ushort timer = 5)
        {
            
            if (Properties.Settings.Default.UseInfoBanner == false)
            {
                MessageBox.Show(text);
                return;
            }

            if (text != "")
            {                                
                InfoQueue.Enqueue(Tuple.Create(text, type, timer));
            }

            var border = (Border) LogicalTreeHelper.GetParent(MW.InfoBox);

           //if ((DateTime.Now - _infoLastShown).TotalSeconds < 7)
            if (border.Visibility == Visibility.Visible)
            {           
                Debug.WriteLine("trying to trigger too soon");
                return;
            }

            if (InfoQueue.Count == 0)
                return;

            var info = InfoQueue.Dequeue(); //item1 = text, item2 = type, item3 = timer

            //MW.Dispatcher.Invoke(() => //Make sure we execute all work with WPF UI elements in the same thread 
            //{    
                MW.InfoBox.Text = info.Item1;                

                if (info.Item2 == 0)
                    border.Style = (Style)MW.FindResource("InfoBoxOkBg");
                else
                    border.Style = (Style)MW.FindResource("InfoBoxErrorBg");
            
                var sb = MW.FindResource("Dissolve") as Storyboard;
                Storyboard.SetTarget(sb, border);
                sb.Begin();                
            //});

            _infoLastShown = DateTime.Now;
            Debug.WriteLine(_infoLastShown);

            /*_infoTimer = new Timer(9000);
            _infoTimer.Elapsed += (source, e) =>
                {
                    _infoTimer.Stop(); 
                    _infoTimer.Dispose(); 
                    Debug.WriteLine("boom event"); 
                    Info("");
                };
            _infoTimer.Enabled = true;*/
        }

        private void BookGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
               
        }        

        private void Sync_OnClick(object sender, RoutedEventArgs e)
        {            
            Info(String.Format("Attempting {0} sync...", Properties.Settings.Default.UsbModel));

            new UsbSync();
        }

        private void InfoBox_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var border = (Border) sender;
            if(border.Visibility == Visibility.Hidden)
                Info("");
        }
    }
}
