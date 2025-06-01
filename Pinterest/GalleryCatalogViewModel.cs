using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace Pinterest
{
    public class GalleryCatalogViewModel : ViewModel
    {
        private readonly Data.ImageRepository _repo;
        private List<ImageMetaData> _allImages;

        private ObservableCollection<GalleryItemViewModel> _images;
        public ObservableCollection<GalleryItemViewModel> Images
        {
            get => _images;
            set { _images = value; OnPropertyChanged(); }
        }

        public GalleryCatalogViewModel()
        {
            string connectionString = "Data Source=NOUTBOOK;Initial Catalog=PinterestDB;Integrated Security=True;TrustServerCertificate=True";
            var userRepo = new UserRepository(connectionString);
            _repo = new Data.ImageRepository(connectionString, userRepo);
            Images = new ObservableCollection<GalleryItemViewModel>();
        }


        public void LoadImages()
        {
            _allImages = _repo.GetAll();
            Images = new ObservableCollection<GalleryItemViewModel>(
                _allImages.Select(meta => new GalleryItemViewModel(meta))
            );
        }

        public void FilterImages(string authorFilter, string tagFilter)
        {
            List<ImageMetaData> filtered;

            if (string.IsNullOrEmpty(authorFilter) && string.IsNullOrEmpty(tagFilter))
            {
                filtered = new List<ImageMetaData>(_allImages);
            }
            else
            {
                filtered = _repo.Search(authorFilter, tagFilter);
            }

            Images = new ObservableCollection<GalleryItemViewModel>(
                filtered.Select(meta => new GalleryItemViewModel(meta))
            );
        }

        public void AddImage(ImageMetaData image)
        {
            _repo.Add(image);
            LoadImages();
        }

        public void UpdateImage(ImageMetaData image)
        {
            _repo.Update(image);
            LoadImages();
        }

        public void DeleteImage(int id)
        {
            _repo.Delete(id);
            LoadImages();
        }
    }

    public class GalleryItemViewModel
    {
        public ImageMetaData MetaData { get; }
        public BitmapImage ImageSource { get; }

        public GalleryItemViewModel(ImageMetaData meta)
        {
            MetaData = meta;
            ImageSource = meta.ImageSource;
        }
    }
}
