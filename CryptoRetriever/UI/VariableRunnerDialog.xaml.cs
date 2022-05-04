using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Linq;
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
    public partial class VariableRunnerDialog : Window {
        private VariableRunner _result = null;
        private VariableRunner _workingValue = null;
        private Strategy _strategy;
        private List<IValue> _varOptions;

        public VariableRunner Result {
            get { return _result; }
        }

        /// <summary>
        /// Set a starting runner to be edited
        /// </summary>
        /// <param name="val">The runner to edit</param>
        public void SetWorkingValue(VariableRunner val) {
            _workingValue = val;
        }

        public VariableRunnerDialog(Strategy strategy) {
            InitializeComponent();
            DataContext = this;
            _strategy = strategy;
        }

        private void OnCreateButtonClicked(object sender, RoutedEventArgs e) {
            double start;
            if (!Double.TryParse(_startTb.Text, out start)) {
                MessageBox.Show("The start must be a number.");
                return;
            }

            double end;
            if (!Double.TryParse(_endTb.Text, out end)) {
                MessageBox.Show("The end must be a number.");
                return;
            }

            double step;
            if (!Double.TryParse(_stepTb.Text, out step)) {
                MessageBox.Show("The step must be a number.");
                return;
            }

            int varIndex = _variablesList.SelectedIndex;
            if (varIndex < 0) {
                MessageBox.Show("You must select a variable.");
                return;
            }

            UserNumberVariable variable = (UserNumberVariable)_varOptions[varIndex];
            _result = new VariableRunner() {
                Variable = variable,
                Start = start,
                End = end,
                Step = step                
            };

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
            _varOptions = _strategy.UserVars.Where(new Func<IValue, bool>((val) => {
                return val is UserNumberVariable;
            })).ToList();
            foreach (IValue val in _varOptions) {
                _variablesList.Items.Add(val.GetLabel());
            }

            if (_workingValue != null) {
                _variablesList.SelectedIndex = IndexOf(_workingValue.Variable.GetLabel());
                _startTb.Text = "" + _workingValue.Start;
                _endTb.Text = "" + _workingValue.End;
                _stepTb.Text = "" + _workingValue.Step;
            }
        }

        private int IndexOf(String label) {
            for (int i = 0; i < _variablesList.Items.Count; i++) {
                if (_variablesList.Items[i].Equals(label))
                    return i;
            }
            return -1;
        }

        private void OnEnterTextKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                Close();
            }
        }
    }
}
