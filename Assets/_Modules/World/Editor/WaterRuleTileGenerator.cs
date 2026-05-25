using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GuildSim.World.Editor
{
    /// <summary>
    /// punyworld の「草に囲まれた水」9パターンから Water RuleTile をコード生成する。
    /// 隣接の陸/水を判定して海岸線スプライトを自動選択するため、
    /// どんなマップでも手動配置なしで海岸線が描かれる。
    ///
    /// メニュー: GuildSim → ワールドマップ Water RuleTile 生成
    ///
    /// index 対応（タイルセット上の 4x4 ブロック）:
    ///   270 271 272 273     270=左上角 271=上辺 273=右上角
    ///   297 298 299 300     297=左辺  298=開水面 300=右辺
    ///   324 325 326 327
    ///   351 352 353 354     351=左下角 352=下辺 354=右下角
    /// ※ 見た目が合わない場合は下の index 定数を調整する。
    /// </summary>
    public static class WaterRuleTileGenerator
    {
        private const string TilesetPath = "Assets/Art/Tileset/punyworld-overworld-tileset.png";
        private const string OutPath     = "Assets/Art/Tileset/RuleTiles/RT_Water.asset";

        // ── 海岸線スプライトの slice-index（必要なら調整）──
        //   277 278 279
        //   304 305 306
        //   331 332 333
        private const int CENTER = 305; // 開水面（周囲すべて水）
        private const int N_EDGE = 278; // 上が陸
        private const int S_EDGE = 332; // 下が陸
        private const int W_EDGE = 304; // 左が陸
        private const int E_EDGE = 306; // 右が陸
        private const int NW     = 277; // 上＆左が陸
        private const int NE     = 279; // 上＆右が陸
        private const int SW     = 331; // 下＆左が陸
        private const int SE     = 333; // 下＆右が陸

        // RuleTile の隣接判定値
        private const int This    = 1;  // 同じ RuleTile（＝水）
        private const int NotThis = 2;  // それ以外（＝陸）

        private static readonly Vector3Int Up    = new(0, 1, 0);
        private static readonly Vector3Int Down  = new(0, -1, 0);
        private static readonly Vector3Int Left  = new(-1, 0, 0);
        private static readonly Vector3Int Right = new(1, 0, 0);

        [MenuItem("GuildSim/ワールドマップ Water RuleTile 生成")]
        public static void Generate()
        {
            var sprites = LoadSprites();
            if (sprites == null) return;

            var rt = ScriptableObject.CreateInstance<RuleTile>();
            rt.m_DefaultSprite       = sprites[CENTER];
            rt.m_DefaultColliderType = Tile.ColliderType.None;
            rt.m_TilingRules = new List<RuleTile.TilingRule>
            {
                // 角（陸が2方向）を先に（辺ルールより具体的）
                MakeRule(sprites[NW], up: NotThis, left: NotThis, down: This,    right: This),
                MakeRule(sprites[NE], up: NotThis, right: NotThis, down: This,   left: This),
                MakeRule(sprites[SW], down: NotThis, left: NotThis, up: This,    right: This),
                MakeRule(sprites[SE], down: NotThis, right: NotThis, up: This,   left: This),
                // 辺（陸が1方向）
                MakeRule(sprites[N_EDGE], up: NotThis, down: This, left: This, right: This),
                MakeRule(sprites[S_EDGE], down: NotThis, up: This, left: This, right: This),
                MakeRule(sprites[W_EDGE], left: NotThis, up: This, down: This, right: This),
                MakeRule(sprites[E_EDGE], right: NotThis, up: This, down: This, left: This),
                // それ以外（開水面・チャネル等）は m_DefaultSprite=CENTER
            };

            EnsureFolder("Assets/Art/Tileset/RuleTiles");
            var existing = AssetDatabase.LoadAssetAtPath<RuleTile>(OutPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(rt, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(rt, OutPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(OutPath);
            AssignToWater(ruleTile);
            RegenerateMap();

            Debug.Log($"[WaterRuleTile] 生成完了 → {OutPath}　深海/浅瀬に割当てマップ再生成しました");
        }

        private static RuleTile.TilingRule MakeRule(
            Sprite sprite, int up = 0, int down = 0, int left = 0, int right = 0)
        {
            var rule = new RuleTile.TilingRule
            {
                m_Sprites           = new[] { sprite },
                m_NeighborPositions = new List<Vector3Int>(),
                m_Neighbors         = new List<int>(),
                m_Output            = RuleTile.TilingRuleOutput.OutputSprite.Single,
                m_ColliderType      = Tile.ColliderType.None,
            };
            void Add(Vector3Int p, int n) { if (n != 0) { rule.m_NeighborPositions.Add(p); rule.m_Neighbors.Add(n); } }
            Add(Up, up); Add(Down, down); Add(Left, left); Add(Right, right);
            return rule;
        }

        private static Sprite[] LoadSprites()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TilesetPath);
            if (tex == null) { Debug.LogError($"[WaterRuleTile] タイルセットなし: {TilesetPath}"); return null; }

            var sprites = AssetDatabase.LoadAllAssetsAtPath(TilesetPath)
                .OfType<Sprite>()
                .OrderBy(s => ParseIndex(s.name))
                .ToArray();

            if (sprites.Length <= SE)
            {
                Debug.LogError($"[WaterRuleTile] スプライト数不足: {sprites.Length}");
                return null;
            }
            return sprites;
        }

        private static int ParseIndex(string name)
        {
            int u = name.LastIndexOf('_');
            return (u >= 0 && int.TryParse(name.Substring(u + 1), out var n)) ? n : int.MaxValue;
        }

        private static void AssignToWater(RuleTile ruleTile)
        {
#if UNITY_2023_1_OR_NEWER
            var painter = Object.FindFirstObjectByType<WorldMapTilemapPainter>();
#else
            var painter = Object.FindObjectOfType<WorldMapTilemapPainter>();
#endif
            if (painter == null) { Debug.LogWarning("[WaterRuleTile] Painter 未検出"); return; }

            var so = new SerializedObject(painter);
            so.FindProperty("deepOceanTile").objectReferenceValue    = ruleTile;
            so.FindProperty("shallowWaterTile").objectReferenceValue = ruleTile;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(painter);
        }

        private static void RegenerateMap()
        {
#if UNITY_2023_1_OR_NEWER
            var painter = Object.FindFirstObjectByType<WorldMapTilemapPainter>();
#else
            var painter = Object.FindObjectOfType<WorldMapTilemapPainter>();
#endif
            painter?.GenerateMap();
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
