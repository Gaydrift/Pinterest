using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pinterest
{
    public class MainViewModel : ViewModel
    {
        public ICommand OpenGalleryCommand { get; }
        public ICommand OpenSubscriptionsCommand { get; }
        public ICommand OpenUploadCommand { get; }
        public ICommand SearchCommand { get; }

        private ICommand _openAccountMenuCommand;
        public ICommand OpenAccountMenuCommand =>
            _openAccountMenuCommand ??= new RelayCommand(p => OpenAccountMenu(p as FrameworkElement));

        private UserControl _currentView;
        public UserControl CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }
        public IUserRepository UserRepository { get; }
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        private string _userInitial;
        public string UserInitial
        {
            get => _userInitial;
            set { _userInitial = value; OnPropertyChanged(); }
        }

        private GalleryCatalogViewModel _galleryViewModel;
        private GalleryCatalog _galleryCatalog;

        public MainViewModel()
        {
            _galleryViewModel = new GalleryCatalogViewModel();
            _galleryCatalog = new GalleryCatalog { DataContext = _galleryViewModel };
            string connectionString = "Data Source=NOUTBOOK;Initial Catalog=PinterestDB;Integrated Security=True;TrustServerCertificate=True";
            UserRepository = new UserRepository(connectionString);
            OpenGalleryCommand = new RelayCommand(_ => OpenGallery());
            OpenSubscriptionsCommand = new RelayCommand(_ => OpenSubscriptions());
            OpenUploadCommand = new RelayCommand(_ => OpenUpload());
            SearchCommand = new RelayCommand(_ => ExecuteSearch());
            _galleryViewModel = new GalleryCatalogViewModel();
            _galleryCatalog = new GalleryCatalog { DataContext = _galleryViewModel };

            if (Session.IsLoggedIn)
            {
                UpdateUserInitial();
                _galleryViewModel.LoadImages();
                CurrentView = _galleryCatalog;
            }
            else
            {
                UserInitial = "👤";
                CurrentView = new AccountControl();
            }
        }

        private void UpdateUserInitial()
        {
            var name = Session.CurrentUser?.Name;
            UserInitial = !string.IsNullOrWhiteSpace(name)
                ? name.Substring(0, 1).ToUpper()
                : "👤";
        }
        public void UpdateUserStatus()
        {
            UpdateUserInitial();
            _galleryViewModel.LoadImages();
            CurrentView = _galleryCatalog;
        }

        private void OpenGallery()
        {
            CurrentView = _galleryCatalog;
            _galleryViewModel.LoadImages();
        }

        private void OpenSubscriptions()
        {
            CurrentView = new SubscriptionsControl(this);
        }

        private void OpenUpload()
        {
            CurrentView = new UploadImageControl();
        }

        private void ExecuteSearch()
        {
            string query = SearchText?.Trim().ToLower() ?? "";
            string authorFilter = null;
            string tagFilter = null;

            var parts = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.StartsWith("автор:"))
                    authorFilter = part.Substring(6);
                else if (part.StartsWith("тег:"))
                    tagFilter = part.Substring(4);
                else
                    tagFilter = part;
            }

            _galleryViewModel.FilterImages(authorFilter, tagFilter);
            CurrentView = _galleryCatalog;
        }

        private void OpenAccountMenu(FrameworkElement placementTarget)
        {
            if (Session.CurrentUser == null)
            {
                CurrentView = new AccountControl();
                return;
            }

            var menu = new ContextMenu();

            var profileItem = new MenuItem { Header = "Профиль" };
            profileItem.Click += (s, e) => CurrentView = new UserProfileWindow(UserRepository);

            var logoutItem = new MenuItem { Header = "Выйти" };
            logoutItem.Click += (s, e) =>
            {
                Session.Logout();
                UserInitial = "👤";
                MessageBox.Show("Вы вышли из аккаунта.");
                CurrentView = new AccountControl();
            };

            menu.Items.Add(profileItem);
            menu.Items.Add(logoutItem);

            menu.PlacementTarget = placementTarget;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public void AddUser(User user)
        {
            if (_users.Any(u => u.Email == user.Email))
                throw new InvalidOperationException("Пользователь уже существует.");
            _users.Add(user);
        }

        public void UpdateUser(User user)
        {
            var index = _users.FindIndex(u => u.Email == user.Email);
            if (index >= 0)
                _users[index] = user;
        }

        public User GetUserByLogin(string email)
        {
            return _users.FirstOrDefault(u => u.Email == email);
        }

        public List<User> GetAllUsers()
        {
            return _users;
        }
    }

    public interface IUserRepository
    {
        void AddUser(User user);
        void UpdateUser(User user);
        User GetUserByLogin(string email);
        List<User> GetAllUsers();
    }

}
