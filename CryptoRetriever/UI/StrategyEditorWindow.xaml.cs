using System;
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
using CryptoRetriever.Filter;
using CryptoRetriever.Strats;
using CryptoRetriever.Utility.JsonObjects;
using Utf8Json;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Allows the user to modify or create a strategy with a UI
    /// </summary>
    public partial class StrategyEditorWindow : Window {
        private Strategy _strategy; // The currently edited strategy
        private Strategy _result = null; // This will be set to the strategy when the editing is done
        private StrategyManager _strategyManager;
        private bool _isEditing = false;
        private String _originalName; // Keep track of the original name if editing so we can remove the old file

        /// <summary>
        /// Gets the strategy modified or created by this editor or
        /// null if the editor was cancelled or closed without saving.
        /// </summary>
        public Strategy Strategy {
            get {
                return _result;
            }
        }

        public StrategyEditorWindow(StrategyManager manager) {
            InitializeComponent();
            SetWorkingStrategy(new Strategy());

            _strategyManager = manager;
        }

        /// <summary>
        /// Sets the strategy to edit and adjusts the UI to reflect the
        /// values contained as its defaults.
        /// </summary>
        /// <param name="strategyToEdit">The strategy to edit.</param>
        public void SetWorkingStrategy(Strategy strategyToEdit) {
            _isEditing = true;
            _originalName = strategyToEdit.Name;
            _strategy = strategyToEdit;
            _nameTextBox.Text = strategyToEdit.Name;
            _accountStartingFiatTextBox.Text = "" + _strategy.Account.CurrencyBalance;
            _accountStartingAssetsTextBox.Text = "" + _strategy.Account.AssetBalance;
            _exchangeTransactionFeeTextBox.Text = "" + _strategy.ExchangeAssumptions.TransactionFee;
            _exchangeTransationTimeTextBox.Text = "" + _strategy.ExchangeAssumptions.TransactionTimeS;
            if (_strategy.Start != DateTime.MinValue)
                _startDatePicker.SelectedDate = _strategy.Start;
            if (_strategy.End != DateTime.MinValue)
                _endDatePicker.SelectedDate = _strategy.End;
            _filtersView.ItemsSource = _strategy.Filters;
            _userVars.ItemsSource = _strategy.UserVars;
            _triggersView.ItemsSource = _strategy.Triggers;
            _userVarRunners.ItemsSource = _strategy.VariableRunners;
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnSaveButtonClicked(object sender, RoutedEventArgs e) {
            // Validate the name 
            String name = _nameTextBox.Text;
            if (String.IsNullOrWhiteSpace(name)) {
                MessageBox.Show("You must enter a name.");
                return;
            }

            if (!_isEditing && _strategyManager.GetStrategyByName(name) != null) {
                MessageBox.Show("A strategy by that name already exists.");
                return;
            }

            // Validate account
            double startingFiat = -1;
            double startingAssets = -1;
            if (!Double.TryParse(_accountStartingFiatTextBox.Text, out startingFiat) ||
                !Double.TryParse(_accountStartingAssetsTextBox.Text, out startingAssets) ||
                startingFiat < 0 || 
                startingAssets < 0) {
                MessageBox.Show("Starting Fiat and Assets need to be 0 or greater.");
                return;
            }

            // Check the start/end dates. If they're valid dates use them otherwise
            // indicate that the dataset start and end should be used.
            // The validity of these dates depends on the dataset. Arguably they shouldn't
            // be set until the strategy is actually run on a dataset.
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;
            if (_startDatePicker.SelectedDate.HasValue)
                startDate = _startDatePicker.SelectedDate.Value;
            if (_endDatePicker.SelectedDate.HasValue)
                endDate = _endDatePicker.SelectedDate.Value;

            if (endDate < startDate) {
                MessageBox.Show("The end date cannot be before the start date.");
                return;
            }

            // Exchange assumptions
            ExchangeAssumptions assumptions = new ExchangeAssumptions();
            double transactionFee;
            if (!Double.TryParse(_exchangeTransactionFeeTextBox.Text, out transactionFee)) {
                MessageBox.Show("The transaction fee must be a number.");
                return;
            }
            double transactionTime;
            if (!Double.TryParse(_exchangeTransationTimeTextBox.Text, out transactionTime)) {
                MessageBox.Show("The transaction time must be a number.");
                return;
            }
            assumptions.TransactionFee = transactionFee;
            assumptions.TransactionTimeS = transactionTime;

            // The Filters/States/Triggers are updated
            // as we go on the working Strategy so no
            // need to do anything for those.

            _strategy.Name = name;
            _strategy.Account = new Account(startingFiat, startingAssets);
            _strategy.ExchangeAssumptions = assumptions;
            _strategy.Start = startDate;
            _strategy.End = endDate;
            _result = _strategy;

            Close();

            if (_isEditing) {
                if (!_result.Name.Equals(_originalName))
                    _strategyManager.DeleteStrategyByName(_originalName);
                _strategyManager.UpdateStrategy(_result);
            } else {
                _strategyManager.AddStrategy(_result);
            }
        }

        private void OnAddFilterClicked(object sender, RoutedEventArgs e) {
            AddFilterDialog addFilterDialog = new AddFilterDialog();
            UiHelper.CenterWindowInWindow(addFilterDialog, this);
            addFilterDialog.ShowDialog();

            if (addFilterDialog.Filter != null) {
                _strategy.Filters.Add(addFilterDialog.Filter);
            }
        }

        private void OnRemoveFilterClicked(object sender, RoutedEventArgs e) {
            UiHelper.RemoveSelectedItemsFromListBox(_strategy.Filters, _filtersView);
        }

        private void OnMoveFilterUpClicked(object sender, RoutedEventArgs e) {
            UiHelper.MoveSelectedItemUpInListBox(_strategy.Filters, _filtersView);
        }

        private void OnMoveFilterDownClicked(object sender, RoutedEventArgs e) {
            UiHelper.MoveSelectedItemDownInListBox(_strategy.Filters, _filtersView);
        }

        private void OnAddUserVarClicked(object sender, RoutedEventArgs e) {
            UserVarDialog inputBox = new UserVarDialog();
            inputBox.Title = "New User Var";
            UiHelper.CenterWindowInWindow(inputBox, this);
            inputBox.ShowDialog();

            IValue result = inputBox.Result;
            while (result != null) {
                if (!StrategyContainsVarName(inputBox.Result)) {
                    _strategy.UserVars.Add(inputBox.Result);
                    break;
                } else {
                    // Show it until they cancel or get one that works
                    var repeat = new UserVarDialog();
                    repeat.Title = inputBox.Title;
                    repeat.SetWorkingValue(result);
                    UiHelper.CenterWindowInWindow(repeat, this);
                    repeat.ShowDialog();
                    result = repeat.Result;
                }
            }
        }

        private void OnRemoveUserVarClicked(object sender, RoutedEventArgs e) {
            UiHelper.RemoveSelectedItemsFromListBox(_strategy.UserVars, _userVars);
        }

        private void OnMoveUserVarUpClicked(object sender, RoutedEventArgs e) {
            UiHelper.MoveSelectedItemUpInListBox(_strategy.UserVars, _userVars);
        }

        private void OnMoveUserVarDownClicked(object sender, RoutedEventArgs e) {
            UiHelper.MoveSelectedItemDownInListBox(_strategy.UserVars, _userVars);
        }

        private void OnMoveUserVarRunnerUpClicked(object sender, RoutedEventArgs e) {
            UiHelper.MoveSelectedItemUpInListBox(_strategy.VariableRunners, _userVarRunners);
        }

        private void OnMoveUserVarRunnerDownClicked(object sender, RoutedEventArgs e) {
            UiHelper.MoveSelectedItemDownInListBox(_strategy.VariableRunners, _userVarRunners);
        }

        private void OnAddUserVarRunnerClicked(object sender, RoutedEventArgs e) {
            VariableRunnerDialog dialog = new VariableRunnerDialog(_strategy);
            dialog.Title = "New Variable Runner";
            UiHelper.CenterWindowInWindow(dialog, this);
            dialog.ShowDialog();

            if (dialog.Result != null)
                _strategy.VariableRunners.Add(dialog.Result);
        }

        private void OnRemoveUserVarRunnerClicked(object sender, RoutedEventArgs e) {
            UiHelper.RemoveSelectedItemsFromListBox(_strategy.VariableRunners, _userVarRunners);
        }

        private void OnAddTriggerClicked(object sender, RoutedEventArgs e) {
            TriggerEditorWindow triggerEditor = new TriggerEditorWindow(_strategy);
            UiHelper.CenterWindowInWindow(triggerEditor, this);
            triggerEditor.ShowDialog();

            if (triggerEditor.Trigger != null) {
                _strategy.Triggers.Add(triggerEditor.Trigger);
            }
        }

        private void OnTriggerDoubleClicked(object sender, MouseButtonEventArgs e) {
            if (_triggersView.SelectedIndex >= 0) {
                TriggerEditorWindow triggerEditor = new TriggerEditorWindow(_strategy);
                UiHelper.CenterWindowInWindow(triggerEditor, this);

                // ?? TODO: This should be a deep copy but there's no way to save conditions
                // right now so it will be a reference until that is implemented.
                triggerEditor.WorkingTrigger = _strategy.Triggers[_triggersView.SelectedIndex];
                triggerEditor.ShowDialog();

                if (triggerEditor.Trigger != null) {
                    _strategy.Triggers[_triggersView.SelectedIndex] = triggerEditor.Trigger;
                }
            }
        }

        private void OnRemoveTriggerClicked(object sender, RoutedEventArgs e) {
            UiHelper.RemoveSelectedItemsFromListBox(_strategy.Triggers, _triggersView);
        }

        private void OnMoveTriggerUpClicked(object sender, RoutedEventArgs e) {
            UiHelper.MoveSelectedItemUpInListBox(_strategy.Triggers, _triggersView);
        }

        private void OnMoveTriggerDownClicked(object sender, RoutedEventArgs e) {
            UiHelper.MoveSelectedItemDownInListBox(_strategy.Triggers, _triggersView);
        }

        private bool StrategyContainsVarName(IValue valueToCheck) {
            IVariable varToCheck = valueToCheck as IVariable;
            foreach (IValue val in _strategy.UserVars) {
                IVariable var = val as IVariable;
                if (var.GetVariableName().Equals(varToCheck.GetVariableName()))
                    return true;
            }
            return false;
        }
    }
}
