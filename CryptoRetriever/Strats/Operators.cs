using System;
using System.Collections.Generic;

namespace CryptoRetriever.Strats {
    public static class Operators {
        public static Dictionary<String, Operator> GetAllOperators() {
            var operators = new Dictionary<String, Operator>();

            foreach (Operator op in GetMathOperators().Values)
                AddOperator(operators, op);
            foreach (Operator op in GetStringComparisonOperators().Values)
                AddOperator(operators, op);
            foreach (Operator op in GetNumberComparisonOperators().Values)
                AddOperator(operators, op);
            foreach (Operator op in GetLogicOperators().Values)
                AddOperator(operators, op);
            foreach (Operator op in GetToOperators().Values)
                AddOperator(operators, op);

            return operators;
        }

        public static Dictionary<String, Operator> GetMathOperators() {
            var operators = new Dictionary<String, Operator>();

            AddOperator(operators, new AdditionOperator());
            AddOperator(operators, new SubtractionOperator());
            AddOperator(operators, new MultiplicationOperator());
            AddOperator(operators, new DivisionOperator());
            AddOperator(operators, new ExponentOperator());

            return operators;
        }

        public static Dictionary<string, Operator> GetStringComparisonOperators() {
            var operators = new Dictionary<String, Operator>();

            AddOperator(operators, new LessThanStringOperator());
            AddOperator(operators, new GreaterThanStringOperator());
            AddOperator(operators, new EqualsStringOperator());
            AddOperator(operators, new DoesNotEqualStringOperator());

            return operators;
        }

        public static Dictionary<String, Operator> GetNumberComparisonOperators() {
            var operators = new Dictionary<String, Operator>();

            AddOperator(operators, new LessThanNumberOperator());
            AddOperator(operators, new GreaterThanNumberOperator());
            AddOperator(operators, new EqualsNumberOperator());
            AddOperator(operators, new DoesNotEqualNumberOperator());

            return operators;
        }

        public static Dictionary<String, Operator> GetToOperators() {
            var operators = new Dictionary<String, Operator>();

            AddOperator(operators, new ToOperator());

            return operators;
        }

        public static Dictionary<String, Operator> GetLogicOperators() {
            var operators = new Dictionary<String, Operator>();

            AddOperator(operators, new AndLogicOperator());
            AddOperator(operators, new OrLogicOperator());

            return operators;
        }

        private static void AddOperator<T>(Dictionary<String, T> dict, T op) where T : Operator {
            dict.Add(op.GetId(), op);
        }
    }
}
