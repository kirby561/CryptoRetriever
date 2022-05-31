using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CryptoRetriever.UI {
    /// <summary>
    /// A Window that includes information about the version of the app and
    /// credits the sources of images and libraries.
    /// </summary>
    public partial class AboutWindow : Window {
        public AboutWindow() {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            };
            Process.Start(psi);
            e.Handled = true;
        }
    }
}
