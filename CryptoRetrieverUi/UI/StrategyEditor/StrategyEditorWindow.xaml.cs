using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using CryptoRetriever.Filter;
using CryptoRetriever.Strats;
using System.Linq;
using ValueType = CryptoRetriever.Strats.ValueType;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Allows the user to modify or create a strategy with a UI
    /// </summary>
    public partial class StrategyEditorWindow : Window {
        private Strategy _strategy; // The currently edited strategy
        private Strategy _result = null; // This will be set to the strategy when the editing is done
        private StrategyManager _strategyManager;
        private bool _isEditing = false;

        private Strategy _originalStrategy; // Keep track of the original strategy if editing so we can remove the old file

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
            _strategyManager = manager;
            UpdateWorkingStrategy(new Strategy());
        }

        /// <summary>
        /// Sets the strategy to edit and adjusts the UI to reflect the
        /// values contained as its defaults.
        /// </summary>
        /// <param name="strategyToEdit">The strategy to edit.</param>
        public void SetWorkingStrategy(Strategy strategyToEdit) {
            _isEditing = true;
            _originalStrategy = strategyToEdit;
            UpdateWorkingStrategy(strategyToEdit.Clone());
        }

        /// <summary>
        /// Updates the working strategy and sets the UI to reflect it.
        /// </summary>
        /// <param name="newStrategy">The new strategy to update to.</param>
        private void UpdateWorkingStrategy(Strategy newStrategy) {
            _strategy = newStrategy;
            _nameTextBox.Text = newStrategy.Name;
            _accountStartingFiatTextBox.Text = "" + _strategy.Account.CurrencyBalance;
            _accountStartingAssetsTextBox.Text = "" + _strategy.Account.AssetBalance;
            _exchangeTransactionFeeTextBox.Text = "" + _strategy.ExchangeAssumptions.TransactionFee;
            _exchangeTransactionFeePercentTextBox.Text = "" + _strategy.ExchangeAssumptions.TransactionFeePercentage;
            _exchangeTransactionTimeTextBox.Text = "" + _strategy.ExchangeAssumptions.TransactionTimeS;
            if (_strategy.Start != DateTime.MinValue)
                _startDatePicker.SelectedDate = _strategy.Start;
            if (_strategy.End != DateTime.MinValue)
                _endDatePicker.SelectedDate = _strategy.End;
            _filtersView.ItemsSource = _strategy.Filters;
            _userVars.ItemsSource = _strategy.UserVars;
            _triggersView.ItemsSource = _strategy.Triggers;
            _userVarRunners.ItemsSource = _strategy.VariableRunners;
            if (_strategy.OptimizationVariable != null)
                _optVariableTb.Text = _strategy.OptimizationVariable.GetVariableName();
            else
                _optVariableTb.Text = "Default";
            _engineTb.Text = _strategy.EngineId;

            // Take this opportunity to check if the engine ID exists and
            // warn the user if it does not.
            if (_strategyManager.GetEngineFactoryById(_strategy.EngineId) == null)
                MessageBox.Show("Warning: The selected engine ID '" + _strategy.EngineId + "' is not currently loaded. Please check that the selected engine is correct.");
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
            double transactionPercentage;
            if (!Double.TryParse(_exchangeTransactionFeePercentTextBox.Text, out transactionPercentage)) {
                MessageBox.Show("The transaction fee % must be a number.");
                return;
            }
            double transactionTime;
            if (!Double.TryParse(_exchangeTransactionTimeTextBox.Text, out transactionTime)) {
                MessageBox.Show("The transaction time must be a number.");
                return;
            }
            assumptions.TransactionFee = transactionFee;
            assumptions.TransactionFeePercentage = transactionPercentage;
            assumptions.TransactionTimeS = transactionTime;

            UserNumberVariable optVariable = null;
            if (_optVariableTb.Text != "Default") {
                foreach (IValue val in _strategy.UserVars) {
                    if (val is UserNumberVariable) {
                        UserNumberVariable var = (UserNumberVariable)val;
                        if (var.GetVariableName().Equals(_optVariableTb.Text))
                            optVariable = var;
                    }
                }
            }

            _strategy.EngineId = _engineTb.Text;

            // The Filters/States/Triggers are updated
            // as we go on the working Strategy so no
            // need to do anything for those.

            _strategy.Name = name;
            _strategy.Account = new Account(startingFiat, startingAssets);
            _strategy.ExchangeAssumptions = assumptions;
            _strategy.OptimizationVariable = optVariable;
            _strategy.Start = startDate;
            _strategy.End = endDate;
            _result = _strategy;

            Close();

            if (_isEditing) {
                _strategyManager.UpdateStrategy(_strategy, _originalStrategy);
            } else {
                _strategyManager.AddStrategy(_result);
            }
        }

        private void OnAddFilterClicked(object sender, RoutedEventArgs e) {
            Dictionary<IFilter, BaseFilterDialog> filterDialogs = FilterUi.GetFilterUiMap();
            List<IFilter> filterOptions = filterDialogs.Keys.ToList();
            ListBoxDialog filterOptionsDialog = new ListBoxDialog();
            filterOptionsDialog.SetItemSource((filter) => {
                return filter.GetType().Name;
            }, filterDialogs.Keys);
            filterOptionsDialog.ShowDialog();

            IFilter result = null;
            if (filterOptionsDialog.SelectedIndex >= 0) {
                IFilter selectedFilter = filterOptions[filterOptionsDialog.SelectedIndex];
                BaseFilterDialog dialog = filterDialogs[selectedFilter];
                if (dialog == null) {
                    // A null dialog means no options so just add the filter
                    result = selectedFilter;
                } else {
                    dialog.SetWorkingFilter(selectedFilter);
                    dialog.ShowDialog();
                    result = dialog.GetResult();
                }
            }

            if (result != null)
                _strategy.Filters.Add(result);
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

        private void OnFiltersDoubleClicked(object sender, MouseButtonEventArgs e) {
            int selectedIndex = _filtersView.SelectedIndex;
            if (selectedIndex >= 0) {
                BaseFilterDialog filterDialog = FilterUi.GetDialogFor(_strategy.Filters[selectedIndex]);
                if (filterDialog == null)
                    return; // This filter cannot be edited

                filterDialog.SetWorkingFilter(_strategy.Filters[selectedIndex]);
                UiHelper.CenterWindowInWindow(filterDialog, this);
                filterDialog.ShowDialog();

                if (filterDialog.GetResult() != null) {
                    _strategy.Filters.RemoveAt(selectedIndex);
                    _strategy.Filters.Insert(selectedIndex, filterDialog.GetResult());
                }
            }
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

        private void OnUserVariablesDoubleClicked(object sender, MouseButtonEventArgs e) {
            int selectedVarIndex = _userVars.SelectedIndex;
            if (selectedVarIndex >= 0) {
                IValue result = _strategy.UserVars[_userVars.SelectedIndex];
                String beforeName = ((IUserVariable)result).GetVariableName();
                while (result != null) {
                    // Show it until they cancel or get one that works
                    var repeat = new UserVarDialog();
                    repeat.Title = "Editing User Var";
                    repeat.SetWorkingValue(result);
                    UiHelper.CenterWindowInWindow(repeat, this);
                    repeat.ShowDialog();
                    result = repeat.Result;

                    if (result != null) {
                        _strategy.UserVars.RemoveAt(selectedVarIndex);
                        _strategy.UserVars.Insert(selectedVarIndex, result);
                        break;
                    }
                }
            }
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

        private void OnUserVariableRunnersDoubleClicked(object sender, MouseButtonEventArgs e) {
            if (_userVarRunners.SelectedIndex >= 0) {
                VariableRunnerDialog dialog = new VariableRunnerDialog(_strategy);
                dialog.SetWorkingValue(_strategy.VariableRunners[_userVarRunners.SelectedIndex]);
                UiHelper.CenterWindowInWindow(dialog, this);
                dialog.ShowDialog();

                if (dialog.Result != null)
                    _strategy.VariableRunners[_userVarRunners.SelectedIndex] = dialog.Result;
            }
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
            int selectedIndex = _triggersView.SelectedIndex;
            if (selectedIndex >= 0) {
                TriggerEditorWindow triggerEditor = new TriggerEditorWindow(_strategy);
                UiHelper.CenterWindowInWindow(triggerEditor, this);

                triggerEditor.WorkingTrigger = _strategy.Triggers[selectedIndex].Clone();
                triggerEditor.ShowDialog();

                if (triggerEditor.Trigger != null) {
                    // Remove/add it again in case it was renamed
                    _strategy.Triggers.RemoveAt(selectedIndex);
                    _strategy.Triggers.Insert(selectedIndex, triggerEditor.Trigger);
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

        private void OnOptVariableTbClicked(object sender, MouseButtonEventArgs e) {
            List<UserNumberVariable> options = new List<UserNumberVariable>();
            
            // Add a "Default" option as well which represents the account value
            UserNumberVariable defaultOption = new UserNumberVariable("Default", 0);
            options.Add(defaultOption);

            foreach (IUserVariable var in _strategy.UserVars)
                if (var.GetValueType().Equals(ValueType.Number))
                    options.Add((UserNumberVariable)var);

            ListBoxDialog dialog = new ListBoxDialog();
            dialog.SetItemSource(
                (UserNumberVariable c) => {
                    return c.GetLabel();
                },
                options);
            UiHelper.CenterWindowInWindow(dialog, this);
            dialog.ShowDialog();

            if (dialog.SelectedIndex >= 0) {
                UserNumberVariable result = options[dialog.SelectedIndex];
                _optVariableTb.Text = result.GetVariableName();
            }
        }

        private void OnEngineTbClicked(object sender, MouseButtonEventArgs e) {
            List<String> options = _strategyManager.GetEngines().Select(x => x.GetId()).ToList();

            ListBoxDialog dialog = new ListBoxDialog();
            dialog.SetItemSource(
                (String c) => {
                    return c;
                },
                options);
            UiHelper.CenterWindowInWindow(dialog, this);
            dialog.ShowDialog();

            if (dialog.SelectedIndex >= 0) {
                String selectedId = options[dialog.SelectedIndex];
                _engineTb.Text = selectedId;
            }
        }
    }
}
