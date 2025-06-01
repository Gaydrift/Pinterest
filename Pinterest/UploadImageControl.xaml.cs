using System.Windows.Controls;

namespace Pinterest
{
    public partial class UploadImageControl : UserControl
    {
        public UploadImageControl()
        {
            InitializeComponent();
            DataContext = new UploadImageViewModel();
        }
    }
}
