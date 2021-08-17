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

namespace StockStratMemes {
    /// <summary>
    /// Interaction logic for EndlessProgressDialog.xaml
    /// </summary>
    public partial class EndlessProgressDialog : Window {
        public EndlessProgressDialog() {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        public void SetText(String text) {
            _text.Text = text;
        }
    }
}
