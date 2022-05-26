using CryptoRetriever.Strats;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Interaction logic for StrategyResultsView.xaml
    /// </summary>
    public partial class StrategyResultsView : UserControl {
        private StrategyRuntimeContext _runContext;
        private ICoordinateFormatter _currencyFormatter = new DollarFormatter();
        private ObservableCollection<UserVarUiEntry> _userVarList = new ObservableCollection<UserVarUiEntry>();

        public StrategyRuntimeContext RunContext {
            get {
                return _runContext;
            }
            set {
                _runContext = value;
                UpdateUi();
            }
        }

        private void UpdateUi() {
            Strategy strategy = _runContext.Strategy;

            _strategyNameTb.Text = strategy.Name;

            // Currency per asset
            double price = _runContext.Dataset.Points[_runContext.Dataset.Points.Count - 1].Y;
            double originalPrice = _runContext.Dataset.Points[0].Y;

            // Account
            double originalValue = (strategy.Account.CurrencyBalance + strategy.Account.AssetBalance * originalPrice);
            double currentValue = (_runContext.Account.CurrencyBalance + _runContext.Account.AssetBalance * price);
            _accountValueTb.Text = _currencyFormatter.Format(currentValue);

            double valueChange = currentValue - originalValue;
            _valueChangeTb.Text = _currencyFormatter.Format(valueChange) + " (" + FormatDouble(100 * valueChange / originalValue) + "%)";
            if (valueChange < 0)
                _valueChangeTb.Foreground = new SolidColorBrush(Colors.Red);
            else
                _valueChangeTb.Foreground = new SolidColorBrush(Colors.Green);

            String currency = _currencyFormatter.Format(_runContext.Account.CurrencyBalance) + " (";
            double balanceDifference = _runContext.Account.CurrencyBalance - strategy.Account.CurrencyBalance;
            if (balanceDifference < 0)
                currency += _currencyFormatter.Format(balanceDifference) + ")";
            else
                currency += "+ " + _currencyFormatter.Format(balanceDifference) + ")";
            _currencyTb.Text = currency;

            String assets = FormatDouble(_runContext.Account.AssetBalance) + " (";
            double assetDifference = _runContext.Account.AssetBalance - strategy.Account.AssetBalance;
            if (assetDifference < 0)
                assets += "-" + FormatDouble(assetDifference).Replace("-", "") + ")";
            else
                assets += "+" + FormatDouble(assetDifference) + ")";
            _assetsTb.Text = assets;

            // User Vars
            _userVarList.Clear();
            foreach (KeyValuePair<String, IValue> pair in _runContext.UserVars) {
                UserVarUiEntry entry = new UserVarUiEntry() {
                    VarName = pair.Key,
                    Type = pair.Value.GetValueType().ToString(),
                    Value = pair.Value.GetStringValue(_runContext)
                };
                _userVarList.Add(entry);
            }
            _userVarsView.ItemsSource = _userVarList;

            // Transactions
            var transactionList = new ObservableCollection<TransactionUiEntry>();
            foreach (Transaction transaction in _runContext.Transactions) {
                TransactionUiEntry uiEntry = new TransactionUiEntry() {
                    Currency = _currencyFormatter.Format(transaction.CurrencyTransferred),
                    Assets = FormatDouble(transaction.AssetTransferred),
                    Fee = _currencyFormatter.Format(transaction.TransactionFee),
                    Price = _currencyFormatter.Format(transaction.CurrentPrice),
                    DatapointIndex = transaction.DatapointIndex,
                    TimestampS = (long)(transaction.TransactionTime - DateTime.UnixEpoch).TotalSeconds
                };
                transactionList.Add(uiEntry);
            }
            _transactionsView.ItemsSource = transactionList;
        }

        public StrategyResultsView() {
            InitializeComponent();
        }

        /// <summary>
        /// Formats a given double to display in a table cell.
        /// </summary>
        /// <param name="input">The input to format.</param>
        /// <returns>Returns the formatted double as a string.</returns>
        private String FormatDouble(double input) {
            return input.ToString("N2");
        }
    }
}
