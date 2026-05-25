using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GuildSim.World.Editor
{
    /// <summary>
    /// punyworld タイルセットから各バイオームのタイルを「index 入力＋即プレビュー」で選び、
    /// Tile アセット生成 → Painter へアサイン → マップ再生成までを一括で行う。
    ///
    /// メニュー: GuildSim → ワールドマップ バイオームタイル選択
    ///
    /// 使い方（段階的でOK）:
    ///   1. タイルセットを確認（自動ロード）
    ///   2. 各バイオームの index を入力 → 右にプレビューが出る
    ///   3. 「適用」ボタンで、入力済みバイオームだけ Tile 生成＋アサイン＋マップ生成
    ///   （index 未入力(-1) のバイオームは現状維持。1つずつ進められる）
    /// </summary>
    public sealed class PunyworldBiomePicker : EditorWindow
    {
        private const string TilesetPath  = "Assets/Art/Tileset/punyworld-overworld-tileset.png";
        private const string OutputFolder = "Assets/Art/Tileset/PunyTiles";
        private const int    Cols         = 27;   // タイルセットの横タイル数（プレビュー位置表示用）

        private Texture2D tileset;
        private Sprite[]  sprites;                 // slice-index 順（_000.._NNN）
        private int[]     biomeIndex;              // 各バイオームの選択 index（-1=未選択）
        private Vector2   scroll;

        private static readonly string[] BiomeLabels =
        {
            "DeepOcean　深海", "ShallowWater　浅瀬", "Beach　砂浜",
            "Plains　草原",    "Forest　森",        "Desert　砂漠",
            "Swamp　沼地",     "Mountain　山脈",     "SnowPeak　雪山",
        };

        private static readonly string[] PainterFields =
        {
            "deepOceanTile", "shallowWaterTile", "beachTile",
            "plainsTile",    "forestTile",       "desertTile",
            "swampTile",     "mountainTile",     "snowPeakTile",
        };

        [MenuItem("GuildSim/ワールドマップ バイオームタイル選択")]
        public static void Open() =>
            GetWindow<PunyworldBiomePicker>("BiomePicker", true).Show();

        private void OnEnable()
        {
            biomeIndex ??= Enumerable.Repeat(-1, 9).ToArray();
            LoadTileset();
        }

        private void LoadTileset()
        {
            tileset = AssetDatabase.LoadAssetAtPath<Texture2D>(TilesetPath);
            if (tileset == null) { sprites = Array.Empty<Sprite>(); return; }

            var all = AssetDatabase.LoadAllAssetsAtPath(TilesetPath);
            // 名前末尾の数値（slice-index）順に並べる（_999 と _1000 の文字列ソート問題を回避）
            sprites = all.OfType<Sprite>()
                         .OrderBy(s => ParseIndex(s.name))
                         .ToArray();
        }

        private static int ParseIndex(string name)
        {
            int u = name.LastIndexOf('_');
            return (u >= 0 && int.TryParse(name.Substring(u + 1), out var n)) ? n : int.MaxValue;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("punyworld バイオームタイル選択", EditorStyles.boldLabel);

            tileset = (Texture2D)EditorGUILayout.ObjectField("タイルセット", tileset, typeof(Texture2D), false);
            if (GUILayout.Button("タイルセット再読み込み")) LoadTileset();

            int count = sprites?.Length ?? 0;
            EditorGUILayout.HelpBox(
                $"スプライト数: {count}　（横 {Cols} 列）\n" +
                "index を入力すると右にプレビュー表示。位置 = 行 index/27, 列 index%27（行0=最上段）\n" +
                "目安: 草原/砂 0〜110 ／ 森 190〜270 ／ 水 270〜600\n" +
                "未入力(-1)のバイオームは現状維持。1つずつ進められます。",
                MessageType.Info);

            if (count == 0)
            {
                EditorGUILayout.HelpBox("タイルセットが読み込めません。パスを確認してください。", MessageType.Error);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < BiomeLabels.Length; i++)
                DrawBiomeRow(i, count);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            if (GUILayout.Button("適用（タイル生成 ＋ アサイン ＋ マップ生成）", GUILayout.Height(30)))
                ApplySelected();
        }

        private void DrawBiomeRow(int i, int count)
        {
            EditorGUILayout.BeginHorizontal("box");

            // ラベル
            EditorGUILayout.LabelField(BiomeLabels[i], GUILayout.Width(140));

            // index 入力
            int idx = EditorGUILayout.IntField(biomeIndex[i], GUILayout.Width(60));
            biomeIndex[i] = Mathf.Clamp(idx, -1, count - 1);

            // 位置表示
            if (biomeIndex[i] >= 0)
                EditorGUILayout.LabelField($"行{biomeIndex[i] / Cols} 列{biomeIndex[i] % Cols}", GUILayout.Width(80));
            else
                EditorGUILayout.LabelField("(未選択)", GUILayout.Width(80));

            // プレビュー
            var rect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
            if (biomeIndex[i] >= 0 && biomeIndex[i] < count)
            {
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
                DrawSpritePreview(rect, sprites[biomeIndex[i]]);
            }
            else
            {
                EditorGUI.DrawRect(rect, new Color(0.25f, 0.1f, 0.1f));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ApplySelected()
        {
            EnsureFolder(OutputFolder);

            var typeNames = Enum.GetNames(typeof(WorldTerrainType));
            var tiles = new Tile[BiomeLabels.Length];
            int created = 0;

            for (int i = 0; i < biomeIndex.Length; i++)
            {
                if (biomeIndex[i] < 0 || biomeIndex[i] >= sprites.Length) continue;

                var sprite   = sprites[biomeIndex[i]];
                var tilePath = $"{OutputFolder}/P_{typeNames[i]}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite       = sprite;
                tile.color        = Color.white;
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tiles[i] = tile;
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int assigned = AssignToPainter(tiles);
            RegenerateMap();

            Debug.Log($"[BiomePicker] {created} 種の punyworld タイルを生成 → {assigned} 種を Painter にアサイン → マップ再生成完了");
        }

        private int AssignToPainter(Tile[] tiles)
        {
#if UNITY_2023_1_OR_NEWER
            var painter = FindFirstObjectByType<WorldMapTilemapPainter>();
#else
            var painter = FindObjectOfType<WorldMapTilemapPainter>();
#endif
            if (painter == null)
            {
                EditorUtility.DisplayDialog("未検出", "シーンに WorldMapTilemapPainter が見つかりません。", "OK");
                return 0;
            }

            var so = new SerializedObject(painter);
            int assigned = 0;
            for (int i = 0; i < PainterFields.Length && i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                var prop = so.FindProperty(PainterFields[i]);
                if (prop == null) continue;
                prop.objectReferenceValue = tiles[i];
                assigned++;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(painter);
            return assigned;
        }

        private void RegenerateMap()
        {
#if UNITY_2023_1_OR_NEWER
            var painter = FindFirstObjectByType<WorldMapTilemapPainter>();
#else
            var painter = FindObjectOfType<WorldMapTilemapPainter>();
#endif
            painter?.GenerateMap();
        }

        private static void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            var tex = sprite.texture;
            var tr  = sprite.textureRect;
            var uv  = new Rect(tr.x / tex.width, tr.y / tex.height,
                               tr.width / tex.width, tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
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
