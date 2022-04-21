using CryptoRetriever.Strats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Allows the user to configure Actions for triggers
    /// to decide what to do when conditions occur or
    /// don't occur.
    /// </summary>
    public partial class ActionEditorWindow : Window {
        private StratAction _action;
        private StrategyRuntimeContext _context = new ExampleStrategyRunParams();

        public StratAction Action {
            get {
                return _action;
            }
            set {
                _action = value;
                UpdateUi();
            }
        }

        public ActionEditorWindow() {
            InitializeComponent();
        }

        /// <returns>
        /// Returns a Dummy context so sample values can be
        /// displayed to the user while they are creating
        /// actions.
        /// </returns>
        public StrategyRuntimeContext GetDummyRuntimeContext() {
            return _context;
        }

        /// <summary>
        /// Refreshes the UI with the current action tree.
        /// </summary>
        public void UpdateUi() {
            _actionsPanel.Children.Clear();

            if (_action == null)
                _action = Actions.GetActions()[ActionId.DoNothing];

            List<TreeUiEntry> nodes = new List<TreeUiEntry>();
            TreeUiEntry.PreOrderTraverseNodes(_action, null, 0, nodes, 0);

            int index = 0;
            int currentIndent = 0;
            Stack<Panel> panelStack = new Stack<Panel>();
            panelStack.Push(_actionsPanel);
            foreach (TreeUiEntry entry in nodes) {
                while (entry.Indentation < currentIndent) {
                    panelStack.Pop();
                    currentIndent--;
                }

                int indentPreMargin = 20;
                int indentPostMargin = 5;
                Border border = new Border();
                border.CornerRadius = new CornerRadius(5);

                TextBlock block = new TextBlock();
                block.Text = entry.Node.GetLabel();
                block.Padding = new Thickness(5);
                block.Foreground = new SolidColorBrush(Colors.White);
                block.FontSize = 14;

                if (entry.Node.GetChildren() == null) {
                    Color backgroundColor;
                    border.Margin = new Thickness(5 + indentPreMargin, 5, 5 + indentPostMargin, 5);
                    border.Child = block;
                    block.TextAlignment = TextAlignment.Center;

                    if (entry.Node.GetId().Equals(ActionId.NotSet)) {
                        backgroundColor = Colors.DarkOrange;
                        border.Background = new SolidColorBrush(backgroundColor);
                    } else {
                        backgroundColor = Colors.DarkGreen;
                        border.Background = new SolidColorBrush(backgroundColor);
                    }
                    panelStack.Peek().Children.Add(border);
                    UiHelper.AddButtonHoverAndClickGraphics(backgroundColor, block);
                    AddEvents(block, entry);
                } else {
                    StackPanel parentPanel = new StackPanel();
                    int intensity = 255 - (20 * entry.Indentation);
                    byte intensityByte = (byte)(intensity - 127);
                    Color color = Color.FromRgb(intensityByte, intensityByte, intensityByte);
                    border.BorderBrush = new SolidColorBrush(color);
                    border.BorderThickness = new Thickness(2, 0, 0, 0);
                    border.Margin = new Thickness(5 + indentPreMargin, 5, 5 + indentPostMargin, 5);
                    parentPanel.Orientation = Orientation.Vertical;
                    block.FontWeight = FontWeights.Bold;
                    block.Margin = new Thickness(2);
                    parentPanel.Children.Add(block);
                    border.Child = parentPanel;
                    panelStack.Peek().Children.Add(border);
                    panelStack.Push(parentPanel);
                    currentIndent = entry.Indentation + 1;

                    UiHelper.AddButtonHoverAndClickGraphics(
                        Colors.Transparent,
                        UiHelper.ShadeColor(Colors.White, -.40),
                        UiHelper.ShadeColor(Colors.White, -.60),
                        block);
                    AddEvents(block, entry);
                }

                index++;
            }

            // Measure the actions panel and increase the window width
            // if it's less than its desired with plus some padding
            _actionsPanel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            if (ActualWidth < _actionsPanel.DesiredSize.Width + 50) {
                Width = _actionsPanel.DesiredSize.Width + 50;
            }
        }

        private void AddEvents(TextBlock block, TreeUiEntry entry) {
            TreeUiEntryClickHandlerBase handler = new ActionClickHandler(this, entry);

            block.MouseDown += handler.OnMouseDown;
            block.MouseLeave += handler.OnMouseLeave;
            block.MouseUp += handler.OnMouseUp;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            UpdateUi();
        }

        private void OnOkayClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            Close();
        }
    }

    /// <summary>
    /// Handles click actions on tree nodes in the Action panel.
    /// This will display the correct dialog depending on what was
    /// clicked and handles actions that require arguments from the
    /// user as well.
    /// </summary>
    class ActionClickHandler : TreeUiEntryClickHandlerBase {
        private ActionEditorWindow _window;

        public ActionClickHandler(ActionEditorWindow window, TreeUiEntry entry)
            : base(entry) {
            _window = window;
        }

        public override void OnMouseUp(object sender, RoutedEventArgs e) {
            if (!_isMouseDown)
                return;
            base.OnMouseDown(sender, e);

            StratAction action = _entry.Node as StratAction;
            Dictionary<String, StratAction> optionsDictionary = Actions.GetActions();
            List<StratAction> options = optionsDictionary.Values.ToList();
            ListBoxDialog dialog = new ListBoxDialog();
            dialog.SetItemSource(
                (StratAction act) => { return act.GetLabel(); },
                options);
            UiHelper.CenterWindowInWindow(dialog, _window);
            dialog.ShowDialog();

            if (dialog.SelectedIndex < 0)
                return; // Nothing selected so don't do anything.

            int index = dialog.SelectedIndex;
            StratAction selectedItem = options[index].Clone();
            if (selectedItem is NumberAction) {
                // String actions need a variable from the user, grab that first
                ConstantVariableWindow constantDialog = new ConstantVariableWindow();
                constantDialog.ShowVariableList = false;
                constantDialog.ShowDialog();

                if (constantDialog.ResultType == ConstantVariableWindow.SelectionType.Constant) {
                    String resultString = constantDialog.Constant;
                    double result;
                    if (Double.TryParse(resultString, out result)) {
                        NumberValue val = new NumberValue(result);
                        NumberAction numAction = selectedItem as NumberAction;
                        numAction.Value = val;
                    }
                }
            } else if (selectedItem is StringAction) {
                // String actions need a variable from the user, grab that first
                ConstantVariableWindow constantDialog = new ConstantVariableWindow();
                constantDialog.ShowVariableList = false;
                constantDialog.ShowDialog();

                if (constantDialog.ResultType == ConstantVariableWindow.SelectionType.Constant) {
                    String result = constantDialog.Constant;
                    StringValue val = new StringValue(result);
                    StringAction stringAction = selectedItem as StringAction;
                    stringAction.Value = val;
                }
            }

            OnItemSelected(selectedItem);
        }

        protected virtual void OnItemSelected(StratAction item) {
            if (item != null) {
                if (_entry.Parent == null) {
                    _window.Action = item.Clone();
                } else {
                    _entry.Parent.SetChild(_entry.ChildIndexInParent, item.Clone());
                }
                _window.UpdateUi();
            }
        }
    }
}
