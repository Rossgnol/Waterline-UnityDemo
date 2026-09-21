using UnityEngine;

namespace Waterline
{
    [CreateAssetMenu(menuName = "Waterline/Dock tuning")]
    public sealed class DockTuning : ScriptableObject
    {
        [Header("首日练习：把注水时间从 12 改为 6")]
        [Tooltip("水面完成上升所需的秒数。编辑前先退出 Play 模式。")]
        [Range(3f, 30f)] public float fillingSeconds = 12f;
        [Header("移动与交互")]
        [Range(1f, 6f)] public float walkSpeed = 3.2f;
        [Range(2f, 8f)] public float runSpeed = 5f;
        [Range(0.03f, 0.25f)] public float mouseSensitivity = 0.10f;
        [Range(65f, 100f)] public float fieldOfView = 78f;
        [Range(1.5f, 4f)] public float interactionDistance = 2.8f;
        [Range(0.3f, 2f)] public float floodHoldSeconds = 0.8f;
    }
}
