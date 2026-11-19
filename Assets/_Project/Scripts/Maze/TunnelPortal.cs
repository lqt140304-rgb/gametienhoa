using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Vùng hoán đổi ẩn đặt ở đoạn khuất giữa đường hầm: khi slime đi vào,
    /// dịch chuyển mượt sang đầu hầm tương ứng của khoang đích và cập nhật khoang đang bật.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TunnelPortal : MonoBehaviour
    {
        [SerializeField] int sourceChamberId;
        [SerializeField] int targetChamberId;
        [SerializeField] int tunnelId;
        [SerializeField] Transform exitSpawnPoint;

        static float lastTeleportTime = -10f;
        const float CooldownSeconds = 1.0f;

        public int SourceChamberId => sourceChamberId;
        public int TargetChamberId => targetChamberId;
        public int TunnelId => tunnelId;
        public Transform ExitSpawnPoint => exitSpawnPoint != null ? exitSpawnPoint : transform;

        public void Configure(int sourceId, int targetId, int edgeId, Transform exitPoint)
        {
            sourceChamberId = sourceId;
            targetChamberId = targetId;
            tunnelId = edgeId;
            exitSpawnPoint = exitPoint;
        }

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Time.time - lastTeleportTime < CooldownSeconds) return;
            var body = other.GetComponentInParent<SlimeBody>();
            if (body == null) return;

            var manager = FindAnyObjectByType<MazeWorldManager>();
            if (manager == null) return;

            lastTeleportTime = Time.time;
            manager.SwapThroughTunnel(body.transform, sourceChamberId, targetChamberId);
        }
    }
}
