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

namespace CryptoRetriever.UI {
    /// <summary>
    /// Displays a UI representing the condtions in the given
    /// Strategy and allows user's to edit them.
    /// </summary>
    public partial class ConditionEditorWindow : Window {
        private Trigger _trigger;
        private StrategyRuntimeContext _dummyContext;

        public Trigger Trigger {
            get {
                return _trigger;
            }
            set {
                _trigger = value;
                UpdateUi();
            }
        }

        public ConditionEditorWindow() {
            InitializeComponent();

            // Make a dummy StrategyRunParams to preview
            // conditions
            _dummyContext = new ExampleStrategyRunParams();
        }

        /// <summary>
        /// Whenever the condition changes, this should be called
        /// to update the UI with the new tree.
        /// </summary>
        public void UpdateUi() {
            _conditionsPanel.Children.Clear();
            if (_trigger.Condition != null) {
                List<TreeUiEntry> nodes = new List<TreeUiEntry>();
                TreeUiEntry.PreOrderTraverseNodes(_trigger.Condition, null, 0, nodes, 0);

                int index = 0;
                int currentIndent = 0;
                Stack<Panel> panelStack = new Stack<Panel>();
                panelStack.Push(_conditionsPanel);
                foreach (TreeUiEntry entry in nodes) {
                    if (entry.Indentation < currentIndent) {
                        panelStack.Pop();
                        currentIndent = entry.Indentation;
                    }

                    int indentPreMargin = 20;
                    int indentPostMargin = 5;
                    Border border = new Border();
                    border.CornerRadius = new CornerRadius(5);

                    TextBlock block = new TextBlock();
                    block.Text = entry.Node.GetId() + " (" + entry.Node.GetStringValue(_dummyContext) + ")";
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
                            block.Text = entry.Node.GetStringValue(_dummyContext);
                        } else {
                            backgroundColor = Colors.DarkOrange;
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
            _conditionsPanel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            if (ActualWidth < _conditionsPanel.DesiredSize.Width + 50) {
                Width = _conditionsPanel.DesiredSize.Width + 50;
            }
        }

        /// <summary>
        /// Returns a dummy context for previewing the condition before
        /// it's actually used in context.
        /// </summary>
        /// <returns>A dummy strategy run context.</returns>
        public StrategyRuntimeContext GetDummyContext() {
            return _dummyContext;
        }

        private void OnOkayClicked(object sender, RoutedEventArgs e) {
            // ?? TODO: Should probably make the strategy not change unless Okay is pressed
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void AddEvents(TextBlock block, TreeUiEntry entry) {
            TreeUiEntryClickHandlerBase handler = null;
            if (entry.Node is Operator) {
                handler = new OperatorClickHandler(this, entry);
            } else if (entry.Node is Condition) {
                handler = new ConditionClickHandler(this, entry);
            } else if (entry.Node is StringValue) {
                handler = new StringValueClickHandler(this, entry);
            } else if (entry.Node is NumberValue) {
                handler = new NumberValueClickHandler(this, entry);
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

        class ConditionWindowClickHandler : TreeUiEntryClickHandlerBase {
            protected ConditionEditorWindow _window;

            public ConditionWindowClickHandler(ConditionEditorWindow window, TreeUiEntry entry)
                : base(entry) {
                _window = window;
            }
        }

        class OperatorClickHandler : ConditionWindowClickHandler {
            public OperatorClickHandler(ConditionEditorWindow window, TreeUiEntry entry)
                : base(window, entry) { }

            public override void OnMouseUp(object sender, RoutedEventArgs e) {
                if (!_isMouseDown)
                    return;
                base.OnMouseDown(sender, e);

                Operator op = _entry.Node as Operator;
                Operator[] options = op.GetOptions();
                ListBoxDialog dialog = new ListBoxDialog();
                dialog.SetItemSource(
                    (Operator op) => { return op.GetStringValue(_window.GetDummyContext()); },
                    options);
                UiHelper.CenterWindowInWindow(dialog, _window);
                dialog.ShowDialog();

                if (dialog.SelectedIndex >= 0) {
                    _entry.Parent.SetChild(_entry.ChildIndexInParent, options[dialog.SelectedIndex]);
                    _window.UpdateUi();
                }
            }
        }

        class ConditionClickHandler : ConditionWindowClickHandler {
            public ConditionClickHandler(ConditionEditorWindow window, TreeUiEntry entry)
                : base(window, entry) { }

            public override void OnMouseUp(object sender, RoutedEventArgs e) {
                if (!_isMouseDown)
                    return;
                base.OnMouseUp(sender, e);

                Condition cond = _entry.Node as Condition;
                Condition[] options = cond.GetOptions();
                ListBoxDialog dialog = new ListBoxDialog();
                dialog.SetItemSource(
                    (Condition c) => {
                        return (c.GetChildren() != null) ? c.GetId() : c.GetStringValue(_window.GetDummyContext());
                    },
                    options);
                UiHelper.CenterWindowInWindow(dialog, _window);
                dialog.ShowDialog();

                if (dialog.SelectedIndex >= 0) {
                    if (_entry.Parent != null) {
                        _entry.Parent.SetChild(_entry.ChildIndexInParent, options[dialog.SelectedIndex]);
                        _window.UpdateUi();
                    } else {
                        _window.Trigger.Condition = options[dialog.SelectedIndex];
                        _window.UpdateUi();
                    }
                }
            }
        }

        class StringValueClickHandler : ConditionWindowClickHandler {
            public StringValueClickHandler(ConditionEditorWindow window, TreeUiEntry entry)
                : base(window, entry) { }

            public override void OnMouseUp(object sender, RoutedEventArgs e) {
                if (!_isMouseDown)
                    return;
                base.OnMouseUp(sender, e);

                StringValue clickedString = _entry.Node as StringValue;
                List<StringVariable> variableOptions = Variables.GetStringVariables();
                ConstantVariableWindow dialog = new ConstantVariableWindow();
                dialog.SetItemSource(
                    (StringVariable c) => {
                        return c.Id;
                    },
                    variableOptions);
                UiHelper.CenterWindowInWindow(dialog, _window);
                dialog.ShowDialog();

                if (dialog.ResultType != ConstantVariableWindow.SelectionType.None) {
                    if (dialog.SelectedIndex >= 0) {
                        StringVariable var = variableOptions[dialog.SelectedIndex];
                        _entry.Parent.SetChild(
                            _entry.ChildIndexInParent,
                            new VariableStringValue(var));
                        _window.UpdateUi();
                    } else if (!String.IsNullOrWhiteSpace(dialog.Constant)) {
                        _entry.Parent.SetChild(
                            _entry.ChildIndexInParent,
                            new StringValue(dialog.Constant));
                        _window.UpdateUi();
                    }
                }
            }
        }

        class NumberValueClickHandler : ConditionWindowClickHandler {
            public NumberValueClickHandler(ConditionEditorWindow window, TreeUiEntry entry)
                : base(window, entry) { }

            public override void OnMouseUp(object sender, RoutedEventArgs e) {
                if (!_isMouseDown)
                    return;
                base.OnMouseUp(sender, e);

                NumberValue clickedNumber = _entry.Node as NumberValue;
                List<NumberVariable> variableOptions = Variables.GetNumberVariables();

                // For numbers, add in MathValue too
                List<object> allOptions = new List<object>();
                allOptions.Add(new MathValue(new NumberValue(0), new AdditionOperator(), new NumberValue(1)));
                int numSpecialOptions = allOptions.Count;
                allOptions.AddRange(variableOptions);

                ConstantVariableWindow dialog = new ConstantVariableWindow();
                dialog.SetItemSource(
                    (object c) => {
                        // The item source is either a NumberVariable or a NumberValue
                        NumberVariable numberVar = c as NumberVariable;
                        if (numberVar != null) {
                            return numberVar.Id;
                        } else {
                            NumberValue number = c as NumberValue;
                            return number.GetId(); 
                        }
                    },
                    allOptions);
                UiHelper.CenterWindowInWindow(dialog, _window);
                dialog.ShowDialog();

                if (dialog.ResultType != ConstantVariableWindow.SelectionType.None) {
                    if (dialog.SelectedIndex >= 0) {
                        NumberValue result;

                        // If it's one of the special options it's not actually a variable
                        // it's a special NumberValue
                        if (dialog.SelectedIndex < numSpecialOptions) {
                            result = allOptions[dialog.SelectedIndex] as NumberValue;
                        } else {
                            // Else it's actually a variable
                            NumberVariable var = variableOptions[dialog.SelectedIndex - numSpecialOptions];
                            result = new VariableNumberValue(var);
                        }

                        _entry.Parent.SetChild(
                            _entry.ChildIndexInParent,
                            result);
                        _window.UpdateUi();
                    } else if (!String.IsNullOrWhiteSpace(dialog.Constant)) {
                        double constantValue;
                        if (Double.TryParse(dialog.Constant, out constantValue)) {
                            _entry.Parent.SetChild(
                                _entry.ChildIndexInParent,
                                new NumberValue(constantValue));
                            _window.UpdateUi();
                        } else {
                            MessageBox.Show("You need to enter a valid number.");
                        }
                    }
                }
            }
        }
    }
}
