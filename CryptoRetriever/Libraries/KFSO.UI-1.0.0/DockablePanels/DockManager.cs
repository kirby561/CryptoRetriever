using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace KFSO.UI.DockablePanels {
    /// <summary>
    /// Manages the list of dockable sections in a window (or across windows).
    /// The manager is given to each DockablePanel so it can handle docking into
    /// locations in any window that uses the same manager.
    /// 
    /// DockManagers can be synced between DockStations manually by using:
    ///     _dockStation1.DockManager = _dockStation2.DockManager;
    /// Alternatively if you want any panel to be able to dock with any DockStation
    /// in a Window or in a subset of the logical tree, you can use the following:
    ///     DockManager dockManager = new DockManager();
    ///     dockManager.UseDockManagerForTree(this);
    /// The above will set dockManager as the one to use for all DockStations/Panels in
    /// the tree starting at "this". You can give the Window or just any element root to
    /// start at.
    /// </summary>
    public class DockManager {
        // How close you need to get to a dock station (squared) to dock to it
        private const double DockDistance = 100;

        // The stations that can be docked to in this manager
        private LinkedList<DockStation> _dockStations = new LinkedList<DockStation>();

        /// <summary>
        /// Adds a docking station to be tracked by this manager. This
        /// will enable it to be seen by DockablePanels that are also
        /// associated with this manager.
        /// </summary>
        /// <param name="station">The station to add.</param>
        public void AddDockStation(DockStation station) {
            station.DockManager = this;
        }

        /// <summary>
        /// Removes the given station from this manager.
        /// </summary>
        /// <param name="station">The station to remove.</param>
        public void RemoveDockStation(DockStation station) {
            station.DockManager = null;
        }

        /// <summary>
        /// Called by DockStation to link itself to this manager.
        /// </summary>
        /// <param name="station">The station that is linking itself.</param>
        internal void LinkDockStation(DockStation station) {
            _dockStations.AddLast(station);
        }

        /// <summary>
        /// Called by DockStation to unlink itself from this manager.
        /// </summary>
        /// <param name="station">The station that is unlinking itself.</param>
        internal void UnlinkDockStation(DockStation station) {
            _dockStations.Remove(station);
        }

        /// <summary>
        /// Gets the closest docking station within the docking range to the given location.
        /// </summary>
        /// <param name="locationScreen">The location in screen coordinates.</param>
        /// <returns>Returns the closest docking station or null if none are within the required distance.</returns>
        public DockStation GetClosestDockInRangeTo(Point locationScreen) {
            double closestDistance = Double.MaxValue;
            DockStation closestStation = null;

            // Measure the x distance from the vertical of the panel
            // as long as the mouse point is between the top and bottom
            // of the panel.
            foreach (DockStation station in _dockStations) {
                Point topLeft = station.GetTopLeftScreen();
                Point bottomRight = station.GetBottomRightScreen();
                Point centerScreen = station.GetCenterScreen();
                double xDistance = Math.Abs(centerScreen.X - locationScreen.X);

                // Check that the Y value is between the top and bottom
                if (locationScreen.Y < topLeft.Y || locationScreen.Y > bottomRight.Y)
                    continue;
                
                if (xDistance < DockDistance) {
                    if (xDistance < closestDistance) {
                        closestDistance = xDistance;
                        closestStation = station;
                    }
                }
            }

            return closestStation;
        }

        /// <summary>
        /// Assigns this manager to every DockStation and DockablePanel in the given visual tree.
        /// </summary>
        /// <param name="visual">The start of the tree.</param>
        public void UseDockManagerForTree(DependencyObject root) {
            foreach (DependencyObject node in GetChildren(root)) {
                DockStation station = node as DockStation;
                if (station != null) {
                    station.DockManager = this;
                } else {
                    DockablePanel panel = node as DockablePanel;
                    if (panel != null) {
                        panel.DockManager = this;
                    }
                }
            }
        }

        /// <summary>
        /// Recurses through the given parent's tree and returns an enumerator over all objects in the tree.
        /// </summary>
        /// <param name="parent">The root DependencyObject to start from.</param>
        /// <returns>An IEnumerable over the full list of DependencyObjects in the logical tree starting at the given parent.</returns>
        private IEnumerable<DependencyObject> GetChildren(DependencyObject parent) {
            Stack<DependencyObject> nodesLeft = new Stack<DependencyObject>();
            nodesLeft.Push(parent);
            while (nodesLeft.Count > 0) {
                DependencyObject next = nodesLeft.Pop();
                foreach (object obj in LogicalTreeHelper.GetChildren(next)) {
                    DependencyObject child = obj as DependencyObject;
                    if (child != null) {
                        // Recurse for this child
                        nodesLeft.Push(child);

                        // Add it to the enumerable
                        yield return child;
                    }
                }
            }
        }
    }
}
