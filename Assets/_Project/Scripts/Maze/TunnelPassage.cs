using System;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Gắn ở miệng đường hầm để kiểm tra kích thước slime (khe nhỏ / hầm vừa / hầm lớn)
    /// và chướng ngại tự nhiên. Tự động mở lối khi người chơi đủ điều kiện.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TunnelPassage : MonoBehaviour
    {
        [SerializeField] TunnelSize tunnelSize = TunnelSize.Medium;
        [SerializeField] ObstacleKind obstacle = ObstacleKind.None;
        [SerializeField] Collider blockingCollider;

        float lastLogTime = -10f;

        public TunnelSize Size => tunnelSize;
        public ObstacleKind Obstacle => obstacle;

        public event Action<string> PassageBlocked;

        public void Configure(TunnelSize size, ObstacleKind obs)
        {
            tunnelSize = size;
            obstacle = obs;
        }

        void Awake()
        {
            var trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        void OnTriggerStay(Collider other)
        {
            if (!SlimeCapabilityLookup.TryGet(other, out var caps)) return;

            bool canPass = PassageRules.EvaluatePassage(tunnelSize, obstacle, caps, out string reason);
            if (blockingCollider != null)
                blockingCollider.enabled = !canPass;

            if (!canPass && Time.time - lastLogTime > 2.5f)
            {
                lastLogTime = Time.time;
                PassageBlocked?.Invoke(reason);
                Debug.Log($"[Mê cung] {reason}");
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!SlimeCapabilityLookup.TryGet(other, out _)) return;
            if (blockingCollider != null) blockingCollider.enabled = true;
        }
    }
}
