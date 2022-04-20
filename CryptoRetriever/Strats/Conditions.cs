using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Strats {
    public static class Conditions {
        public static Dictionary<String, Condition> GetConditions() {
            var conditions = new Dictionary<String, Condition>();

            AddCond(conditions, new BoolCondition("True", true));
            AddCond(conditions, new BoolCondition("False", false));
            AddCond(conditions, new NumberComparison());
            AddCond(conditions, new LogicComparison());
            AddCond(conditions, new StringComparison());

            return conditions;
        }

        private static void AddCond<T>(Dictionary<String, T> dict, T cond) where T : Condition {
            dict.Add(cond.GetId(), cond);
        }
    }
}
