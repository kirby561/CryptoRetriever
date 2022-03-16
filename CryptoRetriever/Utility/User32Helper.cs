using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRetriever {
    public class User32Helper {
        /// <returns>Returns the system configured double click time in ms.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int GetDoubleClickTime();
    }
}
