using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GuildSim.World.Editor
{
    /// <summary>
    /// メニュー: GuildSim → ワールドマップ タイルセットウィザード
    /// 手順: ① スライス → ② スプライトをバイオームに割り当て → ③ Tile 生成 → ④ 自動アサイン
    /// </summary>
    public sealed class WorldMapTileSetupWizard : EditorWindow
    {
        // ── Step 1
        private Texture2D sourceTexture;
        private int cellWidth  = 48;
        private int cellHeight = 48;

        // ── Step 2
        private Vector2 spriteScroll;
        private Sprite[] allSprites;
        private const float IconSize = 48f;
        private const int   GridCols = 10;

        // ── Step 3
        private readonly Sprite[] biomeSprites = new Sprite[9];

        // ── Step 4
        private string outputFolder = "Assets/Art/Tileset/Tiles";
        private readonly TileBase[] createdTiles = new TileBase[9];

        private Vector2 mainScroll;

        // ── enum 名と Painter フィールド名の対応（WorldTerrainType の順序に合わせる）
        private static readonly string[] BiomeLabels =
        {
            "DeepOcean　深海",
            "ShallowWater　浅瀬",
            "Beach　砂浜",
            "Plains　草原",
            "Forest　森",
            "Desert　砂漠",
            "Swamp　沼地",
            "Mountain　山脈",
            "SnowPeak　雪山",
        };

        private static readonly string[] PainterFields =
        {
            "deepOceanTile", "shallowWaterTile", "beachTile",
            "plainsTile",    "forestTile",       "desertTile",
            "swampTile",     "mountainTile",     "snowPeakTile",
        };

        // ────────────────────────────────────────────

        [MenuItem("GuildSim/ワールドマップ タイルセットウィザード")]
        public static void Open() =>
            GetWindow<WorldMapTileSetupWizard>("TileWizard", true).Show();

        private void OnGUI()
        {
            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

            DrawStep1();
            EditorGUILayout.Space(10);
            DrawStep2();
            EditorGUILayout.Space(10);
            DrawStep3();
            EditorGUILayout.Space(10);
            DrawStep4();

            EditorGUILayout.EndScrollView();
        }

        // ────────────── Step 1: インポート設定 & スライス ──────────────

        private void DrawStep1()
        {
            SectionLabel("Step 1 ── インポート設定 & グリッドスライス");

            sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
                "タイルセット PNG", sourceTexture, typeof(Texture2D), false);
            cellWidth  = EditorGUILayout.IntField("タイル幅 (px)",  cellWidth);
            cellHeight = EditorGUILayout.IntField("タイル高さ (px)", cellHeight);

            if (sourceTexture != null)
            {
                int cols = sourceTexture.width  / cellWidth;
                int rows = sourceTexture.height / cellHeight;
                EditorGUILayout.HelpBox(
                    $"画像サイズ {sourceTexture.width}×{sourceTexture.height}px → {cols}×{rows} = {cols * rows} スプライト",
                    MessageType.None);
            }

            using (new EditorGUI.DisabledScope(sourceTexture == null))
            {
                if (GUILayout.Button("① インポート設定を適用してスライス", GUILayout.Height(28)))
                {
                    SliceTexture();
                    RefreshSpriteList();
                }
            }
        }

        // ────────────── Step 2: スプライトグリッド ──────────────

        private void DrawStep2()
        {
            SectionLabel("Step 2 ── スプライト確認（クリックで Project ハイライト）");

            if (allSprites == null || allSprites.Length == 0)
            {
                if (sourceTexture != null &&
                    GUILayout.Button("スプライト一覧を再読み込み"))
                    RefreshSpriteList();

                EditorGUILayout.HelpBox(
                    "Step 1 でスライス後、または既スライス済み PNG を選択してボタンを押してください",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"{allSprites.Length} 枚を検出");

            spriteScroll = EditorGUILayout.BeginScrollView(
                spriteScroll, GUILayout.Height(Mathf.Min(300, IconSize * 2 + 60)));

            for (int i = 0; i < allSprites.Length; i += GridCols)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + GridCols, allSprites.Length); j++)
                    DrawSpriteButton(allSprites[j], j);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSpriteButton(Sprite sprite, int index)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(IconSize + 2));

            // スプライトプレビューボタン
            var rect = GUILayoutUtility.GetRect(
                IconSize, IconSize, GUILayout.Width(IconSize), GUILayout.Height(IconSize));

            if (GUI.Button(rect, GUIContent.none, "box"))
                EditorGUIUtility.PingObject(sprite);

            DrawSpritePreview(rect, sprite);

            // インデックスラベル
            EditorGUILayout.LabelField(
                index.ToString(),
                EditorStyles.centeredGreyMiniLabel,
                GUILayout.Width(IconSize + 2));

            EditorGUILayout.EndVertical();
        }

        // ────────────── Step 3: バイオーム割り当て ──────────────

        private void DrawStep3()
        {
            SectionLabel("Step 3 ── バイオームにスプライトを割り当て");
            EditorGUILayout.HelpBox(
                "Project ウィンドウでタイルセットを ▶ 展開し、各スプライトをドラッグしてください。\n" +
                "Step 2 でインデックスを確認して探すと早いです。",
                MessageType.Info);

            for (int i = 0; i < BiomeLabels.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // 小プレビュー
                var previewRect = GUILayoutUtility.GetRect(
                    32, 32, GUILayout.Width(32), GUILayout.Height(32));
                if (biomeSprites[i] != null)
                    DrawSpritePreview(previewRect, biomeSprites[i]);
                else
                    EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));

                biomeSprites[i] = (Sprite)EditorGUILayout.ObjectField(
                    BiomeLabels[i], biomeSprites[i], typeof(Sprite), false);

                EditorGUILayout.EndHorizontal();
            }
        }

        // ────────────── Step 4: Tile 生成 & 自動アサイン ──────────────

        private void DrawStep4()
        {
            SectionLabel("Step 4 ── Tile アセット生成 & 自動アサイン");

            outputFolder = EditorGUILayout.TextField("出力フォルダ", outputFolder);

            if (GUILayout.Button("② Tile アセットを一括生成", GUILayout.Height(28)))
                CreateTileAssets();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("③ シーンの WorldMapTilemapPainter に自動アサイン", GUILayout.Height(28)))
                AutoAssignToPainter();
        }

        // ────────────── ロジック ──────────────

        private void SliceTexture()
        {
            var path     = AssetDatabase.GetAssetPath(sourceTexture);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);

            importer.textureType         = TextureImporterType.Sprite;
            importer.filterMode          = FilterMode.Point;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = cellWidth;
            importer.spriteImportMode    = SpriteImportMode.Multiple;

            int cols = sourceTexture.width  / cellWidth;
            int rows = sourceTexture.height / cellHeight;

#pragma warning disable CS0618
            var metas = new List<SpriteMetaData>(cols * rows);
            for (int row = rows - 1; row >= 0; row--)
            for (int col = 0; col < cols; col++)
            {
                metas.Add(new SpriteMetaData
                {
                    name      = $"{sourceTexture.name}_{metas.Count:D3}",
                    rect      = new Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight),
                    pivot     = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center,
                });
            }
            importer.spritesheet = metas.ToArray();
#pragma warning restore CS0618

            importer.SaveAndReimport();
            Debug.Log($"[TileWizard] {cols}×{rows} = {metas.Count} スプライトにスライス完了");
        }

        private void RefreshSpriteList()
        {
            if (sourceTexture == null) { allSprites = null; return; }

            var path   = AssetDatabase.GetAssetPath(sourceTexture);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var list   = new List<Sprite>();
            foreach (var a in assets)
                if (a is Sprite s) list.Add(s);

            // 名前順でソートしてインデックスが安定するようにする
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            allSprites = list.ToArray();
            Repaint();
        }

        private void CreateTileAssets()
        {
            EnsureFolder(outputFolder);

            var typeNames = System.Enum.GetNames(typeof(WorldTerrainType));
            int count = 0;

            for (int i = 0; i < biomeSprites.Length; i++)
            {
                if (biomeSprites[i] == null) continue;

                var assetPath = $"{outputFolder}/T_{typeNames[i]}.asset";

                // 既存アセットを上書き
                var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                Tile tile;
                if (existing != null)
                {
                    tile = existing;
                }
                else
                {
                    tile = CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, assetPath);
                }

                tile.sprite = biomeSprites[i];
                EditorUtility.SetDirty(tile);
                createdTiles[i] = tile;
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TileWizard] {count} 件の Tile アセットを生成 → {outputFolder}");
        }

        private void AutoAssignToPainter()
        {
#if UNITY_2023_1_OR_NEWER
            var painter = FindFirstObjectByType<WorldMapTilemapPainter>();
#else
            var painter = FindObjectOfType<WorldMapTilemapPainter>();
#endif
            if (painter == null)
            {
                EditorUtility.DisplayDialog(
                    "未検出",
                    "シーンに WorldMapTilemapPainter が見つかりません。\nシーンに追加してから実行してください。",
                    "OK");
                return;
            }

            var so = new SerializedObject(painter);
            int assigned = 0;

            for (int i = 0; i < PainterFields.Length; i++)
            {
                if (createdTiles[i] == null) continue;
                var prop = so.FindProperty(PainterFields[i]);
                if (prop == null) continue;
                prop.objectReferenceValue = createdTiles[i];
                assigned++;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(painter);
            Debug.Log($"[TileWizard] {assigned} 件を WorldMapTilemapPainter にアサインしました");
        }

        // ────────────── ユーティリティ ──────────────

        private static void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            var tex = sprite.texture;
            var tr  = sprite.textureRect;
            var uv  = new Rect(
                tr.x      / tex.width,
                tr.y      / tex.height,
                tr.width  / tex.width,
                tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
        }

        private static void SectionLabel(string text)
        {
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
            var r = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(4);
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
