using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StockStratMemes {
    /// <summary>
    /// Renders a dataset to the given canvas with some options.
    /// Can be controlled with mouse or touch input using a GraphController.
    /// </summary>
    class GraphRenderer {
        private Canvas _canvas;
        private RenderParams _renderParams = new RenderParams();
        private Geometry _datasetGeometry;
        private Path _datasetPath;

        public GraphRenderer(Canvas canvas, Dataset dataset) {
            _canvas = canvas;
            _renderParams.Dataset = dataset;

            // Initialize a geometry for the dataset
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext stream = geometry.Open()) {
                if (_renderParams.Dataset.Points.Count > 0) {
                    Point p1 = _renderParams.Dataset.Points[0];
                    stream.BeginFigure(p1, false, false);
                } else {
                    return; // Nothing to draw
                }

                for (int i = 1; i < _renderParams.Dataset.Points.Count; i++) {
                    Point p = _renderParams.Dataset.Points[i];
                    stream.LineTo(p, true, true);
                }

                _datasetGeometry = geometry;

                _datasetPath = new Path();
                _datasetPath.Data = geometry;
                Canvas.SetLeft(_datasetPath, 0);
                Canvas.SetTop(_datasetPath, 0);
                Canvas.SetRight(_datasetPath, 0);
                Canvas.SetBottom(_datasetPath, 0);
            }

            // Start the domain/range to fit the data
            InitializeDomainAndRange();

            _canvas.LayoutUpdated += OnCanvasLayoutUpdates;
        }

        private void OnCanvasLayoutUpdates(object sender, EventArgs e) {
            if (_canvas.ActualWidth == _renderParams.CanvasSizePx.Width && _canvas.ActualHeight == _renderParams.CanvasSizePx.Height)
                return;

            _renderParams.CanvasSizePx = new Size(_canvas.ActualWidth, _canvas.ActualHeight);
            Draw();
        }

        private void InitializeDomainAndRange() {
            // Iterate over the dataset and get the max/min in each dimension
            double minX = Double.PositiveInfinity;
            double maxX = Double.NegativeInfinity;
            double minY = Double.PositiveInfinity;
            double maxY = Double.NegativeInfinity;

            foreach (Point p in _renderParams.Dataset.Points) {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            SetDomain(minX, maxX);
            SetRange(minY, maxY);

            Draw();
        }

        public void SetDomain(double start, double end) {
            _renderParams.Domain.Start = start;
            _renderParams.Domain.End = end;
        }

        public void SetRange(double start, double end) {
            _renderParams.Range.Start = start;
            _renderParams.Range.End = end;
        }

        public void Draw() {
            _canvas.Children.Clear();
            DrawData();
            DrawAxis();
        }

        private Point ScalePoint(Point p, Size scale) {
            return new Point(
                    p.X * scale.Width,
                    p.Y * scale.Height);
        }

        private void DrawData() {
            SolidColorBrush brush = new SolidColorBrush(_renderParams.LineOptions.Color);

            double xScale = _renderParams.CanvasSizePx.Width / (_renderParams.Domain.End - _renderParams.Domain.Start);
            double yScale = _renderParams.CanvasSizePx.Height / (_renderParams.Range.End - _renderParams.Range.Start);

            Matrix layoutTransform = new Matrix();

            // Shift the curve to 0 so the first point starts at the top left
            layoutTransform.Translate(-_renderParams.Domain.Start, -_renderParams.Range.Start);

            // Now map left to right on the curve to left to right on the screen. The -yScale is to flip
            //     the coordinates since the canvas has 0, 0 at the top left and positive is down.
            layoutTransform.Scale(xScale, -yScale);

            // Now shift the whole curve down by the height of the canvas since our coordinates at this point
            //     are starting at the top of the screen and should be starting at the bottom of the screen
            layoutTransform.Translate(0, _renderParams.CanvasSizePx.Height);

            _datasetGeometry.Transform = new MatrixTransform(layoutTransform);
            _datasetPath.Stroke = brush;
            _datasetPath.StrokeThickness = _renderParams.LineOptions.Thickness;

            _canvas.Children.Add(_datasetPath);
        }

        private void DrawAxis() {
            double axisWidthPx = 3;
            SolidColorBrush brush = new SolidColorBrush(Colors.Black);
            Line xAxis = new Line();
            xAxis.Fill = brush;
            xAxis.Stroke = brush;
            xAxis.StrokeThickness = axisWidthPx;
            xAxis.X1 = 0;
            xAxis.Y1 = _renderParams.CanvasSizePx.Height;
            xAxis.X2 = _renderParams.CanvasSizePx.Width;
            xAxis.Y2 = xAxis.Y1;
            _canvas.Children.Add(xAxis);

            Line yAxis = new Line();
            yAxis.Fill = brush;
            yAxis.Stroke = brush;
            yAxis.StrokeThickness = axisWidthPx;
            yAxis.X1 = 0;
            yAxis.Y1 = 0;
            yAxis.X2 = 0;
            yAxis.Y2 = _renderParams.CanvasSizePx.Height;
            _canvas.Children.Add(yAxis);
        }
    }

    class LineOptions {
        public Color Color { get; set; }
        public double Thickness { get; set; }
 
        public LineOptions(Color color, double thickness) {
            Color = color;
            Thickness = thickness;
        }

        public LineOptions Clone() {
            return new LineOptions(Color, Thickness);
        }

        public bool IsEqual(LineOptions other) {
            return other.Color.Equals(Color) && other.Thickness == Thickness;
        }
    }

    class Range {
        public double Start { get; set; } = 0.0;
        public double End { get; set; } = 0.0;

        public Range() {
            // Nothing to do
        }

        public Range(double start, double end) {
            Start = start;
            End = end;
        }

        public Range Clone() {
            return new Range(Start, End);
        }

        public bool IsEqual(Range other) {
            return other.Start == Start && other.End == End;
        }
    }

    class RenderParams {
        public Dataset Dataset { get; set; }
        public Range Domain { get; set; } = new Range();
        public Range Range { get; set; } = new Range();
        public Size CanvasSizePx { get; set; } = new Size();
        public LineOptions LineOptions { get; set; } = new LineOptions(Colors.Blue, 1.0);

        public RenderParams Clone() {
            RenderParams clone = new RenderParams();
            clone.Dataset = Dataset; // Treat the Dataset as immutable.
            clone.Domain = Domain.Clone();
            clone.Range = Range.Clone();
            clone.CanvasSizePx = new Size(CanvasSizePx.Width, CanvasSizePx.Height);
            clone.LineOptions = new LineOptions(LineOptions.Color, LineOptions.Thickness);
            return clone;
        }

        public bool IsEqual(RenderParams other) {
            if (other == null)
                return false;

            return other.Dataset == Dataset &&
                other.Domain.IsEqual(Domain) &&
                other.Range.IsEqual(Range) &&
                other.CanvasSizePx.Equals(CanvasSizePx) &&
                other.LineOptions.IsEqual(LineOptions);
        }
    }
}
