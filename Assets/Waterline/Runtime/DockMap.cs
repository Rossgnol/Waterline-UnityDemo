using UnityEngine;

namespace Waterline
{
    // A schematic drawn from the same world axes as the actual level.
    public sealed class DockMap : MonoBehaviour
    {
        public DockRoutes routes;
        private GUIStyle label, heading, caption;
        private readonly Color ink = new Color(0.04f, 0.09f, 0.12f, 0.98f);
        private readonly Color open = new Color(0.15f, 0.43f, 0.43f);
        private readonly Color closed = new Color(0.42f, 0.17f, 0.13f);

        private void OnGUI()
        {
            if (!routes.session.MapVisible || routes.session.State == null) return;
            if (label == null)
            {
                var font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 18);
                label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                label.normal.textColor = Color.white;
                heading = new GUIStyle(label) { fontSize = 28, alignment = TextAnchor.MiddleLeft };
                caption = new GUIStyle(label) { alignment = TextAnchor.MiddleLeft };
            }
            GUI.depth = -10;
            Matrix4x4 matrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float x = Screen.width / scale / 2 - 570, y = Screen.height / scale / 2 - 285;
            Fill(new Rect(x, y, 1140, 570), ink);
            GUI.Label(new Rect(x + 28, y + 20, 1040, 45), "船坞路线 / M 或 Esc 返回", heading);
            GUI.Label(new Rect(x + 28, y + 68, 1040, 28), "当前目标：" + routes.session.Objective, caption);
            DrawDeck(new Rect(x + 30, y + 155, 520, 295), false);
            DrawDeck(new Rect(x + 590, y + 155, 520, 295), true);
            GUI.Label(new Rect(x + 30, y + 465, 1080, 34),
                routes.LowerOpen ? "下层开放：西梯 → 工具间 → 船底绕行 → 东梯。解锁后可利用上层两条回程路线。" :
                "下层已封闭。上层检修桥与南侧栈道均可抵达东侧登艇区。", caption);
            GUI.Label(new Rect(x + 30, y + 512, 1080, 28), "白点：你的位置    青色：开放路线    红色：尚未开放或已封闭    查看地图时暂停", caption);
            GUI.matrix = matrix;
            GUI.depth = 0;
        }

        private void DrawDeck(Rect area, bool lower)
        {
            var state = routes.session.State;
            GUI.Label(new Rect(area.x, area.y - 42, area.width, 32), lower ? "下层 / −3.5 m      北 ↑" : "上层 / ±0 m      北 ↑", label);
            Fill(area, new Color(0.025f, 0.05f, 0.065f));
            if (lower)
            {
                Block(area, -14.5f, 14.5f, -4.5f, 5.3f, routes.LowerOpen ? open : closed);
                Block(area, -1.5f, 1.5f, -2.75f, 2.75f, ink);
                Text(area, new Vector2(0, 0), "船体");
                Text(area, new Vector2(-8, 0), state.HasPin || state.PinInstalled ? "工具间\n零件已取" : "工具间\n联轴销");
                Text(area, new Vector2(8, 1), "检修廊");
                Text(area, new Vector2(-12.6f, 3.8f), "西梯");
                Text(area, new Vector2(12.6f, 3.8f), "东梯");
                if (!routes.LowerOpen) GUI.Label(new Rect(area.x + 90, area.y + 205, area.width - 180, 36), "已淹没 / 不可进入", label);
            }
            else
            {
                Block(area, -11, -5, -5, 5, open);
                Block(area, 5, 11, -5, 5, open);
                Block(area, -11, 11, -7.5f, -4.5f, open);
                Block(area, -5, 5, 2.5f, 4.5f, state.BridgeDeployed ? open : closed);
                Block(area, 3.7f, 4.3f, -7.5f, -4.5f, state.GateUnlocked ? open : closed);
                Block(area, -14.3f, -11, -5, -3, routes.LowerOpen ? open : closed);
                Block(area, 11, 14.3f, -5, -3, routes.LowerOpen ? open : closed);
                Text(area, new Vector2(-8, 1.7f), "水控室\n安装 / 注水");
                Text(area, new Vector2(8, 2.4f), "绞盘间\n解锁 / 展桥");
                Text(area, new Vector2(8, -1.7f), "登艇区");
                Text(area, new Vector2(0, 3.5f), routes.session.threat==null?"检修桥":"检修桥\n金属 / 声响大");
                Text(area, new Vector2(-1, -6), routes.session.threat==null?"南侧栈道":"南侧栈道\n橡胶 / 较安静");
                Text(area, new Vector2(-12.6f, -4), "西梯");
                Text(area, new Vector2(12.6f, -4), "东梯");
            }
            Vector3 p = routes.player.position;
            if ((p.y < -0.5f) == lower)
            {
                Vector2 point = Project(area, new Vector2(p.x, p.z));
                Fill(new Rect(point.x - 6, point.y - 6, 12, 12), Color.white);
                GUI.Label(new Rect(point.x - 30, point.y + 7, 60, 22), "你", label);
            }
        }

        private static Vector2 Project(Rect r, Vector2 world)
        { return new Vector2(r.x + (world.x + 16) / 32 * r.width, r.y + (6 - world.y) / 14 * r.height); }

        private static void Block(Rect r, float left, float right, float south, float north, Color color)
        {
            Vector2 a = Project(r, new Vector2(left, north)), b = Project(r, new Vector2(right, south));
            Fill(new Rect(a.x, a.y, b.x - a.x, b.y - a.y), color);
        }

        private void Text(Rect r, Vector2 world, string value)
        { Vector2 p = Project(r, world); GUI.Label(new Rect(p.x - 57, p.y - 25, 114, 50), value, label); }

        private static void Fill(Rect r, Color color)
        { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
    }
}
