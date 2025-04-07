using Avalonia.Controls;
using System;

namespace ParkAccess
{
    public partial class PopupWindow : Window
    {
        public PopupWindow()
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
