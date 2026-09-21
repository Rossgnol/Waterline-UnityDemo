using UnityEngine;
using Waterline.Core;

namespace Waterline
{
    public sealed class FloodedLowerDeck : MonoBehaviour
    {
        public HarborLayout layout;
        public Transform water, platform;
        public TextMesh[] statusLabels;
        public bool Raised { get { return (layout.Planner==null || layout.Planner.Ready) && (layout.dock.State.Phase == DockPhase.Flooded || layout.dock.State.Phase == DockPhase.Escaped); } }
        public bool Closed { get { return layout.dock.State.Phase != DockPhase.Dry; } }
        public Rect Opening { get { return Rect.MinMaxRect(-4, 6.3f, 4, 11); } }
        public Rect LowerMapBounds { get { return Rect.MinMaxRect(-23, -18, 23, 21); } }
        private void LateUpdate() { Apply(); }
        public void Apply()
        {
            if (layout.dock.State == null) return;
            var p = water.position; p.y = Mathf.Lerp(layout.dock.dryWaterHeight, layout.dock.fullWaterHeight, layout.dock.State.WaterProgress); water.position = p;
            p = platform.position; p.y = Mathf.Lerp(-3.65f, -.15f, layout.Planner!=null && !layout.Planner.Ready?0:layout.dock.State.WaterProgress); platform.position = p;
            foreach (var label in statusLabels) label.text = Raised ? "FLOAT / OPEN" : Closed ? "FLOAT / RISING" : "FLOAT / LOW";
            if(layout.Planner!=null)foreach(var label in statusLabels)label.text=layout.Planner.FloatStatus;
            if (layout.Visited.Contains("float_pit")) layout.Visited.Add("float_link");
        }
        public void ReadPlan()
        {
            if(layout.Planner!=null){layout.Planner.ReadPlan();return;}
            var a = layout.annex;
            a.NoteOpen = true; a.NoteTitle = "排水检修记录 / 同一空间的两种水位";
            a.NoteBody = "干坞时：工具间北门通往缆具间，联轴销已移交北侧工作台。缆具间、浮台井、排水检修间沿北侧通道相连，可绕开中央坞底巡查，但路程更长。\n\n注水会淹没整个下层维护舱段，两侧楼梯及维护舱入口同时关闭。先带走联轴销，再回上层启动；这里的纸张可抄入 J 随身记录，不必回来。\n\n水涨起来后，浮台井中的检修平台升至中央大厅地面高度。等待两侧护门打开，便可从大厅西侧跨到东侧，避开船坞上层巡查，再从已解锁的东门前往登艇台。\n\n平台未到顶前不能上去。橙色水位线以下均为淹没区。";
        }
    }
}
