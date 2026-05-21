using UnityEngine;

namespace GuildSim.Shared
{
    public static class MathUtility
    {
        public static float NormalizedDifficulty(int power, int difficulty, float coefficient)
            => Mathf.Clamp01(0.5f + (power - difficulty) * coefficient);
    }
}
