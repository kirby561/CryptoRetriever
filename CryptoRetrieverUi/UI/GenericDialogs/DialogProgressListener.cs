using CryptoRetriever.Utility;

namespace CryptoRetriever.UI.GenericDialogs {
    /// <summary>
    /// A listener that updates a ProgressDialog in response to
    /// progress events from a given ProgressListener.
    /// </summary>
    public class DialogProgressListener : ProgressListener {
        private ProgressDialog _dialog;

        /// <summary>
        /// Creates a listener that reports progress to the given dialog.
        /// </summary>
        /// <param name="dialog">The dialog to report progress to.</param>
        public DialogProgressListener(ProgressDialog dialog) {
            _dialog = dialog;
        }

        public void OnProgress(long currentValue, long maxProgress) {
            _dialog.Dispatcher.Invoke(() => {
                _dialog.CurrentProgress = currentValue;
                _dialog.MaximumProgress = maxProgress;
            });
        }
    }
}
