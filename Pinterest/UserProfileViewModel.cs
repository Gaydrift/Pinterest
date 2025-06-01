using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Pinterest
{
    public class UserProfileViewModel : ViewModelBase
    {
        public ObservableCollection<GalleryItemViewModel> UserUploadedImages { get; set; } = new();
        public ObservableCollection<GalleryItemViewModel> SavedImages { get; set; } = new();
        public ObservableCollection<string> UserCollections { get; set; } = new();
        public ObservableCollection<GalleryItemViewModel> SelectedCollectionImages { get; set; } = new();

        private string selectedCollectionName;
        public string SelectedCollectionName
        {
            get => selectedCollectionName;
            set
            {
                if (selectedCollectionName != value)
                {
                    selectedCollectionName = value;
                    RaisePropertyChanged(nameof(SelectedCollectionName));

                    if (!string.IsNullOrEmpty(selectedCollectionName))
                        LoadSelectedCollectionImages();
                    else
                        SelectedCollectionImages.Clear();
                }
            }
        }

        public RelayCommand<string> AddToCollectionCommand { get; }
        public ICommand DeleteCollectionCommand { get; }

        public string WelcomeMessage => $"Привет, {Session.CurrentUser?.Name}!";

        public UserProfileViewModel()
        {
            AddToCollectionCommand = new RelayCommand<string>(AddToCollection);
            DeleteCollectionCommand = new RelayCommand(_ => ExecuteDeleteCollection(), _ => !string.IsNullOrEmpty(SelectedCollectionName));
            LoadUserCollections();
            LoadUserImages();
        }

        private void LoadUserCollections()
        {
            UserCollections.Clear();
            var userId = DatabaseHelper.GetUserId(Session.CurrentUser.Name);
            var collections = DatabaseHelper.GetUserCollections(userId);
            foreach (var name in collections)
                UserCollections.Add(name);
        }

        private void LoadSelectedCollectionImages()
        {
            SelectedCollectionImages.Clear();

            if (string.IsNullOrEmpty(SelectedCollectionName))
                return;

            var userId = DatabaseHelper.GetUserId(Session.CurrentUser.Name);
            var images = DatabaseHelper.GetImagesFromCollection(userId, SelectedCollectionName);
            foreach (var image in images)
                SelectedCollectionImages.Add(new GalleryItemViewModel(image));
        }

        private void AddToCollection(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName)) return;

            var userId = DatabaseHelper.GetUserId(Session.CurrentUser.Name);
            if (!UserCollections.Contains(collectionName))
            {
                DatabaseHelper.CreateCollection(userId, collectionName);
                UserCollections.Add(collectionName);
            }
        }

        private void ExecuteDeleteCollection()
        {
            if (string.IsNullOrEmpty(SelectedCollectionName)) return;

            var result = MessageBox.Show($"Удалить коллекцию \"{SelectedCollectionName}\"?", "Удаление",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var userId = DatabaseHelper.GetUserId(Session.CurrentUser.Name);
                DatabaseHelper.DeleteCollection(userId, SelectedCollectionName);
                LoadUserCollections();
                SelectedCollectionName = null;
                SelectedCollectionImages.Clear();
            }
        }

        public void SaveToCollection(string collectionName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(fileName)) return;

            var userId = DatabaseHelper.GetUserId(Session.CurrentUser.Name);
            DatabaseHelper.AddImageToCollection(userId, collectionName, fileName);

            if (SelectedCollectionName == collectionName)
                LoadSelectedCollectionImages();
        }

        public void LoadUserImages()
        {
            UserUploadedImages.Clear();
            SavedImages.Clear();

            if (Session.CurrentUser == null) return;

            var userId = DatabaseHelper.GetUserId(Session.CurrentUser.Name);
            var images = DatabaseHelper.GetUserImages(userId);
            foreach (var image in images)
                UserUploadedImages.Add(new GalleryItemViewModel(image));

            var likedImages = DatabaseHelper.GetLikedImagesByUser(Session.CurrentUser.Name);
            foreach (var image in likedImages)
                SavedImages.Add(new GalleryItemViewModel(image));
        }

        public void SaveImageMeta(ImageMetaData meta)
        {
            DatabaseHelper.SaveImageMeta(meta);
        }
    }
}
