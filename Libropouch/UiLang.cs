﻿using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Markup;

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

    /// <summary>
    /// Replace placeholder localization variables in XAML files with translated strings
    /// </summary>
    public class Lng : MarkupExtension
    {
        public Lng(string value)
        {
            Value = value;
        }

        public string Value { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return String.IsNullOrWhiteSpace(Value) ? Value : UiLang.Get(Value);
        }
    }
}
