using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Allows a user to select a single item from the configured ItemsSource.
    /// When the dialog closes, the SelectedIndex will be set to the index of
    /// the selected item within ItemsSource. If the user closes the dialog
    /// without selecting Okay, the SelectedIndex will be -1.
    /// </summary>
    public partial class ListBoxDialog : Window {
        /// <summary>
        /// Sets the ItemsSource to use in the list in this dialog.
        /// The index returned by SelectedIndex will be with respect to
        /// this collection. Items in the source must have a Text property.
        /// If not, you can set the source using the SetItemSource method.
        /// </summary>
        public IEnumerable ItemsSource {
            get {
                return _itemsList.ItemsSource;
            }
            set {
                _itemsList.ItemsSource = value;
            }
        }

        /// <summary>
        /// Set to the selected index in the list box if any.
        /// If this is -1 it means no selection was made.
        /// </summary>
        public int SelectedIndex { get; set; } = -1;

        /// <summary>
        /// Set the items in the list for any type and give a function that gets the text
        /// to display from the item somehow. Note that this needs to be called again if 
        /// the text changes if using this method.
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="getTextFunc">A function that gets the display text from the given object.</param>
        /// <param name="itemsSource">The list of items of type T</param>
        public void SetItemSource<T>(Func<T, String> getTextFunc, IEnumerable<T> itemSource) {
            List<ItemWrapper> wrapperList = new List<ItemWrapper>();
            foreach (T item in itemSource) {
                wrapperList.Add(new ItemWrapper(getTextFunc.Invoke(item)));
            }
            ItemsSource = wrapperList;
        }

        public ListBoxDialog() {
            InitializeComponent();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnOkayClicked(object sender, RoutedEventArgs e) {
            SelectedIndex = _itemsList.SelectedIndex;
            Close();
        }

        private void OnCheckEnterPressed(object sender, KeyEventArgs e) {
            // Accept on enter
            if (e.Key == Key.Enter) {
                OnOkayClicked(null, null);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            _itemsList.Focus();
            _itemsList.SelectedIndex = 0;
        }

        private void OnDoubleClick(object sender, MouseButtonEventArgs e) {
            if (_itemsList.SelectedIndex >= 0) {
                OnOkayClicked(null, null);
            }
        }

        class ItemWrapper {
            public String Text { get; set; }

            public ItemWrapper(String text) {
                Text = text;
            }
        }
    }
}
