using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CryptoRetriever.UI {
    /// <summary>
    /// Contains common helpful methods for UI elements
    /// like centering windows when they're launched from
    /// their parent, finding the parent window, etc..
    /// </summary>
    public class UiHelper {
        /// <summary>
        /// Centers the given child window within the parent window.
        /// Note: This will not happen immediately if the window hasn't been
        /// loaded yet.
        /// </summary>
        /// <param name="childWindow">The child window to be centered.</param>
        /// <param name="parentWindow">The parent window.</param>
        public static void CenterWindowInWindow(Window childWindow, Window parentWindow) {
            CenterWindowTask centerTask = new CenterWindowTask(childWindow, parentWindow);
            centerTask.Center();
        }
    }

    /// <summary>
    /// Used to center a child window after it has been loaded
    /// if it isn't yet.
    /// </summary>
    internal class CenterWindowTask {
        private Window _childWindow;
        private Window _parentWindow;
        private bool _waitingForOnLoadEvent = false;

        public CenterWindowTask(Window childWindow, Window parentWindow) {
            _childWindow = childWindow;
            _parentWindow = parentWindow;
        }

        /// <summary>
        /// Centers the window now if the window is loaded
        /// or waits for it to be loaded first if not.
        /// </summary>
        public void Center() {
            if (!_childWindow.IsLoaded) {
                _waitingForOnLoadEvent = true;
                _childWindow.Loaded += CenterWindowOnLoaded;
            } else {
                CenterWindowOnLoaded(null, null);
            }
        }

        private void CenterWindowOnLoaded(object sender, RoutedEventArgs e) {
            if (_waitingForOnLoadEvent) {
                _waitingForOnLoadEvent = false;
                _childWindow.Loaded -= CenterWindowOnLoaded;
            }

            _childWindow.Left =
                _parentWindow.Left +
                _parentWindow.ActualWidth / 2 -
                _childWindow.ActualWidth / 2;

            _childWindow.Top =
                _parentWindow.Top +
                _parentWindow.ActualHeight / 2 -
                _childWindow.ActualHeight / 2;
        }
    }
}
