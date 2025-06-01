using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pinterest
{
    /// <summary>
    /// Логика взаимодействия для OtherUserProfileWindow.xaml
    /// </summary>
    public partial class OtherUserProfileWindow : UserControl
    {
        private readonly IUserRepository _userRepository;

        public OtherUserProfileWindow(OtherUserProfileViewModel viewModel, IUserRepository userRepository)
        {
            InitializeComponent();
            DataContext = viewModel;
            _userRepository = userRepository;
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var imageVm = ((FrameworkElement)sender).DataContext as GalleryItemViewModel;
            if (imageVm == null) return;

            var mainVm = (MainViewModel)((MainWindow)Application.Current.MainWindow).DataContext;
            var currentProfileControl = this;

            Action<ImageMetaData> showImageDetails = null;

            showImageDetails = selectedMeta => mainVm.CurrentView = new ImageDetailsControl(
                selectedMeta,
                () => mainVm.CurrentView = currentProfileControl,
                showImageDetails,
                _userRepository,
                mainVm);

            showImageDetails(imageVm.MetaData);
        }


    }
}
