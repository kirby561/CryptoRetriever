using System;
using System.Windows;

namespace CryptoRetriever {
    /// <summary>
    /// Interaction logic for EndlessProgressDialog.xaml
    /// </summary>
    public partial class EndlessProgressDialog : Window {
        public EndlessProgressDialog() {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        public void SetText(String text) {
            _text.Text = text;
        }
    }
}
