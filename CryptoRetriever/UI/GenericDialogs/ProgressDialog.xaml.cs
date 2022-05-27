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

namespace CryptoRetriever.UI.GenericDialogs {
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window {
        private long _maxProgress = 100;
        private long _currentProgress = 0;

        public ProgressDialog() {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Loaded += OnProgressDialogLoaded;
            _progressBackground.SizeChanged += OnProgressBackgroundSizeChanged;
        }

        /// <summary>
        /// Gets or sets the label above the progress indicator.
        /// </summary>
        public String Label {
            get {
                return _labelTb.Text;
            }
            set {
                _labelTb.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the upper limit of the progress bar.
        /// Default value is 100.
        /// </summary>
        public long MaximumProgress {
            get {
                return _maxProgress;
            }
            set {
                _maxProgress = value;
                _progressMaxTextTb.Text = "" + _maxProgress;
                UpdateProgressRectangle();
            }
        }

        /// <summary>
        /// Gets or sets the current progress (out of MaximumProgress).
        /// </summary>
        public long CurrentProgress {
            get {
                return _currentProgress;
            }
            set {
                _currentProgress = value;
                _progressTextTb.Text = "" + _currentProgress;
                UpdateProgressRectangle();
            }
        }

        private void UpdateProgressRectangle() {
            double fraction = (double)_currentProgress / (double)_maxProgress;
            _progressRectangle.Width = fraction * _progressBackground.ActualWidth;
        }

        private void OnProgressBackgroundSizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateProgressRectangle();
        }

        private void OnProgressDialogLoaded(object sender, RoutedEventArgs e) {
            UpdateProgressRectangle();
        }
    }
}
