using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;

namespace Pinterest
{
    public class SubscriptionsViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        public ObservableCollection<ImageMetaData> PremiumImages { get; set; }
        public ICommand OpenImageCommand { get; }

        private const string ConnectionString = @"Data Source=NOUTBOOK;Initial Catalog=PinterestDB;Integrated Security=True";

        public SubscriptionsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            PremiumImages = new ObservableCollection<ImageMetaData>();
            OpenImageCommand = new RelayCommand(meta => OpenImage(meta as ImageMetaData));

            LoadImages();
        }

        private void OpenImage(ImageMetaData meta)
        {
            if (meta == null) return;
            var userRepository = new UserRepository(ConnectionString);

            _mainViewModel.CurrentView = new ImageDetailsControl(
                meta,
                () => _mainViewModel.CurrentView = new SubscriptionsControl(_mainViewModel),
                ShowImageDetails,
                userRepository,
                _mainViewModel
            );
        }

        private void ShowImageDetails(ImageMetaData selectedMeta)
        {
            var userRepository = new UserRepository(ConnectionString);

            _mainViewModel.CurrentView = new ImageDetailsControl(
                selectedMeta,
                () => _mainViewModel.CurrentView = new SubscriptionsControl(_mainViewModel),
                ShowImageDetails,
                userRepository,
                _mainViewModel
            );
        }


        private void LoadImages()
        {
            if (Session.CurrentUser == null)
                return;

            PremiumImages.Clear();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                string query = @"
    SELECT i.Id, i.FileName, i.OriginalName, i.UploadedBy, i.Description, i.Tags, i.IsPremium
    FROM Images i
    INNER JOIN UserSubscriptions s ON i.UploadedBy = s.SubscribedTo
    WHERE s.UserId = @userId AND i.IsPremium = 1";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", Session.CurrentUser.Id);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var image = new ImageMetaData
                            {
                                Id = reader.GetInt32(0),
                                FileName = reader.GetString(1),
                                OriginalName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                UploadedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Tags = reader.IsDBNull(5)
                                    ? new List<string>()
                                    : reader.GetString(5).Split(',').Select(t => t.Trim()).ToList(),
                                IsPremium = reader.GetBoolean(6)
                            };

                            PremiumImages.Add(image);
                        }
                    }
                }
            }
        }

    }
}
