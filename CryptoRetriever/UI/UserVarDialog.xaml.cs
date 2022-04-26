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
using CryptoRetriever.Strats;
using ValueType = CryptoRetriever.Strats.ValueType;

namespace CryptoRetriever.UI {
    /// <summary>
    /// A generic window for creating a new user variable.
    /// </summary>
    public partial class UserVarDialog : Window {
        private IValue _result = null;
        private IValue _workingValue = null;

        public IValue Result {
            get { return _result; }
        }

        /// <summary>
        /// Set a starting value to be edited
        /// </summary>
        /// <param name="val">The value to edit</param>
        public void SetWorkingValue(IValue val) {
            // The inheritance hierarchy doesn't work properly for this
            // May want to revisit to avoid casting and make the interface clearer.
            if (!(val is IVariable))
                throw new ArgumentException("Value must be user variable.");
            _workingValue = val;
        }

        public UserVarDialog() {
            InitializeComponent();
            DataContext = this;
        }

        private void OnCreateButtonClicked(object sender, RoutedEventArgs e) {
            if (String.IsNullOrWhiteSpace(_variableName.Text)) {
                MessageBox.Show("You must provide a name.");
                return;
            }

            ValueType type = (ValueType)Enum.GetValues(ValueType.String.GetType()).GetValue(_typesBox.SelectedIndex);
            if (type == ValueType.String) {
                _result = new UserStringVariable("User." + _variableName.Text, _defaultValue.Text);
            } else if (type == ValueType.Number) {
                double defaultNum;
                if (Double.TryParse(_defaultValue.Text, out defaultNum))
                    _result = new UserNumberVariable("User." + _variableName.Text, defaultNum);
                else
                    MessageBox.Show("The default must be a number.");
            } else {
                throw new Exception("Unknown type: " + type.ToString());
            }

            // If there wasn't something wrong with the input, close
            if (_result != null)
                Close();
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e) {
            _result = null;
            Close();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            // If the window is relaunched, clear the result
            // in case they cancel from here.
            _result = null;

            // Fill out the combobox items
            foreach (ValueType type in Enum.GetValues(ValueType.String.GetType())) {
                _typesBox.Items.Add(type.ToString());
            }

            if (_workingValue != null) {
                IUserVariable userVariable = (IUserVariable)_workingValue;
                _variableName.Text = (_workingValue as IUserVariable).GetVariableName().Replace("User.", "");
                _defaultValue.Text = userVariable.GetDefaultValue();
                _typesBox.SelectedIndex = Array.IndexOf(Enum.GetValues(_workingValue.GetValueType().GetType()), _workingValue.GetValueType());
            }
        }

        private void OnEnterTextKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                Close();
            }
        }
    }
}
