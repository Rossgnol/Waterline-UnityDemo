using UnityEngine;
namespace Waterline
{
    [CreateAssetMenu(menuName="Waterline/Threat tuning")]
    public sealed class ThreatTuning : ScriptableObject
    {
        [Header("移动")]
        public float patrolSpeed = 1.25f;
        public float investigateSpeed = 1.9f;
        public float chaseSpeed = 3.7f;
        public float evacuationSpeed = 4f;
        public float quietPlayerSpeed = 1.6f;
        [Header("感知与反应")]
        public float viewDistance = 8f;
        [Range(30,180)] public float fieldOfView = 100f;
        public float recognitionSeconds = 1.0f;
        public float loseSightSeconds = 1.5f;
        public float searchSeconds = 4f;
        public float captureDistance = 1.05f;
        public float captureWindup = 0.9f;
        public float retryGraceSeconds = 4f;
        [Range(0.1f,1)] public float obstructedSoundMultiplier = 0.45f;
    }
}
