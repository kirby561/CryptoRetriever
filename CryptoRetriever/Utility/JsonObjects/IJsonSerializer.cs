using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoRetriever.Utility.JsonObjects {
    public interface IJsonSerializer<T> {
        /// <summary>
        /// Takes an object and returns a JsonObject representing it.
        /// There should be enough information to be able to reproduce
        /// the object later.
        /// </summary>
        /// <param name="obj">The object to convert to JSON.</param>
        /// <returns>Returns a JsonObject with the needed properties.</returns>
        JsonObject ToJson(T obj);

        /// <summary>
        /// Converts a JsonObject back into the object it came from.
        /// </summary>
        /// <param name="json">A JsonObject containing the information from the original object.</param>
        /// <returns>Returns a reconstructed object that is the same type and has the same data as the original.</returns>
        T ToObject(JsonObject json);
    }
}
