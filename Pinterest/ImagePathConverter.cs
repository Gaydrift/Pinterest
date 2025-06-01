using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace Pinterest
{
    [ValueConversion(typeof(string), typeof(Uri))]
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fileName = value as string;
            if (string.IsNullOrEmpty(fileName)) return null;

            string path = Path.Combine("GalleryImages", fileName);
            if (!File.Exists(path)) return null;

            return new Uri(Path.GetFullPath(path));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
