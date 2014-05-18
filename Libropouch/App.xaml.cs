using System.Windows;

namespace Libropouch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!Libropouch.Properties.Settings.Default.SplashScreen) 
                return;

            var splashScreen = new SplashScreen("Img/splashscreen.png");
            splashScreen.Show(true);
        }
    }


}
