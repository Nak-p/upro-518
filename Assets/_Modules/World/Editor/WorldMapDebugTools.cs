using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GuildSim.World.Editor
{
    /// <summary>
    /// ワールドマップの描画パイプラインを「基本から」検証するためのデバッグツール群。
    /// 外部タイルセットに依存せず、コードで単色テクスチャを生成する。
    ///
    /// メニュー: GuildSim → デバッグ
    ///   ① カメラを2Dマップ用に設定   … カメラ設定を一発で正常化
    ///   ② 単色スプライトを1枚表示     … 最小レンダリングテスト（赤い四角）
    ///   ③ 単色タイルを生成してアサイン … 9バイオーム分のベタ塗りタイルを生成
    /// </summary>
    public static class WorldMapDebugTools
    {
        private const string OutputFolder = "Assets/Art/Tileset/SolidTiles";
        private const int    TileSizePx   = 32;

        // WorldTerrainType の並び順に対応する色（見分けやすい配色）
        private static readonly Color[] BiomeColors =
        {
            new Color(0.05f, 0.15f, 0.45f), // DeepOcean    濃紺
            new Color(0.25f, 0.55f, 0.85f), // ShallowWater 水色
            new Color(0.92f, 0.86f, 0.55f), // Beach        砂
            new Color(0.45f, 0.75f, 0.35f), // Plains       草原
            new Color(0.15f, 0.45f, 0.18f), // Forest       森（濃緑）
            new Color(0.90f, 0.78f, 0.35f), // Desert       砂漠（黄）
            new Color(0.40f, 0.45f, 0.28f), // Swamp        沼（オリーブ）
            new Color(0.55f, 0.50f, 0.45f), // Mountain     山（灰茶）
            new Color(0.97f, 0.97f, 1.00f), // SnowPeak     雪（白）
        };

        private static readonly string[] PainterFields =
        {
            "deepOceanTile", "shallowWaterTile", "beachTile",
            "plainsTile",    "forestTile",       "desertTile",
            "swampTile",     "mountainTile",     "snowPeakTile",
        };

        // ────────────── ① カメラ設定 ──────────────

        [MenuItem("GuildSim/デバッグ/① カメラを2Dマップ用に設定", priority = 100)]
        public static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("未検出",
                    "MainCamera タグのカメラが見つかりません。\n" +
                    "カメラに 'MainCamera' タグを付けてから実行してください。", "OK");
                return;
            }

            Undo.RecordObject(cam, "Setup 2D Camera");
            Undo.RecordObject(cam.transform, "Setup 2D Camera");

            cam.orthographic     = true;
            cam.orthographicSize = 70f;                                   // 128マップ全体が見える程度
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.08f, 0.08f, 0.10f);        // タイルと区別できる暗色
            cam.nearClipPlane    = 0.1f;
            cam.cullingMask      = ~0;                                    // Everything

            var p = cam.transform.position;
            cam.transform.position = new Vector3(p.x, p.y, -10f);         // ★Z=-10 が最重要

            EditorUtility.SetDirty(cam);
            Debug.Log("[DebugTools] MainCamera を 2D マップ用に設定しました " +
                      "(Orthographic / Size=70 / Z=-10 / SolidColor)");
        }

        // ────────────── ② 単色スプライト1枚（最小テスト）──────────────

        [MenuItem("GuildSim/デバッグ/② 単色スプライトを1枚表示（最小テスト）", priority = 101)]
        public static void SpawnSingleSprite()
        {
            // 赤い 32x32 スプライトを生成
            var sprite = CreateSolidSprite("DEBUG_Red", Color.red);

            var go = new GameObject("DEBUG_SingleSprite");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color  = Color.white;
            go.transform.position = new Vector3(0f, 0f, 0f);
            go.transform.localScale = Vector3.one * 5f;                   // 大きく表示

            Undo.RegisterCreatedObjectUndo(go, "Spawn Debug Sprite");
            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("[DebugTools] 原点(0,0,0) に赤い四角を生成しました。\n" +
                      "→ これが Game ビューに赤く映ればパイプラインは正常です。\n" +
                      "→ 映らない場合はカメラ設定（①を実行）を確認してください。");
        }

        // ────────────── ③ 単色タイル生成＆アサイン ──────────────

        [MenuItem("GuildSim/デバッグ/③ 単色タイルを生成してアサイン", priority = 102)]
        public static void GenerateSolidTiles()
        {
            EnsureFolder(OutputFolder);

            var typeNames = System.Enum.GetNames(typeof(WorldTerrainType));
            var tiles = new Tile[BiomeColors.Length];

            for (int i = 0; i < BiomeColors.Length; i++)
                tiles[i] = CreateSolidTile(typeNames[i], BiomeColors[i]);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int assigned = AssignToPainter(tiles);

            Debug.Log($"[DebugTools] 単色タイル {tiles.Length} 個を生成 → {OutputFolder}\n" +
                      $"WorldMapTilemapPainter に {assigned} 個アサインしました。\n" +
                      "→ 次に Grid を右クリック → Generate Map を実行してください。");
        }

        // ────────────── 内部処理 ──────────────

        /// <summary>単色 PNG を生成しインポートして Sprite を返す。</summary>
        private static Sprite CreateSolidSprite(string name, Color color)
        {
            EnsureFolder(OutputFolder);
            var pngPath = $"{OutputFolder}/Tex_{name}.png";
            WriteSolidPng(pngPath, color);
            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        /// <summary>単色 Sprite から Tile アセットを生成（または上書き）して返す。</summary>
        private static Tile CreateSolidTile(string name, Color color)
        {
            var pngPath = $"{OutputFolder}/Tex_{name}.png";
            WriteSolidPng(pngPath, color);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);

            var tilePath = $"{OutputFolder}/S_{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite       = sprite;
            tile.color        = Color.white;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>単色テクスチャを PNG としてディスクに書き出し、Sprite としてインポートする。</summary>
        private static void WriteSolidPng(string pngPath, Color color)
        {
            var tex = new Texture2D(TileSizePx, TileSizePx, TextureFormat.RGBA32, false);
            var pixels = new Color[TileSizePx * TileSizePx];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            System.IO.File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.filterMode          = FilterMode.Point;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = TileSizePx;          // 32px=1unit → Grid 1セルにぴったり
            importer.mipmapEnabled       = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static int AssignToPainter(Tile[] tiles)
        {
#if UNITY_2023_1_OR_NEWER
            var painter = Object.FindFirstObjectByType<WorldMapTilemapPainter>();
#else
            var painter = Object.FindObjectOfType<WorldMapTilemapPainter>();
#endif
            if (painter == null)
            {
                EditorUtility.DisplayDialog("未検出",
                    "シーンに WorldMapTilemapPainter が見つかりません。", "OK");
                return 0;
            }

            var so = new SerializedObject(painter);
            int assigned = 0;
            for (int i = 0; i < PainterFields.Length && i < tiles.Length; i++)
            {
                var prop = so.FindProperty(PainterFields[i]);
                if (prop == null || tiles[i] == null) continue;
                prop.objectReferenceValue = tiles[i];
                assigned++;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(painter);
            return assigned;
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
