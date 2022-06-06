﻿using System;
using System.Collections.Generic;
using System.Windows;

namespace CryptoRetriever.Data {
    public class Dataset {
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

        /// <summary>
        /// Returns the number of points in this dataset.
        /// </summary>
        public int Count {
            get {
                return Points.Count;
            }
        }

        public Dataset() {
            Points = new List<Point>();
        }

        public Dataset(int initialSize) {
            Points = new List<Point>(initialSize);
        }

        public Dataset(List<Point> points) {
            Points = points;
        }

        /// <summary>
        /// Creates a new dataset with the given points.
        /// </summary>
        /// <param name="points">The list of points to start with.</param>
        /// <param name="granularitySeconds">The granularity of points.</param>
        public Dataset(List<Point> points, double granularitySeconds) {
            Points = points;
            Granularity = granularitySeconds;
        }

        /// <summary>
        /// Creates a deep copy of the given dataset.
        /// </summary>
        /// <param name="other">The other dataset to copy.</param>
        public Dataset(Dataset other) {
            Points = new List<Point>(other.Points.Count);
            foreach (Point p in other.Points) {
                Points.Add(new Point(p.X, p.Y));
            }
            Granularity = other.Granularity;
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
        /// Adds the given dataset to the end of this dataset.
        /// Does not assume any partiuclar ordering.
        /// </summary>
        /// <param name="other">The other dataset to add.</param>
        public void Add(Dataset other) {
            // If the other set is empty or null we're done
            if (other.Points == null || other.Points.Count == 0)
                return;

            Points.AddRange(other.Points);
        }

        /// <summary>
        /// Finds the insertion point for the given x value assuming ascending order of Points by X value.
        /// 
        /// Note: The index returned can be equal to the size of the dataset (one passed the last index) indicating
        /// that the insertion point is after the last element. If you're using this to find the closest index, you have
        /// to check that it is not passed the end!
        /// </summary>
        /// <param name="x">The X value to search for</param>
        /// <returns>Returns the first index with a greater value than x. If you are inserting in order, this would be the index to insert at.</returns>
        public int BinarySearchForX(double x) {
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

        /// <summary>
        /// Gets the X value closest to the given x value in this dataset.
        /// It is an error to call this on an empty dataset.
        /// 
        /// Note: This assumes the dataset is in order!
        /// </summary>
        /// <param name="xValue">The x value to compare.</param>
        /// <returns>Returns the closest X value in this dataset.</returns>
        public double GetClosestXTo(double xValue) {
            if (Points.Count == 1)
                return Points[0].X;

            // BinarySearchForX gets the closest value to the right of the point
            // so the closest will be the returned point or the one to the left.
            // BinarySearchForX can return the index passed the end so right can
            // potentially be out of bounds without the Math.Min
            int right = Math.Min(Count - 1, BinarySearchForX(xValue));
            int left = right - 1;

            if (left < 0)
                return Points[right].X;

            double leftDist = Math.Abs(xValue - Points[left].X);
            double rightDist = Math.Abs(Points[right].X - xValue);
            if (leftDist < rightDist)
                return Points[left].X;
            else
                return Points[right].X;
        }

        /// <summary>
        /// Checks if this dataset is evenly spaced. If it is, the second return param
        /// will be the approximate spacing. Assumes the dataset is at least 2 items.
        /// </summary>
        /// <param name="dataset">The dataset to check.</param>
        /// <param name="allowedPercentDifference">The allowed percent difference from the average to be considered even. Defaults to 0.1%</param>
        /// <returns>A pair with the first being true if the dataset is evenly spaced and if so the second being the amount it is spaced by.</returns>
        public Tuple<bool, double> IsEvenlySpaced(double allowedPercentDifference = 0.1) {
            double totalSpacing = 0;
            int numSpaces = Count - 1;
            double previousX = Points[0].X;
            double largestSpace = Double.MinValue;
            double smallestSpace = Double.MaxValue;
            int smallestIndex = -1;
            for (int i = 1; i < Count; i++) {
                double currentX = Points[i].X;
                double spacing = (currentX - previousX);
                totalSpacing += spacing;
                previousX = currentX;

                if (largestSpace < spacing)
                    largestSpace = spacing;
                if (smallestSpace > spacing) {
                    smallestSpace = spacing;
                    smallestIndex = i;
                }
            }

            // Check if the percent difference from the average
            // to the smallest and largest spaces are greater than
            // the allowed spacing.
            double averageSpacing = totalSpacing / numSpaces;
            if (100 * ((largestSpace - averageSpacing) / averageSpacing) > allowedPercentDifference)
                return new Tuple<bool, double>(false, averageSpacing);
            if (100 * ((averageSpacing - smallestSpace) / averageSpacing) > allowedPercentDifference)
                return new Tuple<bool, double>(false, averageSpacing);

            // We're evenly spaced within the tolerance.
            return new Tuple<bool, double>(true, averageSpacing);
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
