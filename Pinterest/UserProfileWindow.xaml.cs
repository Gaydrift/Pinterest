using System.Globalization;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Pinterest
{
    public partial class UserProfileWindow : UserControl
    {
        private readonly IUserRepository _userRepository;

        public UserProfileWindow(IUserRepository userRepository)
        {
            InitializeComponent();
            _userRepository = userRepository;
            DataContext = new UserProfileViewModel();
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var imageVm = ((FrameworkElement)sender).DataContext as GalleryItemViewModel;
            if (imageVm == null) return;

            var mainVm = (MainViewModel)((MainWindow)Application.Current.MainWindow).DataContext;

            Action<ImageMetaData> showImageDetails = null;

            showImageDetails = selectedMeta => mainVm.CurrentView = new ImageDetailsControl(
                selectedMeta,
                () => mainVm.CurrentView = new UserProfileWindow(_userRepository),
                showImageDetails,
                _userRepository,
                mainVm);

            showImageDetails(imageVm.MetaData);
        }




    }
    public class NullOrEmptyToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
