using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Pinterest
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static void ChangeLanguage(string language)
        {
            ResourceDictionary dict = new ResourceDictionary();

            switch (language)
            {
                case "ru":
                    dict.Source = new Uri("language/Strings.ru.xaml", UriKind.Relative);
                    break;
                case "en":
                    dict.Source = new Uri("language/Strings.en.xaml", UriKind.Relative);
                    break;
            }

            var oldDict = Current.Resources.MergedDictionaries
                             .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("language/Strings."));

            if (oldDict != null)
                Current.Resources.MergedDictionaries.Remove(oldDict);

            Current.Resources.MergedDictionaries.Add(dict);
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                string cursorPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language", "hand-2.cur");
                Mouse.OverrideCursor = new Cursor(cursorPath);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Ошибка при установке курсора: " + ex.Message);
            }
        }


    }


}
