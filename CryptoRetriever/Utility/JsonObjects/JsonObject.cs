using System;
using System.Collections;
using System.Collections.Generic;
using Utf8Json;

namespace CryptoRetriever.Utility.JsonObjects {
    /// <summary>
    /// An object that can be used for complex JSON serialization/deserialization
    /// in conjunction with Utf8Json. Basically build a tree of primitive objects as
    /// a JsonObject and call toJsonString() or toJsonBytes()
    /// </summary>
    public class JsonObject {
        public Dictionary<String, object> Children { get; set; } = new Dictionary<String, object>();

        public JsonObject() {
            // Nothing to do
        }

        protected JsonObject(Dictionary<String, object> children) {
            Children = children;
        }

        public JsonObject Put(String key, JsonObject obj) {
            Children.Add(key, obj.Children);
            return this;
        }

        public JsonObject GetObject(String key) {
            if (!Children.ContainsKey(key))
                return null;
            return new JsonObject((Dictionary<String, object>)Children[key]);
        }

        public JsonObject Put(String key, IEnumerable<IJsonable> objectArray) {
            List<Dictionary<String, object>> childrenArray = null;
            foreach (IJsonable obj in objectArray) {
                if (childrenArray == null)
                    childrenArray = new List<Dictionary<String, object>>();
                childrenArray.Add(obj.ToJson().Children);
            }
            Children.Add(key, childrenArray);
            return this;
        }

        public List<JsonObject> GetObjectArray(String key) {
            if (Children.ContainsKey(key)) {
                IEnumerable arrayObjects = (IEnumerable)Children[key];
                if (arrayObjects != null) {
                    List<JsonObject> result = new List<JsonObject>();
                    foreach (Dictionary<String, object> childCollection in arrayObjects)
                        result.Add(new JsonObject(childCollection));
                    return result;
                }
            }
            return null;
        }

        public JsonObject Put<T>(String key, T[] anArray) {
            Children.Add(key, anArray);
            return this;
        }

        public T[] GetArray<T>(String key) {
            return (T[])Children[key];
        }

        public JsonObject Put(String key, bool b) {
            Children.Add(key, b);
            return this;
        }

        public bool GetBool(String key) {
            return (bool)Children[key];
        }

        public JsonObject Put(String key, long num) {
            // Store longs as strings to ensure no data loss
            String numStr = "" + num;
            return Put(key, numStr);
        }

        public long GetLong(String key) {
            // Longs are stored as strings to prevent data loss
            String numStr = GetString(key);
            return long.Parse(numStr);
        }

        public JsonObject Put(String key, int num) {
            Children.Add(key, num);
            return this;
        }

        public int GetInt(String key) {
            return Convert.ToInt32(Children[key]);
        }

        public JsonObject Put(String key, double num) {
            Children.Add(key, num);
            return this;
        }

        public double GetDouble(String key) {
            return (double)Children[key];
        }

        public JsonObject Put(String key, String thing) {
            Children.Add(key, thing);
            return this;
        }

        public String GetString(String key) {
            return (String)Children[key];
        }

        public JsonObject Put(String key, DateTime dateTime) {
            if (dateTime == DateTime.MinValue) {
                Children.Add(key, null);
                return this;
            }

            // Store the unix timestamp in ms
            long time = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            return Put(key, time);
        }

        public DateTime GetDateTime(String key) {
            object val = Children[key];
            if (val == null)
                return DateTime.MinValue;
            long dateTimestamp = GetLong(key);
            return DateTimeOffset.FromUnixTimeMilliseconds(dateTimestamp).DateTime;
        }

        public byte[] ToJsonBytes() {
            return JsonSerializer.Serialize(Children);
        }

        public byte[] ToPrettyJsonBytes() {
            return JsonSerializer.PrettyPrintByteArray(ToJsonBytes());
        }

        public String ToJsonString() {
            return JsonSerializer.PrettyPrint(ToJsonBytes());
        }

        public static JsonObject FromJsonBytes(byte[] bytes) {
            var children = JsonSerializer.Deserialize<Dictionary<String, object>>(bytes);
            return new JsonObject(children);
        }
    }
}
