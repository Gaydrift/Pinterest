using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Pinterest
{
    public partial class ImageDetailsControl : UserControl
    {
        public ImageDetailsControl(ImageMetaData meta,Action onBack,Action<ImageMetaData> showImageDetails,IUserRepository userRepository,MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = new ImageDetailsViewModel(meta, onBack, showImageDetails, userRepository, mainViewModel);
        }

    }



    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(Visibility.Visible);
        }
    }
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }
    }

    public class ImageFeedback
    {
        public string FileName { get; set; }
        public int Likes { get; set; }
        public List<CommentItem> Comments { get; set; } = new List<CommentItem>();
        public List<string> LikedBy { get; set; } = new List<string>();
    }

    public class CommentItem : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Text { get; set; }

        private bool _isHidden;
        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                if (_isHidden != value)
                {
                    _isHidden = value;
                    OnPropertyChanged(nameof(IsHidden));
                }
            }
        }

        public string UserName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }


    public class BoolToShowHideConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isHidden)
                return isHidden ? "Показать" : "Скрыть";
            return "Скрыть";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
