using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;

namespace Libropouch
{
    /// <summary>
    /// User interface translation handling tools
    /// </summary>
    static class UiLang
    {
        private static readonly ResourceManager Translations = new ResourceManager("Libropouch.Lang.Lang", Assembly.GetExecutingAssembly());

        static UiLang()
        {
            if (Properties.Settings.Default.Language != "")
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language);
        }

        public static string Get(string key)
        {
            return Translations.GetString(key);
        }
    }
}
