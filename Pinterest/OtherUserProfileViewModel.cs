using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.IO;

namespace Pinterest
{
    public class OtherUserProfileViewModel : ViewModelBase
    {
        public ObservableCollection<GalleryItemViewModel> UserUploadedImages { get; set; } = new();
        public ObservableCollection<GalleryItemViewModel> SavedImages { get; set; } = new();
        public ObservableCollection<string> UserCollections { get; set; } = new();
        public ObservableCollection<GalleryItemViewModel> SelectedCollectionImages { get; set; } = new();
        private readonly IUserRepository _userRepository;


        public ICommand BackCommand { get; }
        private readonly Action onBack;

        private string selectedCollectionName;
        public string SelectedCollectionName
        {
            get => selectedCollectionName;
            set
            {
                selectedCollectionName = value;
                RaisePropertyChanged(nameof(SelectedCollectionName));
                LoadSelectedCollectionImages();
            }
        }

        public string WelcomeMessage => $"Профиль пользователя {OtherUser?.Name}";

        public User OtherUser { get; }

        public OtherUserProfileViewModel(User otherUser, Action onBack = null)
        {
            OtherUser = otherUser ?? Session.CurrentViewedUser;
            this.onBack = onBack;
            BackCommand = new RelayCommand(_ => onBack?.Invoke());

            LoadUserCollections();
            LoadUserImages();
            LoadSavedImages();
        }

        private void LoadUserCollections()
        {
            UserCollections.Clear();
            if (OtherUser == null) return;

            int userId = DatabaseHelper.GetUserId(OtherUser.Name);
            var collections = DatabaseHelper.GetUserCollections(userId);

            foreach (var col in collections)
            {
                UserCollections.Add(col);
            }
        }

        private void LoadUserImages()
        {
            UserUploadedImages.Clear();
            if (OtherUser == null) return;

            var userId = DatabaseHelper.GetUserId(OtherUser.Name);
            var images = DatabaseHelper.GetUserImages(userId);
            foreach (var img in images)
            {
                AddImageToCollection(UserUploadedImages, img);
            }
        }


        private void LoadSavedImages()
        {
            SavedImages.Clear();
            if (OtherUser == null) return;

            var likedImages = DatabaseHelper.GetLikedImagesByUser(OtherUser.Name);
            foreach (var img in likedImages)
            {
                if (img.UploadedBy != OtherUser.Name)
                {
                    AddImageToCollection(SavedImages, img);
                }
            }
        }

        private void LoadSelectedCollectionImages()
        {
            SelectedCollectionImages.Clear();
            if (OtherUser == null || string.IsNullOrEmpty(SelectedCollectionName)) return;

            int userId = DatabaseHelper.GetUserId(OtherUser.Name);
            var images = DatabaseHelper.GetImagesFromCollection(userId, SelectedCollectionName);

            foreach (var img in images)
            {
                AddImageToCollection(SelectedCollectionImages, img);
            }
        }

        private void AddImageToCollection(ObservableCollection<GalleryItemViewModel> collection, ImageMetaData meta)
        {
            string imagePath = Path.Combine("GalleryImages", meta.FileName);
            if (!File.Exists(imagePath)) return;

            try
            {
                collection.Add(new GalleryItemViewModel(meta));
            }
            catch
            {

            }
        }
    }
}
