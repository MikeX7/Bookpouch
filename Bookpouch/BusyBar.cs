using System;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Shell;

namespace Bookpouch
{
    partial class MainWindow
    {
        private static Timer _busyTitleTimer;
        private static int _busyOn;
        private static int _busyOff;

        /// <summary>
        /// Displays or removes busy indicator from the main window
        /// </summary>
        /// <param name="toggle">true = turn the indicator on, false = turn it off</param>
        public static void Busy(bool toggle)
        {
            if (toggle) _busyOn++; else _busyOff++;

            if (_busyTitleTimer == null && toggle)
            {
                MW.Dispatcher.Invoke(() =>
                {
                    MW.BusyText.Visibility = MW.BusyBar.Visibility = Visibility.Visible;
                    MW.MenuStack.IsEnabled = false;
                    MW.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
                });

                _busyTitleTimer = new Timer(300);

                _busyTitleTimer.Disposed += delegate
                {
                    MW.Dispatcher.Invoke(() => { MW.Title = "Bookpouch"; });
                    //After the timer gets disposed of, set the window title back to default
                };

                _busyTitleTimer.Elapsed += delegate
                {
                    MW.Dispatcher.Invoke(() =>
                    {
                        MW.Title = MW.Title.Substring(0, 2) == "▣•" ? "•▣" : "▣•";
                        //Switch between these two sets of symbols in the window's title, to make it look like a simple animation
                        MW.Title += " Bookpouch - " + UiLang.Get("Working");
                    });
                };

                _busyTitleTimer.Start();
            }
            else if (!toggle && _busyTitleTimer != null && _busyOff >= _busyOn)
            {
                MW.Dispatcher.Invoke(() =>
                {
                    MW.BusyText.Visibility = MW.BusyBar.Visibility = Visibility.Collapsed;
                    MW.BusyBar.IsIndeterminate = true;
                    MW.TaskBar.ProgressState = TaskbarItemProgressState.None;
                    MW.MenuStack.IsEnabled = true;
                    MW.BusyText.Text = String.Empty;

                });

                _busyTitleTimer.Stop();
                _busyTitleTimer.Dispose();
                _busyTitleTimer = default(Timer);
            }
        }

        /// <summary>
        /// Displays the supplied text on top of the progress bar
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        public static void Busy(string text)
        {
            if (text.Count() > 85)
                text = "..." + text.Substring(text.Count() - 85);

            MW.Dispatcher.Invoke(() =>
            {
                MW.BusyText.Text = text;
            });
        }

        /// <summary>
        /// Sets the bar as determinate and sets its maximum progress to the supplied value
        /// </summary>
        /// <param name="max">Maximum value for the progress to reach</param>        
        public static void BusyMax(int max)
        {
            MW.Dispatcher.Invoke(() =>
            {
                MW.BusyBar.Maximum = max;
                MW.BusyBar.IsIndeterminate = false;
                MW.TaskBar.ProgressState = TaskbarItemProgressState.Normal;
            });
        }

        /// <summary>
        /// Moves the progress to the supplied value 
        /// </summary>
        /// <param name="current"></param>        
        public static void Busy(int current)
        {
            MW.Dispatcher.Invoke(() =>
            {
                MW.BusyBar.Value = current;
                MW.TaskBar.ProgressValue = ((1 / MW.BusyBar.Maximum) * current);                
            });
        }

    }
}
