using CryptoRetriever.Strats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Condition = CryptoRetriever.Strats.Condition;
using Trigger = CryptoRetriever.Strats.Trigger;
using ValueType = CryptoRetriever.Strats.ValueType;

namespace CryptoRetriever.UI {
    public abstract class TreeEditorWindow : Window {
        private Strategy _strategy;
        private StrategyRuntimeContext _dummyContext;

        public TreeEditorWindow(Strategy strategy) {
            // Keep track of the strategy this condition will be part of
            // so we can access things like user variables.
            _strategy = strategy;

            // Make a dummy StrategyRunParams to preview
            // conditions
            _dummyContext = new StrategyRuntimeContext(_strategy, new ExampleDataset());

            Loaded += OnWindowLoaded;
        }

        public Strategy GetStrategy() {
            return _strategy;
        }

        public abstract ITreeNode GetRoot();
        public abstract void SetRoot(ITreeNode root);

        protected void AddEvents(TextBlock block, TreeUiEntry entry) {
            TreeUiEntryClickHandlerBase handler = null;
            if (entry.Node is Operator) {
                handler = new OperatorClickHandler(this, entry);
            } else if (entry.Node is Condition) {
                handler = new ConditionClickHandler(this, entry);
            } else if (entry.Node is IStringValue) {
                handler = new StringValueClickHandler(this, entry);
            } else if (entry.Node is INumberValue) {
                handler = new NumberValueClickHandler(this, entry);
            } else if (entry.Node is StratAction) {
                handler = new ActionClickHandler(this, entry);
            }

            if (handler != null) {
                block.MouseDown += handler.OnMouseDown;
                block.MouseLeave += handler.OnMouseLeave;
                block.MouseUp += handler.OnMouseUp;
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            UpdateUi();
        }

        /// <summary>
        /// Returns a dummy context for previewing the condition before
        /// it's actually used in context.
        /// </summary>
        /// <returns>A dummy strategy run context.</returns>
        public StrategyRuntimeContext GetDummyContext() {
            return _dummyContext;
        }

        protected abstract Panel GetConditionsPanel();

        /// <summary>
        /// Whenever the condition changes, this should be called
        /// to update the UI with the new tree.
        /// </summary>
        public void UpdateUi() {
            Panel conditionsPanel = GetConditionsPanel();
            ITreeNode root = GetRoot();
            conditionsPanel.Children.Clear();
            if (root != null) {
                List<TreeUiEntry> nodes = new List<TreeUiEntry>();
                TreeUiEntry.PreOrderTraverseNodes(root, null, 0, nodes, 0);

                int index = 0;
                int currentIndent = 0;
                Stack<Panel> panelStack = new Stack<Panel>();
                panelStack.Push(GetConditionsPanel());
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

                        if (entry.Node is Operator) {
                            backgroundColor = Colors.DarkGreen;
                            border.Background = new SolidColorBrush(backgroundColor);
                        } else if (entry.Node.GetId().Equals(ActionId.NotSet)) {
                            backgroundColor = Colors.DarkOrange;
                            border.Background = new SolidColorBrush(backgroundColor);
                        } else {
                            backgroundColor = Color.FromRgb(0x72, 0x9f, 0xcf);
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
            }

            // Measure the conditions panel and increase the window width
            // if it's less than its desired with
            conditionsPanel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            if (ActualWidth < conditionsPanel.DesiredSize.Width + 50) {
                Width = conditionsPanel.DesiredSize.Width + 50;
            }
        }
    }


    class TreeEditorWindowClickHandler : TreeUiEntryClickHandlerBase {
        protected TreeEditorWindow _window;

        public TreeEditorWindowClickHandler(TreeEditorWindow window, TreeUiEntry entry)
            : base(entry) {
            _window = window;
        }
    }

    class OperatorClickHandler : TreeEditorWindowClickHandler {
        public OperatorClickHandler(TreeEditorWindow window, TreeUiEntry entry)
            : base(window, entry) { }

        public override void OnMouseUp(object sender, RoutedEventArgs e) {
            if (!_isMouseDown)
                return;
            base.OnMouseDown(sender, e);

            Operator op = _entry.Node as Operator;
            Operator[] options = op.GetOptions().Values.ToArray();
            ListBoxDialog dialog = new ListBoxDialog();
            dialog.SetItemSource(
                (Operator op) => { return op.GetLabel(); },
                options);
            UiHelper.CenterWindowInWindow(dialog, _window);
            dialog.ShowDialog();

            if (dialog.SelectedIndex >= 0) {
                _entry.Parent.SetChild(_entry.ChildIndexInParent, options[dialog.SelectedIndex].Clone());
                _window.UpdateUi();
            }
        }
    }

    class ConditionClickHandler : TreeEditorWindowClickHandler {
        public ConditionClickHandler(TreeEditorWindow window, TreeUiEntry entry)
            : base(window, entry) { }

        public override void OnMouseUp(object sender, RoutedEventArgs e) {
            if (!_isMouseDown)
                return;
            base.OnMouseUp(sender, e);

            Condition cond = _entry.Node as Condition;
            Condition[] options = Conditions.GetConditions().Values.ToArray();
            ListBoxDialog dialog = new ListBoxDialog();
            dialog.SetItemSource(
                (Condition c) => {
                    return c.GetLabel();
                },
                options);
            UiHelper.CenterWindowInWindow(dialog, _window);
            dialog.ShowDialog();

            if (dialog.SelectedIndex >= 0) {
                Condition selectedCondition = options[dialog.SelectedIndex].Clone();
                if (_entry.Parent != null) {
                    _entry.Parent.SetChild(_entry.ChildIndexInParent, selectedCondition);
                    _window.UpdateUi();
                } else {
                    _window.SetRoot(selectedCondition);
                    _window.UpdateUi();
                }
            }
        }
    }

    class StringValueClickHandler : TreeEditorWindowClickHandler {
        public StringValueClickHandler(TreeEditorWindow window, TreeUiEntry entry)
            : base(window, entry) { }

        public override void OnMouseUp(object sender, RoutedEventArgs e) {
            if (!_isMouseDown)
                return;
            base.OnMouseUp(sender, e);

            IStringValue clickedString = _entry.Node as IStringValue;
            List<IValue> variableOptions = _window.GetStrategy().GetValuesOfType(ValueType.String).Values.ToList();
            ConstantVariableWindow dialog = new ConstantVariableWindow();
            dialog.SetItemSource(
                (IValue c) => {
                    return c.GetLabel();
                },
                variableOptions);
            UiHelper.CenterWindowInWindow(dialog, _window);
            dialog.ShowDialog();

            if (dialog.ResultType != ConstantVariableWindow.SelectionType.None) {
                if (dialog.SelectedIndex >= 0) {
                    IValue var = variableOptions[dialog.SelectedIndex].Clone();
                    _entry.Parent.SetChild(
                        _entry.ChildIndexInParent,
                        var);
                    _window.UpdateUi();
                } else if (!String.IsNullOrWhiteSpace(dialog.Constant)) {
                    _entry.Parent.SetChild(
                        _entry.ChildIndexInParent,
                        new SimpleStringValue(dialog.Constant));
                    _window.UpdateUi();
                }
            }
        }
    }

    class NumberValueClickHandler : TreeEditorWindowClickHandler {
        public NumberValueClickHandler(TreeEditorWindow window, TreeUiEntry entry)
            : base(window, entry) { }

        public override void OnMouseUp(object sender, RoutedEventArgs e) {
            if (!_isMouseDown)
                return;
            base.OnMouseUp(sender, e);

            INumberValue clickedNumber = _entry.Node as INumberValue;
            List<IValue> options = _window.GetStrategy().GetValuesOfType(ValueType.Number).Values.ToList();

            ConstantVariableWindow dialog = new ConstantVariableWindow();
            dialog.SetItemSource(
                (IValue c) => {
                    return c.GetLabel();
                },
                options);
            UiHelper.CenterWindowInWindow(dialog, _window);
            dialog.ShowDialog();

            if (dialog.ResultType != ConstantVariableWindow.SelectionType.None) {
                if (dialog.SelectedIndex >= 0) {
                    IValue result = options[dialog.SelectedIndex];

                    _entry.Parent.SetChild(
                        _entry.ChildIndexInParent,
                        result);
                    _window.UpdateUi();
                } else if (!String.IsNullOrWhiteSpace(dialog.Constant)) {
                    double constantValue;
                    if (Double.TryParse(dialog.Constant, out constantValue)) {
                        _entry.Parent.SetChild(
                            _entry.ChildIndexInParent,
                            new SimpleNumberValue(constantValue));
                        _window.UpdateUi();
                    } else {
                        MessageBox.Show("You need to enter a valid number.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handles click actions on tree nodes in the Action panel.
    /// This will display the correct dialog depending on what was
    /// clicked and handles actions that require arguments from the
    /// user as well.
    /// </summary>
    class ActionClickHandler : TreeUiEntryClickHandlerBase {
        private TreeEditorWindow _window;

        public ActionClickHandler(TreeEditorWindow window, TreeUiEntry entry)
            : base(entry) {
            _window = window;
        }

        public override void OnMouseUp(object sender, RoutedEventArgs e) {
            if (!_isMouseDown)
                return;
            base.OnMouseDown(sender, e);

            StratAction action = _entry.Node as StratAction;
            Dictionary<String, StratAction> optionsDictionary = _window.GetStrategy().GetActions();
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

            OnItemSelected(selectedItem);
        }

        protected virtual void OnItemSelected(StratAction item) {
            if (item != null) {
                if (_entry.Parent == null) {
                    _window.SetRoot(item.Clone());
                } else {
                    _entry.Parent.SetChild(_entry.ChildIndexInParent, item.Clone());
                }
                _window.UpdateUi();
            }
        }
    }
}
