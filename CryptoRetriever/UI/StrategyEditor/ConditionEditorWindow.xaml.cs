using CryptoRetriever.Strats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Trigger = CryptoRetriever.Strats.Trigger;
using Condition = CryptoRetriever.Strats.Condition;
using System.Linq;
using ValueType = CryptoRetriever.Strats.ValueType;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Displays a UI representing the condtions in the given
    /// Strategy and allows user's to edit them.
    /// </summary>
    public partial class ConditionEditorWindow : TreeEditorWindow {
        private Trigger _trigger;

        public ConditionEditorWindow(Strategy strategy, Trigger trigger) : base(strategy) {
            InitializeComponent();
            _trigger = trigger;
        }

        public override ITreeNode GetRoot() {
            return _trigger.Condition;
        }

        public override void SetRoot(ITreeNode root) {
            _trigger.Condition = (Condition)root;
        }

        protected override Panel GetConditionsPanel() {
            return _conditionsPanel;
        }

        private void OnOkayClicked(object sender, RoutedEventArgs e) {
            // ?? TODO: Should probably make the strategy not change unless Okay is pressed
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
