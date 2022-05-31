namespace CryptoRetriever.Utility {
    public interface ProgressListener {
        void OnProgress(long currentValue, long maxProgress);
    }
}
