using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    public sealed class DockHud : MonoBehaviour
    {
        public DockSession session;
        public FirstPersonController player;
        private GUIStyle title, body, small, button;
        private Font font;
        private DockRoutes routes;
        private readonly Color panel = new Color(0.025f, 0.055f, 0.07f, 0.94f);
        private readonly Color accent = new Color(0.35f, 0.88f, 0.8f);

        private void Awake() { routes = FindFirstObjectByType<DockRoutes>(); }

        private void EnsureStyles()
        {
            if (title != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 20);
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, wordWrap = true };
            body.normal.textColor = new Color(0.92f, 0.96f, 0.97f);
            title = new GUIStyle(body) { fontSize = 30, fontStyle = FontStyle.Bold };
            small = new GUIStyle(body) { fontSize = 15 };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18 };
        }

        private static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }

        private void OnGUI()
        {
            if (session == null || session.State == null || session.HideGameplayHud) return;
            EnsureStyles();
            var oldMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            var state = session.State;
            Fill(new Rect(24, 24, 350, 202), panel);
            var harbor=session.annex!=null?session.annex.Harbor:null;
            var currentRoom=harbor!=null?harbor.RoomAt(player.transform.position):null;
            GUI.Label(new Rect(42, 36, 300, 42), currentRoom!=null?currentRoom.title:"吃水线", title);
            GUI.Label(new Rect(42, 81, 315, 24), session.Objective, small);
            if(harbor!=null && state.PinInstalled)
            {
                Condition(42,114,session.annex.State.PowerRestored,"恢复北楼动力");
                Condition(42,145,session.annex.State.GaugeAligned,"标定测潮架");
                Condition(42,176,session.annex.State.PressureBalanced,"完成三阀泄压");
            }
            else
            {
                Condition(42,114,state.PinInstalled,"安装联轴销");
                Condition(42,145,state.GateUnlocked,"解除坞门机械锁");
                Condition(42,176,state.BridgeDeployed,"展开上层检修桥");
            }

            Fill(new Rect(width - 252, 24, 228, 91), panel);
            string phase = state.Phase == DockPhase.Dry ? "干坞 / DRY" : state.Phase == DockPhase.Filling ? "注水中 / FILLING" : "浮航水位 / AFLOAT";
            GUI.Label(new Rect(width - 234, 40, 200, 28), phase, body);
            Fill(new Rect(width - 234, 83, 192, 5), new Color(0.2f, 0.3f, 0.33f));
            Fill(new Rect(width - 234, 83, 192 * state.WaterProgress, 5), accent);

            if (!session.InputBlocked)
            {
                if(player.DoorTarget!=null)
                {
                    Fill(new Rect(width/2-270,height-180,540,64),panel);
                    GUI.Label(new Rect(width/2-250,height-168,500,48),player.DoorTarget.Description,body);
                }
                if(player.AnnexTarget!=null)
                {
                    Fill(new Rect(width/2-250,height-210,500,94),panel);
                    GUI.Label(new Rect(width/2-230,height-200,460,29),"E  "+player.AnnexTarget.displayName,body);
                    var reason=player.AnnexTarget.annex.BlockedReason(player.AnnexTarget.action);
                    GUI.Label(new Rect(width/2-230,height-165,460,40),reason.Length>0?reason:player.AnnexTarget.description,small);
                }
                Fill(new Rect(width / 2 - 2, height / 2 - 2, 4, 4), accent);
                if (player.Target != null)
                {
                    var target = player.Target;
                    Fill(new Rect(width / 2 - 250, height - 210, 500, 94), panel);
                    GUI.Label(new Rect(width / 2 - 230, height - 200, 460, 29),
                        (target.needsHold ? "长按 E  " : "E  ") + target.displayName, body);
                    var reason = session.BlockedReason(target.action);
                    GUI.Label(new Rect(width / 2 - 230, height - 165, 460, 40),
                        reason.Length > 0 ? reason : target.description, small);
                    if (target.needsHold) Fill(new Rect(width / 2 - 230, height - 120, 460 * player.HoldProgress, 3), accent);
                }
            }

            Fill(new Rect(24, height - 95, width - 48, 70), panel);
            GUI.Label(new Rect(42, height - 86, width - 84, 30), session.LastMessage, body);
            GUI.Label(new Rect(42, height - 52, width - 84, 22), "WASD 移动   鼠标观察   E 交互   Shift 快走   Esc 暂停   F1 调试" +
                (session.explorationLayout ? "   M 路线图" : ""), small);

            if (session.DebugVisible) DrawDebug(width, height);
            else if (state.Phase == DockPhase.Escaped) DrawFinish(width, height);
            else if (session.Paused)
            {
                Fill(new Rect(width / 2 - 200, height / 2 - 88, 400, 176), panel);
                GUI.Label(new Rect(width / 2 - 175, height / 2 - 65, 350, 40), "已暂停", title);
                if (GUI.Button(new Rect(width / 2 - 175, height / 2 - 5, 170, 45), "继续 / Esc", button)) session.Paused = false;
                if (GUI.Button(new Rect(width / 2 + 10, height / 2 - 5, 165, 45), "重新开始", button)) session.Restart();
            }
            GUI.matrix = oldMatrix;
        }

        private void Condition(float x, float y, bool complete, string label)
        {
            Fill(new Rect(x, y + 7, 10, 10), complete ? accent : new Color(0.75f, 0.52f, 0.28f));
            GUI.Label(new Rect(x + 23, y, 280, 28), (complete ? "已完成 · " : "待完成 · ") + label, body);
        }

        private void DrawDebug(float width, float height)
        {
            float x = width / 2 - 290, y = height / 2 - 170;
            Fill(new Rect(x, y, 580, 340), panel);
            GUI.Label(new Rect(x + 24, y + 18, 530, 40), "状态观察 / DEBUG", title);
            GUI.Label(new Rect(x + 24, y + 75, 530, 112),
                "阶段：" + session.State.Phase + "    水位进度：" + session.State.WaterProgress.ToString("P0") +
                "\n携带联轴销：" + session.State.HasPin + "    注水时长：" + session.tuning.fillingSeconds + " 秒" +
                (routes == null ? "" : "\n下层：" + (routes.LowerOpen ? "开放" : "封闭") + "    已探索：" + routes.LowerVisited +
                "    活跃用时：" + routes.ElapsedSeconds.ToString("F0") + " 秒") +
                (session.threat==null?"":"\n敌人："+session.threat.Mode+"  目标节点："+session.threat.TargetNode+"  重试："+(session.threat.Attempts-1)), body);
            var missing = session.State.MissingFloodConditions();
            GUI.Label(new Rect(x + 24, y + 188, 530, 36), "未满足条件：" + (missing.Length == 0 ? "全部满足" : string.Join(" / ", missing)), body);
            GUI.Label(new Rect(x + 24, y + 225, 530, 40), "面板打开时暂停推进。参数请退出 Play 后在 DockTuning 中修改。", small);
            if (GUI.Button(new Rect(x + 24, y + 277, 245, 42), "关闭 / F1", button)) session.DebugVisible = false;
            if (GUI.Button(new Rect(x + 287, y + 277, 269, 42), "重新开始", button)) session.Restart();
        }

        private void DrawFinish(float width, float height)
        {
            float x = width / 2 - 290, y = height / 2 - 140;
            Fill(new Rect(x, y, 580, 280), panel);
            GUI.Label(new Rect(x + 25, y + 25, 530, 42), "检修艇已离坞", title);
            string recap = routes == null ? "你完成了：修复 → 解锁 → 展桥 → 注水 → 撤离。\n下一次试试提前启动注水，观察失败提示是否清楚。" :
                "你完成了：下层探索 → 上层回环 → 注水封路 → 撤离。\n累计游戏时间：" + routes.ElapsedSeconds.ToString("F0") + " 秒" +
                (session.threat==null?"（不含暂停）":"   重试："+(session.threat.Attempts-1)+" 次") +
                "\n注水后经过：" + (routes.BridgeUsedAfterFlood ? "检修桥  " : "") + (routes.SouthUsedAfterFlood ? "南侧栈道" : "");
            GUI.Label(new Rect(x + 25, y + 88, 530, 90), recap, body);
            if (GUI.Button(new Rect(x + 25, y + 209, 250, 45), "再试一次", button)) session.Restart();
        }
    }
}
