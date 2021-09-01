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
        private RenderParams lastDrawParams = null;

        public GraphRenderer(Canvas canvas, Dataset dataset) {
            _canvas = canvas;
            _renderParams.Dataset = dataset;

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
            DrawData();
            DrawAxis();
        }

        private Point ScalePoint(Point p, Size scale) {
            return new Point(
                    p.X * scale.Width,
                    p.Y * scale.Height);
        }

        private void DrawData() {
            _canvas.Children.Clear();
            SolidColorBrush brush = new SolidColorBrush(_renderParams.LineOptions.Color);

            double xScale = _renderParams.CanvasSizePx.Width / (_renderParams.Domain.End - _renderParams.Domain.Start);
            double yScale = _renderParams.CanvasSizePx.Height / (_renderParams.Range.End - _renderParams.Range.Start);
            Size scale = new Size(xScale, yScale);
            for (int i = 0; i < _renderParams.Dataset.Points.Count - 1; i++) {
                Point p1Orig = _renderParams.Dataset.Points[i];
                Point p2Orig = _renderParams.Dataset.Points[i + 1];
                Point p1 = new Point(p1Orig.X, p1Orig.Y);
                Point p2 = new Point(p2Orig.X, p2Orig.Y);

                p1.X -= _renderParams.Domain.Start;
                p2.X -= _renderParams.Domain.Start;
                p1.Y -= _renderParams.Range.Start;
                p2.Y -= _renderParams.Range.Start;

                p1 = ScalePoint(p1, scale);
                p2 = ScalePoint(p2, scale);

                // Flip about the Y axis since positive is down
                p1.Y *= -1;
                p2.Y *= -1;

                // Translate by the height to push it into view
                p1.Y += _renderParams.CanvasSizePx.Height;
                p2.Y += _renderParams.CanvasSizePx.Height;

                Line line = new Line();
                line.Fill = brush;
                line.Stroke = brush;
                line.StrokeThickness = _renderParams.LineOptions.Thickness;
                line.X1 = p1.X;
                line.Y1 = p1.Y;
                line.X2 = p2.X;
                line.Y2 = p2.Y;
                _canvas.Children.Add(line);
            }
        }

        private void DrawAxis() {

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
