using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace CryptoRetriever.Data {
    /// <summary>
    /// Handles input and controls the display of the GraphRenderer in response.
    /// </summary>
    public class GraphController {
        private GraphRenderer _graphRenderer;
        private bool _mouseInView = false;

        // Keep track of the mouse up times so we know when 
        // the graph was double clicked
        private DateTime _lastMouseUpTime = DateTime.MinValue;

        // Keep track of if we're panning
        private bool _isPanning = false;
        private Point _lastDragPoint; // The mouse start position when the pan started.

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

            // Check if we're panning
            if (_isPanning) {
                Matrix pxToDataSpaceMatrix = _graphRenderer.GetPixelToDataSpaceTransform();

                // Pan based on the drag amount
                Point prevPosDataSpace = pxToDataSpaceMatrix.Transform(_lastDragPoint);
                Point curPosDataSpace = pxToDataSpaceMatrix.Transform(positionPx);

                // Get the difference
                Point diffDataSpace = new Point(
                    curPosDataSpace.X - prevPosDataSpace.X,
                    curPosDataSpace.Y - prevPosDataSpace.Y
                );

                Range currentDomain = _graphRenderer.GetDomain();
                Range boundingDomain = _graphRenderer.GetBoundingDomain();
                Range newDomain = new Range(
                    currentDomain.Start - diffDataSpace.X,
                    currentDomain.End - diffDataSpace.X
                );

                // Clamp
                double domainStartDiff = currentDomain.Start - boundingDomain.Start;
                double domainEndDiff = boundingDomain.End - currentDomain.End;
                if (domainStartDiff < 0) {
                    newDomain.Start += -domainStartDiff;
                    newDomain.End += -domainStartDiff;
                } else if (domainEndDiff < 0) {
                    newDomain.Start += domainEndDiff;
                    newDomain.End += domainEndDiff;
                }

                Range currentRange = _graphRenderer.GetRange();
                Range boundingRange = _graphRenderer.GetBoundingRange();
                Range newRange = new Range(
                    currentRange.Start - diffDataSpace.Y,
                    currentRange.End - diffDataSpace.Y
                );

                // Clamp
                double rangeStartDiff = currentRange.Start - boundingRange.Start;
                double rangeEndDiff = boundingRange.End - currentRange.End;
                if (rangeStartDiff < 0) {
                    newRange.Start += -rangeStartDiff;
                    newRange.End += -rangeStartDiff;
                } else if (rangeEndDiff < 0) {
                    newRange.Start += rangeEndDiff;
                    newRange.End += rangeEndDiff;
                }

                // Update
                _graphRenderer.SetDomain(newDomain.Start, newDomain.End);
                _graphRenderer.SetRange(newRange.Start, newRange.End);
                _graphRenderer.UpdateAll();

                _lastDragPoint = positionPx;
            }
        }

        /// <summary>
        /// Called when the mouse wheel on the canvas being controlled moves.
        /// </summary>
        /// <param name="delta">Positive for "forward" and negative for backwards. The magnitude is the amount that it moved.</param>
        /// <param name="mousePositionPx">The mouse position in pixels from the top left of the canvas.</param>
        public void OnMouseWheel(int delta, Point mousePositionPx) {
            // Zoom in or out depending on the direction
            DateTime before = DateTime.Now;
            if (delta > 0)
                Zoom(1.2, mousePositionPx);
            else
                Zoom(0.8, mousePositionPx);
            Console.WriteLine("Zoom time: " + (DateTime.Now - before).TotalMilliseconds);
        }

        public void OnMouseUp(Point positionPx) {
            // We're no longer panning
            _isPanning = false;

            // Check if this was a double click
            DateTime now = DateTime.Now;
            long doubleClickTimeMs = User32Helper.GetDoubleClickTime();
            double diffMs = (now - _lastMouseUpTime).TotalMilliseconds;
            if (diffMs - (double)doubleClickTimeMs <= 0) {
                if (_graphRenderer.IsZoomedOut()) {
                    Zoom(4, positionPx);
                } else {
                    ZoomOut();
                }
            }
            _lastMouseUpTime = now;
        }

        public void OnMouseDown(Point positionPx) {
            _isPanning = true;
            _lastDragPoint = positionPx;
        }

        private void Zoom(double zoomFactor, Point centerPx) {
            // Zooming in is the equivilent of scaling down the domain/range
            double domainRangeFactor = 1.0 / zoomFactor; 

            // Get dataset limits
            Dataset dataset = _graphRenderer.GetOriginalDataset();
            if (dataset.Points == null || dataset.Points.Count < 2)
                return; // No point in zooming if we have 0 or only 1 point

            // Get the bounding zoom
            Range boundingDomain = _graphRenderer.GetBoundingDomain();
            Range boundingRange = _graphRenderer.GetBoundingRange();

            // Get the center in data space
            Matrix pixelToDataMatrix = _graphRenderer.GetPixelToDataSpaceTransform();
            Point centerDataSpace = pixelToDataMatrix.Transform(centerPx);

            // Start with the domain
            Range domain = _graphRenderer.GetDomain();
            double domainLength = domain.End - domain.Start;

            // Apply the scale factor
            double scaledDomainLength = domainLength * domainRangeFactor;
            double domainLengthDiff = scaledDomainLength - domainLength;

            // Now shift so we zoom around the center point
            double startCenterDist = centerDataSpace.X - domain.Start;
            double endCenterDist = domain.End - centerDataSpace.X;
            double startOffset = domainLengthDiff * (startCenterDist / domainLength);
            double endOffset = domainLengthDiff * (endCenterDist / domainLength);

            Range newDomain = new Range(
                Math.Max(domain.Start - startOffset, boundingDomain.Start),
                Math.Min(domain.End + endOffset, boundingDomain.End)
            );

            // Now do the same for the range
            Range range = _graphRenderer.GetRange();
            double rangeLength = range.End - range.Start;

            // Apply the scale factor
            double scaledRangeLength = rangeLength * domainRangeFactor;
            double rangeLengthDiff = scaledRangeLength - rangeLength;

            // Now shift so we zoom around the center point
            double rangeStartCenterDist = centerDataSpace.Y - range.Start;
            double rangeEndCenterDist = range.End - centerDataSpace.Y;
            double rangeStartOffset = rangeLengthDiff * (rangeStartCenterDist / rangeLength);
            double rangeEndOffset = rangeLengthDiff * (rangeEndCenterDist / rangeLength);

            Range newRange = new Range(
                Math.Max(range.Start - rangeStartOffset, boundingRange.Start),
                Math.Min(range.End + rangeEndOffset, boundingRange.End)
            );

            _graphRenderer.SetDomain(newDomain.Start, newDomain.End);
            _graphRenderer.SetRange(newRange.Start, newRange.End);
            _graphRenderer.UpdateAll();
        }

        private void ZoomOut() {
            Range boundingDomain = _graphRenderer.GetBoundingDomain();
            Range boundingRange = _graphRenderer.GetBoundingRange();
            _graphRenderer.SetDomain(boundingDomain.Start, boundingDomain.End);
            _graphRenderer.SetRange(boundingRange.Start, boundingRange.End);
            _graphRenderer.UpdateAll();
        }
    }
}
