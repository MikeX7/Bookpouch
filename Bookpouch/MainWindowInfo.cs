using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Bookpouch
{
    partial class MainWindow
    {
        private static readonly Queue<Tuple<string, byte>> InfoQueue = new Queue<Tuple<string, byte>>();
        private static bool _infoBoxVisible;

        public static void Info(string text, byte type = 0)
        {
            if (text != "")
            {
                DebugConsole.WriteLine((type == 0 ? "Info: " : "Error: ") + text);

                if (TrayIcon.Visible)
                {
                    TrayIcon.BalloonTipText = text;
                    TrayIcon.ShowBalloonTip(5000);
                    return;
                }
            }

            if (Properties.Settings.Default.UseInfoBanner == false)
            {
                MessageBox.Show(text);
                return;
            }

            if (text != "")
                InfoQueue.Enqueue(Tuple.Create(text, type));
            //Add info into the info queue                                                              

            if (_infoBoxVisible || InfoQueue.Count == 0)
                //Proceed only if infobox isn't currently displayed and if the info queue isn't empty
                return;

            var info = InfoQueue.Dequeue(); //item1 = text, item2 = type
            text = info.Item1;
            type = info.Item2;
            var delay = (int)(text.Length * 0.09);
            //How long will be the infobox displayed, based on the text length, 1 letter = 0.09 sec

            MW.Dispatcher.Invoke(() => //In case the info is called from another thread, make sure all MW references to the main are executed from the GUI thread
            {
                MW.InfoBox.Text = text;

                var border = (Border) LogicalTreeHelper.GetParent(MW.InfoBox);

                if (type == 0)
                    //Change colors of the infobox depending on the type of info being displayed (normal info, or error)
                    border.Style = (Style) MW.FindResource("InfoBoxOkBg");
                else
                    border.Style = (Style) MW.FindResource("InfoBoxErrorBg");

                _infoBoxVisible = true;

                var sb = (Storyboard) MW.FindResource("InfoDissolve");
                sb.Children.OfType<DoubleAnimation>().First(animation => animation.Name == "OutOpacity")
                    .BeginTime = new TimeSpan(0, 0, delay);

                sb.Children.OfType<ObjectAnimationUsingKeyFrames>()
                    .Where(oaukf => oaukf.Name == "OutVisibility")
                    .SelectMany(oaukf => oaukf.KeyFrames.Cast<DiscreteObjectKeyFrame>()).First().KeyTime =
                    new TimeSpan(0, 0, 0, delay + 1);

                Storyboard.SetTarget(sb, border);
                sb.Begin();
            });
        }
        private void InfoBox_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        //Upon closing (hiding) the infobox, trigger the info method, to check if there is another info waiting in the queue
        {
            var border = (Border)sender;

            if (border.Visibility == Visibility.Hidden)
            {
                _infoBoxVisible = false;
                Info("");
            }
            else
                _infoBoxVisible = true;
        }


        private void InfoBox_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {            
            var sb = (Storyboard)MW.FindResource("InfoDissolve");

            sb.Children.OfType<DoubleAnimation>().First(animation => animation.Name == "OutOpacity")
                .BeginTime = new TimeSpan(0, 0, 0);

            sb.Children.OfType<ObjectAnimationUsingKeyFrames>()
                        .Where(oaukf => oaukf.Name == "OutVisibility")
                        .SelectMany(oaukf => oaukf.KeyFrames.Cast<DiscreteObjectKeyFrame>()).First().KeyTime = new TimeSpan(0, 0, 1);
           
            sb.Begin();
        }
    }
}
