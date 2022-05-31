using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CryptoRetriever.Strats;
using Trigger = CryptoRetriever.Strats.Trigger;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Allows the user to create or modify a trigger.
    /// If the window closes from the user pressing Okay,
    /// the result is stored in Trigger. Otherwise Trigger
    /// will be null if, which can happen if the user cancels
    /// or closes the window without pressing Okay.
    /// 
    /// If you want to edit a Trigger, set WorkingTrigger before
    /// showing the window.
    /// </summary>
    public partial class TriggerEditorWindow : Window {
        /// <summary>
        /// Contains the trigger being worked on.
        /// When Okay is pressed, this becomes the result Trigger.
        /// This can be set before showing the window to edit
        /// an existing trigger.
        /// </summary>
        public Trigger WorkingTrigger { get; set; }

        /// <summary>
        /// Contains the resulting Trigger or null if
        /// the window was closed without pressing Okay. 
        /// </summary>
        public Trigger Trigger { get; private set; } = null;

        private Strategy _strategy;

        public TriggerEditorWindow(Strategy strategy) {
            InitializeComponent();
            _strategy = strategy;
        }

        private void OnConditionClicked(object sender, MouseButtonEventArgs e) {
            ConditionEditorWindow editor = new ConditionEditorWindow(_strategy, WorkingTrigger);
            UiHelper.CenterWindowInWindow(editor, this);
            editor.ShowDialog();
            UpdateUi();
        }

        private void OnOkayClicked(object sender, RoutedEventArgs e) {
            Trigger = WorkingTrigger;
            Trigger.Name = _triggerNameTextBox.Text;
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            if (WorkingTrigger == null) {
                // Create a dummy trigger for now
                WorkingTrigger = new Trigger("DefaultTrigger");
            }

            if (WorkingTrigger.Condition == null)
                WorkingTrigger.Condition = Conditions.GetConditions()["True"].Clone();
            if (WorkingTrigger.TrueAction == null)
                WorkingTrigger.TrueAction = Actions.GetActions()[ActionId.DoNothing];
            if (WorkingTrigger.FalseAction == null)
                WorkingTrigger.FalseAction = Actions.GetActions()[ActionId.DoNothing];

            _triggerNameTextBox.Text = WorkingTrigger.Name;

            UiHelper.AddButtonHoverAndClickGraphics(Color.FromRgb(0x72, 0x9f, 0xcf), _conditionBorder);
            UiHelper.AddButtonHoverAndClickGraphics(Color.FromRgb(0x72, 0x9f, 0xcf), _thenActionBorder);
            UiHelper.AddButtonHoverAndClickGraphics(Color.FromRgb(0x72, 0x9f, 0xcf), _elseActionBorder);

            UpdateUi();
        }

        private void OnThenActionClicked(object sender, MouseButtonEventArgs e) {
            ActionEditorWindow editor = new ActionEditorWindow(_strategy);
            editor.Action = WorkingTrigger.TrueAction;
            UiHelper.CenterWindowInWindow(editor, this);
            editor.ShowDialog();
            WorkingTrigger.TrueAction = editor.Action;
            UpdateUi();
        }

        private void OnElseActionClicked(object sender, MouseButtonEventArgs e) {
            ActionEditorWindow editor = new ActionEditorWindow(_strategy);
            editor.Action = WorkingTrigger.FalseAction;
            UiHelper.CenterWindowInWindow(editor, this);
            editor.ShowDialog();
            WorkingTrigger.FalseAction = editor.Action;
            UpdateUi();
        }

        private void UpdateUi() {
            _conditionTb.Text = WorkingTrigger.Condition.GetLabel();
            _thenActionTb.Text = WorkingTrigger.TrueAction.GetLabel();
            _elseActionTb.Text = WorkingTrigger.FalseAction.GetLabel();
        }
    }
}
