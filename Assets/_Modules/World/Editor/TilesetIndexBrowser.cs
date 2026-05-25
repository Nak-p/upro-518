using UnityEditor;
using UnityEngine;

namespace GuildSim.World.Editor
{
    /// <summary>
    /// タイルセットを「slice-index 付きグリッド」で表示する確認用ブラウザ。
    /// 各セルに index を重ねて表示するので、海岸線などの自動タイル配置を
    /// 正確に特定できる（RuleTile のルール作成に使用）。
    ///
    /// メニュー: GuildSim → ワールドマップ タイルセットIndexブラウザ
    /// index = 行 * 27 + 列（行0 = 最上段）
    /// </summary>
    public sealed class TilesetIndexBrowser : EditorWindow
    {
        private const string TilesetPath = "Assets/Art/Tileset/punyworld-overworld-tileset.png";
        private const int    Cols        = 27;
        private const int    CellPx      = 16;

        private Texture2D tex;
        private int       rows;
        private float     zoom = 2.5f;
        private Vector2   scroll;

        [MenuItem("GuildSim/ワールドマップ タイルセットIndexブラウザ")]
        public static void Open() =>
            GetWindow<TilesetIndexBrowser>("TilesetIndex", true).Show();

        private void OnEnable()
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TilesetPath);
            if (tex != null) rows = tex.height / CellPx;
        }

        private void OnGUI()
        {
            zoom = EditorGUILayout.Slider("ズーム", zoom, 1f, 6f);
            EditorGUILayout.LabelField($"{Cols} 列 × {rows} 行　index = 行×{Cols}+列（行0=最上段）",
                EditorStyles.miniLabel);

            if (tex == null)
            {
                EditorGUILayout.HelpBox($"タイルセットが見つかりません: {TilesetPath}", MessageType.Error);
                return;
            }

            float cell = CellPx * zoom;
            float w = Cols * cell;
            float h = rows * cell;

            scroll = EditorGUILayout.BeginScrollView(scroll);
            var area = GUILayoutUtility.GetRect(w, h);

            GUI.DrawTexture(area, tex, ScaleMode.StretchToFill, true);

            // グリッド線
            var line = new Color(0f, 0f, 0f, 0.35f);
            for (int c = 0; c <= Cols; c++)
                EditorGUI.DrawRect(new Rect(area.x + c * cell, area.y, 1, h), line);
            for (int r = 0; r <= rows; r++)
                EditorGUI.DrawRect(new Rect(area.x, area.y + r * cell, w, 1), line);

            // index ラベル（表示範囲のみ描画して軽量化）
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = Mathf.Max(7, (int)(zoom * 3.2f)),
                normal   = { textColor = Color.white },
                alignment = TextAnchor.UpperLeft,
            };
            var shadow = new GUIStyle(style) { normal = { textColor = Color.black } };

            float viewTop    = scroll.y;
            float viewBottom = scroll.y + (position.height - 60f);

            for (int r = 0; r < rows; r++)
            {
                float cy = r * cell;
                if (cy + cell < viewTop || cy > viewBottom) continue;   // 画面外スキップ

                for (int c = 0; c < Cols; c++)
                {
                    int index = r * Cols + c;
                    var lr = new Rect(area.x + c * cell + 1, area.y + cy + 1, cell, cell);
                    GUI.Label(new Rect(lr.x + 1, lr.y + 1, cell, cell), index.ToString(), shadow);
                    GUI.Label(lr, index.ToString(), style);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
