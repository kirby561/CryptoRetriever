using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CryptoRetriever.UI {
    /// <summary>
    /// A generic window for grabbing text from the user.
    /// You can customize the Message, InitialText and Title to fit the use.
    /// Grab the result from EnteredText.
    /// </summary>
    public partial class InputBoxDialog : Window {
        private String _result = null;

        public String Message { 
            get {
                return _messageText.Text;
            }
            set {
                _messageText.Text = value;
            }
        }

        public String InitialText {
            get {
                return _enteredText.Text;
            }
            set {
                _enteredText.Text = value;
            }
        }

        public String EnteredText {
            get {
                return _result;
            }
        }

        public InputBoxDialog() {
            InitializeComponent();
            DataContext = this;
        }

        private void OnCreateButtonClicked(object sender, RoutedEventArgs e) {
            _result = _enteredText.Text;
            Close();
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e) {
            _result = null;
            Close();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            _enteredText.Focus();
        }

        private void OnEnterTextKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                _result = _enteredText.Text;
                Close();
            }
        }
    }
}
