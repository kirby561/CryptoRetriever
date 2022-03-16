using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRetriever {
    public interface ICoordinateFormatter {
        String Format(double coordinate);
    }
}
