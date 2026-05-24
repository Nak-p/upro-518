using GuildSim.Shared;
using UnityEngine;

namespace GuildSim.World
{
    [CreateAssetMenu(menuName = "GuildSim/World/Map Generator Config", fileName = "WorldMapGeneratorConfig")]
    public sealed class WorldMapGeneratorConfig : BaseConfig
    {
        [Header("マップサイズ")]
        [SerializeField] private int mapWidth  = 128;
        [SerializeField] private int mapHeight = 128;
        [SerializeField] private int seed      = 42;

        [Header("高さノイズ（地形の骨格）")]
        [SerializeField] private float heightScale       = 0.04f;
        [SerializeField] private int   heightOctaves     = 5;
        [SerializeField] private float heightPersistence = 0.5f;
        [SerializeField] private float heightLacunarity  = 2.0f;

        [Header("島マスク（中心から離れると海になる）")]
        [Tooltip("1.0=全面陸, 0.5=小島")]
        [SerializeField] private float islandFalloffRadius   = 0.85f;
        [Tooltip("値が大きいほど海岸線がシャープ")]
        [SerializeField] private float islandFalloffSharpness = 3.0f;

        [Header("気候ノイズ")]
        [SerializeField] private float temperatureScale = 0.030f;
        [SerializeField] private float moistureScale    = 0.045f;

        [Header("バイオーム閾値（高さ 0–1）")]
        [SerializeField] private float seaLevel      = 0.38f;
        [SerializeField] private float shallowsLevel = 0.43f;
        [SerializeField] private float beachLevel    = 0.47f;
        [SerializeField] private float mountainLevel = 0.72f;
        [SerializeField] private float snowLevel     = 0.85f;

        [Header("バイオーム閾値（気候）")]
        [Tooltip("この気温以上 かつ 湿度低い → 砂漠")]
        [SerializeField] private float desertTemperatureMin = 0.62f;
        [Tooltip("この湿度以上 → 沼地")]
        [SerializeField] private float swampMoistureMin    = 0.72f;
        [Tooltip("この湿度以上 → 森")]
        [SerializeField] private float forestMoistureMin   = 0.50f;

        // ---- Properties ----
        public int   MapWidth    => Mathf.Max(8, mapWidth);
        public int   MapHeight   => Mathf.Max(8, mapHeight);
        public int   Seed        => seed;

        public float HeightScale       => Mathf.Max(0.001f, heightScale);
        public int   HeightOctaves     => Mathf.Max(1, heightOctaves);
        public float HeightPersistence => Mathf.Clamp01(heightPersistence);
        public float HeightLacunarity  => Mathf.Max(1f, heightLacunarity);

        public float IslandFalloffRadius    => Mathf.Clamp(islandFalloffRadius, 0.1f, 2f);
        public float IslandFalloffSharpness => Mathf.Max(0.5f, islandFalloffSharpness);

        public float TemperatureScale => Mathf.Max(0.001f, temperatureScale);
        public float MoistureScale    => Mathf.Max(0.001f, moistureScale);

        public float SeaLevel      => seaLevel;
        public float ShallowsLevel => shallowsLevel;
        public float BeachLevel    => beachLevel;
        public float MountainLevel => mountainLevel;
        public float SnowLevel     => snowLevel;

        public float DesertTemperatureMin => desertTemperatureMin;
        public float SwampMoistureMin     => swampMoistureMin;
        public float ForestMoistureMin    => forestMoistureMin;
    }
}
