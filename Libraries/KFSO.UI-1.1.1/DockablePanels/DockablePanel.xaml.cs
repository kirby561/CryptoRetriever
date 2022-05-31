﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace KFSO.UI.DockablePanels {
    /// <summary>
    /// A DockablePanel is a control that can be used to wrap another control
    /// to make it dockable and undockable from DockStations. To use it, create one,
    /// set its HostedContent property to any UIElement, and place it in a DockStation.
    /// DockablePanels can be dragged out of their hosting DockStation to create a new
    /// window that contains the panel. They can be dragged back into the DockStation
    /// they came from or into a new one. Note that DockStations must have the same
    /// DockManager in order to drag a panel from one station to another.
    /// </summary>
    public partial class DockablePanel : UserControl {
        private UIElement _content;
        private DockablePanelWindow _floatingWindow = null;
        private Window _parentWindow; // The original parent window when the floating window was created.

        // Drag members
        private Point _mouseDownPointScreen;
        private Point _panelStartPositionScreen;
        private bool _isDragging = false;
        private bool _mouseDown = false;
        private DockStation _previewStation = null; // Set to non-null if we're previewing in a DockStation

        public DockablePanel() {
            InitializeComponent();

            Loaded += DockablePanel_Loaded;
            Unloaded += DockablePanel_Unloaded;
        }

        private void DockablePanel_Loaded(object sender, RoutedEventArgs e) {
            IsShown = true;
        }

        private void DockablePanel_Unloaded(object sender, RoutedEventArgs e) {
            IsShown = false;
        }

        /// <summary>
        /// Gets or sets the DockManager that this panel will use
        /// to search for DockStations it can dock with. This will
        /// default to the DockManager of the first DockStation this
        /// panel is docked to if it is not set before then.
        /// </summary>
        public DockManager DockManager { get; set; }

        /// <summary>
        /// Returns true if this panel is either being shown in a docking station
        /// or in a floating window. Returns false if it is not currently shown.
        /// 
        /// This is a dependency property.
        /// </summary>
        public bool IsShown {
            get {
                return (bool)GetValue(IsShownProperty);
            }
            private set {
                SetValue(IsShownProperty, value);
            }
        }

        /// <summary>
        /// Returns true if this panel is docked, false otherwise.
        /// </summary>
        public bool IsDocked {
            get {
                return IsShown && _floatingWindow == null;
            }
        }

        /// <summary>
        /// Returns true if this panel is in a floating window, false otherwise.
        /// </summary>
        public bool IsFloating {
            get {
                return IsShown && _floatingWindow != null;
            }
        }

        /// <summary>
        /// Gets or sets the content to display in this DockablePanel.
        /// It can be any UI element. The parent will behave like a grid
        /// that has the full space of the panel.
        /// </summary>
        public UIElement HostedContent {
            get {
                return _content;
            }
            set {
                _content = value;
                _contentArea.Children.Clear();

                if (_content != null)
                    _contentArea.Children.Add(_content);
            }
        }

        /// <summary>
        /// Gets or sets the title text of this panel.
        /// </summary>
        public String TitleText {
            get {
                return _titleTextBox.Text;
            }
            set {
                _titleTextBox.Text = value;
            }
        }

        private void _titleBarBackground_MouseDown(object sender, MouseButtonEventArgs e) {
            // Get the position relative to the screen because the panel is going to move
            _mouseDownPointScreen = PointToScreen(e.GetPosition(this));
            _mouseDown = true;
            _titleBarBackground.Fill = new SolidColorBrush(Colors.LightBlue);
            e.Handled = true;
        }

        private void _titleBarBackground_MouseUp(object sender, MouseButtonEventArgs e) {
            _mouseDown = false;
            _isDragging = false;
            _titleBarBackground.Fill = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));

            // Uncapture the mouse since we're done
            Mouse.Capture(null);

            // If we're previewing into a station, remove that
            // and dock with it now.
            if (_previewStation != null) {
                DockStation station = _previewStation;
                _previewStation.CancelDockPreview(this);
                _previewStation = null;

                Dock(station);
            }
        }

        private void _titleBarBackground_MouseMove(object sender, MouseEventArgs e) {
            if (!_mouseDown) return;

            e.Handled = true;

            // Check if we're dragging or not
            if (!_isDragging) {
                // If the panel is docked, check if we've dragged far 
                // enough to pull it out into a window.
                if (_floatingWindow == null) {
                    // Get the current position in screen coordinates
                    Point position = PointToScreen(e.GetPosition(this));

                    // If we're not dragging yet, check if we should start.
                    // We wait a few pixels just to make sure it's not glitchy
                    double distanceSquared = (position - _mouseDownPointScreen).LengthSquared;
                    if (distanceSquared > 100) {
                        _isDragging = true;

                        // Panel screen position
                        _panelStartPositionScreen = PointToScreen(new Point(0, 0));
                        Point panelStartPositionWpf = ScreenCoordinateToWpfCoordinate(_panelStartPositionScreen);

                        ShowFloatingWindow(panelStartPositionWpf, GetParentWindow());

                        // Capture the mouse so that you can't lose the window while dragging and we will get
                        // the mouse up event when done.
                        Mouse.Capture(sender as IInputElement);
                    }
                } else {
                    // Capture the mouse so that you can't lose the window while dragging and we will get
                    // the mouse up event when done.
                    Mouse.Capture(sender as IInputElement);

                    // Just start dragging
                    _isDragging = true;

                    // Record the data needed by the dragging step
                    _panelStartPositionScreen = PointToScreen(new Point(0, 0));
                }
            } else {
                // We're dragging and we should have a window
                Point position = PointToScreen(e.GetPosition(this));
                Vector delta = position - _mouseDownPointScreen;
                Point newPanelPositionScreen = new Point(_panelStartPositionScreen.X + delta.X, _panelStartPositionScreen.Y + delta.Y);
                Point newPanelPositionWpf = ScreenCoordinateToWpfCoordinate(newPanelPositionScreen);

                // Move the window
                _floatingWindow.Left = newPanelPositionWpf.X;
                _floatingWindow.Top = newPanelPositionWpf.Y;

                // Check with the manager if there's any panels we can dock with
                if (DockManager != null) {
                    DockStation nearestStation = DockManager.GetClosestDockInRangeTo(position);
                    if (nearestStation != null && _previewStation == null) {
                        _previewStation = nearestStation;
                        nearestStation.PreviewDock(this);
                    } else if (nearestStation == null && _previewStation != null) {
                        _previewStation.CancelDockPreview(this);
                        _previewStation = null;
                    }
                }
            }
        }

        /// <summary>
        /// Shows this panel in a floating window.
        /// If it's docked into a station it is undocked first.
        /// <param name="startPositionWpf">The starting position in WPF coordinates.</param>
        /// <param name="parentWindow">The parent window of this floating window. The floating window will be closed if the parent window is.</param>
        /// </summary>
        public void ShowFloatingWindow(Point startPositionWpf, Window parentWindow) {
            if (IsFloating)
                return;

            // Undock from the parent
            Undock();

            // Break out the panel into its own window and continue dragging
            _floatingWindow = new DockablePanelWindow();
            _floatingWindow.HostedPanel = this;
            _floatingWindow.Left = startPositionWpf.X;
            _floatingWindow.Top = startPositionWpf.Y;
            _floatingWindow.Title = TitleText;
            _floatingWindow.Closed += _floatingWindow_Closed;
            _floatingWindow.Show();

            _parentWindow = parentWindow;
            _parentWindow.Closed += OnParentWindowClosed;
        }

        private void _floatingWindow_Closed(object sender, EventArgs e) {
            // Make sure the window is null when closed.
            _floatingWindow = null;

            // Check for null because if the parent window
            // is closed it will clear the parent window
            // there so we don't double remove the event.
            if (_parentWindow != null) {
                _parentWindow.Closed -= OnParentWindowClosed;
                _parentWindow = null;
            }

            // Make sure we have no parent
                Panel parent = Parent as Panel;
            if (parent != null) {
                parent.Children.Remove(this);
            }
        }

        private void OnParentWindowClosed(object sender, EventArgs e) {
            // When a parent window closes, all child 
            // panel windows should also close.
            _floatingWindow.Close();

            // Check for null because if the floating window
            // is manually closed it will clear the parent window
            // there so we don't double remove the event.
            if (_parentWindow != null) {
                // Remove the event
                _parentWindow.Closed -= OnParentWindowClosed;
                _parentWindow = null;
            }
        }

        /// <summary>
        /// Docks this panel into the given station.
        /// </summary>
        /// <param name="station">The station to dock with.</param>
        public void Dock(DockStation station) {
            // First check if we're in a floating window
            if (_floatingWindow != null) {
                // Remove parent window closed event
                GetParentWindow().Closed -= OnParentWindowClosed;

                // Remove us from the floating window and destroy
                // it first
                _floatingWindow.HostedPanel = null;
                _floatingWindow.Close();
                _floatingWindow = null;
            }

            station.Dock(this);

            // If a dock manager wasn't explicitly set yet,
            // initialize to this station's manager.
            DockManager = station.DockManager;
        }

        /// <summary>
        /// Removes this panel from its parent.
        /// </summary>
        private void Undock() {
            if (!IsDocked)
                return;

            // Remove this panel from its current parent
            DockStation station = Parent as DockStation;
            station.Undock(this);

            if (DockManager == null) {
                // Use the same dock manager as our parent if we're being detached
                // and don't have one yet.
                DockManager = station.DockManager;
            }
        }

        /// <summary>
        /// WPF has a different coordinate system than the Desktop's Screen coordinate system
        /// so this gets the given screen location in the WPF's coordinate system.
        /// </summary>
        /// <returns>The top/left coordinate of this panel in Window coordinates.</returns>
        private Point ScreenCoordinateToWpfCoordinate(Point screenCoordinate) {
            // Transform screen point to WPF device independent point
            PresentationSource source = PresentationSource.FromVisual(this);

            Point targetPoint = source.CompositionTarget.TransformFromDevice.Transform(screenCoordinate);
            return targetPoint;
        }

        /// <summary>
        /// Iterates of the logical tree to find the window this
        /// panel is a part of.
        /// </summary>
        /// <returns>Returns the window or null if this panel is not in a window.</returns>
        private Window GetParentWindow() {
            // Walk the tree to find this panel's parent window
            DependencyObject parent = Parent;
            while (parent != null) {
                Window window = parent as Window;
                if (window != null)
                    return window;

                FrameworkElement uiElement = parent as FrameworkElement;
                if (uiElement != null)
                    parent = uiElement.Parent;
            }
            return null;
        }

        private void _closeButton_MouseUp(object sender, MouseButtonEventArgs e) {
            if (_floatingWindow != null) {
                _floatingWindow.Close();
            } else {
                // Remove this panel from its current parent
                Panel parentPanel = Parent as Panel;
                parentPanel.Children.Remove(this);
            }
        }

        public static readonly DependencyProperty IsShownProperty =
            DependencyProperty.Register("IsShown", typeof(bool), typeof(DockablePanel), new PropertyMetadata(true));
    }
}
