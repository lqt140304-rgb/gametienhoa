using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Chướng ngại tự nhiên độc lập đặt trong hầm hoặc trước vách (ngập nước, vách cao, đá vụn, nóng khô, tối đặc).
    /// Tái sử dụng chung khung điều kiện với TunnelPassage.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ObstacleBarrier : MonoBehaviour
    {
        [SerializeField] ObstacleKind obstacle = ObstacleKind.HighWall;
        [SerializeField] Collider barrierCollider;

        float lastLogTime = -10f;

        public ObstacleKind Obstacle => obstacle;

        public void Configure(ObstacleKind obs) => obstacle = obs;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerStay(Collider other)
        {
            if (!SlimeCapabilityLookup.TryGet(other, out var caps)) return;

            bool canPass = PassageRules.EvaluatePassage(TunnelSize.Large, obstacle, caps, out string reason);
            if (barrierCollider != null)
                barrierCollider.enabled = !canPass;

            if (!canPass && Time.time - lastLogTime > 2.5f)
            {
                lastLogTime = Time.time;
                Debug.Log($"[Chướng ngại] {reason}");
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!SlimeCapabilityLookup.TryGet(other, out _)) return;
            if (barrierCollider != null) barrierCollider.enabled = true;
        }
    }
}
