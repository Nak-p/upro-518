using UnityEngine;
using UnityEngine.UIElements;

namespace GuildSim.World
{
    // Painter2D の描画タイミング問題を避けるため、
    // VisualElement の背景色 + border-radius でブロブ地形を生成する。
    internal static class WorldMapTerrainDrawer
    {
        private const string ZoneClass = "terrain-zone";

        internal static void Register(VisualElement canvas)
        {
            if (canvas == null) return;

            // 海（キャンバス背景色）
            canvas.style.backgroundColor = new Color(0.08f, 0.28f, 0.52f);

            // 大陸（草原ベース）
            AddZone(canvas,
                x: 4f,  y: 7f,  w: 73f, h: 84f,
                color: new Color(0.36f, 0.60f, 0.22f),
                tl: 24f, tr: 20f, bl: 16f, br: 14f);

            // 森（大陸北西）y=40% 以下に出ないようにして GoblinHunt(20%, 45%)を草原に残す
            AddZone(canvas,
                x: 4f,  y: 10f, w: 28f, h: 30f,
                color: new Color(0.10f, 0.38f, 0.12f),
                tl: 22f, tr: 16f, bl: 26f, br: 20f);

            // 山脈（大陸北中央）
            AddZone(canvas,
                x: 32f, y: 6f,  w: 44f, h: 44f,
                color: new Color(0.52f, 0.46f, 0.36f),
                tl: 20f, tr: 22f, bl: 26f, br: 16f);

            // 雪帽子（山頂）
            AddZone(canvas,
                x: 38f, y: 6f,  w: 30f, h: 18f,
                color: new Color(0.90f, 0.92f, 0.95f),
                tl: 20f, tr: 20f, bl: 30f, br: 30f);

            // 砂漠（大陸南東）
            AddZone(canvas,
                x: 40f, y: 52f, w: 38f, h: 38f,
                color: new Color(0.84f, 0.70f, 0.30f),
                tl: 16f, tr: 14f, bl: 22f, br: 24f);

            // 湖（大陸中央）— border-radius 50% で楕円
            AddZone(canvas,
                x: 22f, y: 53f, w: 16f, h: 14f,
                color: new Color(0.20f, 0.55f, 0.80f),
                tl: 50f, tr: 50f, bl: 50f, br: 50f);
        }

        internal static void Unregister(VisualElement canvas)
        {
            if (canvas == null) return;
            canvas.style.backgroundColor = StyleKeyword.Null;
            foreach (var zone in canvas.Query(className: ZoneClass).ToList())
                zone.RemoveFromHierarchy();
        }

        private static void AddZone(
            VisualElement parent,
            float x, float y, float w, float h,
            Color color,
            float tl, float tr, float bl, float br)
        {
            var e = new VisualElement();
            e.AddToClassList(ZoneClass);
            e.pickingMode = PickingMode.Ignore;

            e.style.position = Position.Absolute;
            e.style.left   = new Length(x, LengthUnit.Percent);
            e.style.top    = new Length(y, LengthUnit.Percent);
            e.style.width  = new Length(w, LengthUnit.Percent);
            e.style.height = new Length(h, LengthUnit.Percent);

            e.style.backgroundColor = color;

            e.style.borderTopLeftRadius     = new Length(tl, LengthUnit.Percent);
            e.style.borderTopRightRadius    = new Length(tr, LengthUnit.Percent);
            e.style.borderBottomLeftRadius  = new Length(bl, LengthUnit.Percent);
            e.style.borderBottomRightRadius = new Length(br, LengthUnit.Percent);

            parent.Add(e);
        }
    }
}
