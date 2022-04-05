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
        private StrategyRunParams _dummyContext;

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
                List<UiEntry> nodes = new List<UiEntry>();
                PreOrderTraverseNodes(_trigger.Condition, null, 0, nodes, 0);

                int index = 0;
                int currentIndent = 0;
                Stack<Panel> panelStack = new Stack<Panel>();
                panelStack.Push(_conditionsPanel);
                foreach (UiEntry entry in nodes) {
                    if (entry.Indentation < currentIndent) {
                        panelStack.Pop();
                        currentIndent = entry.Indentation;
                    }

                    int indentPreMargin = 20;
                    int indentPostMargin = 5;
                    Border border = new Border();
                    border.CornerRadius = new CornerRadius(5);

                    TextBlock block = new TextBlock();
                    block.Text = entry.Node.GetId() + " (" + entry.Node.GetStringValue() + ")";
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
                            block.Text = entry.Node.GetStringValue();
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
            if (ActualWidth < _conditionsPanel.DesiredSize.Width + 5) {
                Width = _conditionsPanel.DesiredSize.Width;
            }
        }

        /// <summary>
        /// Returns a dummy context for previewing the condition before
        /// it's actually used in context.
        /// </summary>
        /// <returns>A dummy strategy run context.</returns>
        public StrategyRunParams GetDummyContext() {
            return _dummyContext;
        }

        /// <summary>
        /// Walks the tree in Pre-Order and builds a list of UiEntry nodes representing
        /// the order the UI should layout the blocks in. The indentation, parent,
        /// and child index are also recorded for later so the UI can properly indent
        /// each child.
        /// </summary>
        /// <param name="current">The current node being looked at.</param>
        /// <param name="parent">The parent of the current node or null if it's the first.</param>
        /// <param name="childIndex">The index the child is in the parent's list of children.</param>
        /// <param name="nodes">The list of nodes in the order they should appear.</param>
        /// <param name="indentation">The current indentation level.</param>
        private void PreOrderTraverseNodes(ITreeNode current, ITreeNode parent, int childIndex, List<UiEntry> nodes, int indentation) {
            nodes.Add(new UiEntry(current, parent, childIndex, indentation));
            if (current.GetChildren() != null) {
                int index = 0;
                foreach (ITreeNode node in current.GetChildren()) {
                    PreOrderTraverseNodes(node, current, index, nodes, indentation + 1);
                    index++;
                }
            }
        }

        private void OnOkayClicked(object sender, RoutedEventArgs e) {
            // ?? TODO: Should probably make the strategy not change unless Okay is pressed
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void AddEvents(TextBlock block, UiEntry entry) {
            ClickHandlerBase handler = null;
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

        class UiEntry {
            public ITreeNode Node { get; set; }
            public ITreeNode Parent { get; set; }
            public int ChildIndexInParent { get; set; }
            public int Indentation { get; set; }

            public UiEntry(ITreeNode node, ITreeNode parent, int childIndexInParent, int indentation) {
                Node = node;
                Parent = parent;
                ChildIndexInParent = childIndexInParent;
                Indentation = indentation;
            }
        }

        class ClickHandlerBase {
            protected ConditionEditorWindow _context;
            protected UiEntry _entry;

            // Keek track of if the mouse was pressed on this
            // element so that we don't fire the even if you clicked on
            // something in a window over this button and then the up
            // event mis-fires.
            protected bool _isMouseDown = false;

            protected Color _initialColor;
            protected Color _hoverColor;
            protected Color _pressedColor;

            public ClickHandlerBase(ConditionEditorWindow context, UiEntry entry) {
                _context = context;
                _entry = entry;
            }

            public virtual void OnMouseDown(object sender, RoutedEventArgs e) {
                _isMouseDown = true;
            }

            public virtual void OnMouseLeave(object sender, RoutedEventArgs e) {
                _isMouseDown = false;
            }

            public virtual void OnMouseUp(object sender, RoutedEventArgs e) {
                _isMouseDown = false;
            }
        }

        class OperatorClickHandler : ClickHandlerBase {
            public OperatorClickHandler(ConditionEditorWindow context, UiEntry entry)
                : base(context, entry) { }

            public override void OnMouseUp(object sender, RoutedEventArgs e) {
                if (!_isMouseDown)
                    return;
                base.OnMouseDown(sender, e);

                Operator op = _entry.Node as Operator;
                Operator[] options = op.GetOptions();
                ListBoxDialog dialog = new ListBoxDialog();
                dialog.SetItemSource(
                    (Operator op) => { return op.GetStringValue(); },
                    options);
                UiHelper.CenterWindowInWindow(dialog, _context);
                dialog.ShowDialog();

                if (dialog.SelectedIndex >= 0) {
                    _entry.Parent.SetChild(_entry.ChildIndexInParent, options[dialog.SelectedIndex]);
                    _context.UpdateUi();
                }
            }
        }

        class ConditionClickHandler : ClickHandlerBase {
            public ConditionClickHandler(ConditionEditorWindow context, UiEntry entry)
                : base(context, entry) { }

            public override void OnMouseUp(object sender, RoutedEventArgs e) {
                if (!_isMouseDown)
                    return;
                base.OnMouseUp(sender, e);

                Condition cond = _entry.Node as Condition;
                Condition[] options = cond.GetOptions();
                ListBoxDialog dialog = new ListBoxDialog();
                dialog.SetItemSource(
                    (Condition c) => {
                        return (c.GetChildren() != null) ? c.GetId() : c.GetStringValue();
                    },
                    options);
                UiHelper.CenterWindowInWindow(dialog, _context);
                dialog.ShowDialog();

                if (dialog.SelectedIndex >= 0) {
                    if (_entry.Parent != null) {
                        _entry.Parent.SetChild(_entry.ChildIndexInParent, options[dialog.SelectedIndex]);
                        _context.UpdateUi();
                    } else {
                        _context.Trigger.Condition = options[dialog.SelectedIndex];
                        _context.UpdateUi();
                    }
                }
            }
        }

        class StringValueClickHandler : ClickHandlerBase {
            public StringValueClickHandler(ConditionEditorWindow context, UiEntry entry)
                : base(context, entry) { }

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
                UiHelper.CenterWindowInWindow(dialog, _context);
                dialog.ShowDialog();

                if (dialog.ResultType != ConstantVariableWindow.SelectionType.None) {
                    if (dialog.SelectedIndex >= 0) {
                        StringVariable var = variableOptions[dialog.SelectedIndex];
                        _entry.Parent.SetChild(
                            _entry.ChildIndexInParent,
                            new VariableStringValue(_context.GetDummyContext(), var));
                        _context.UpdateUi();
                    } else if (!String.IsNullOrWhiteSpace(dialog.Constant)) {
                        _entry.Parent.SetChild(
                            _entry.ChildIndexInParent,
                            new StringValue(dialog.Constant));
                        _context.UpdateUi();
                    }
                }
            }
        }

        class NumberValueClickHandler : ClickHandlerBase {
            public NumberValueClickHandler(ConditionEditorWindow context, UiEntry entry)
                : base(context, entry) { }

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
                UiHelper.CenterWindowInWindow(dialog, _context);
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
                            result = new VariableNumberValue(_context.GetDummyContext(), var);
                        }

                        _entry.Parent.SetChild(
                            _entry.ChildIndexInParent,
                            result);
                        _context.UpdateUi();
                    } else if (!String.IsNullOrWhiteSpace(dialog.Constant)) {
                        double constantValue;
                        if (Double.TryParse(dialog.Constant, out constantValue)) {
                            _entry.Parent.SetChild(
                                _entry.ChildIndexInParent,
                                new NumberValue(constantValue));
                            _context.UpdateUi();
                        } else {
                            MessageBox.Show("You need to enter a valid number.");
                        }
                    }
                }
            }
        }

        private class ExampleStrategy : Strategy {
            public ExampleStrategy() {
                Name = "ExampleStrategy";
                States.Add(new State("Default"));
            }
        }

        private class ExampleDataset : Dataset {
            public ExampleDataset() {
                Points.Add(new Point(1, 10));
                Points.Add(new Point(2, 20));
                Points.Add(new Point(3, 30));
                Points.Add(new Point(4, 40));
                Points.Add(new Point(5, 50));
            }
        }

        private class ExampleStrategyRunParams : StrategyRunParams {
            public ExampleStrategyRunParams() : base(new ExampleStrategy(), new ExampleDataset()) {
                CurrentState = Strategy.States[0].GetId();
            }
        }
    }
}
