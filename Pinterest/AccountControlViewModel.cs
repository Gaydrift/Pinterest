using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Pinterest
{
    public class AccountControlViewModel : INotifyPropertyChanged
    {
        private string _name;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _loginEmail;
        private string _loginPassword;
        private string _role;
        private readonly IUserRepository _userRepository;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
            }
        }

        public string LoginEmail
        {
            get => _loginEmail;
            set
            {
                _loginEmail = value;
                OnPropertyChanged();
            }
        }

        public string LoginPassword
        {
            get => _loginPassword;
            set
            {
                _loginPassword = value;
                OnPropertyChanged();
            }
        }

        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }

        public AccountControlViewModel()
        {
            string connStr = "Data Source=NOUTBOOK;Initial Catalog=PinterestDB;Integrated Security=True;TrustServerCertificate=True";
            _userRepository = new UserRepository(connStr);

            RegisterCommand = new RelayCommand(Register);
            LoginCommand = new RelayCommand(Login);
        }

        private void Register(object parameter)
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email)
                || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Role))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.");
                return;
            }

            var existing = _userRepository.GetUserByLogin(Email);
            if (existing != null)
            {
                MessageBox.Show("Пользователь с таким email уже существует.");
                return;
            }

            var user = new User
            {
                Name = Name,
                Email = Email,
                Password = Password,
                Role = Role,
                IsAdmin = Role?.Equals("Администратор", StringComparison.OrdinalIgnoreCase) == true
            };

            var validationContext = new ValidationContext(user);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(user, validationContext, validationResults, true))
            {
                string errors = string.Join("\n", validationResults.Select(vr => vr.ErrorMessage));
                MessageBox.Show("Ошибки валидации:\n" + errors);
                return;
            }

            _userRepository.AddUser(user);
            Session.Login(user);

            var mainVM = (MainViewModel)Application.Current.MainWindow.DataContext;
            mainVM.UpdateUserStatus();
            //MessageBox.Show("Регистрация прошла успешно!");

            if (parameter is FrameworkElement element &&
                Window.GetWindow(element)?.DataContext is MainViewModel mainVm)
            {
                mainVm.OpenAccountMenuCommand.Execute(element);
            }
        }


        private void Login(object parameter)
        {
            if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
            {
                MessageBox.Show("Введите email и пароль.");
                return;
            }

            var user = _userRepository.GetUserByLogin(LoginEmail);
            if (user == null || user.Password != LoginPassword)
            {
                MessageBox.Show("Неверный email или пароль.");
                return;
            }

            if (user.IsBanned)
            {
                MessageBox.Show("Вы заблокированы.");
                return;
            }

            Session.Login(user);

            var mainVM = (MainViewModel)Application.Current.MainWindow.DataContext;
            mainVM.UpdateUserStatus();

            //MessageBox.Show("Вы вошли!");

            if (parameter is FrameworkElement element)
                mainVM.OpenAccountMenuCommand.Execute(element);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
