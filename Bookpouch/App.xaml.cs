using System.Windows;

namespace Bookpouch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!Bookpouch.Properties.Settings.Default.SplashScreen) 
                return;

            var splashScreen = new SplashScreen("Img/splashscreen.png");
            splashScreen.Show(true);
        }
    }


}
