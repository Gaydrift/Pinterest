using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pinterest
{
    public partial class GalleryCatalog : UserControl
    {
        public GalleryCatalog()
        {
            InitializeComponent();
            DataContext = new GalleryCatalogViewModel();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is GalleryCatalogViewModel vm)
                vm.LoadImages();
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var imageVm = ((FrameworkElement)sender).DataContext as GalleryItemViewModel;
            if (imageVm == null) return;

            var mainVm = (MainViewModel)((MainWindow)Application.Current.MainWindow).DataContext;
            var userRepository = mainVm.UserRepository;

            Action<ImageMetaData> showImageDetails = null;

            showImageDetails = selectedMeta =>
            {
                var detailsControl = new ImageDetailsControl(
                    selectedMeta,
                    () => mainVm.CurrentView = new GalleryCatalog(),
                    showImageDetails,
                    userRepository,
                    mainVm);

                mainVm.CurrentView = detailsControl;
            };

            showImageDetails(imageVm.MetaData);
        }


    }
}
