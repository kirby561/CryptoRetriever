using CryptoRetriever.Strats;
using CryptoRetriever.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CryptoRetriever.Data {
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
        private HoverPointOptions _hoverPointOptions = new HoverPointOptions(Color.FromRgb(0x72, 0x9f, 0xcf), 10, 2, 14.0);
        private Ellipse _hoverPointEllipse;
        private TextBlock _hoverPointText;
        private Rectangle _hoverPointTextBackground;

        // Datasets - Keep track of the original and post-filter so we
        // can display them together for reference. The filtered one can
        // be null if the dataset has not been filtered.
        private Dataset _originalDataset;
        private Dataset _filteredDataset = null;

        // Dataset drawing pieces
        private Geometry _originalDatasetGeometry;
        private Path _originalDatasetPath;
        private Geometry _filteredDatasetGeometry;
        private Path _filteredDatasetPath;

        // Transactions drawing pieces
        private List<Transaction> _transactions = null;
        private List<FrameworkElement> _transactionMarkers = new List<FrameworkElement>();
        private Path _buyIndicatorPath;
        private Path _sellIndicatorPath;

        // Axis drawing pieces
        private Line _xAxis;
        private Line _yAxis;
        private TextBlock _xAxisLabel;
        private TextBlock _yAxisLabel;
        private List<FrameworkElement> _graphTicks = new List<FrameworkElement>();
        private bool _isAxisEnabled = true;
        private bool _areTicksEnabled = true;
        private bool _areXGridlinesEnabled = true;
        private bool _areYGridlinesEnabled = true;
        private bool _startRangeAt0 = false; // True to lock the bottom of the bounding range to 0
        private bool _showOriginalDataset = true;

        // Colors
        private SolidColorBrush _axisBrush = new SolidColorBrush(Color.FromRgb(0x72, 0x9f, 0xcf));// Colors.Green);
        private SolidColorBrush _gridlineBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));// Colors.Green);
        private SolidColorBrush _foregroundColor = new SolidColorBrush(Colors.White); // Color of data and text
        private SolidColorBrush _buyBrush = new SolidColorBrush(Colors.Red); // Color for buy indicator
        private SolidColorBrush _sellBrush = new SolidColorBrush(Colors.Green); // Color for sell indicator

        // Constants
        private static readonly double X_AXIS_X_OFFSET = 50; // Distance from the left of the window to the left of the X axis in pixels
        private static readonly double X_AXIS_Y_OFFSET = 100; // Distance from the bottom of the window to the X axis in pixels
        private static readonly double Y_AXIS_X_OFFSET = X_AXIS_X_OFFSET + 50; // Distance from the left of the window to the Y axis in pixels
        private static readonly double Y_AXIS_Y_OFFSET = 50; // Distance from the bottom of the window to the bototm of the Y axis in pixels

        private static readonly int FILTERED_DATA_ZINDEX = -1;
        private static readonly int ORIGINAL_DATA_ZINDEX = -2;
        private static readonly int GRIDLINE_ZINDEX = -3;

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

        public GraphRenderer(Canvas canvas, Dataset originalDataset) : this(canvas, originalDataset, null) { }

        public GraphRenderer(Canvas canvas, Dataset originalDataset, Dataset filteredDataset) {
            _canvas = canvas;
            _originalDataset = originalDataset;
            _filteredDataset = filteredDataset;

            // Start the domain/range to fit the data
            InitializeDomainAndRange();

            _canvas.LayoutUpdated += OnCanvasLayoutUpdates;
        }

        public List<Transaction> Transactions {
            get {
                return _transactions;
            }
            set {
                _transactions = value;
                UpdateTransactions();
            }
        }

        private void SetPathStroke(Path path, Brush brush, double thickness) {
            path.Stroke = brush;
            path.StrokeThickness = thickness;
            path.IsHitTestVisible = false;
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
        /// True if ticks should be drawn, false otherwise.
        /// </summary>
        public bool AreTicksEnabled {
            get {
                return _areTicksEnabled;
            }
            set {
                _areTicksEnabled = value;
                UpdateGraphScales();
            }
        }

        /// <summary>
        /// True if gridlines should be drawn at each X tick.
        /// False otherwise.
        /// </summary>
        public bool AreXGridlinesEnabled {
            get {
                return _areXGridlinesEnabled;
            }
            set {
                _areXGridlinesEnabled = value;
                UpdateGraphScales();
            }
        }

        /// <summary>
        /// True if gridlines should be drawn at each Y tick.
        /// False otherwise.
        /// </summary>
        public bool AreYGridlinesEnabled {
            get {
                return _areYGridlinesEnabled;
            }
            set {
                _areYGridlinesEnabled = value;
                UpdateGraphScales();
            }
        }

        /// <summary>
        /// True if the bounding range should start at 0,
        /// False if it should start at the lowest point in the dataset.
        /// </summary>
        public bool ShouldStartRangeAt0 {
            get {
                return _startRangeAt0;
            }
            set {
                _startRangeAt0 = value;
                InitializeDomainAndRange();
            }
        }

        /// <summary>
        /// True if the original dataset should still be
        /// shown when the dataset is filtered.
        /// </summary>
        public bool ShowOriginalDataset {
            get {
                return _showOriginalDataset;
            }
            set {
                _showOriginalDataset = value;
                UpdateAll();
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

            foreach (Point p in _originalDataset.Points) {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            if (_filteredDataset != null) {
                foreach (Point p in _filteredDataset.Points) {
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }
            }

            _boundingDomain = new Range(minX, maxX);
            _boundingRange = new Range(minY, maxY);

            if (_startRangeAt0)
                _boundingRange.Start = 0;

            SetDomain(_boundingDomain.Start, _boundingDomain.End);
            SetRange(_boundingRange.Start, _boundingRange.End);

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

        public Dataset GetOriginalDataset() {
            return _originalDataset;
        }

        public bool IsZoomedOut() {
            return GetDomain().IsEqual(_boundingDomain) &&
                GetRange().IsEqual(_boundingRange);
        }

        /// <summary>
        /// Updates all the graphics to reflect the current state
        /// </summary>
        public void UpdateAll() {
            // Check that there is enough room to render something
            if (_renderParams.CanvasSizePx.Width <= Y_AXIS_X_OFFSET || _renderParams.CanvasSizePx.Height < X_AXIS_Y_OFFSET)
                return;

            if (_filteredDataset != null && _filteredDataset.Count <= 1)
                return; // Not enough data to show

            if (_originalDataset == null || _originalDataset.Count <= 1)
                return; // Not enough data to show

            UpdateAxis();
            UpdateData();
            UpdateTransactions();
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

            // Setup axis
            _xAxis = new Line();
            _xAxis.Fill = _axisBrush;
            _xAxis.Stroke = _axisBrush;
            _xAxis.StrokeThickness = axisWidthPx;
            _xAxis.X1 = X_AXIS_X_OFFSET;
            _xAxis.Y1 = _renderParams.CanvasSizePx.Height - X_AXIS_Y_OFFSET;
            _xAxis.X2 = _renderParams.CanvasSizePx.Width - X_AXIS_X_OFFSET;
            _xAxis.Y2 = _xAxis.Y1;
            _xAxis.IsHitTestVisible = false;

            _canvas.Children.Add(_xAxis);

            _yAxis = new Line();
            _yAxis.Fill = _axisBrush;
            _yAxis.Stroke = _axisBrush;
            _yAxis.StrokeThickness = axisWidthPx;
            _yAxis.X1 = Y_AXIS_X_OFFSET;
            _yAxis.Y1 = Y_AXIS_Y_OFFSET;
            _yAxis.X2 = _yAxis.X1;
            _yAxis.Y2 = _xAxis.Y1 + Y_AXIS_Y_OFFSET;
            _yAxis.IsHitTestVisible = false;

            _canvas.Children.Add(_yAxis);

            // Setup axis labels
            _xAxisLabel = new TextBlock()
            {
                FontSize = 24,
                Foreground = _foregroundColor,
                Text = "Date/Time (EST)",
                TextWrapping = TextWrapping.Wrap
            };

            _xAxisLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(_xAxisLabel, ((_xAxis.X2 + _yAxis.X1) / 2) - (_xAxisLabel.DesiredSize.Width / 2));
            Canvas.SetBottom(_xAxisLabel, 10);
            _canvas.Children.Add(_xAxisLabel);

            _yAxisLabel = new TextBlock()
            {
                FontSize = 24,
                Foreground = _foregroundColor,
                RenderTransform = new RotateTransform(270),
                Text = "Price (USD)",
                TextWrapping = TextWrapping.Wrap
            };

            _yAxisLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(_yAxisLabel, 10);
            Canvas.SetTop(_yAxisLabel, ((_xAxis.Y1 + _yAxis.Y1) / 2) + (_yAxisLabel.DesiredSize.Width / 2));
            _canvas.Children.Add(_yAxisLabel);            
        }

        /// <summary>
        /// Applies transformations to data
        /// </summary>
        private void UpdateData() {
            if (_originalDatasetPath != null)
                _canvas.Children.Remove(_originalDatasetPath);

            if (_filteredDatasetPath != null)
                _canvas.Children.Remove(_filteredDatasetPath);

            double xAxisSize = _xAxis.X2 - _yAxis.X1;
			double yAxisSize = _xAxis.Y1 - _yAxis.Y1;

			double xScale = xAxisSize / (_renderParams.Domain.End - _renderParams.Domain.Start);

            double yScale = yAxisSize / (_renderParams.Range.End - _renderParams.Range.Start);

            Matrix layoutTransform = new Matrix();

            // Shift the curve to 0 so the first point starts at the top left
            layoutTransform.Translate(-_renderParams.Domain.Start, -_renderParams.Range.Start);

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

            // Create a geometry for the dataset
            Tuple<Geometry, Path> originalGeomAndPath = CalculateGeometryAndPath(_originalDataset, _canvas.ActualWidth, _pixelToDataSpaceTransform);
            _originalDatasetGeometry = originalGeomAndPath.Item1;
            _originalDatasetPath = originalGeomAndPath.Item2;

            if (_filteredDataset != null) {
                Tuple<Geometry, Path> filteredGeomAndPath = CalculateGeometryAndPath(_filteredDataset, _canvas.ActualWidth, _pixelToDataSpaceTransform);
                _filteredDatasetGeometry = filteredGeomAndPath.Item1;
                _filteredDatasetPath = filteredGeomAndPath.Item2;
            }

            SolidColorBrush foregroundBrush = new SolidColorBrush(_renderParams.ForegroundDataLineOptions.Color);
            SolidColorBrush backgroundBrush = new SolidColorBrush(_renderParams.BackgroundDataLineOptions.Color);
            if (_filteredDataset != null) {
                SetPathStroke(_originalDatasetPath, backgroundBrush, _renderParams.BackgroundDataLineOptions.Thickness);
                SetPathStroke(_filteredDatasetPath, foregroundBrush, _renderParams.ForegroundDataLineOptions.Thickness);
            } else {
                SetPathStroke(_originalDatasetPath, foregroundBrush, _renderParams.ForegroundDataLineOptions.Thickness);
            }

            _originalDatasetGeometry.Transform = new MatrixTransform(layoutTransform);

            // Clip to within the axis bounds if we've been laid out.
            if (_renderParams.CanvasSizePx.Width > 0 && _renderParams.CanvasSizePx.Height > 0) {
                RectangleGeometry clipGeometry = GetGraphClipArea();
                _originalDatasetPath.Clip = clipGeometry;

                if (_filteredDatasetPath != null)
                    _filteredDatasetPath.Clip = clipGeometry;
            }

            // Show the original dataset if there's no filtered dataset
            // or if the user wants to show it too
            if (_filteredDatasetPath == null || _showOriginalDataset) {
                Canvas.SetZIndex(_originalDatasetPath, ORIGINAL_DATA_ZINDEX);
                _canvas.Children.Add(_originalDatasetPath);
            }

            if (_filteredDatasetPath != null) {
                _filteredDatasetGeometry.Transform = new MatrixTransform(layoutTransform);
                Canvas.SetZIndex(_filteredDatasetPath, FILTERED_DATA_ZINDEX);
                _canvas.Children.Add(_filteredDatasetPath);
            }
        }

        /// <summary>
        /// Updates the transaction indicators if any Transactions are set.
        /// </summary>
        private void UpdateTransactions() {
            foreach (FrameworkElement fe in _transactionMarkers)
                _canvas.Children.Remove(fe);
            _transactionMarkers.Clear();

            if (_transactions == null)
                return;

            if (_renderParams.CanvasSizePx.Width == 0 || _renderParams.CanvasSizePx.Height == 0)
                return;

            StreamGeometry buyGeometry = new StreamGeometry();
            bool buyStarted = false;
            StreamGeometry sellGeometry = new StreamGeometry();
            bool sellStarted = false;
            using (StreamGeometryContext buyStream = buyGeometry.Open()) {
                using (StreamGeometryContext sellStream = sellGeometry.Open()) {
                    foreach (Transaction transaction in _transactions) {
                        double moneyRecieved = transaction.CurrencyTransferred;
                        double timestamp = (transaction.TransactionTime - DateTime.UnixEpoch).TotalSeconds;

                        Point curvePoint = new Point(timestamp, _originalDataset.ValueAt(timestamp).Result);
                        Point curvePointPx = _dataToPixelSpaceTransform.Transform(curvePoint);
                        Point bottomPx = new Point(curvePointPx.X, _renderParams.CanvasSizePx.Height - X_AXIS_Y_OFFSET);

                        if (moneyRecieved < 0) {
                            // Buy
                            if (!buyStarted) {
                                buyStream.BeginFigure(curvePointPx, true, false);
                                buyStarted = true;
                            } else {
                                buyStream.LineTo(curvePointPx, false, false);
                            }
                            buyStream.LineTo(bottomPx, true, false);
                        } else {
                            // Sell
                            if (!sellStarted) {
                                sellStream.BeginFigure(curvePointPx, true, false);
                                sellStarted = true;
                            } else {
                                sellStream.LineTo(curvePointPx, false, false);
                            }
                            sellStream.LineTo(bottomPx, true, false);
                        }
                    }
                }
            }

            _buyIndicatorPath = new Path();
            _buyIndicatorPath.Data = buyGeometry;
            SetPathStroke(_buyIndicatorPath, _buyBrush, 1);
            _buyIndicatorPath.StrokeDashArray = new DoubleCollection(new double[] { 8, 5 });
            _buyIndicatorPath.Clip = GetGraphClipArea();
            Canvas.SetLeft(_buyIndicatorPath, 0);
            Canvas.SetTop(_buyIndicatorPath, 0);
            Canvas.SetRight(_buyIndicatorPath, 0);
            Canvas.SetBottom(_buyIndicatorPath, 0);

            _sellIndicatorPath = new Path();
            _sellIndicatorPath.Data = sellGeometry;
            SetPathStroke(_sellIndicatorPath, _sellBrush, 1);
            _sellIndicatorPath.StrokeDashArray = new DoubleCollection(new double[] { 10, 5 });
            _sellIndicatorPath.Clip = GetGraphClipArea();
            Canvas.SetLeft(_sellIndicatorPath, 0);
            Canvas.SetTop(_sellIndicatorPath, 0);
            Canvas.SetRight(_sellIndicatorPath, 0);
            Canvas.SetBottom(_sellIndicatorPath, 0);

            _transactionMarkers.Add(_buyIndicatorPath);
            _canvas.Children.Add(_buyIndicatorPath);
            _transactionMarkers.Add(_sellIndicatorPath);
            _canvas.Children.Add(_sellIndicatorPath);
        }

        /// <summary>
        /// Set up tick marks on axis
        /// </summary>
        private void UpdateGraphScales() {
            for (int i = 0; i < _graphTicks.Count; i++) {
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
            if (_areTicksEnabled) {
                for (int i = 0; i < dblPointsY.Count; i++) {
                    string label = dblPointsY[i].ToString("C");

                    TextBlock block = CreateGraphTickLabel(
                        label,
                        12,
                        _foregroundColor,
                        dblPointsY[i] > GetRange().End || dblPointsY[i] < GetRange().Start ? true : false);

                    block.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

                    Canvas.SetLeft(block, rangeTickData[i].X - block.DesiredSize.Width);
                    Canvas.SetTop(block, rangeTickData[i].Y - block.DesiredSize.Height);
                    _graphTicks.Add(block);

                    Line tick = CreateTickMark(_axisBrush, "y", dblPointsY[i] > GetRange().End || dblPointsY[i] < GetRange().Start);
                    Canvas.SetLeft(tick, rangeTickData[i].X);
                    Canvas.SetTop(tick, rangeTickData[i].Y);
                    _graphTicks.Add(tick);
                }
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
            if (_areTicksEnabled) {
                for (int i = 0; i < dblPointsX.Count; i++) {
                    DateTime date = DateTime.Parse(new TimestampToDateFormatter().Format(dblPointsX[i]));

                    string label;
                    switch (domainTickData.Item1) {
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
                    TextBlock xTickLabel = CreateGraphTickLabel(label, 12, _foregroundColor, blurTick);
                    xTickLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    Canvas.SetLeft(xTickLabel, domainTickData.Item2[i].X);
                    Canvas.SetTop(xTickLabel, domainTickData.Item2[i].Y);
                    _graphTicks.Add(xTickLabel);

                    Line xTick = CreateTickMark(_axisBrush, "x", blurTick);
                    Canvas.SetLeft(xTick, domainTickData.Item2[i].X);
                    Canvas.SetTop(xTick, domainTickData.Item2[i].Y);
                    _graphTicks.Add(xTick);
                }
            }

            // If X Gridlines are enabled, add them at
            // the same places but dashed and full length
            if (_areXGridlinesEnabled && domainTickData.Item2.Length > 0) {
                StreamGeometry verticalGridlineGeometry = new StreamGeometry();
                using (StreamGeometryContext stream = verticalGridlineGeometry.Open()) {
                    double yStart = Y_AXIS_Y_OFFSET;
                    double yEnd = _renderParams.CanvasSizePx.Height - X_AXIS_Y_OFFSET;
                    stream.BeginFigure(new Point(domainTickData.Item2[0].X, yStart), false, false); // Starts at the top
                    stream.LineTo(new Point(domainTickData.Item2[0].X, yEnd), true, false);

                    for (int i = 1; i < domainTickData.Item2.Length; i++) {
                        Point top = new Point(domainTickData.Item2[i].X, yStart);
                        Point bottom = new Point(top.X, yEnd);
                        stream.LineTo(top, false, false);
                        stream.LineTo(bottom, true, false);
                    }
                }
                Path verticalGridlines = MakePathFromGeometry(_gridlineBrush, verticalGridlineGeometry, 1);
                Canvas.SetZIndex(verticalGridlines, GRIDLINE_ZINDEX);
                _graphTicks.Add(verticalGridlines);
            }

            if (_areYGridlinesEnabled && rangeTickData.Length > 0) {
                StreamGeometry horizontalGridlineGeometry = new StreamGeometry();
                using (StreamGeometryContext stream = horizontalGridlineGeometry.Open()) {
                    double xStart = Y_AXIS_X_OFFSET;
                    double xEnd = _renderParams.CanvasSizePx.Width - X_AXIS_X_OFFSET;
                    stream.BeginFigure(new Point(xStart, rangeTickData[0].Y), false, false);
                    stream.LineTo(new Point(xEnd, rangeTickData[0].Y), true, false);

                    for (int i = 1; i < rangeTickData.Length; i++) {
                        Point left = new Point(xStart, rangeTickData[i].Y);
                        Point right = new Point(xEnd, rangeTickData[i].Y);
                        stream.LineTo(left, false, false);
                        stream.LineTo(right, true, false);
                    }
                }
                Path horizontalGridlines = MakePathFromGeometry(_gridlineBrush, horizontalGridlineGeometry, 1);
                Canvas.SetZIndex(horizontalGridlines, GRIDLINE_ZINDEX);
                _graphTicks.Add(horizontalGridlines);
            }

            foreach (FrameworkElement element in _graphTicks)
                _canvas.Children.Add(element);
        }

        private Path MakePathFromGeometry(Brush stroke, StreamGeometry geometry, double thickness) {
            Path path = new Path();
            path.Data = geometry;
            path.Stroke = _gridlineBrush;
            path.StrokeThickness = thickness;
            Canvas.SetLeft(path, 0);
            Canvas.SetTop(path, 0);
            Canvas.SetRight(path, 0);
            Canvas.SetBottom(path, 0);
            return path;
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

            Dataset dataset = GetActiveDataset();

            // Get the data's Y point from here
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
        private Tuple<String, Point[]> GetDomainTickData() {
            double seconds = GetDomain().End - GetDomain().Start;
            double minutes = seconds / 60;
            double hours = minutes / 60;
            double days = hours / 24;
            double months = days / 30;

            String unit;
            long granularitySeconds;

            const long hoursPerDay = 24;
            const long minutesPerHour = 60;
            const long secondsPerMinute = 60;

            if (months > 1) {
                unit = "m";
                granularitySeconds = 0; // NA, months aren't evenly spaced
            } else if (days > 1) {
                unit = "d";
                granularitySeconds = hoursPerDay * minutesPerHour * secondsPerMinute;
            } else if (hours > 1) {
                unit = "h";
                granularitySeconds = minutesPerHour * secondsPerMinute;
            } else if (minutes > 1) {
                unit = "mi";
                granularitySeconds = secondsPerMinute;
            } else {
                unit = "s";
                granularitySeconds = 1;
            }

            // Months have uneven spacing so they are a special case
            List<Point> points = new List<Point>();
            if (unit == "m") {
                // Start with the first month after the domain start
                double domainStart = GetDomain().Start;
                double domainEnd = GetDomain().End;
                DateTime dt = DateTimeConstant.UnixStart.AddSeconds(domainStart).ToLocalTime();
                int dayOffset = dt.Date.Day - 1;
                DateTime firstMonth = dt.Date.Subtract(TimeSpan.FromDays(dayOffset));
                if (dt.Date < dt) {
                    firstMonth = firstMonth.AddMonths(1);
                }

                // Label the month before/after the domain too to create the fading out effect
                DateTime previousMonth = firstMonth.AddMonths(-1);
                points.Add(new Point(DateTimeHelper.GetUnixTimestampSeconds(previousMonth.ToUniversalTime()), 0));

                points.Add(new Point(DateTimeHelper.GetUnixTimestampSeconds(firstMonth.ToUniversalTime()), 0));
                DateTime nextMonth = firstMonth.AddMonths(1).ToUniversalTime();
                while (DateTimeHelper.GetUnixTimestampSeconds(nextMonth) < domainEnd) {
                    points.Add(new Point(DateTimeHelper.GetUnixTimestampSeconds(nextMonth), 0));
                    nextMonth = nextMonth.AddMonths(1);
                }

                // Label the month before/after the domain too to create the fading out effect
                points.Add(new Point(DateTimeHelper.GetUnixTimestampSeconds(nextMonth.ToUniversalTime()), 0));
            } else {
                // Get the first tick within range
                long startInteger = (long)GetDomain().Start;
                long nextTick = granularitySeconds * (startInteger / granularitySeconds);
                // If it's not a multiple of the granularity, start at the first tick within the domain
                if (nextTick != startInteger)
                    nextTick += granularitySeconds;

                // Label 1 tick before/after too to create the fading out effect
                points.Add(new Point(nextTick - granularitySeconds, 0));

                while (nextTick < GetDomain().End) {
                    points.Add(new Point(nextTick, 0));
                    nextTick += granularitySeconds;
                }

                // Label 1 tick before/after too to create the fading out effect
                points.Add(new Point(nextTick, 0));
            }

            return new Tuple<String, Point[]>(unit, points.ToArray());
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
        /// Calculates the geometry and path for the given list of points.
        /// </summary>
        /// <param name="dataset">The list to calculate from.</param>
        /// <param name="screenWidthPx">The current width of the rendering surface in pixels. This is used to optimize the path size for performance reasons.</param>
        /// <param name="pixelToDataTransform">A matrix that converts pixel space to data space.</param>
        /// <returns>Returns the geometry and the path as a Tuple, respectively.</returns>
        private Tuple<Geometry, Path> CalculateGeometryAndPath(Dataset dataset, double screenWidthPx, Matrix pixelToDataTransform) {
            Path path = null;
            StreamGeometry geometry = new StreamGeometry();
            if (dataset.Count > 1) {
                bool started = false;
                using (StreamGeometryContext stream = geometry.Open()) {
                    // Adjust the dataset to fit the number of pixels on the screen
                    Point pixelSpacePoint = new Point();
                    for (int x = 0; x < screenWidthPx; x++) {
                        pixelSpacePoint.X = x;
                        Point dataSpacePoint = pixelToDataTransform.Transform(pixelSpacePoint);

                        DataResult result = dataset.ValueAt(dataSpacePoint.X);
                        if (result.Succeeded)
                            dataSpacePoint.Y = result.Result;
                        else
                            continue;

                        if (!started) {
                            started = true;
                            stream.BeginFigure(dataSpacePoint, false, false);
                        } else {
                            stream.LineTo(dataSpacePoint, true, true);
                        }
                    }
                }
            } else {
                return new Tuple<Geometry, Path>(new RectangleGeometry(), new Path()); // Nothing to draw
            }

            path = new Path();
            path.Data = geometry;
            Canvas.SetLeft(path, 0);
            Canvas.SetTop(path, 0);
            Canvas.SetRight(path, 0);
            Canvas.SetBottom(path, 0);

            return new Tuple<Geometry, Path>(geometry, path);
        }

        /// <summary>
        /// Returns the latest dataset version. If the dataset was filtered,
        /// it will return that one. Otherwise it will return the original.
        /// </summary>
        /// <returns>Returns the active dataset.</returns>
        private Dataset GetActiveDataset() {
            Dataset dataset;
            if (_filteredDataset != null)
                dataset = _filteredDataset;
            else
                dataset = _originalDataset;
            return dataset;
        }

        private RectangleGeometry GetGraphClipArea() {
            return new RectangleGeometry(
                    new Rect(
                        Y_AXIS_X_OFFSET,
                        0,
                        _renderParams.CanvasSizePx.Width - Y_AXIS_X_OFFSET,
                        _renderParams.CanvasSizePx.Height - X_AXIS_Y_OFFSET
                    )
            );
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
        public Range Domain { get; set; } = new Range();
        public Range Range { get; set; } = new Range();
        public Size CanvasSizePx { get; set; } = new Size();
        public LineOptions ForegroundDataLineOptions { get; set; } = new LineOptions(Colors.White, 1.0);
        public LineOptions BackgroundDataLineOptions { get; set; } = new LineOptions(Colors.DarkGray, 0.5);

        public RenderParams Clone() {
            RenderParams clone = new RenderParams();
            clone.Domain = Domain.Clone();
            clone.Range = Range.Clone();
            clone.CanvasSizePx = new Size(CanvasSizePx.Width, CanvasSizePx.Height);
            clone.ForegroundDataLineOptions = new LineOptions(ForegroundDataLineOptions.Color, ForegroundDataLineOptions.Thickness);
            clone.BackgroundDataLineOptions = new LineOptions(BackgroundDataLineOptions.Color, BackgroundDataLineOptions.Thickness);
            return clone;
        }

        public bool IsEqual(RenderParams other) {
            if (other == null)
                return false;

            return other.Domain.IsEqual(Domain) &&
                other.Range.IsEqual(Range) &&
                other.CanvasSizePx.Equals(CanvasSizePx) &&
                other.ForegroundDataLineOptions.IsEqual(ForegroundDataLineOptions) &&
                other.BackgroundDataLineOptions.IsEqual(BackgroundDataLineOptions);
        }
    }
}
