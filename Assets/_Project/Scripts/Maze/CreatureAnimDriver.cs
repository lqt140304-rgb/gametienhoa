using Evolve.Creatures;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Điều khiển Animator trên các prefab sinh vật đã gắn xương mà không cần sửa Scripts/Creatures/.
    /// Đọc vận tốc di chuyển thực tế và lượng máu giảm để kích hoạt Idle / Walk / Hit / Action.
    /// </summary>
    public class CreatureAnimDriver : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] string actionTriggerName = "Attack";
        [SerializeField] float walkThreshold = 0.08f;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int HitHash = Animator.StringToHash("Hit");
        static readonly int ChasingHash = Animator.StringToHash("Alarming");

        Prey prey;
        Predator predator;
        Vector3 prevPos;
        float prevHealth = -1f;
        float smoothedSpeed;

        public void Configure(Animator anim, string primaryActionTrigger)
        {
            animator = anim;
            if (!string.IsNullOrEmpty(primaryActionTrigger))
                actionTriggerName = primaryActionTrigger;
        }

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            prey = GetComponent<Prey>();
            predator = GetComponent<Predator>();
            prevPos = transform.position;
            prevHealth = ReadHealth();
        }

        void Update()
        {
            if (animator == null) return;

            float dt = Mathf.Max(0.001f, Time.deltaTime);
            Vector3 delta = transform.position - prevPos;
            delta.y = 0f;
            float instSpeed = delta.magnitude / dt;
            prevPos = transform.position;

            smoothedSpeed = Mathf.Lerp(smoothedSpeed, instSpeed, Mathf.Clamp01(dt * 10f));
            animator.SetFloat(SpeedHash, smoothedSpeed > walkThreshold ? smoothedSpeed : 0f);

            if (predator != null)
                animator.SetBool(ChasingHash, predator.IsChasing);

            float hp = ReadHealth();
            if (prevHealth > 0f && hp < prevHealth - 0.01f)
                animator.SetTrigger(HitHash);
            prevHealth = hp;
        }

        public void TriggerAction()
        {
            if (animator != null && !string.IsNullOrEmpty(actionTriggerName))
                animator.SetTrigger(actionTriggerName);
        }

        float ReadHealth()
        {
            if (prey != null) return prey.Health;
            if (predator != null) return predator.Health;
            return -1f;
        }
    }
}
