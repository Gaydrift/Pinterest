using System.Windows.Controls;
using System.Windows.Input;

namespace Pinterest
{
    public partial class SubscriptionsControl : UserControl
    {
        public SubscriptionsControl(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = new SubscriptionsViewModel(mainViewModel);
        }
    }

}
