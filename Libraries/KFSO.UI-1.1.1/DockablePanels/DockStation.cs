using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KFSO.UI.DockablePanels {
    /// <summary>
    /// A DockStation represents a place where panels can be docked.
    /// It is intended that its children will be DockablePanels which
    /// can be added programatically or in the xaml file.
    /// 
    /// Note that panels can be dragged between DockStations that share the same DockManager.
    /// The DockManager can be automatically set to be the same for all DockStations in a 
    /// Window by adding the following after InitializeComponent:
    ///    DockManager dockManager = new DockManager();
    ///    dockManager.UseDockManagerForTree(this);
    /// 
    /// Example use:
    ///     <local:DockStation x:Name="_dockPanelSpot" Grid.Row="1" Grid.Column="0" MinWidth="0" MinHeight="1" Background="Red">
    ///            <local:DockablePanel TitleText="Controls" >
    ///                <local:DockablePanel.HostedContent>
    ///                 <ScrollViewer VerticalScrollBarVisibility="Auto" Background="#cccccc" VerticalAlignment="Stretch" HorizontalAlignment="Stretch">
    ///                     <StackPanel Orientation = "Vertical" HorizontalAlignment="Stretch">
    ///                         <Button Content="A button" />
    ///                         < Button Content="Another Button" />
    ///                         <CheckBox Content="A checkbox" />
    ///                     </StackPanel >
    ///                 </ScrollViewer >
    ///             </local:DockablePanel.HostedContent>
    ///         </local:DockablePanel>
    ///     </local:DockStation>
    /// </summary>
    public class DockStation : Grid {
        // Members
        private DockManager _dockManager;
        private bool _previewActive = false;
        private Rectangle _previewRect = null;

        /// <summary>
        /// Gets or sets the DockManager that should be used with
        /// this station. Stations that share a DockManager will 
        /// be able to have panels dragged between them. If they
        /// have different managers then panels taken from one will 
        /// not be able to be docked in the other.
        /// </summary>
        public DockManager DockManager {
            get {
                return _dockManager;
            }
            set {
                DockManager oldManager = _dockManager;
                _dockManager = value;

                if (oldManager != null)
                    oldManager.UnlinkDockStation(this);
                _dockManager.LinkDockStation(this);
            }
        }

        /// <summary>
        /// The delegate to call when the children in this panel have changed.
        /// </summary>
        /// <param name="dockStation">The dockstation (this) for convenience.</param>
        public delegate void ChildrenChangedEventHandler(DockStation dockStation);

        /// <summary>
        /// Called when the children of this station are changed.
        /// Use GetDockedPanels() to see what is docked and how many there are.
        /// </summary>
        public event ChildrenChangedEventHandler ChildrenChanged;

        public DockStation() : base() {
            DockManager = new DockManager();
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved) {
            // Call base function
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            UpdatePanels();
            if (ChildrenChanged != null) {
                ChildrenChanged(this);
            }
        }

        /// <summary>
        /// Returns a list of panels currently docked with this station.
        /// </summary>
        /// <returns>The list of panels. If there are no docked panels an empty list is returned.</returns>
        public List<DockablePanel> GetDockedPanels() {
            var panels = new List<DockablePanel>();

            foreach (UIElement uiElement in Children) {
                DockablePanel panel = uiElement as DockablePanel;
                if (panel != null)
                    panels.Add(panel);
            }

            return panels;
        }

        /// <summary>
        /// Docks the given panel with this station.
        /// It will be added to the end.
        /// </summary>
        /// <param name="panel">The panel to dock.</param>
        public void Dock(DockablePanel panel) {
            Children.Add(panel); // This will cause OnVisualChildrenChanged to be called which will adjust the layout
        }

        /// <summary>
        /// Undocks the given panel with this sation.
        /// </summary>
        /// <param name="panel">The panel to undock.</param>
        public void Undock(DockablePanel panel) {
            Children.Remove(panel); // This will cause OnVisualChildrenChanged to be called which will adjust the layout
        }

        private void UpdatePanels() {
            RowDefinitions.Clear();

            foreach (UIElement uiElement in Children) {
                DockablePanel panel = uiElement as DockablePanel;

                // The panel can be null because UpdatePanels is called mid-update
                // and it seems like WPF sets children to null first before removing them
                // entirely so we just treat that as being removed already
                if (panel != null) {
                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Height = new GridLength(1, GridUnitType.Star);
                    RowDefinitions.Add(rowDefinition);
                }
            }

            int row = 0;
            foreach (UIElement uiElement in Children) {
                DockablePanel panel = uiElement as DockablePanel;

                // The panel can be null because UpdatePanels is called mid-update
                // and it seems like WPF sets children to null first before removing them
                // entirely so we just treat that as being removed already
                if (panel != null) {
                    Grid.SetRow(panel, row);
                    row++;
                }
            }

            // Is there a preview right now?
            if (_previewActive) {
                RowDefinition previewDefinition = new RowDefinition();
                previewDefinition.Height = new GridLength(1, GridUnitType.Star);
                RowDefinitions.Add(previewDefinition);
                Grid.SetRow(_previewRect, row);
            }
        }

        /// <returns>Gets the center of this dock station in screen coordinates.</returns>
        public Point GetCenterScreen() {
            double width = ActualWidth;
            double height = ActualHeight;
            return PointToScreen(new Point(width / 2, height / 2));
        }

        /// <returns>Returns the top left point in screen coordinates.</returns>
        public Point GetTopLeftScreen() {
            return PointToScreen(new Point(0, 0));
        }

        /// <returns>Returns the bottom left point in screen coordinates.</returns>
        public Point GetBottomRightScreen() {
            double width = ActualWidth;
            double height = ActualHeight;
            return PointToScreen(new Point(width, height));
        }

        /// <summary>
        /// Previews where the panel will go when docked and gives the user a visual indication of
        /// what will happen when they let go and if they're close enough.
        /// </summary>
        /// <param name="panel">The panel to preview.</param>
        public void PreviewDock(DockablePanel panel) {
            if (_previewRect == null) {
                _previewActive = true;
                _previewRect = new Rectangle();
                _previewRect.Width = Math.Max(panel.ActualWidth, ActualWidth);
                _previewRect.Fill = new SolidColorBrush(Colors.LightBlue);
                Children.Add(_previewRect);
            }
        }

        /// <summary>
        /// Cancels/hides the preview from PreviewDock.
        /// </summary>
        /// <param name="panel">The panel that was being previewed.</param>
        public void CancelDockPreview(DockablePanel panel) {
            _previewActive = false;
            Children.Remove(_previewRect);
            _previewRect = null;
        }
    }
}
