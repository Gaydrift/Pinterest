using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Pinterest
{
    public partial class MainWindow : Window
    {
        private bool _isDarkStyle = false;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ToggleStyleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDarkStyle)
            {
                // Вернуть светлую тему
                this.Resources["MainButtonStyle"] = this.Resources["MainButtonStyleLight"];
            }
            else
            {
                // Применить темную тему
                this.Resources["MainButtonStyle"] = this.Resources["MainButtonStyleDark"];
            }
            _isDarkStyle = !_isDarkStyle;
        }
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is MainViewModel vm)
            {
                vm.SearchCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    public class ImageMetaData
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string OriginalName { get; set; }
        public string AuthorDisplayName { get; set; }
        public string UploadedBy { get; set; }
        public List<string> Tags { get; set; }
        public string Description { get; set; }
        public bool IsPremium { get; set; }

        [JsonIgnore]
        public string ImagePath => Path.Combine("GalleryImages", FileName);

        [JsonIgnore]
        public BitmapImage ImageSource
        {
            get
            {
                try
                {
                    string fullPath = Path.GetFullPath(ImagePath);
                    if (File.Exists(fullPath))
                        return new BitmapImage(new Uri(fullPath));
                }
                catch { }
                return null;
            }
        }
    }




}
