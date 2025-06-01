using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel.DataAnnotations;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
using System;

namespace Pinterest
{
    public partial class AccountControl : UserControl
    {
        public AccountControl()
        {
            InitializeComponent();
            if (Session.CurrentUser != null)
            {
                var mainVM = (MainViewModel)Application.Current.MainWindow.DataContext;
                mainVM.UpdateUserStatus();
            }

        }

        private void RegisterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountControlViewModel vm)
                vm.Password = ((PasswordBox)sender).Password;
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountControlViewModel vm)
                vm.ConfirmPassword = ((PasswordBox)sender).Password;
        }

        private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountControlViewModel vm)
                vm.LoginPassword = ((PasswordBox)sender).Password;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem selectedItem)
            {
                var languageCode = selectedItem.Tag.ToString();

                string dictPath = $"language/Strings.{languageCode}.xaml";
                var dict = new ResourceDictionary() { Source = new Uri(dictPath, UriKind.Relative) };

                var oldDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("language/Strings."));

                if (oldDict != null)
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);

                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
        }



    }

    public class User : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Имя должно содержать от 5 до 100 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не короче 6 символов")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Роль обязательна")]
        [RegularExpression("^(Пользователь|Администратор)$", ErrorMessage = "Роль должна быть 'Пользователь' или 'Администратор'")]
        public string Role { get; set; }
        public bool CanUpload { get; set; } = true;

        public List<string> SubscribedTo { get; set; } = new();
        public bool IsAdmin { get; set; }
        public bool IsBanned { get; set; } = false;
        public List<string> SavedImageFileNames { get; set; } = new();
        public List<string> LikedImageFileNames { get; set; } = new();
        public Dictionary<string, List<string>> Collections { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (SavedImageFileNames.Any(string.IsNullOrWhiteSpace))
                results.Add(new ValidationResult("Список сохранённых изображений содержит пустые значения", new[] { nameof(SavedImageFileNames) }));

            if (LikedImageFileNames.Any(string.IsNullOrWhiteSpace))
                results.Add(new ValidationResult("Список понравившихся изображений содержит пустые значения", new[] { nameof(LikedImageFileNames) }));

            foreach (var collection in Collections)
            {
                if (string.IsNullOrWhiteSpace(collection.Key))
                    results.Add(new ValidationResult("Название коллекции не может быть пустым", new[] { nameof(Collections) }));

                if (collection.Value == null || collection.Value.Any(string.IsNullOrWhiteSpace))
                    results.Add(new ValidationResult($"Коллекция '{collection.Key}' содержит недопустимые значения", new[] { nameof(Collections) }));
            }

            return results;
        }
    }


    public class UserStorage
    {
        private const string FileName = "users.json";

        public void SaveUser(User user)
        {
            List<User> users = LoadUsers();
            users.Add(user);

            string json = JsonConvert.SerializeObject(users, Formatting.Indented);
            File.WriteAllText(FileName, json);
        }

        public List<User> LoadUsers()
        {
            if (!File.Exists(FileName))
                return new List<User>();

            string json = File.ReadAllText(FileName);
            return JsonConvert.DeserializeObject<List<User>>(json);
        }
    }

    public static class Session
    {
        public static User CurrentUser { get; private set; }
        public static User CurrentViewedUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin => CurrentUser?.IsAdmin ?? false;

        public static void Login(User user)
        {
            CurrentUser = user;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }

}
