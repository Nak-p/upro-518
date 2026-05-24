using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GuildSim.World
{
    /// <summary>
    /// WorldMapGeneratorConfig の設定をもとにタイルマップを自動描画する。
    /// Inspector から [Generate Map] を右クリック実行すれば即確認できる。
    /// </summary>
    public sealed class WorldMapTilemapPainter : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private WorldMapGeneratorConfig config;

        [Header("タイルマップ")]
        [Tooltip("地形ベースレイヤー（必須）")]
        [SerializeField] private Tilemap baseTilemap;
        [Tooltip("木・山頂などの装飾レイヤー（任意）")]
        [SerializeField] private Tilemap detailTilemap;

        [Header("地形タイル（各バイオームにアサイン）")]
        [SerializeField] private TileBase deepOceanTile;
        [SerializeField] private TileBase shallowWaterTile;
        [SerializeField] private TileBase beachTile;
        [SerializeField] private TileBase plainsTile;
        [SerializeField] private TileBase forestTile;
        [SerializeField] private TileBase desertTile;
        [SerializeField] private TileBase swampTile;
        [SerializeField] private TileBase mountainTile;
        [SerializeField] private TileBase snowPeakTile;

        [Header("装飾タイル（detailTilemap 用・省略可）")]
        [SerializeField] private TileBase forestDetailTile;
        [SerializeField] private TileBase mountainDetailTile;

        [Header("動作")]
        [SerializeField] private bool generateOnStart = true;

        /// <summary>地形ベースタイルマップ（ピンのワールド投影に使用）。</summary>
        public Tilemap BaseTilemap => baseTilemap;

        private void Start()
        {
            if (generateOnStart)
                GenerateMap();
        }

        [ContextMenu("Generate Map")]
        public void GenerateMap()
        {
            if (config == null)
            {
                Debug.LogError("[WorldMapTilemapPainter] config が未アサインです。");
                return;
            }
            if (baseTilemap == null)
            {
                Debug.LogError("[WorldMapTilemapPainter] baseTilemap が未アサインです。");
                return;
            }

            var biomeMap = WorldMapGenerator.Generate(config);
            PaintTiles(biomeMap);

            Debug.Log($"[WorldMapTilemapPainter] {config.MapWidth}x{config.MapHeight} タイルマップ生成完了 (seed={config.Seed})");
        }

        [ContextMenu("Clear Map")]
        public void ClearMap()
        {
            baseTilemap?.ClearAllTiles();
            detailTilemap?.ClearAllTiles();
        }

        [ContextMenu("Debug: タイル状態を確認")]
        public void DebugCheckTiles()
        {
            if (baseTilemap == null) { Debug.LogError("baseTilemap が null"); return; }

            baseTilemap.CompressBounds();
            var bounds = baseTilemap.cellBounds;
            Debug.Log($"[Debug] Tilemap bounds: {bounds}  (size {bounds.size.x}x{bounds.size.y})");

            int total = 0, nullCount = 0, spriteNull = 0;
            foreach (var pos in bounds.allPositionsWithin)
            {
                var tile = baseTilemap.GetTile<UnityEngine.Tilemaps.Tile>(pos);
                if (tile == null) { nullCount++; continue; }
                total++;
                if (tile.sprite == null) spriteNull++;
            }

            Debug.Log($"[Debug] 非null タイル: {total}  /  null セル: {nullCount}  /  sprite null タイル: {spriteNull}");

            // 中心付近のタイルを1つサンプリング
            var center = new Vector3Int(bounds.x + bounds.size.x / 2, bounds.y + bounds.size.y / 2, 0);
            var sample = baseTilemap.GetTile<UnityEngine.Tilemaps.Tile>(center);
            if (sample != null)
                Debug.Log($"[Debug] 中心タイル: {sample.name}  sprite={sample.sprite?.name ?? "NULL"}  spriteTexture={sample.sprite?.texture?.name ?? "NULL"}");
            else
                Debug.Log("[Debug] 中心タイル: null（タイルが置かれていない）");
        }

        // ---- private ----

        private void PaintTiles(WorldTerrainType[,] biomeMap)
        {
            int w = biomeMap.GetLength(0);
            int h = biomeMap.GetLength(1);

            baseTilemap.ClearAllTiles();
            detailTilemap?.ClearAllTiles();

            // SetTiles バッチで一括書き込み（SetTile ループより高速）
            var positions = new Vector3Int[w * h];
            var tiles      = new TileBase[w * h];
            int idx = 0;

            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                positions[idx] = new Vector3Int(x, y, 0);
                tiles[idx]     = TileForTerrain(biomeMap[x, y]);
                idx++;
            }

            baseTilemap.SetTiles(positions, tiles);

            // 装飾レイヤー
            if (detailTilemap != null)
            {
                var detailPos   = new List<Vector3Int>(w * h / 4);
                var detailTiles = new List<TileBase>(w * h / 4);

                for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    var detail = DetailTile(biomeMap[x, y]);
                    if (detail != null)
                    {
                        detailPos.Add(new Vector3Int(x, y, 0));
                        detailTiles.Add(detail);
                    }
                }

                detailTilemap.SetTiles(detailPos.ToArray(), detailTiles.ToArray());
            }

            baseTilemap.CompressBounds();
            CenterCamera();
        }

        private void CenterCamera()
        {
            var bounds = baseTilemap.cellBounds;
            var center = baseTilemap.CellToWorld(new Vector3Int(
                bounds.x + bounds.size.x / 2,
                bounds.y + bounds.size.y / 2, 0));

            var cam = GetComponentInParent<Camera>() ?? Camera.main;
            if (cam != null)
                cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);
        }

        private TileBase TileForTerrain(WorldTerrainType t) => t switch
        {
            WorldTerrainType.DeepOcean    => deepOceanTile,
            WorldTerrainType.ShallowWater => shallowWaterTile,
            WorldTerrainType.Beach        => beachTile,
            WorldTerrainType.Plains       => plainsTile,
            WorldTerrainType.Forest       => forestTile,
            WorldTerrainType.Desert       => desertTile,
            WorldTerrainType.Swamp        => swampTile,
            WorldTerrainType.Mountain     => mountainTile,
            WorldTerrainType.SnowPeak     => snowPeakTile,
            _                             => plainsTile,
        };

        private TileBase DetailTile(WorldTerrainType t) => t switch
        {
            WorldTerrainType.Forest   => forestDetailTile,
            WorldTerrainType.Mountain => mountainDetailTile,
            _                         => null,
        };
    }
}
