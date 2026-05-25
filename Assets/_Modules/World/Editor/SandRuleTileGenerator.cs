using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GuildSim.World.Editor
{
    /// <summary>
    /// punyworld の「草に囲まれた砂」3x3 から Sand RuleTile（TerrainRuleTile）を生成する。
    /// 水(RT_Water)を sibling 登録するので、砂浜は水側には縁取らず草側だけ縁取る。
    ///
    /// メニュー: GuildSim → ワールドマップ Sand RuleTile 生成
    ///
    ///   10 11 12
    ///   37 38 39      38=砂ベタ塗り（中央）
    ///   64 65 66
    /// </summary>
    public static class SandRuleTileGenerator
    {
        private const string TilesetPath = "Assets/Art/Tileset/punyworld-overworld-tileset.png";
        private const string WaterPath   = "Assets/Art/Tileset/RuleTiles/RT_Water.asset";
        private const string OutPath     = "Assets/Art/Tileset/RuleTiles/RT_Sand.asset";

        // 砂⇄草 遷移スプライトの slice-index
        private const int CENTER = 38;
        private const int N_EDGE = 11;
        private const int S_EDGE = 65;
        private const int W_EDGE = 37;
        private const int E_EDGE = 39;
        private const int NW     = 10;
        private const int NE     = 12;
        private const int SW     = 64;
        private const int SE     = 66;

        private const int This    = 1;
        private const int NotThis = 2;

        private static readonly Vector3Int Up    = new(0, 1, 0);
        private static readonly Vector3Int Down  = new(0, -1, 0);
        private static readonly Vector3Int Left  = new(-1, 0, 0);
        private static readonly Vector3Int Right = new(1, 0, 0);

        [MenuItem("GuildSim/ワールドマップ Sand RuleTile 生成")]
        public static void Generate()
        {
            var sprites = LoadSprites();
            if (sprites == null) return;

            var rt = ScriptableObject.CreateInstance<TerrainRuleTile>();
            rt.m_DefaultSprite       = sprites[CENTER];
            rt.m_DefaultColliderType = Tile.ColliderType.None;

            // 水を仲間にして、水側には縁取りを出さない
            var water = AssetDatabase.LoadAssetAtPath<TileBase>(WaterPath);
            rt.siblings = new List<TileBase>();
            if (water != null) rt.siblings.Add(water);
            else Debug.LogWarning("[SandRuleTile] RT_Water が見つかりません。先に Water RuleTile を生成してください。");

            rt.m_TilingRules = new List<RuleTile.TilingRule>
            {
                MakeRule(sprites[NW], up: NotThis, left: NotThis, down: This,  right: This),
                MakeRule(sprites[NE], up: NotThis, right: NotThis, down: This, left: This),
                MakeRule(sprites[SW], down: NotThis, left: NotThis, up: This,  right: This),
                MakeRule(sprites[SE], down: NotThis, right: NotThis, up: This, left: This),
                MakeRule(sprites[N_EDGE], up: NotThis, down: This, left: This, right: This),
                MakeRule(sprites[S_EDGE], down: NotThis, up: This, left: This, right: This),
                MakeRule(sprites[W_EDGE], left: NotThis, up: This, down: This, right: This),
                MakeRule(sprites[E_EDGE], right: NotThis, up: This, down: This, left: This),
            };

            EnsureFolder("Assets/Art/Tileset/RuleTiles");
            var existing = AssetDatabase.LoadAssetAtPath<TerrainRuleTile>(OutPath);
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

            var ruleTile = AssetDatabase.LoadAssetAtPath<TerrainRuleTile>(OutPath);
            AssignToBeach(ruleTile);
            RegenerateMap();

            Debug.Log($"[SandRuleTile] 生成完了 → {OutPath}　砂浜に割当てマップ再生成（水は sibling=縁取りなし）");
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
            if (tex == null) { Debug.LogError($"[SandRuleTile] タイルセットなし: {TilesetPath}"); return null; }

            var sprites = AssetDatabase.LoadAllAssetsAtPath(TilesetPath)
                .OfType<Sprite>()
                .OrderBy(s => ParseIndex(s.name))
                .ToArray();

            if (sprites.Length <= SE) { Debug.LogError("[SandRuleTile] スプライト数不足"); return null; }
            return sprites;
        }

        private static int ParseIndex(string name)
        {
            int u = name.LastIndexOf('_');
            return (u >= 0 && int.TryParse(name.Substring(u + 1), out var n)) ? n : int.MaxValue;
        }

        private static void AssignToBeach(TerrainRuleTile ruleTile)
        {
#if UNITY_2023_1_OR_NEWER
            var painter = Object.FindFirstObjectByType<WorldMapTilemapPainter>();
#else
            var painter = Object.FindObjectOfType<WorldMapTilemapPainter>();
#endif
            if (painter == null) { Debug.LogWarning("[SandRuleTile] Painter 未検出"); return; }

            var so = new SerializedObject(painter);
            so.FindProperty("beachTile").objectReferenceValue = ruleTile;
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
