using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Pinterest
{
    public class UploadImageViewModel : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        private string _tags;
        public string Tags
        {
            get => _tags;
            set => Set(ref _tags, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => Set(ref _description, value);
        }

        private bool _isPremium;
        public bool IsPremium
        {
            get => _isPremium;
            set => Set(ref _isPremium, value);
        }

        private string _selectedFilePath;
        public BitmapImage PreviewImage { get; private set; }

        public ICommand SelectImageCommand { get; }
        public ICommand UploadCommand { get; }

        public UploadImageViewModel()
        {
            SelectImageCommand = new RelayCommand<object>(SelectImage);
            UploadCommand = new RelayCommand<object>(UploadImage);
        }

        private void SelectImage(object parameter)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Изображения (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedFilePath = dialog.FileName;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_selectedFilePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                PreviewImage = bitmap;
                RaisePropertyChanged(nameof(PreviewImage));
            }
        }
        private bool CanCurrentUserUpload()
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            var command = new SqlCommand("SELECT CanUploadImages FROM Users WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", Session.CurrentUser.Email);

            var result = command.ExecuteScalar();
            return result != null && (bool)result;
        }

        private void UploadImage(object parameter)
        {
            if (Session.CurrentUser == null)
            {
                MessageBox.Show("Только авторизованные пользователи могут загружать изображения.");
                return;
            }

            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                MessageBox.Show("Выберите изображение для загрузки.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                MessageBox.Show("Введите название изображения.");
                return;
            }

            if (!CanCurrentUserUpload())
            {
                MessageBox.Show("Ваша возможность загружать изображения ограничена администратором.");
                return;
            }

            string fileName = Guid.NewGuid() + Path.GetExtension(_selectedFilePath);
            string destDir = "GalleryImages";
            string destPath = Path.Combine(destDir, fileName);

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            File.Copy(_selectedFilePath, destPath);

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand(@"
                    INSERT INTO Images (FileName, OriginalName, UploadedBy, Description, IsPremium, Tags)
                    VALUES (@FileName, @OriginalName, @UploadedBy, @Description, @IsPremium, @Tags)", connection);

                command.Parameters.AddWithValue("@FileName", fileName);
                command.Parameters.AddWithValue("@OriginalName", Title);
                command.Parameters.AddWithValue("@UploadedBy", Session.CurrentUser.Email);
                command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(Description) ? (object)DBNull.Value : Description);
                command.Parameters.AddWithValue("@IsPremium", IsPremium);
                command.Parameters.AddWithValue("@Tags", string.IsNullOrWhiteSpace(Tags) ? (object)DBNull.Value : Tags);

                try
                {
                    command.ExecuteNonQuery();
                    MessageBox.Show("Изображение успешно загружено!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении изображения: {ex.Message}");
                    return;
                }
            }

            Title = Tags = Description = string.Empty;
            IsPremium = false;
            _selectedFilePath = null;
            PreviewImage = null;
            RaisePropertyChanged(nameof(PreviewImage));
        }

        private string ConnectionString =>
            @"Data Source=NOUTBOOK;Initial Catalog=PinterestDB;Integrated Security=True";
    }
}
