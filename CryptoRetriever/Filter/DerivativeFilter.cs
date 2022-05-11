using CryptoRetriever.Data;
using CryptoRetriever.Utility.JsonObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CryptoRetriever.Filter {
    public class DerivativeFilter : IFilter {
        public string Summary {
            get {
                return "DerivativeFilter";
            }
        }

        public Dataset Filter(Dataset input) {
            Dataset result = new Dataset(input.Count);
            result.Granularity = input.Granularity;

            if (input.Count < 2)
                return result;

            for (int i = 1; i < input.Count; i++) {
                double previousX = input.Points[i - 1].X;
                double previousY = input.Points[i - 1].Y;
                double x = input.Points[i].X;
                double y = input.Points[i].Y;
                double dY = (y - previousY);
                double dX = (x - previousX);

                // Make sure we have no vertical asymptotes
                if (dX <= 0) {
                    Console.WriteLine("Dataset must have increasing X values for the DerivativeFilter. Doing nothing.");
                    return new Dataset(input);
                }
                result.Points.Add(new Point(x, dY / dX));
            }

            return result;
        }

        public void FromJson(JsonObject json) {
            // Nothing to do
        }

        public JsonObject ToJson() {
            JsonObject obj = new JsonObject();
            obj.Put("Type", "DerivativeFilter");
            return obj;
        }
    }
}
