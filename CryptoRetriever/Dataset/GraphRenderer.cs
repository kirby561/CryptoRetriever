using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CryptoRetriever {
    /// <summary>
    /// Renders a dataset to the given canvas with some options.
    /// Can be controlled with mouse or touch input using a GraphController.
    /// </summary>
    public class GraphRenderer {
        private Canvas _canvas;
        private RenderParams _renderParams = new RenderParams();

        // Mouse hover dependencies
        private Point _mouseHoverPointPx;
        private bool _mouseHoverPointEnabled = false;
        private HoverPointOptions _hoverPointOptions = new HoverPointOptions(Colors.Green, 10, 2, 14.0);
        private Ellipse _hoverPointEllipse;
        private TextBlock _hoverPointText;
        private Rectangle _hoverPointTextBackground;

        // Drawing pieces
        private Geometry _datasetGeometry;
        private Path _datasetPath;
        private Line _xAxis;
        private Line _yAxis;
        private TextBlock _xAxisLabel;
        private TextBlock _yAxisLabel;
        private List<Grid> _graphTicks = new List<Grid>();

        // Calculated transforms
        //     Pixel space is from the top left of the canvas in pixels, down is positive.
        //     Data space is the data's coordinate system.
        private Matrix _dataToPixelSpaceTransform;
        private Matrix _pixelToDataSpaceTransform;

        // Converters for displaying coordinates to the user
        private ICoordinateFormatter _xCoordFormatter = null;
        private ICoordinateFormatter _yCoordFormatter = null;

        // Keep track of the bounding box
        private Range _boundingDomain;
        private Range _boundingRange;

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

            SolidColorBrush brush = new SolidColorBrush(_renderParams.LineOptions.Color);
            _datasetPath.Stroke = brush;
            _datasetPath.StrokeThickness = _renderParams.LineOptions.Thickness;
            _datasetPath.IsHitTestVisible = false;

            // Start the domain/range to fit the data
            InitializeDomainAndRange();

            _canvas.LayoutUpdated += OnCanvasLayoutUpdates;
        }

        /// <summary>
        /// Sets formatters that are used when displaying x or y coordinates to the user.
        /// This can be used to convert/display the units they are in, add a dollar symbol, 
        /// control the number of decimal places, etc..
        /// </summary>
        /// <param name="xFormatter">The formatter to use when displaying an x coordinate (or null for none).</param>
        /// <param name="yFormatter">The formatter to use when displaying a y coordinate (or null for none).</param>
        public void SetCoordinateFormatters(ICoordinateFormatter xFormatter, ICoordinateFormatter yFormatter) {
            _xCoordFormatter = xFormatter;
            _yCoordFormatter = yFormatter;
        }

        /// <summary>
        /// Sets and enables (if not already enabled) the mouse hover point.
        /// This is the mouse location. The mouse hover point will be translated
        /// to the Y value of the closest X value in the dataset.
        /// </summary>
        /// <param name="pointPx">The mouse hover point in pixels.</param>
        public void SetMouseHoverPoint(Point pointPx) {
            _mouseHoverPointPx = pointPx;
            _mouseHoverPointEnabled = true;
            UpdateHoverPoint();
        }

        /// <summary>
        /// Disables and hides the mouse hover point.
        /// </summary>
        public void DisableMouseHoverPoint() {
            _mouseHoverPointEnabled = false;
            UpdateHoverPoint();
        }

        private void OnCanvasLayoutUpdates(object sender, EventArgs e) {
            if (_canvas.ActualWidth == _renderParams.CanvasSizePx.Width && _canvas.ActualHeight == _renderParams.CanvasSizePx.Height)
                return;

            _renderParams.CanvasSizePx = new Size(_canvas.ActualWidth, _canvas.ActualHeight);
            UpdateAll();
        }

        private void InitializeDomainAndRange() {
            // Iterate over the dataset and get the max/min in each dimension
            double minX = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;

            foreach (Point p in _renderParams.Dataset.Points) {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            _boundingDomain = new Range(minX, maxX);
            _boundingRange = new Range(minY, maxY);

            SetDomain(minX, maxX);
            SetRange(minY, maxY);

            UpdateAll();
        }

        /// <summary>
        /// Sets the domain of the X axis. The coordinate system is the dataset's coordinates.
        /// </summary>
        /// <param name="start">The start of the axis (lower number).</param>
        /// <param name="end">The end of the axis (higher number).</param>
        public void SetDomain(double start, double end) {
            _renderParams.Domain.Start = start;
            _renderParams.Domain.End = end;
        }

        /// <summary>
        /// Sets the range of the Y axis. The coordinate system is the dataset's coordinates.
        /// </summary>
        /// <param name="start">The start of the axis (Lower number).</param>
        /// <param name="end">The end of the axis (Higher number).</param>
        public void SetRange(double start, double end) {
            _renderParams.Range.Start = start;
            _renderParams.Range.End = end;
        }

        /// <returns>Returns the min/max X of the full dataset.</returns>
        public Range GetBoundingDomain() {
            return _boundingDomain;
        }

        /// <returns>Returns the min/max Y of the full dataset.</returns>
        public Range GetBoundingRange() {
            return _boundingRange;
        }

        public Matrix GetDataToPixelSpaceTransform() {
            return _dataToPixelSpaceTransform;
        }

        public Matrix GetPixelToDataSpaceTransform() {
            return _pixelToDataSpaceTransform;
        }

        public Range GetDomain() {
            return _renderParams.Domain;
        }

        public Range GetRange() {
            return _renderParams.Range;
        }

        public Dataset GetDataset() {
            return _renderParams.Dataset;
        }

        public bool IsZoomedOut() {
            return GetDomain().IsEqual(_boundingDomain) &&
                GetRange().IsEqual(_boundingRange);
        }

        /// <summary>
        /// Updates all the graphics to reflect the current state
        /// </summary>
        public void UpdateAll() {

            UpdateAxis();
            UpdateData();
            UpdateHoverPoint();
        }

        private void UpdateAxis() {
            // Setup axis lines
            if (_xAxis != null)
            {
                _canvas.Children.Remove(_xAxis);
                _canvas.Children.Remove(_xAxisLabel);
                for (int i = 0; i < _graphTicks.Count; i++)
                {
                    _canvas.Children.Remove(_graphTicks[0]);
                }
                _canvas.Children.Clear();
            }

            if (_yAxis != null)
            {
                _canvas.Children.Remove(_yAxis);
                _canvas.Children.Remove(_yAxisLabel);
            }         

            double axisWidthPx = 3;
            SolidColorBrush brush = new SolidColorBrush(Colors.Green);

            // Setup axis
            _xAxis = new Line();

            _xAxis.Fill = brush;
            _xAxis.Stroke = brush;
            _xAxis.StrokeThickness = axisWidthPx;
            _xAxis.X1 = 50;
            _xAxis.Y1 = _renderParams.CanvasSizePx.Height - 100;
            _xAxis.X2 = _renderParams.CanvasSizePx.Width - 50;
            _xAxis.Y2 = _xAxis.Y1;
            _xAxis.IsHitTestVisible = false;

            //Canvas.SetLeft(_xAxis, _xAxisLabel.DesiredSize.Width);
            _canvas.Children.Add(_xAxis);

            _yAxis = new Line();
            _yAxis.Fill = brush;
            _yAxis.Stroke = brush;
            _yAxis.StrokeThickness = axisWidthPx;
            _yAxis.X1 = _xAxis.X1 + 50;
            _yAxis.Y1 = 50;
            _yAxis.X2 = _yAxis.X1;
            _yAxis.Y2 = _xAxis.Y1 + 50;
            _yAxis.IsHitTestVisible = false;

            //Canvas.SetLeft(_yAxis, _xAxisLabel.DesiredSize.Width + 50);
            _canvas.Children.Add(_yAxis);

            // Setup axis labels
            _xAxisLabel = new TextBlock()
            {
                FontSize = 24,
                Foreground = brush,
                Text = "Date/Time (EST)",
                TextWrapping = TextWrapping.Wrap

            };

            _xAxisLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(_xAxisLabel, ((_xAxis.X2 - _yAxis.X1) / 2) - (_xAxisLabel.DesiredSize.Width / 2) + 100);
            Canvas.SetBottom(_xAxisLabel, 10);
            _canvas.Children.Add(_xAxisLabel);

            _yAxisLabel = new TextBlock()
            {
                FontSize = 24,
                Foreground = brush,
                RenderTransform = new RotateTransform(270),
                Text = "Price (USD)",
                TextWrapping = TextWrapping.Wrap

            };

            _yAxisLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(_yAxisLabel, 10);
            Canvas.SetTop(_yAxisLabel, ((_xAxis.Y1 - _yAxis.Y1) / 2) + (_yAxisLabel.DesiredSize.Width / 2) + 50);
            _canvas.Children.Add(_yAxisLabel);

            // Setup axis ticks w/ labels
            //      x-axis
			for (int i = 0; i < _renderParams.Dataset.Points.Count; i++)
			{
				Grid graphTick = CreateGraphTick("Test", 12, brush, new SolidColorBrush(Colors.White), true, true);
                //Grid graphTick = CreateGraphTick(new SolidColorBrush(Colors.White), _xCoordFormatter.Format(_renderParams.Dataset.Points[i].X), 16, true);
                Canvas.SetLeft(graphTick, _yAxis.X1 + ((_xAxis.X2 - _yAxis.X2) / (_renderParams.Dataset.Points.Count) * (i + 1)));
				Canvas.SetTop(graphTick, _xAxis.Y1 - 8);
				_canvas.Children.Add(graphTick);
			}
            //      y-axis (max 10 ticks for now)
            for (int i = 0; i < 10; i++)
			{
                //int scale = 
			}

		}

        private void UpdateData()
        {
            if (_datasetPath != null)
                _canvas.Children.Remove(_datasetPath);

            //double xScale = _renderParams.CanvasSizePx.Width / (_renderParams.Domain.End - _renderParams.Domain.Start);
            double xInterval = (_xAxis.X2 - _yAxis.X1) / _renderParams.Dataset.Points.Count;
            double xScale = (xInterval * (_renderParams.Dataset.Points.Count - 1)) / (_renderParams.Domain.End - _renderParams.Domain.Start);
            //double yScale = _renderParams.CanvasSizePx.Height / (_renderParams.Range.End - _renderParams.Range.Start);
            double yInterval = (_xAxis.Y1 - _yAxis.Y1) / 15;
            double yScale = (yInterval * 13) / (_renderParams.Range.End - _renderParams.Range.Start);

            Matrix layoutTransform = new Matrix();

            // Shift the curve to 0 so the first point starts at the top left
            layoutTransform.Translate(-_renderParams.Domain.Start, -_renderParams.Range.Start);
            //layoutTransform.Translate(-_renderParams.Domain.Start + _xAxisLabel.DesiredSize.Width + 50, -_renderParams.Range.Start + _yAxis.Y1);

            // Now map left to right on the curve to left to right on the screen. The -yScale is to flip
            //     the coordinates since the canvas has 0, 0 at the top left and positive is down.
            layoutTransform.Scale(xScale, -yScale);

            // Now shift the whole curve down by the height of the canvas since our coordinates at this point
            //     are starting at the top of the screen and should be starting at the bottom of the screen
            //layoutTransform.Translate(0, _renderParams.CanvasSizePx.Height);
            layoutTransform.Translate(_yAxis.X1 + xInterval, _xAxis.Y1 - yInterval);

            _dataToPixelSpaceTransform = MatrixUtil.CloneMatrix(layoutTransform);
            _pixelToDataSpaceTransform = MatrixUtil.CloneMatrix(layoutTransform);
            if (_pixelToDataSpaceTransform.HasInverse)
                _pixelToDataSpaceTransform.Invert();

            _datasetGeometry.Transform = new MatrixTransform(layoutTransform);

            _canvas.Children.Add(_datasetPath);
        }

        /// <summary>
        /// Updates the graphics for the hover point (the point highlighted when you hover
        /// the mouse over the graph).
        /// </summary>
        private void UpdateHoverPoint() {
            // Everything is null or nothing is.
            // Null everything out after so we can enable/disable the feature.
            if (_hoverPointText != null) {
                _canvas.Children.Remove(_hoverPointText);
                _canvas.Children.Remove(_hoverPointEllipse);
                _canvas.Children.Remove(_hoverPointTextBackground);
                _hoverPointText = null;
                _hoverPointEllipse = null;
                _hoverPointTextBackground = null;
            }

            // If we're not enabled, we're done
            if (!_mouseHoverPointEnabled)
                return;

            // First get the mouse location in data space
            Point mousePositionInDataSpace = _pixelToDataSpaceTransform.Transform(_mouseHoverPointPx);

            // Get the data's Y point from here
            Dataset dataset = _renderParams.Dataset;
            double closestX = dataset.GetClosestXTo(mousePositionInDataSpace.X);
            DataResult yValue = dataset.ValueAt(closestX);
            if (yValue.Succeeded) {
                mousePositionInDataSpace.X = closestX;
                mousePositionInDataSpace.Y = yValue.Result;
            } else {
                // Invalid location in the dataset so dont highlight anything
                return;
            }

            // Transform back into pixel space for display
            Point highlightPointPx = _dataToPixelSpaceTransform.Transform(mousePositionInDataSpace);

            _hoverPointEllipse = new Ellipse();
            _hoverPointEllipse.Width = _hoverPointOptions.Size;
            _hoverPointEllipse.Height = _hoverPointOptions.Size;
            _hoverPointEllipse.Stroke = new SolidColorBrush(_hoverPointOptions.Color);
            _hoverPointEllipse.StrokeThickness = _hoverPointOptions.StrokeThickness;
            _hoverPointEllipse.IsHitTestVisible = false;
            Canvas.SetLeft(_hoverPointEllipse, highlightPointPx.X - _hoverPointOptions.Size / 2.0);
            Canvas.SetTop(_hoverPointEllipse, highlightPointPx.Y - _hoverPointOptions.Size / 2.0);
            _canvas.Children.Add(_hoverPointEllipse);

            String xCoordinate = "" + mousePositionInDataSpace.X;
            if (_xCoordFormatter != null)
                xCoordinate = _xCoordFormatter.Format(mousePositionInDataSpace.X);
            String yCoordinate = "" + mousePositionInDataSpace.Y;
            if (_yCoordFormatter != null)
                yCoordinate = _yCoordFormatter.Format(mousePositionInDataSpace.Y);
            _hoverPointText = new TextBlock();
            _hoverPointText.Text = "(" + xCoordinate + ", " + yCoordinate + ")";
            _hoverPointText.FontSize = _hoverPointOptions.FontSize;
            _hoverPointText.FontWeight = FontWeights.Bold;
            _hoverPointText.Foreground = new SolidColorBrush(Colors.White);// _hoverPointOptions.Color);
            _hoverPointText.IsHitTestVisible = false;

            // Measure the text so we can position it. This is valid because the canvas will not impose
            // any constraints on it so it can use all the space it needs.
            _hoverPointText.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            double textWidth = _hoverPointText.DesiredSize.Width;
            double textHeight = _hoverPointText.DesiredSize.Height;

            double textVerticalOffset = 10;
            double textLeftPx = highlightPointPx.X - textWidth / 2.0;
            double textTopPx = highlightPointPx.Y - textHeight - textVerticalOffset - _hoverPointOptions.Size;

            // Clamp the left/right/top/bottom of the text to the canvas
            double canvasPadding = 5;
            if (textLeftPx < canvasPadding)
                textLeftPx = canvasPadding;
            if (textLeftPx + textWidth + canvasPadding > _canvas.ActualWidth)
                textLeftPx -= (textLeftPx + textWidth + canvasPadding) - _canvas.ActualWidth;
            if (textTopPx < canvasPadding)
                textTopPx = canvasPadding;
            if (textTopPx + textHeight + canvasPadding > _canvas.ActualHeight)
                textTopPx -= (textTopPx + textHeight + canvasPadding) - _canvas.ActualHeight;

            Canvas.SetLeft(_hoverPointText, textLeftPx);
            Canvas.SetTop(_hoverPointText, textTopPx);

            double backgroundPadding = 5.0;
            double cornerRadius = 5;
            _hoverPointTextBackground = new Rectangle();
            _hoverPointTextBackground.Width = backgroundPadding * 2 + textWidth;
            _hoverPointTextBackground.Height = backgroundPadding * 2 + textHeight;
            _hoverPointTextBackground.Fill = new SolidColorBrush(Color.FromArgb(0xaa, 0, 0, 0));
            _hoverPointTextBackground.RadiusX = cornerRadius;
            _hoverPointTextBackground.RadiusY = cornerRadius;
            Canvas.SetLeft(_hoverPointTextBackground, textLeftPx - backgroundPadding);
            Canvas.SetTop(_hoverPointTextBackground, textTopPx - backgroundPadding);
            _hoverPointTextBackground.IsHitTestVisible = false;
            _canvas.Children.Add(_hoverPointTextBackground);
            _canvas.Children.Add(_hoverPointText); // Add the text after the background so it is in front
        }

        private Grid CreateGraphTick(string text, double fontSize, Brush lineColor, Brush textColor, bool showTick, bool isXAxis)
        {
            Grid grid = new Grid();
            Line line = new Line()
            {
                Fill = lineColor,
                Stroke = lineColor,
                StrokeThickness = 1,
                X1 = 0,
                X2 = 0,
                Y1 = 0,
                Y2 = 16
            };
            
            TextBlock textBlock = new TextBlock()
            {
                FontSize = fontSize,
                Foreground = textColor,
                RenderTransform = new RotateTransform(45),
                RenderTransformOrigin = new Point(0.25, 0.25),
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };

            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            Grid.SetRow(line, 0);
            Grid.SetRow(textBlock, 1);

            grid.Children.Add(line);
            grid.Children.Add(textBlock);

            if (!showTick)
                line.Visibility = Visibility.Hidden;

            return grid;
        }
    }

    class HoverPointOptions {
        public Color Color { get; set; }
        public double Size { get; set; } // In Pixels
        public double StrokeThickness { get; set; } // In Pixels
        public double FontSize { get; set; }

        public HoverPointOptions(Color color, double size, double strokeThickness, double fontSize) {
            Color = color;
            Size = size;
            StrokeThickness = strokeThickness;
            FontSize = fontSize;
        }

        public HoverPointOptions Clone() {
            return new HoverPointOptions(Color, Size, StrokeThickness, FontSize);
        }

        public bool IsEqual(HoverPointOptions other) {
            return other.Color.Equals(Color) &&
                other.Size == Size &&
                other.StrokeThickness == StrokeThickness &&
                other.FontSize == FontSize;
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

    public class Range {
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
        public LineOptions LineOptions { get; set; } = new LineOptions(Colors.White, 1.0);

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
