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
            double valueChange = currentValue - originalValue;
            _valueChangeTb.Text = _currencyFormatter.Format(valueChange) + " (" + TrimDouble("" + 100 * valueChange / originalValue) + "%)";
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

            String assets = TrimDouble("" + _runContext.Account.AssetBalance) + " (";
            double assetDifference = _runContext.Account.AssetBalance - strategy.Account.AssetBalance;
            if (assetDifference < 0)
                assets += "-" + TrimDouble("" + assetDifference).Replace("-", "") + ")";
            else
                assets += "+" + TrimDouble("" + assetDifference) + ")";
            _assetsTb.Text = assets;

            // Transactions
            var transactionList = new ObservableCollection<TransactionUiEntry>();
            foreach (Transaction transaction in _runContext.Transactions) {
                TransactionUiEntry uiEntry = new TransactionUiEntry() {
                    Currency = _currencyFormatter.Format(transaction.CurrencyTransferred),
                    Assets = TrimDouble("" + transaction.AssetTransferred),
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

        private readonly static HashSet<char> _nonzeroDigits = new HashSet<char>(
            new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' }
        );
        private readonly static HashSet<char> _digits = new HashSet<char>(
            new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }
        );
        private String TrimDouble(String doubleString) {
            // Just go until we get 3 non-zero digits or hit the end
            String trimmedString = "";
            int count = 0;
            bool gotNonZero = false;
            foreach (char c in doubleString) {
                trimmedString += c;

                if (gotNonZero && _digits.Contains(c)) {
                    count++;
                    if (count >= 3)
                        break;
                } else if (!gotNonZero) {
                    gotNonZero = _nonzeroDigits.Contains(c);
                    if (gotNonZero)
                        count++;
                }
            }

            return trimmedString;
        }
    }
}
