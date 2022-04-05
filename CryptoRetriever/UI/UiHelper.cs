using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        /// <summary>
        /// Shades the color by the given percent. 
        /// A negative value will make it darker, positive lighter.
        /// </summary>
        /// <param name="color">The color to shade.</param>
        /// <param name="percent">The percent to shade it by. 0.20 = 20% brighter, -0.20 = 20% darker.</param>
        /// <returns></returns>
        public static Color ShadeColor(Color color, double percent) {
            double r = color.R;
            double b = color.B;
            double g = color.G;

            if (percent > 0) {
                // Brighten
                r = Math.Min((1 + percent) * r, 255);
                b = Math.Min((1 + percent) * b, 255);
                g = Math.Min((1 + percent) * g, 255);
            } else {
                // Darken
                r = Math.Max((1 + percent) * r, 0);
                b = Math.Max((1 + percent) * b, 0);
                g = Math.Max((1 + percent) * g, 0);
            }

            return Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Gives default hover/click shades to the given FrameworkElement assuming it has
        /// a Background property.
        /// </summary>
        /// <param name="baseColor">The base color of the button.</param>
        /// <param name="element">The element to set the background color on.</param>
        public static void AddButtonHoverAndClickGraphics(Color baseColor, dynamic element) {
            // Make sure the given element has a Background property
            var whatever = element.Background;
            Color hoverColor = UiHelper.ShadeColor(baseColor, .20);
            Color pressedColor = UiHelper.ShadeColor(baseColor, -.20);
            var setter = new ButtonHoverAndClickGraphicSetter(baseColor, hoverColor, pressedColor, element);
            setter.Initialize();
        }

        /// <summary>
        /// Gives default hover/click shades to the given FrameworkElement assuming it has
        /// a Background property.
        /// </summary>
        /// <param name="baseColor">The base color of the button.</param>
        /// <param name="hoverColor">The hover color of the button.</param>
        /// <param name="pressedColor">The pressed color of the button.</param>
        /// <param name="element">The element to set the background color on.</param>
        public static void AddButtonHoverAndClickGraphics(Color baseColor, Color hoverColor, Color pressedColor, dynamic element) {
            // Make sure the given element has a Background property
            var whatever = element.Background;
            var setter = new ButtonHoverAndClickGraphicSetter(baseColor, hoverColor, pressedColor, element);
            setter.Initialize();
        }
    }

    internal class ButtonHoverAndClickGraphicSetter {
        private Color _baseColor;
        private Color _hoverColor;
        private Color _pressedColor;
        private dynamic _element;

        public ButtonHoverAndClickGraphicSetter(Color baseColor, Color hoverColor, Color pressedColor, dynamic element) {
            _element = element;
            _baseColor = baseColor;
            _hoverColor = hoverColor;
            _pressedColor = pressedColor;
        }

        public void Initialize() {
            FrameworkElement element = _element as FrameworkElement;
            element.MouseDown += OnMouseDown;
            element.MouseEnter += OnMouseEnter;
            element.MouseLeave += OnMouseLeave;
            element.MouseUp += OnMouseUp;
        }

        private void OnMouseEnter(object sender, RoutedEventArgs args) {
            _element.Background = new SolidColorBrush(_hoverColor);
        }

        private void OnMouseDown(object sender, RoutedEventArgs args) {
            _element.Background = new SolidColorBrush(_pressedColor);
        }

        private void OnMouseUp(object sender, RoutedEventArgs args) {
            _element.Background = new SolidColorBrush(_baseColor);
        }

        private void OnMouseLeave(object sender, RoutedEventArgs args) {
            _element.Background = new SolidColorBrush(_baseColor);
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
