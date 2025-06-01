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
using System.Windows.Shapes;

namespace Pinterest
{
    /// <summary>
    /// Логика взаимодействия для SubscribeDialog.xaml
    /// </summary>
    public partial class SubscribeDialog : Window
    {
        public bool IsConfirmed { get; private set; } = false;
        public bool IsUnsubscribed { get; private set; } = false;

        public SubscribeDialog(bool isAlreadySubscribed)
        {
            InitializeComponent();
            if (isAlreadySubscribed)
            {
                SubscribeButton.Visibility = Visibility.Collapsed;
                UnsubscribeButton.Visibility = Visibility.Visible;
            }
            else
            {
                SubscribeButton.Visibility = Visibility.Visible;
                UnsubscribeButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.Close();
        }

        private void Unsubscribe_Click(object sender, RoutedEventArgs e)
        {
            IsUnsubscribed = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }


}
