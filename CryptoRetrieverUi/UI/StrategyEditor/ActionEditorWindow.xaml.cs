using CryptoRetriever.Strats;
using System.Windows;
using System.Windows.Controls;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Allows the user to configure Actions for triggers
    /// to decide what to do when conditions occur or
    /// don't occur.
    /// </summary>
    public partial class ActionEditorWindow : TreeEditorWindow {
        private StratAction _action;

        public StratAction Action {
            get {
                return _action;
            }
            set {
                _action = value;
                UpdateUi();
            }
        }

        public ActionEditorWindow(Strategy strategy) : base(strategy) {
            InitializeComponent();
        }

        public override ITreeNode GetRoot() {
            if (_action == null)
                _action = Actions.GetActions()[ActionId.DoNothing];
            return _action;
        }

        public override void SetRoot(ITreeNode root) {
            _action = (StratAction)root;
        }

        protected override Panel GetConditionsPanel() {
            return _actionsPanel;
        }

        private void OnOkayClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
