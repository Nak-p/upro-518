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

            SmoothCoastline(result, cfg);

            return result;
        }

        // ---- 海岸線スムージング（オートタイル向け整形）----

        private static bool IsWater(WorldTerrainType t) =>
            t == WorldTerrainType.DeepOcean || t == WorldTerrainType.ShallowWater;

        /// <summary>
        /// 水/陸マスクを平滑化して、カーディナル4方向だけのオートタイルでも
        /// 破綻しない海岸線（斜めだけの接続・細い半島・孤立セルが無い）に整える。
        /// 仕上げに水深（深海/浅瀬）と砂浜を隣接から再判定する。
        /// </summary>
        private static void SmoothCoastline(WorldTerrainType[,] map, WorldMapGeneratorConfig cfg)
        {
            int iterations = cfg.CoastSmoothingIterations;
            if (iterations <= 0) return;

            int w = map.GetLength(0);
            int h = map.GetLength(1);

            var water = new bool[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                water[x, y] = IsWater(map[x, y]);

            for (int it = 0; it < iterations; it++)
                water = SmoothStep(water);

            // 市松模様（斜めのみ接続）を解消（複数パス）
            BreakDiagonals(water);
            BreakDiagonals(water);

            // バイオーム再構築
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (water[x, y])
                {
                    map[x, y] = HasCardinalLand(water, x, y)
                        ? WorldTerrainType.ShallowWater   // 海岸リング
                        : WorldTerrainType.DeepOcean;     // 内部
                }
                else
                {
                    var orig = map[x, y];
                    if (IsWater(orig) || HasCardinalWater(water, x, y))
                        map[x, y] = WorldTerrainType.Beach;  // 新規陸 or 海沿いの陸 → 砂浜
                    // それ以外は元の陸バイオームを維持
                }
            }
        }

        /// <summary>セルラーオートマトン1ステップ。範囲外は海（島を海で囲む）。</summary>
        private static bool[,] SmoothStep(bool[,] water)
        {
            int w = water.GetLength(0);
            int h = water.GetLength(1);
            var next = new bool[w, h];

            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                int c = CountWaterNeighbors(water, x, y);
                next[x, y] = c > 4 ? true : c < 4 ? false : water[x, y];
            }
            return next;
        }

        private static int CountWaterNeighbors(bool[,] water, int x, int y)
        {
            int w = water.GetLength(0);
            int h = water.GetLength(1);
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) count++;       // 範囲外=海
                else if (water[nx, ny]) count++;
            }
            return count;
        }

        /// <summary>2x2 が市松（対角同士が同じで隣が違う）なら1セル反転して解消。</summary>
        private static void BreakDiagonals(bool[,] water)
        {
            int w = water.GetLength(0);
            int h = water.GetLength(1);
            for (int x = 0; x < w - 1; x++)
            for (int y = 0; y < h - 1; y++)
            {
                bool a = water[x, y], b = water[x + 1, y];
                bool c = water[x, y + 1], d = water[x + 1, y + 1];
                if (a == d && b == c && a != b)
                    water[x + 1, y + 1] = c;   // 対角を崩す
            }
        }

        private static bool HasCardinalLand(bool[,] water, int x, int y) =>
            !NeighborIsWater(water, x, y, 0, 1) || !NeighborIsWater(water, x, y, 0, -1) ||
            !NeighborIsWater(water, x, y, 1, 0) || !NeighborIsWater(water, x, y, -1, 0);

        private static bool HasCardinalWater(bool[,] water, int x, int y) =>
            NeighborIsWater(water, x, y, 0, 1) || NeighborIsWater(water, x, y, 0, -1) ||
            NeighborIsWater(water, x, y, 1, 0) || NeighborIsWater(water, x, y, -1, 0);

        /// <summary>範囲外は海として扱う。</summary>
        private static bool NeighborIsWater(bool[,] water, int x, int y, int dx, int dy)
        {
            int w = water.GetLength(0);
            int h = water.GetLength(1);
            int nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= w || ny >= h) return true;
            return water[nx, ny];
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
