using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Filter {
    public interface IFilter {
        Dataset Filter(Dataset input);
    }
}
