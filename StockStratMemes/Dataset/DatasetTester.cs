using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StockStratMemes {
    /// <summary>
    /// Tests classes in the Dataset package.
    /// </summary>
    class DatasetTester {
        public static bool TestDatasetInsertAndAdd() {
            List<Point> expectedResult = new List<Point>();
            Dataset dataSet = new Dataset();

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

        public static bool TestDatasetGetClosestX() {
            // Test a dataset that caused a real failure in the past
            Dataset testData = new Dataset();
            testData.Insert(new Point(1647129600, 0.0511465572343779));
            testData.Insert(new Point(1647216000, 0.0492648469933117));
            testData.Insert(new Point(1647302400, 0.0510670903662445));
            double testPoint = 1647146709;

            // Should be index testData[0].X = 1647129600
            double closestX = testData.GetClosestXTo(testPoint);
            if (!IsApproxEqual(closestX, testData.Points[0].X)) return false;

            // Move the point to the right and try again. This time it should be closer to the middle one (index 1)
            testPoint = testData.Points[1].X - 25;
            closestX = testData.GetClosestXTo(testPoint);
            if (!IsApproxEqual(closestX, testData.Points[1].X)) return false;

            // Try boundary cases, first left of the first point
            testPoint = testData.Points[0].X - 25;
            closestX = testData.GetClosestXTo(testPoint);
            if (!IsApproxEqual(closestX, testData.Points[0].X)) return false;

            // Boundary case, passed the rightmost point
            testPoint = testData.Points[2].X + 25;
            closestX = testData.GetClosestXTo(testPoint);
            if (!IsApproxEqual(closestX, testData.Points[2].X)) return false;

            // Try each point exactly and make sure it returns that point
            for (int i = 0; i < testData.Points.Count; i++) {
                testPoint = testData.Points[i].X;
                closestX = testData.GetClosestXTo(testPoint);
                if (!IsApproxEqual(closestX, testData.Points[i].X)) return false;
            }

            // Try a dataset with 1 point
            testData = new Dataset();
            testData.Insert(new Point(10, 20));

            // Check a point to the left of the 1 point
            testPoint = 5;
            closestX = testData.GetClosestXTo(testPoint);
            if (!IsApproxEqual(closestX, testData.Points[0].X)) return false;

            // A point to the right of the 1 point
            testPoint = 15;
            closestX = testData.GetClosestXTo(testPoint);
            if (!IsApproxEqual(closestX, testData.Points[0].X)) return false;

            // The one point itself
            testPoint = testData.Points[0].X;
            closestX = testData.GetClosestXTo(testPoint);
            if (!IsApproxEqual(closestX, testData.Points[0].X)) return false;

            return true;
        }

        public static bool Test() {
            bool result = true;

            result &= TestDatasetInsertAndAdd();
            result &= TestDatasetGetClosestX();

            return result;
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
}
