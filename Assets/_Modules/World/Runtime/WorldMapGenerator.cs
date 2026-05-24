using UnityEngine;

namespace GuildSim.World
{
    // 副作用なし純粋計算 — Unity API は Mathf.PerlinNoise のみ使用
    public static class WorldMapGenerator
    {
        /// <summary>
        /// Perlin noise ベースのバイオームマップを生成して返す。
        /// 戻り値: [x, y] インデックスの WorldTerrainType 二次元配列。
        /// </summary>
        public static WorldTerrainType[,] Generate(WorldMapGeneratorConfig cfg)
        {
            int w = cfg.MapWidth;
            int h = cfg.MapHeight;

            var rng = new System.Random(cfg.Seed);
            var hoOff = RandomOffset(rng);
            var tmOff = RandomOffset(rng);
            var moOff = RandomOffset(rng);

            float[,] heightMap = GenerateNoiseMap(w, h, cfg.HeightScale,
                cfg.HeightOctaves, cfg.HeightPersistence, cfg.HeightLacunarity, hoOff);

            ApplyIslandMask(heightMap, cfg);

            float[,] tempMap  = GenerateNoiseMap(w, h, cfg.TemperatureScale, 3, 0.5f, 2f, tmOff);
            float[,] moistMap = GenerateNoiseMap(w, h, cfg.MoistureScale,    3, 0.5f, 2f, moOff);

            var result = new WorldTerrainType[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                result[x, y] = Classify(heightMap[x, y], tempMap[x, y], moistMap[x, y], cfg);

            return result;
        }

        // ---- private ----

        private static Vector2 RandomOffset(System.Random rng) =>
            new Vector2(rng.Next(-99999, 99999), rng.Next(-99999, 99999));

        /// <summary>
        /// フラクタルブラウン運動 (fBm) による Perlin ノイズマップ。値域 0–1。
        /// </summary>
        private static float[,] GenerateNoiseMap(
            int w, int h, float scale,
            int octaves, float persistence, float lacunarity,
            Vector2 offset)
        {
            var map = new float[w, h];

            // 正規化用の最大振幅を事前計算
            float maxAmp = 0f;
            float amp = 1f;
            for (int o = 0; o < octaves; o++) { maxAmp += amp; amp *= persistence; }

            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float value    = 0f;
                float curAmp   = 1f;
                float curFreq  = 1f;

                for (int o = 0; o < octaves; o++)
                {
                    float nx = (x + offset.x) * scale * curFreq;
                    float ny = (y + offset.y) * scale * curFreq;
                    value   += Mathf.PerlinNoise(nx, ny) * curAmp;
                    curAmp  *= persistence;
                    curFreq *= lacunarity;
                }

                map[x, y] = value / maxAmp;
            }

            return map;
        }

        /// <summary>
        /// 中心から離れるほど高さを 0 に引き下げて島形にする。
        /// </summary>
        private static void ApplyIslandMask(float[,] heightMap, WorldMapGeneratorConfig cfg)
        {
            int w  = heightMap.GetLength(0);
            int h  = heightMap.GetLength(1);
            float cx = w * 0.5f;
            float cy = h * 0.5f;

            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                // 正規化距離（対角を 1 とする）
                float nx   = (x - cx) / cx;
                float ny   = (y - cy) / cy;
                float dist = Mathf.Sqrt(nx * nx + ny * ny) / Mathf.Sqrt(2f);

                // falloffRadius 外側は海に引き下げる
                float normalized = dist / cfg.IslandFalloffRadius;
                float mask = 1f - Mathf.Pow(Mathf.Clamp01(normalized), cfg.IslandFalloffSharpness);

                heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] * mask);
            }
        }

        private static WorldTerrainType Classify(
            float height, float temp, float moist,
            WorldMapGeneratorConfig cfg)
        {
            if (height < cfg.SeaLevel)      return WorldTerrainType.DeepOcean;
            if (height < cfg.ShallowsLevel) return WorldTerrainType.ShallowWater;
            if (height < cfg.BeachLevel)    return WorldTerrainType.Beach;
            if (height >= cfg.SnowLevel)    return WorldTerrainType.SnowPeak;
            if (height >= cfg.MountainLevel) return WorldTerrainType.Mountain;

            // 陸地バイオーム：気候で分岐
            if (temp >= cfg.DesertTemperatureMin && moist < 0.40f)
                return WorldTerrainType.Desert;
            if (moist >= cfg.SwampMoistureMin)
                return WorldTerrainType.Swamp;
            if (moist >= cfg.ForestMoistureMin)
                return WorldTerrainType.Forest;

            return WorldTerrainType.Plains;
        }
    }
}
