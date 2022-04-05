using System.Windows.Media;

namespace CryptoRetriever {
    public static class MatrixUtil {
        public static Matrix CloneMatrix(Matrix input) {
            return new Matrix(
                input.M11,
                input.M12,
                input.M21,
                input.M22,
                input.OffsetX,
                input.OffsetY
            );
        }
    }
}
