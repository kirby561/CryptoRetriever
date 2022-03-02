using System;

namespace StockStratMemes {
    class JsonUtil {
        /// <summary>
        /// Do a poor mans prettify since JavaScriptSerializer doesn't support
        /// formatting the output.  This could be improved by using an actual library like JSON.net.
        /// 
        /// It puts newlines after object declarations and endings as well as between properties.
        /// </summary>
        /// <param name="input">The string to format (this is read only)</param>
        /// <returns>Returns a new formatted string.</returns>
        public static String PoorMansJsonFormat(String input) {
            input = input.Insert(input.IndexOf("{") + 1, Environment.NewLine + "\t");
            input = input.Insert(input.LastIndexOf("}"), Environment.NewLine);
            input = input.Replace("\":", "\": ");
            input = input.Replace(",\"", "," + Environment.NewLine + "\t\"");
            return input;
        }
    }
}
