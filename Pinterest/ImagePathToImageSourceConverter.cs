using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Pinterest
{
    public class ImagePathToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        Debug.WriteLine($"Файл не найден: {path}");
                        return new BitmapImage(new Uri(path, UriKind.Absolute));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Файл не найден: {path}");
                }
            }
            return null;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
