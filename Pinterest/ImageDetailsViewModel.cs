using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Pinterest;
using Microsoft.VisualBasic;
using System.Windows.Navigation;
using System.Windows.Controls;
using static Pinterest.Session;
using static System.Net.Mime.MediaTypeNames;
using System.CodeDom;
using System.Data.SqlClient;
using System.Configuration;
using Application = System.Windows.Application;



namespace Pinterest
{
    public class ImageItem
    {
        public string ImagePath { get; set; }
    }

    public class ImageDetailsViewModel : ViewModel
    {
        public ImageMetaData Meta { get; set; }
        private readonly MainViewModel _mainViewModel;
        public string ImagePath => Path.Combine("D:\\labs\\OOP_2SEM\\Pinterest\\Pinterest\\bin\\Debug\\GalleryImages", Meta.FileName);
        private readonly Action<ImageMetaData> showImageDetails;
        public ObservableCollection<CommentItem> Comments { get; set; } = new();
        public RelayCommand<CommentItem> HideCommentCommand { get; }
        public IEnumerable<CommentItem> VisibleComments =>
    Comments.Where(c => !c.IsHidden || IsAdmin);

        public bool AdminCanEdit { get; set; }
        public int ViewCount { get; set; }
        public ObservableCollection<string> UserCollections { get; set; } = new();
        private string selectedCollection;
        public string SelectedCollection
        {
            get => selectedCollection;
            set
            {
                if (selectedCollection != value)
                {
                    selectedCollection = value;
                    OnPropertyChanged(nameof(SelectedCollection));
                    OnPropertyChanged(nameof(AddToCollectionButtonText));
                }
            }
        }

        public string AddToCollectionMessage { get; set; }
        public string AddToCollectionButtonText =>
    string.IsNullOrEmpty(SelectedCollection) ? "Добавить" :
    Session.CurrentUser.Collections.TryGetValue(SelectedCollection, out var list) && list.Contains(Meta.FileName)
        ? "Удалить"
        : "Добавить";

        public List<ImageMetaData> SimilarImages { get; set; }
        public ICommand BackCommand { get; }
        public ICommand LikeCommand { get; }
        public ICommand AddCommentCommand { get; }
        private ICommand _toggleCommentVisibilityCommand;
        public ICommand ToggleCommentVisibilityCommand => _toggleCommentVisibilityCommand ??= new RelayCommand<CommentItem>(ToggleCommentVisibility);
        private ICommand _subscribeCommand;
        public ICommand SubscribeCommand => _subscribeCommand ??= new RelayCommand<ImageMetaData>(ExecuteSubscribe);
        public ICommand AddToCollectionCommand { get; }

        public ICommand OpenSimilarImageCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand EditImageCommand { get; }
        public ICommand ToggleBanUserCommand { get; }
        public ICommand OpenAuthorProfileCommand { get; }
        public ICommand ToggleUploadPermissionCommand { get; }
        private string uploadPermissionButtonText;
        public string UploadPermissionButtonText
        {
            get => uploadPermissionButtonText;
            set
            {
                uploadPermissionButtonText = value;
                OnPropertyChanged(nameof(UploadPermissionButtonText));
            }
        }

        public string EditableTitle { get; set; }
        public string EditableTags { get; set; }
        public string EditableDescription { get; set; }
        private readonly IUserRepository _userRepository;

        public bool CanOpenAuthorProfile
        {
            get
            {
                var authorLogin = Meta?.UploadedBy;
                var authorUser = _userRepository.GetUserByLogin(authorLogin);
                return authorUser != null && authorUser.Name != Session.CurrentUser?.Name;
            }
        }


        public string Title { get; set; }
        public string Author { get; set; }
        public string Tags { get; set; }
        public string Description { get; set; }
        private string _likesCount;
        public string LikesCount
        {
            get => _likesCount;
            set { _likesCount = value; OnPropertyChanged(); }
        }
        private bool _isLiked;
        public bool IsLiked
        {
            get => _isLiked;
            set { _isLiked = value; OnPropertyChanged(); OnPropertyChanged(nameof(LikeButtonText)); }
        }

        public string LikeButtonText => IsLiked ? "💔 Убрать лайк" : "❤️ Поставить лайк";

        public string FeedbackMessage { get; set; }
        private string _banButtonText;
        public string BanButtonText
        {
            get => _banButtonText;
            set
            {
                _banButtonText = value;
                OnPropertyChanged(nameof(BanButtonText));
            }
        }

        private bool _isSubscribed;
        public bool IsSubscribed
        {
            get => _isSubscribed;
            set
            {
                if (_isSubscribed != value)
                {
                    _isSubscribed = value;
                    OnPropertyChanged(nameof(IsSubscribed));
                }
            }
        }
        public bool CanSubscribe { get; set; }
        public bool IsAdmin { get; set; }
        public bool CanDeleteImage => IsAdmin || Session.CurrentUser?.Email == Meta?.UploadedBy;
        public bool IsLoggedIn => Session.CurrentUser != null;

        public ImageDetailsViewModel(ImageMetaData meta,Action onBack,Action<ImageMetaData> showImageDetails,IUserRepository userRepository,MainViewModel mainViewModel)
        {
            Meta = meta;
            this.showImageDetails = showImageDetails;
            _userRepository = userRepository;
            _mainViewModel = mainViewModel;

            BackCommand = new RelayCommand(_ => onBack?.Invoke());
            LikeCommand = new RelayCommand(param => ExecuteLike(param));
            AddCommentCommand = new RelayCommand(param => ExecuteAddComment(param));
            HideCommentCommand = new RelayCommand<CommentItem>(HideComment);
            DeleteImageCommand = new RelayCommand(_ => ExecuteDeleteImage());
            EditImageCommand = new RelayCommand(_ => ExecuteEditImage());
            ToggleBanUserCommand = new RelayCommand<object>(ToggleUserBan);
            AddToCollectionCommand = new RelayCommand(_ => ExecuteAddToCollection());
            ToggleUploadPermissionCommand = new RelayCommand(_ => ToggleUploadPermission());
            OpenSimilarImageCommand = new RelayCommand(img =>
            {
                if (img is ImageMetaData selected)
                {
                    var detailsViewModel = new ImageDetailsViewModel(
                        selected,
                        () => _mainViewModel.CurrentView = new ImageDetailsControl(meta, onBack, showImageDetails, _userRepository, _mainViewModel),
                        showImageDetails,
                        _userRepository,
                        _mainViewModel);

                    var detailsControl = new ImageDetailsControl(selected, () => BackCommand.Execute(null), showImageDetails, _userRepository, _mainViewModel);
                    detailsControl.DataContext = detailsViewModel;

                    _mainViewModel.CurrentView = detailsControl;
                }
            });

            OpenAuthorProfileCommand = new RelayCommand<object>(param =>
            {
                var authorLogin = Meta?.UploadedBy;
                if (string.IsNullOrEmpty(authorLogin)) return;

                var authorUser = _userRepository.GetUserByLogin(authorLogin);
                if (authorUser == null || authorUser.Name == Session.CurrentUser?.Name) return;

                Session.CurrentViewedUser = authorUser;

                var viewModel = new OtherUserProfileViewModel(authorUser, () =>
                {
                    var detailsViewModel = new ImageDetailsViewModel(
                        Meta,
                        onBack,
                        showImageDetails,
                        _userRepository,
                        _mainViewModel);

                    var detailsControl = new ImageDetailsControl(Meta, onBack, showImageDetails, _userRepository, _mainViewModel);
                    detailsControl.DataContext = detailsViewModel;

                    _mainViewModel.CurrentView = detailsControl;
                });

                var profileControl = new OtherUserProfileWindow(viewModel, _userRepository);
                _mainViewModel.CurrentView = profileControl;
            });

            LoadUserCollections();
            UpdateBanButtonText();
            UpdateUploadPermissionText();
            LoadImageData();
            LoadSimilarImages();
            LoadSubscriptionStatus();
        }

        private void LoadImageData()
        {
            Title = $"Название: {Meta.OriginalName}";
            Author = $"Автор: {Meta.UploadedBy}";
            Tags = $"Теги: {string.Join(", ", Meta.Tags)}";
            Description = string.IsNullOrEmpty(Meta.Description) ? "Описание отсутствует" : Meta.Description;

            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Author));
            OnPropertyChanged(nameof(Tags));
            OnPropertyChanged(nameof(Description));

            if (IsLoggedIn && Session.CurrentUser.Name != Meta.UploadedBy)
            {
                CanSubscribe = true;
                IsSubscribed = Session.CurrentUser.SubscribedTo?.Contains(Meta.UploadedBy) == true;
            }

            IsAdmin = Session.CurrentUser?.IsAdmin ?? false;

            if (IsAdmin)
            {
                AdminCanEdit = true;
                LoadViewCount();
            }

            LoadFeedback();
            IncrementViewCount();
        }
        private void LoadUserCollections()
        {
            UserCollections.Clear();

            if (Session.CurrentUser == null)
            {
                OnPropertyChanged(nameof(UserCollections));
                return;
            }

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                int userId = GetUserIdByName(Session.CurrentUser.Name, conn);
                if (userId == -1) return;

                var cmd = new SqlCommand("SELECT Name FROM Collections WHERE UserId = @userId", conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    UserCollections.Add(reader.GetString(0));
                }
            }

            OnPropertyChanged(nameof(UserCollections));
        }
        private void ExecuteAddToCollection()
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(SelectedCollection))
            {
                AddToCollectionMessage = "Выберите коллекцию.";
                OnPropertyChanged(nameof(AddToCollectionMessage));
                return;
            }

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            var getIdCmd = new SqlCommand("SELECT Id FROM Collections WHERE UserId = @userId AND Name = @name", conn);
            getIdCmd.Parameters.AddWithValue("@userId", Session.CurrentUser.Id);
            getIdCmd.Parameters.AddWithValue("@name", SelectedCollection);
            var collectionId = (int?)getIdCmd.ExecuteScalar();

            if (collectionId == null)
            {
                AddToCollectionMessage = "Коллекция не найдена.";
                OnPropertyChanged(nameof(AddToCollectionMessage));
                return;
            }

            if (Meta.Id == 0)
            {
                AddToCollectionMessage = "Изображение не сохранено.";
                OnPropertyChanged(nameof(AddToCollectionMessage));
                return;
            }

            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM CollectionImages WHERE CollectionId = @id AND ImageId = @imageId", conn);
            checkCmd.Parameters.AddWithValue("@id", collectionId);
            checkCmd.Parameters.AddWithValue("@imageId", Meta.Id);
            var exists = (int)checkCmd.ExecuteScalar() > 0;

            if (exists)
            {
                var delCmd = new SqlCommand("DELETE FROM CollectionImages WHERE CollectionId = @id AND ImageId = @imageId", conn);
                delCmd.Parameters.AddWithValue("@id", collectionId);
                delCmd.Parameters.AddWithValue("@imageId", Meta.Id);
                delCmd.ExecuteNonQuery();

                if (Session.CurrentUser.Collections.TryGetValue(SelectedCollection, out var list))
                {
                    list.Remove(Meta.FileName);
                }

                AddToCollectionMessage = $"Удалено из \"{SelectedCollection}\".";
            }
            else
            {
                var addCmd = new SqlCommand("INSERT INTO CollectionImages (CollectionId, ImageId) VALUES (@id, @imageId)", conn);
                addCmd.Parameters.AddWithValue("@id", collectionId);
                addCmd.Parameters.AddWithValue("@imageId", Meta.Id);
                addCmd.ExecuteNonQuery();

                if (!Session.CurrentUser.Collections.ContainsKey(SelectedCollection))
                {
                    Session.CurrentUser.Collections[SelectedCollection] = new List<string>();
                }
                if (!Session.CurrentUser.Collections[SelectedCollection].Contains(Meta.FileName))
                {
                    Session.CurrentUser.Collections[SelectedCollection].Add(Meta.FileName);
                }

                AddToCollectionMessage = $"Добавлено в \"{SelectedCollection}\".";
            }

            OnPropertyChanged(nameof(AddToCollectionButtonText));
            OnPropertyChanged(nameof(AddToCollectionMessage));
        }


        public static class NavigationService
        {
            private static ContentControl _mainContent;

            public static void Configure(ContentControl mainContent)
            {
                _mainContent = mainContent;
            }

            public static void NavigateTo(UserControl view)
            {
                _mainContent.Content = view;
            }
        }

        private void IncrementViewCount()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            MERGE ImageAnalytics AS target
            USING (SELECT @FileName AS FileName) AS source
            ON target.FileName = source.FileName
            WHEN MATCHED THEN 
                UPDATE SET ViewCount = target.ViewCount + 1
            WHEN NOT MATCHED THEN
                INSERT (FileName, ViewCount) VALUES (@FileName, 1);", conn);

                cmd.Parameters.AddWithValue("@FileName", Meta.FileName);
                cmd.ExecuteNonQuery();
            }
        }

        private void LoadViewCount()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT ViewCount FROM ImageAnalytics WHERE FileName = @FileName", conn);
                cmd.Parameters.AddWithValue("@FileName", Meta.FileName);
                var result = cmd.ExecuteScalar();
                ViewCount = result != null ? Convert.ToInt32(result) : 0;
                OnPropertyChanged(nameof(ViewCount));
            }
        }

        public class ImageAnalytics
        {
            public string FileName { get; set; }
            public int ViewCount { get; set; }
        }
        public void ExecuteDeleteImage()
        {
            var result = System.Windows.MessageBox.Show(
                "Вы уверены, что хотите удалить это изображение?",
                "Подтверждение удаления",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        int? imageId = GetImageIdByFileName(conn, tx, Meta.FileName);

                        if (imageId == null)
                        {
                            tx.Rollback();
                            AddToCollectionMessage = "Изображение не найдено.";
                            OnPropertyChanged(nameof(AddToCollectionMessage));
                            return;
                        }

                        var cmdDeleteFromCollections = new SqlCommand(
                            "DELETE FROM CollectionImages WHERE ImageId = @ImageId", conn, tx);
                        cmdDeleteFromCollections.Parameters.AddWithValue("@ImageId", imageId.Value);
                        cmdDeleteFromCollections.ExecuteNonQuery();

                        var cmdDeleteLikes = new SqlCommand(
                            "DELETE FROM UserLikes WHERE ImageId = @ImageId", conn, tx);
                        cmdDeleteLikes.Parameters.AddWithValue("@ImageId", imageId.Value);
                        cmdDeleteLikes.ExecuteNonQuery();

                        DeleteCommentsByImageId(conn, tx, imageId.Value);

                        var cmdDeleteImage = new SqlCommand(
                            "DELETE FROM Images WHERE Id = @ImageId", conn, tx);
                        cmdDeleteImage.Parameters.AddWithValue("@ImageId", imageId.Value);
                        cmdDeleteImage.ExecuteNonQuery();

                        tx.Commit();

                        AddToCollectionMessage = "Изображение успешно удалено.";
                        OnPropertyChanged(nameof(AddToCollectionMessage));

                        BackCommand.Execute(null);
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        AddToCollectionMessage = "Ошибка удаления изображения: " + ex.Message;
                        OnPropertyChanged(nameof(AddToCollectionMessage));
                    }
                }
            }
        }


        private void ExecuteEditImage()
        {
            if (!IsAdmin) return;

            if (string.IsNullOrWhiteSpace(EditableTitle) || string.IsNullOrWhiteSpace(EditableTags))
            {
                MessageBox.Show("Пожалуйста, заполните название и теги перед сохранением.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"UPDATE Images
                           SET OriginalName = @name,
                               Tags = @tags,
                               Description = @desc
                           WHERE FileName = @file", conn);

                cmd.Parameters.AddWithValue("@name", EditableTitle);
                cmd.Parameters.AddWithValue("@tags", EditableTags);
                cmd.Parameters.AddWithValue("@desc", EditableDescription ?? "");
                cmd.Parameters.AddWithValue("@file", Meta.FileName ?? "");

                cmd.ExecuteNonQuery();
                MessageBox.Show("Изображение обновлено.");
            }
        }

        private void ToggleUploadPermission()
        {
            var user = _userRepository.GetUserByLogin(Meta.UploadedBy);
            if (user == null) return;

            user.CanUpload = !user.CanUpload;
            _userRepository.UpdateUser(user);

            UploadPermissionButtonText = user.CanUpload ? "🚫 Запретить загрузку" : "✅ Разрешить загрузку";
            OnPropertyChanged(nameof(UploadPermissionButtonText));
        }
        private void UpdateUploadPermissionText()
        {
            var user = _userRepository.GetUserByLogin(Meta.UploadedBy);
            UploadPermissionButtonText = user?.CanUpload == true
                ? "🚫 Запретить загрузку"
                : "✅ Разрешить загрузку";
            OnPropertyChanged(nameof(UploadPermissionButtonText));
        }


        private void ToggleUserBan(object parameter)
        {
            if (parameter is not string username) return;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var getCmd = new SqlCommand("SELECT IsBanned FROM Users WHERE Name = @name", conn);
                getCmd.Parameters.AddWithValue("@name", username);
                var currentStatus = (bool?)getCmd.ExecuteScalar();

                if (currentStatus == null)
                {
                    MessageBox.Show("Пользователь не найден.");
                    return;
                }

                bool newStatus = !(currentStatus.Value);
                var updateCmd = new SqlCommand("UPDATE Users SET IsBanned = @ban WHERE Name = @name", conn);
                updateCmd.Parameters.AddWithValue("@ban", newStatus);
                updateCmd.Parameters.AddWithValue("@name", username);
                updateCmd.ExecuteNonQuery();

                string action = newStatus ? "заблокирован" : "разблокирован";
                MessageBox.Show($"Пользователь {username} {action}.");
                UpdateBanButtonText();
            }
        }

        private void UpdateBanButtonText()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT IsBanned FROM Users WHERE Name = @name", conn);
                cmd.Parameters.AddWithValue("@name", Meta.UploadedBy);
                var isBanned = (bool?)cmd.ExecuteScalar();

                BanButtonText = isBanned == true
                    ? "✅ Разблокировать пользователя"
                    : "🚫 Заблокировать пользователя";
            }
        }



        private void ExecuteLike(object param)
        {
            if (!IsLoggedIn || param is not ImageMetaData image) return;

            var currentUser = Session.CurrentUser;
            if (currentUser == null) return;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                int feedbackId = GetOrCreateFeedbackId(image.FileName, conn);
                int userId = currentUser.Id;

                bool isLiked;
                using (var cmdCheckLike = new SqlCommand(
                    "SELECT COUNT(*) FROM FeedbackLikes WHERE FeedbackId = @fid AND UserId = @uid", conn))
                {
                    cmdCheckLike.Parameters.AddWithValue("@fid", feedbackId);
                    cmdCheckLike.Parameters.AddWithValue("@uid", userId);
                    isLiked = (int)cmdCheckLike.ExecuteScalar() > 0;
                }

                SqlCommand cmd;
                if (isLiked)
                {
                    cmd = new SqlCommand("DELETE FROM FeedbackLikes WHERE FeedbackId = @fid AND UserId = @uid", conn);
                }
                else
                {
                    cmd = new SqlCommand("INSERT INTO FeedbackLikes (FeedbackId, UserId) VALUES (@fid, @uid)", conn);
                }

                cmd.Parameters.AddWithValue("@fid", feedbackId);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.ExecuteNonQuery();
            }

            LoadFeedback();
            OnPropertyChanged(nameof(LikesCount));
        }




        private int GetFeedbackId(string fileName)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            int? imageId = null;
            using (var cmdImg = new SqlCommand("SELECT Id FROM Images WHERE FileName = @fileName", conn))
            {
                cmdImg.Parameters.AddWithValue("@fileName", fileName);
                var res = cmdImg.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                    imageId = Convert.ToInt32(res);
            }

            if (imageId == null)
                return 0;

            using var cmd = new SqlCommand("SELECT ISNULL(Id, 0) FROM ImageFeedback WHERE ImageId = @imgId", conn);
            cmd.Parameters.AddWithValue("@imgId", imageId);

            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }



        private void SaveComments()
        {
            int feedbackId = GetFeedbackId(Meta.FileName);
            if (feedbackId == 0)
            {
                CreateImageFeedback(Meta.FileName);
                feedbackId = GetFeedbackId(Meta.FileName);
            }

            if (feedbackId == 0)
                return;

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            using (var delCmd = new SqlCommand("DELETE FROM FeedbackComments WHERE FeedbackId = @feedbackId", conn))
            {
                delCmd.Parameters.AddWithValue("@feedbackId", feedbackId);
                delCmd.ExecuteNonQuery();
            }

            foreach (var comment in Comments)
            {
                using var insertCmd = new SqlCommand(
                    @"INSERT INTO FeedbackComments (FeedbackId, Text, IsHidden, UserName) 
          VALUES (@feedbackId, @text, @isHidden, @userName)", conn);

                insertCmd.Parameters.AddWithValue("@feedbackId", feedbackId);
                insertCmd.Parameters.AddWithValue("@text", comment.Text);
                insertCmd.Parameters.AddWithValue("@isHidden", comment.IsHidden);
                insertCmd.Parameters.AddWithValue("@userName", comment.UserName ?? string.Empty);

                insertCmd.ExecuteNonQuery();
            }

        }

        private void ToggleCommentVisibility(CommentItem comment)
        {
            if (comment == null) return;

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand("UPDATE FeedbackComments SET IsHidden = @hidden WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@hidden", !comment.IsHidden);
            cmd.Parameters.AddWithValue("@id", comment.Id);
            cmd.ExecuteNonQuery();

            comment.IsHidden = !comment.IsHidden;
            OnPropertyChanged(nameof(Comments));
            OnPropertyChanged(nameof(VisibleComments));
        }

        private void HideComment(CommentItem comment)
        {
            if (comment == null) return;

            comment.IsHidden = true;

            SaveComments();

            OnPropertyChanged(nameof(Comments));
        }

        private void ExecuteAddComment(object param)
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(FeedbackMessage)) return;

            int feedbackId = GetFeedbackId(Meta.FileName);
            if (feedbackId == 0)
            {
                CreateImageFeedback(Meta.FileName);
                feedbackId = GetFeedbackId(Meta.FileName);
            }
            if (feedbackId == 0)
            {
                MessageBox.Show("Не удалось создать обратную связь для изображения.");
                return;
            }

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand(
    @"INSERT INTO FeedbackComments (FeedbackId, Text, IsHidden, UserName)
      VALUES (@feedbackId, @text, 0, @userName)", conn);

            cmd.Parameters.AddWithValue("@feedbackId", feedbackId);
            cmd.Parameters.AddWithValue("@text", FeedbackMessage);
            cmd.Parameters.AddWithValue("@userName", CurrentUser?.Name ?? "Аноним");
            cmd.ExecuteNonQuery();


            FeedbackMessage = string.Empty;
            LoadFeedback();
            OnPropertyChanged(nameof(FeedbackMessage));
        }

        private void CreateImageFeedback(string fileName)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand(
                @"INSERT INTO ImageFeedback (ImageId)
          SELECT Id FROM Images WHERE FileName = @fileName", conn);
            cmd.Parameters.AddWithValue("@fileName", fileName);
            cmd.ExecuteNonQuery();
        }





        private int GetCurrentUserId()
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT Id FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", Session.CurrentUser.Email);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private void LoadSubscriptionStatus()
        {
            if (Session.CurrentUser == null)
            {
                IsSubscribed = false;
                return;
            }

            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
            {
                IsSubscribed = false;
                return;
            }

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM UserSubscriptions WHERE UserId = @userId AND SubscribedTo = @author", conn);
            cmd.Parameters.AddWithValue("@userId", currentUserId);
            cmd.Parameters.AddWithValue("@author", Meta.UploadedBy);

            int count = (int)cmd.ExecuteScalar();
            IsSubscribed = count > 0;
        }

        private void ExecuteSubscribe(ImageMetaData meta)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
            {
                MessageBox.Show("Не удалось определить пользователя.");
                return;
            }

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                var alreadySubscribedCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM UserSubscriptions WHERE UserId = @userId AND SubscribedTo = @author", conn);
                alreadySubscribedCmd.Parameters.AddWithValue("@userId", currentUserId);
                alreadySubscribedCmd.Parameters.AddWithValue("@author", meta.UploadedBy);
                bool subscribed = (int)alreadySubscribedCmd.ExecuteScalar() > 0;

                var dialog = new SubscribeDialog(subscribed) { Owner = Application.Current.MainWindow };
                dialog.ShowDialog();

                if (dialog.IsConfirmed && !subscribed)
                {
                    var insertCmd = new SqlCommand(
                        "INSERT INTO UserSubscriptions (UserId, SubscribedTo) VALUES (@userId, @author)", conn);
                    insertCmd.Parameters.AddWithValue("@userId", currentUserId);
                    insertCmd.Parameters.AddWithValue("@author", meta.UploadedBy);
                    insertCmd.ExecuteNonQuery();
                    IsSubscribed = true;
                }
                else if (dialog.IsUnsubscribed && subscribed)
                {
                    var deleteCmd = new SqlCommand(
                        "DELETE FROM UserSubscriptions WHERE UserId = @userId AND SubscribedTo = @author", conn);
                    deleteCmd.Parameters.AddWithValue("@userId", currentUserId);
                    deleteCmd.Parameters.AddWithValue("@author", meta.UploadedBy);
                    deleteCmd.ExecuteNonQuery();
                    IsSubscribed = false;
                }
            }
        }


        private void LoadSimilarImages()
        {
            SimilarImages = new List<ImageMetaData>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM Images WHERE FileName != @file", conn);
                cmd.Parameters.AddWithValue("@file", Meta.FileName);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var tags = reader["Tags"]?.ToString()?.Split(',').Select(t => t.Trim()).ToList() ?? new();
                    if (tags.Intersect(Meta.Tags).Any())
                    {
                        SimilarImages.Add(new ImageMetaData
                        {
                            FileName = reader["FileName"].ToString(),
                            OriginalName = reader["OriginalName"].ToString(),
                            UploadedBy = reader["UploadedBy"].ToString(),
                            Tags = tags,
                            IsPremium = (bool)reader["IsPremium"],
                            Description = reader["Description"].ToString()
                        });
                    }
                }
            }
            OnPropertyChanged(nameof(SimilarImages));
        }

        private int GetUserIdByName(string name, SqlConnection conn)
        {
            var cmd = new SqlCommand("SELECT Id FROM Users WHERE Name = @name", conn);
            cmd.Parameters.AddWithValue("@name", name);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }


        private int? GetImageIdByFileName(SqlConnection conn, SqlTransaction tx, string fileName)
        {
            var cmd = new SqlCommand("SELECT Id FROM Images WHERE FileName = @fileName", conn, tx);
            cmd.Parameters.AddWithValue("@fileName", fileName);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                return null;
            return Convert.ToInt32(result);
        }

        private void DeleteCommentsByImageId(SqlConnection conn, SqlTransaction tx, int imageId)
        {
            int? feedbackId = null;
            using (var cmd = new SqlCommand("SELECT Id FROM ImageFeedback WHERE ImageId = @ImageId", conn, tx))
            {
                cmd.Parameters.AddWithValue("@ImageId", imageId);
                var result = cmd.ExecuteScalar();
                if (result != null) feedbackId = (int)result;
            }

            if (feedbackId.HasValue)
            {
                using (var cmdDelComments = new SqlCommand("DELETE FROM FeedbackComments WHERE FeedbackId = @FeedbackId", conn, tx))
                {
                    cmdDelComments.Parameters.AddWithValue("@FeedbackId", feedbackId.Value);
                    cmdDelComments.ExecuteNonQuery();
                }

                using (var cmdDelFeedback = new SqlCommand("DELETE FROM ImageFeedback WHERE Id = @FeedbackId", conn, tx))
                {
                    cmdDelFeedback.Parameters.AddWithValue("@FeedbackId", feedbackId.Value);
                    cmdDelFeedback.ExecuteNonQuery();
                }
            }
        }


        private void LoadFeedback()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                int feedbackId = GetFeedbackId(Meta.FileName, conn);

                if (feedbackId != -1)
                {
                    var cmdLikes = new SqlCommand(
                        "SELECT COUNT(*) FROM FeedbackLikes WHERE FeedbackId = @fid", conn);
                    cmdLikes.Parameters.AddWithValue("@fid", feedbackId);
                    LikesCount = $"Лайков: {cmdLikes.ExecuteScalar()}";

                    var currentUser = Session.CurrentUser;
                    if (currentUser != null)
                    {
                        var cmdCheckUserLike = new SqlCommand(
                            "SELECT COUNT(*) FROM FeedbackLikes WHERE FeedbackId = @fid AND UserId = @uid", conn);
                        cmdCheckUserLike.Parameters.AddWithValue("@fid", feedbackId);
                        cmdCheckUserLike.Parameters.AddWithValue("@uid", currentUser.Id);
                        IsLiked = ((int)cmdCheckUserLike.ExecuteScalar() > 0);
                    }
                    else
                    {
                        IsLiked = false;
                    }
                }
                else
                {
                    LikesCount = "Лайков: 0";
                    IsLiked = false;
                }

                var cmdComments = new SqlCommand(
                    "SELECT Id, Text, IsHidden, UserName FROM FeedbackComments WHERE FeedbackId = @fid", conn);
                cmdComments.Parameters.AddWithValue("@fid", feedbackId);

                var reader = cmdComments.ExecuteReader();
                var commentList = new List<CommentItem>();
                while (reader.Read())
                {
                    commentList.Add(new CommentItem
                    {
                        Id = reader.GetInt32(0),
                        Text = reader.GetString(1),
                        IsHidden = reader.GetBoolean(2),
                        UserName = reader.GetString(3)
                    });
                }
                reader.Close();
                Comments = new ObservableCollection<CommentItem>(commentList);
            }

            OnPropertyChanged(nameof(Comments));
            OnPropertyChanged(nameof(VisibleComments));
        }



        private string connectionString = "Data Source=NOUTBOOK;Initial Catalog=PinterestDB;Integrated Security=True;TrustServerCertificate=True";

        private int GetImageIdByFileName(string fileName, SqlConnection conn)
        {
            var cmd = new SqlCommand("SELECT Id FROM Images WHERE FileName = @file", conn);
            cmd.Parameters.AddWithValue("@file", fileName);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        private int GetUserIdByEmail(string email, SqlConnection conn)
        {
            var cmd = new SqlCommand("SELECT Id FROM Users WHERE Email = @mail", conn);
            cmd.Parameters.AddWithValue("@mail", email);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        private int GetFeedbackId(string fileName, SqlConnection conn)
        {
            int imageId = GetImageIdByFileName(fileName, conn);
            if (imageId == -1) return -1;

            var cmd = new SqlCommand("SELECT Id FROM ImageFeedback WHERE ImageId = @imageId", conn);
            cmd.Parameters.AddWithValue("@imageId", imageId);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }


        private int GetOrCreateFeedbackId(string fileName, SqlConnection conn)
        {
            int id = GetFeedbackId(fileName, conn);
            if (id != -1) return id;

            int imageId = GetImageIdByFileName(fileName, conn);
            if (imageId == -1) throw new Exception("Image not found in DB");

            var insertCmd = new SqlCommand(
                "INSERT INTO ImageFeedback (ImageId) OUTPUT INSERTED.Id VALUES (@imageId)", conn);
            insertCmd.Parameters.AddWithValue("@imageId", imageId);
            return (int)insertCmd.ExecuteScalar();
        }



        public BitmapImage ImageSource
        {
            get
            {
                try
                {
                    return new BitmapImage(new Uri(ImagePath, UriKind.Absolute));
                }
                catch
                {
                    return null;
                }
            }
        }

    }   
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (_ => true);
        }

        public bool CanExecute(object? parameter) => _canExecute((T)parameter);
        public void Execute(object? parameter) => _execute((T)parameter);

        public event EventHandler? CanExecuteChanged;
    }

}
