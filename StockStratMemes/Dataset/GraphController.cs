using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StockStratMemes {
    /// <summary>
    /// Handles input and controls the display of the GraphRenderer in response.
    /// </summary>
    public class GraphController {
        private GraphRenderer _graphRenderer;
        private bool _mouseInView = false;

        public GraphController(GraphRenderer renderer) {
            _graphRenderer = renderer;
        }

        public void OnMouseEntered() {
            _mouseInView = true;
        }

        public void OnMouseLeft() {
            _mouseInView = false;
            _graphRenderer.DisableMouseHoverPoint();
        }

        public void OnMouseMoved(Point positionPx) {
            if (_mouseInView) {
                _graphRenderer.SetMouseHoverPoint(positionPx);
            }
        }
    }
}
