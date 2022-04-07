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
        private List<FrameworkElement> _graphTicks = new List<FrameworkElement>();
        private readonly SolidColorBrush _GREEN_BRUSH = new SolidColorBrush(Colors.Green);
        private readonly SolidColorBrush _WHITE_BRUSH = new SolidColorBrush(Colors.White);
        private bool _isAxisEnabled = true;

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
        /// Detatches this renderer from the canvas it has been
        /// drawing to and removes all event handlers. This needs
        /// to be called prior to attaching a different renderer
        /// to the canvas.
        /// </summary>
        public void Cleanup() {
            // Clear the canvas, we don't own it anymore
            _canvas.LayoutUpdated -= OnCanvasLayoutUpdates;
            _canvas.Children.Clear();
        }

        /// <summary>
        /// True when the axis is being drawn, false to hide it.
        /// </summary>
        public bool IsAxisEnabled {
            get {
                return _isAxisEnabled;
            }
            set {
                _isAxisEnabled = value;
                UpdateAxis();
            }
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
            UpdateGraphScales();
            UpdateHoverPoint();
        }

        /// <summary>
        /// Create graph with axis labels
        /// </summary>
        private void UpdateAxis() {
            // Setup axis lines
            if (_xAxis != null)
            {
                _canvas.Children.Remove(_xAxis);
                _canvas.Children.Remove(_xAxisLabel);
            }

            if (_yAxis != null)
            {
                _canvas.Children.Remove(_yAxis);
                _canvas.Children.Remove(_yAxisLabel);
            }

            if (!IsAxisEnabled)
                return; // Don't draw the axis

            double axisWidthPx = 3;
            //SolidColorBrush brush = new SolidColorBrush(Colors.Green);

            // Setup axis
            _xAxis = new Line();
            _xAxis.Fill = _GREEN_BRUSH;
            _xAxis.Stroke = _GREEN_BRUSH;
            _xAxis.StrokeThickness = axisWidthPx;
            _xAxis.X1 = 50;
            _xAxis.Y1 = _renderParams.CanvasSizePx.Height - 100;
            _xAxis.X2 = _renderParams.CanvasSizePx.Width - 50;
            _xAxis.Y2 = _xAxis.Y1;
            _xAxis.IsHitTestVisible = false;

            //Canvas.SetLeft(_xAxis, _xAxisLabel.DesiredSize.Width);
            _canvas.Children.Add(_xAxis);

            _yAxis = new Line();
            _yAxis.Fill = _GREEN_BRUSH;
            _yAxis.Stroke = _GREEN_BRUSH;
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
                Foreground = _WHITE_BRUSH,
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
                Foreground = _WHITE_BRUSH,
                RenderTransform = new RotateTransform(270),
                Text = "Price (USD)",
                TextWrapping = TextWrapping.Wrap
            };

            _yAxisLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(_yAxisLabel, 10);
            Canvas.SetTop(_yAxisLabel, ((_xAxis.Y1 - _yAxis.Y1) / 2) + (_yAxisLabel.DesiredSize.Width / 2) + 50);
            _canvas.Children.Add(_yAxisLabel);            
        }

        /// <summary>
        /// Applies transformations to data
        /// </summary>
        private void UpdateData()
        {
            if (_datasetPath != null)
                _canvas.Children.Remove(_datasetPath);

			double xAxisSize = _xAxis.X2 - _yAxis.X1;
			double yAxisSize = _xAxis.Y1 - _yAxis.Y1;

			//double xScale = _renderParams.CanvasSizePx.Width / (_renderParams.Domain.End - _renderParams.Domain.Start);
			double xScale = xAxisSize / (_renderParams.Domain.End - _renderParams.Domain.Start);

            //double yScale = _renderParams.CanvasSizePx.Height / (_renderParams.Range.End - _renderParams.Range.Start);
            double yScale = yAxisSize / (_renderParams.Range.End - _renderParams.Range.Start);

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
            layoutTransform.Translate(_yAxis.X1, _xAxis.Y1);

            _dataToPixelSpaceTransform = MatrixUtil.CloneMatrix(layoutTransform);
            _pixelToDataSpaceTransform = MatrixUtil.CloneMatrix(layoutTransform);
            if (_pixelToDataSpaceTransform.HasInverse)
                _pixelToDataSpaceTransform.Invert();

            _datasetGeometry.Transform = new MatrixTransform(layoutTransform);

            _canvas.Children.Add(_datasetPath);
        }       

        /// <summary>
        /// Set up tick marks on axis
        /// </summary>
        private void UpdateGraphScales()
		{
            for (int i = 0; i < _graphTicks.Count; i++)
            {
                _canvas.Children.Remove(_graphTicks[i]);
            }
            _graphTicks.Clear();

            // Setup axis ticks w/ labels
            //      y-axis  
            Point[] rangeTickData = GetRangeTickData();
            List<double> dblPointsY = rangeTickData.Select(x => x.Y).ToList();
            _dataToPixelSpaceTransform.Transform(rangeTickData);

            // Override matrix Y transform for each point
            for (int i = 0; i < rangeTickData.Length; i++)
            {
                rangeTickData[i].X = _yAxis.X1;
            }

            // Set up labels and pin to points
            for (int i = 0; i < dblPointsY.Count; i++)
            {
                string label = dblPointsY[i].ToString("C");

                TextBlock block = CreateGraphTickLabel(
                    label,
                    12,
                    _WHITE_BRUSH,
                    dblPointsY[i] > GetRange().End || dblPointsY[i] < GetRange().Start ? true : false);

                block.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

                Canvas.SetLeft(block, rangeTickData[i].X - block.DesiredSize.Width);
                Canvas.SetTop(block, rangeTickData[i].Y - block.DesiredSize.Height);
                _graphTicks.Add(block);
                _canvas.Children.Add(block);

                Line tick = CreateTickMark(_GREEN_BRUSH, "y", dblPointsY[i] > GetRange().End || dblPointsY[i] < GetRange().Start);
                Canvas.SetLeft(tick, rangeTickData[i].X);
                Canvas.SetTop(tick, rangeTickData[i].Y);
                _graphTicks.Add(tick);
                _canvas.Children.Add(tick);
            }


            //      x-axis

            // Apply graph matrix to axis points
            Tuple<string, Point[]> domainTickData = GetDomainTickData();
            List<double> dblPointsX = domainTickData.Item2.Select(x => x.X).ToList();
            _dataToPixelSpaceTransform.Transform(domainTickData.Item2);
            
            // Override matrix Y transform for each point
            for (int i = 0; i < domainTickData.Item2.Length; i++)
			{
                domainTickData.Item2[i].Y = _xAxis.Y1;
			}

            // Set up labels and pin to points
            for (int i = 0; i < dblPointsX.Count; i++)
            {
                DateTime date = DateTime.Parse(new TimestampToDateFormatter().Format(dblPointsX[i]));

                string label;
                switch (domainTickData.Item1)
                {
                    case "m":
                        label = date.ToString("MMM") + " " + date.Year;
                        break;
                    //case "w":
                    //    label = date.ToString("MMM") + " WK" + i;
                    //    break;
                    case "d":
                        label = date.ToString("MMM") + " " + date.Day;
                        break;
                    case "s":
                        label = date.ToLongTimeString();
                        break;
                    default:
                        label = date.ToShortTimeString();
                        break;
                }
                bool blurTick = dblPointsX[i] > GetDomain().End || dblPointsX[i] < GetDomain().Start ? true : false;
                TextBlock xTickLabel = CreateGraphTickLabel(label, 12, _WHITE_BRUSH, blurTick);
                xTickLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                Canvas.SetLeft(xTickLabel, domainTickData.Item2[i].X);
                Canvas.SetTop(xTickLabel, domainTickData.Item2[i].Y);
                _graphTicks.Add(xTickLabel);
                _canvas.Children.Add(xTickLabel);

                Line xTick = CreateTickMark(_GREEN_BRUSH, "x", blurTick);
                Canvas.SetLeft(xTick, domainTickData.Item2[i].X);
                Canvas.SetTop(xTick, domainTickData.Item2[i].Y);
                _graphTicks.Add(xTick);
                _canvas.Children.Add(xTick);
            }
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

        private TextBlock CreateGraphTickLabel(string text, double fontSize, Brush textColor, bool blurTick) {
            TextBlock textBlock = new TextBlock() {
                FontSize = fontSize,
                Foreground = textColor,
                Opacity = blurTick ? 0.5 : 1.0,
                LayoutTransform = new RotateTransform(45),
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };

            return textBlock;
        }

        private Line CreateTickMark(Brush lineColor, String axis, bool blurTick) {
            Line line = new Line() {
                Fill = lineColor,
                Opacity = blurTick ? 0.5 : 1.0,
                Stroke = lineColor,
                StrokeThickness = 1,
                X1 = 0,
                X2 = axis == "x" ? 0 : 16,
                Y1 = 0,
                Y2 = axis == "x" ? -16 : 0
            };
            return line;
        }

        /// <summary>
        /// Returns the unit we will use for x-axis based on data within domain as well as location of label with
        /// respect to x-axis.
        /// </summary>
        /// <returns></returns>
        private Tuple<string, Point[]> GetDomainTickData()
        {
            double seconds = GetDomain().End - GetDomain().Start;
            double minutes = seconds / 60;
            double hours = minutes / 60;
            double days = hours / 24;
            double weeks = days / 7;
            double months = days / 30;

            string unit;
            double unitV;

            Point[] points;

            if (months > 1)
            {
                unit = "m";
                unitV = months;
            }
            //else if (weeks >= 3)
            //{
            //    unit = "w";
            //    unitV = weeks;
            //}
            else if (days > 1)
            {
                unit = "d";
                unitV = days;
            }
            else if (hours > 1)
            {
                unit = "h";
                unitV = hours;
            }
            else if (minutes > 1)
            {
                unit = "mi";
                unitV = minutes;
            }
            else
            {
                unit = "s";
                unitV = seconds;

            }
            int numTicks = (int)Math.Ceiling(unitV) + 1;
            points = new Point[numTicks];
            for (int i = 0; i < numTicks; i++)
            {
                double intervalSeconds = GetDomain().Start + ((seconds / unitV) * i);
                points[i] = new Point(GetUnitBeginning(unit, intervalSeconds), 0);            
            }
            //points[points.Length - 2] = new Point(GetBoundingDomain().Start, 0);
            //points[points.Length - 1] = new Point(GetBoundingDomain().End, 0);
            //points = points.OrderBy(x => x.X).ToArray();

            return new Tuple<string, Point[]>(unit, points);
        }

        private Point[] GetRangeTickData() {
            double dollars = GetRange().End - GetRange().Start;
            Point[] values;

            int targetTickCount = 5;
            int minTicks = 3;
            double spacingForTarget = dollars / targetTickCount;

            // Get the number of ticks needed for the lower 10 and upper 10
            int closestLower10Power = (int)Math.Floor(Math.Log10(spacingForTarget));
            int closestHigher10Power = (int)Math.Ceiling(Math.Log10(spacingForTarget));
            double lowerSpacing = Math.Pow(10, closestLower10Power);
            double higherSpacing = Math.Pow(10, closestHigher10Power);
            int numLeftSpacingTicks = (int)(dollars / lowerSpacing);
            int numRightSpacingTicks = (int)(dollars / higherSpacing);

            // Pick the spacing closer to the target number of ticks
            double spacing;
            int numTicks;
            if (Math.Abs(numLeftSpacingTicks - targetTickCount) < Math.Abs(numRightSpacingTicks - targetTickCount) 
                    || numRightSpacingTicks < minTicks) {
                // Use lower spacing
                spacing = lowerSpacing;
                numTicks = numLeftSpacingTicks;
            } else {
                // Use higher spacing
                spacing = higherSpacing;
                numTicks = numRightSpacingTicks;
            }

            // Don't let the number of ticks get passed twice the target
            while (numTicks > targetTickCount * 2) {
                numTicks /= 2;
                spacing *= 2;
            }

            // Get the first tick on the range
            double start = GetRange().Start;
            int spacingMultiples = (int)(start / spacing);
            double firstTickSpot = spacingMultiples * spacing;
            if (firstTickSpot < start)
                firstTickSpot += spacing; // Go one more if start isn't an even multiple of spacing (almost always)

            // If we can fit another tick at the end, do so
            //  (changes depending on where the first tick is)
            if (firstTickSpot + spacing * (numTicks - 1) < (GetRange().End - spacing))
                numTicks++;

            values = new Point[numTicks];
            for (int i = 0; i < numTicks; i++) {
                values[i] = new Point(0, firstTickSpot + spacing * i);
            }

            return values;
		}

        /// <summary>
        /// Provided the unit type and seconds value, this will calculate the beginning of that unit in seconds.
        /// For example, supplying unitType = "d" (day) and date "3/11/2022 15:33:41" this function will return
        /// "3/11/2022 00:00:00" (beginning of the day) as seconds
        /// </summary>
        /// <param name="unitType">
        ///     The ID of the unit type:
        ///         m - Month
        ///         d - Day
        ///         h - Hour
        ///         mi - Minute
        /// </param>
        /// <param name="seconds">The UTC timestamp of a point.</param>
        /// <returns>Returns the calculated UTC timestamp of the tick at the given granularity.</returns>
        private double GetUnitBeginning(string unitType, double seconds)
		{
            long utcTimestampSeconds = (long)Math.Round(seconds);
            DateTime utcDateTime = DateTimeConstant.UnixStart.AddSeconds(utcTimestampSeconds);
            DateTime utcBegin;
            
            if (unitType == "m")
			{
                utcBegin = new DateTime(utcDateTime.Year, utcDateTime.Month, 1).ToUniversalTime();
            }
            else if (unitType == "d")
            {
                utcBegin = new DateTime(utcDateTime.Year, utcDateTime.Month, utcDateTime.Day).ToUniversalTime();
            }
            else if (unitType == "h")
			{
                utcBegin = utcDateTime.Date.AddHours(utcDateTime.Hour);
            }
            else if (unitType == "mi")
            {
                utcBegin = utcDateTime.Date.AddHours(utcDateTime.Hour).AddMinutes(utcDateTime.Minute);
            }
            else
            {
                utcBegin = utcDateTime.Date.AddHours(utcDateTime.Hour).AddMinutes(utcDateTime.Minute).AddSeconds(utcDateTime.Second);
            }

            return seconds - (utcDateTime - utcBegin).TotalSeconds;
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

    class DollarFormatter : ICoordinateFormatter
    {
        public string Format(double coordinate)
        {
            return "$" + ((decimal)coordinate).ToString("N");
        }
    }

    class TimestampToDateFormatter : ICoordinateFormatter
    {
        public string Format(double coordinate)
        {
            long utcTimestampSeconds = (long)Math.Round(coordinate);
            DateTime unixStart = DateTimeConstant.UnixStart;
            DateTime localDateTime = unixStart.AddSeconds(utcTimestampSeconds).ToLocalTime();
            return localDateTime.ToString("G");
        }
    }
}
