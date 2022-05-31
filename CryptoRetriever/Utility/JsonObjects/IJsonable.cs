namespace CryptoRetriever.Utility.JsonObjects {
    public interface IJsonable {
        /// <summary>
        /// Stores the data of this object as a JsonObject so
        /// it can be written to disk or sent somewhere or whatever.
        /// </summary>
        /// <returns>Returns the newly created JsonObject.</returns>
        JsonObject ToJson();

        /// <summary>
        /// Sets this object's data from the given JsonObject properties.
        /// This assumes that all the properties are present under the
        /// required names of this object. If they are not, an exception
        /// will be thrown.
        /// </summary>
        /// <param name="json">The JsonObject containing this object's data.</param>
        void FromJson(JsonObject json);
    }
}
