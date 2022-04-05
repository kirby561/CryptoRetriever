using System.Runtime.InteropServices;

namespace CryptoRetriever {
    /// <summary>
    /// Provides static methods for accessing User32.dll things
    /// like configured system defaults.
    /// </summary>
    public class User32Helper {
        /// <returns>Returns the system configured double click time in ms.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int GetDoubleClickTime();
    }
}
