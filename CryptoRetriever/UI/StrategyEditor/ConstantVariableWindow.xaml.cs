using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Allows a user to select a value or a variable from a list
    /// of options. If the user closes the window without making a
    /// selection either by not selecting or canceling the window,
    /// the ResultType will be set to None. Otherwise check Constant
    /// or SelectedIndex for the selection. The list of variables 
    /// should be set prior to showing the window via ItemsSource
    /// or the SetItemSource method.
    /// </summary>
    public partial class ConstantVariableWindow : Window {
        public enum SelectionType {
            Constant,
            Variable,
            None
        }

        // Keep track of the last change the user made so when Ok is
        // pressed we can set the result appropriately. It's stored
        // as a separate variable from ResultType so if they cancel
        // we can indicate that no selection was made.
        private SelectionType _currentSelectionType = SelectionType.None;

        /// <summary>
        /// Set to the type of selection the user made or None if they
        /// did not make one.
        /// The result will be in either Constant or SelectedIndex
        /// depending on if they picked a Constant or a Variable.
        /// </summary>
        public SelectionType ResultType { get; private set; } = SelectionType.None;

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
        /// Retrieves the constant the user entered if any.
        /// </summary>
        public String Constant { get; private set; } = "";

        /// <summary>
        /// Set to the selected index in the list box if any.
        /// If this is -1 it means no selection was made.
        /// </summary>
        public int SelectedIndex { get; set; } = -1;

        /// <summary>
        /// Set to false to only show the constant entry field
        /// and hide the variable list.
        /// </summary>
        public bool ShowVariableList { get; set; } = true;

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

        public ConstantVariableWindow() {
            InitializeComponent();
        }

        private void OnOkayClicked(object sender, RoutedEventArgs e) {
            if (String.IsNullOrEmpty(_constant.Text) && _itemsList.SelectedIndex < 0) {
                MessageBox.Show("Please make a selection first.");
                return;
            }

            ResultType = _currentSelectionType;
            if (ResultType == SelectionType.Constant)
                Constant = _constant.Text;
            else if (ResultType == SelectionType.Variable)
                SelectedIndex = _itemsList.SelectedIndex;
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnConstantChanged(object sender, TextChangedEventArgs e) {
            // The user is entering a constant, clear the selected var
            _currentSelectionType = SelectionType.Constant;
            SelectedIndex = -1;
            _itemsList.SelectedIndex = -1;
        }

        private void OnSelectedVariableChanged(object sender, SelectionChangedEventArgs e) {
            _currentSelectionType = SelectionType.Variable;
            _constant.Text = "";
        }

        private void OnCheckEnterPressed(object sender, KeyEventArgs e) {
            // Accept on enter
            if (e.Key == Key.Enter) {
                OnOkayClicked(null, null);
            }
        }

        private void OnListDoubleClick(object sender, MouseButtonEventArgs e) {
            if (_itemsList.SelectedIndex >= 0) {
                OnOkayClicked(null, null);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            _constant.Focus();

            if (!ShowVariableList) {
                _itemsList.Visibility = Visibility.Collapsed;
                _itemsListTitle.Visibility = Visibility.Collapsed;
                _currentSelectionType = SelectionType.Constant;
                SelectedIndex = -1;
                _itemsList.SelectedIndex = -1;
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
