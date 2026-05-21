using UnityEngine;

namespace GuildSim.Shared
{
    public static class RandomUtility
    {
        public static bool Roll(float successRate) => Random.value < Mathf.Clamp01(successRate);

        public static int RangeInclusive(int min, int max) => Random.Range(min, max + 1);

        public static T Pick<T>(T[] items) => items[Random.Range(0, items.Length)];
    }
}
