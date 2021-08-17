using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StockStratMemes {
    public class Dataset2 {
        /// <summary>
        /// The points are coordinates but they are ordered by X value.
        /// The approximate index can be retrieved by 
        /// </summary>
        public List<Point> Points { get; set; }

        /// <summary>
        /// The approximate number of seconds between points in Points.
        /// This is used to speed up indexing. There is no guarantee the points are
        /// actually this far apart.
        /// </summary>
        public double Granularity { get; set; }

        public Dataset2() {
            Points = new List<Point>();
        }

        public Dataset2(List<Point> points) {
            Points = points;
        }

        public Dataset2(List<Point> points, double granularitySeconds) {
            Points = points;
            Granularity = granularitySeconds;
        }

        public void Insert(Point point) {
            if (Points.Count == 0) {
                Points.Add(point);
            } else {
                double xDelta = point.X - Points[0].X;

                // If we're before the first point just insert at 0
                if (xDelta < 0) {
                    Points.Insert(0, point);
                    return;
                }

                // If this point is greater than the largest point, add it at the end
                // This speeds up sequential additions to the list
                if (point.X > Points[Points.Count - 1].X) {
                    Points.Insert(Points.Count, point);
                    return;
                }

                // Binary search the spot from here
                Points.Insert(BinarySearchForX(point.X), point);
            }
        }

        /// <summary>
        /// Finds the insertion point for the given x value assuming ascending order of Points by X value.
        /// </summary>
        /// <param name="x">The X value to search for</param>
        /// <returns>Returns the first index with a greater value than x. If you are inserting in order, this would be the index to insert at.</returns>
        private int BinarySearchForX(double x) {
            int index = Points.Count / 2;
            int left = 0;
            int right = Points.Count - 1;
            while (true) {
                double value = Points[index].X;
                if (x > value) {
                    left = index + 1; // Left is always set to the index after if point is greater (insert will put it to the left of left)
                    index = (right + left) / 2;
                } else if (x < value) {
                    right = index; // Right is always at an index higher than point
                    index = (right + left) / 2;
                } else { // Equals value
                    return index;
                }

                // Check if we should insert here
                if (right <= left) {
                    return left;
                }
            }
        }

        /// <summary>
        /// Gets the linearly interpolated Y value at the given X coordinate.
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <returns>Returns the interpolated value or an error if the given x value was out of bounds or there wasn't any data.</returns>
        public DataResult ValueAt(double x) {
            // First make sure we have more than 1 point and x is within the domain of this DataSet.
            if (Points.Count == 0)
                return new DataResult("There is no data in this set.");

            if (x < Points[0].X)
                return new DataResult("Out of range. The given value is too small.");

            if (x > Points[Points.Count - 1].X)
                return new DataResult("Out of range. The given value is too large.");

            if (Points[0].X == x)
                return new DataResult(Points[0].Y);

            // Note, because of the checks above, we know there are more than 1 point or one of the 3 range
            // checks would have triggered. We also know that largerIndex can't be 0 because if it was equal
            // or less than the first element we would have returned above.
            int largerIndex = BinarySearchForX(x);
            int smallerIndex = largerIndex - 1;

            Point largerPoint = Points[largerIndex];
            Point smallerPoint = Points[smallerIndex];

            double distToLarger = largerPoint.X - x;
            double distToSmaller = x - smallerPoint.X;

            // It really shouldn't be 0 because we wouldn't have a function but may as well support that.
            if (distToSmaller == 0)
                return new DataResult(smallerPoint.Y);

            if (distToLarger == 0)
                return new DataResult(largerPoint.Y);

            // Interpolate between the nearest 2
            double invDistance = 1 / distToSmaller + 1 / distToLarger;
            double smallerWeight = (1 / distToSmaller) / invDistance;
            double largerWeight = (1 / distToLarger) / invDistance;
            double result = largerPoint.Y * largerWeight + smallerPoint.Y * smallerWeight;

            return new DataResult(result);
        }

        public static bool Test() {
            List<Point> expectedResult = new List<Point>();
            Dataset2 dataSet = new Dataset2();

            Point p1 = new Point(1, 1);
            Point p2 = new Point(3, 6);
            Point p3 = new Point(5, 0);
            Point p4 = new Point(7, 0);

            // Test adding the first element
            //  Should be just p2
            expectedResult.Add(p2);
            dataSet.Insert(p2);
            if (!AreListsEqual(expectedResult, dataSet.Points)) return false;

            // Test adding an element less than it on the left end
            //  Should be p1, p2
            expectedResult.Insert(0, p1);
            dataSet.Insert(p1);
            if (!AreListsEqual(expectedResult, dataSet.Points)) return false;

            // Test adding an element more than it on the right end
            //  Should be p1, p2, p4
            expectedResult.Add(p4);
            dataSet.Insert(p4);
            if (!AreListsEqual(expectedResult, dataSet.Points)) return false;

            // Test adding one in the middle
            //  Should be p1, p2, p3, p4
            expectedResult.Insert(2, p3);
            dataSet.Insert(p3);
            if (!AreListsEqual(expectedResult, dataSet.Points)) return false;

            double expectedValueAt2 = 3.5;
            double valueAt2 = dataSet.ValueAt(2).Result;
            if (!IsApproxEqual(expectedValueAt2, valueAt2)) return false;

            double expectedValueAt2p5 = 4.75;
            double valueAt2p5 = dataSet.ValueAt(2.5).Result;
            if (!IsApproxEqual(expectedValueAt2p5, valueAt2p5)) return false;

            return true;
        }

        private static bool IsApproxEqual(double n1, double n2, double epsilon = 0.0001) {
            return Math.Abs(n1 - n2) <= epsilon;
        }

        private static bool AreListsEqual(List<Point> list1, List<Point> list2) {
            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++) {
                if (list1[i] != list2[i])
                    return false;
            }

            return true;
        }
    }

    public class DataResult {
        public double Result { get; set; }
        public bool Succeeded { get; set; }
        public String ErrorDetails { get; set; }

        public DataResult(double value) {
            Result = value;
            Succeeded = true;
        }

        public DataResult(String errorDetails) {
            Succeeded = false;
            ErrorDetails = errorDetails;
            Result = 0;
        }
    }
}
