namespace PortfolioAI
{
    public class UtilityClass
    {
        public static float CosineSimilarity(float[] v1, float[] v2)
        {
            float dot = 0, norm1 = 0, norm2 = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                norm1 += v1[i] * v1[i];
                norm2 += v2[i] * v2[i];
            }
            return dot / (float)(Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        public static float CosineSimilarityA(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length) throw new ArgumentException("Vectors must be the same length");

            float dot = 0f;
            float magA = 0f;
            float magB = 0f;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dot += vectorA[i] * vectorB[i];
                magA += vectorA[i] * vectorA[i];
                magB += vectorB[i] * vectorB[i];
            }

            return dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB) + 1e-8f);
        }
    }
}
