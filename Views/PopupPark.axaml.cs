using Avalonia.Controls;
using System;

namespace ParkAccess
{
    public partial class PopupPark : Window
    {
        public PopupPark()
        {
            InitializeComponent();
        }

        private void OnSave(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnClose(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
