using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Civilization
{
    /// <summary>
    /// One treefolk (spec 2026-11-17-treefolk-grudge-design): a citizen who potters between its tribe's home points, or a
    /// guard who keeps a post. It remembers who hurt its people (its own <see cref="Mind"/>) and treats a player by the
    /// tribe's standing — but only once it has heard about them. Brains run on the host; on a co-op client it is a replica
    /// that follows what the host sends (TreefolkSync).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Treefolk : MonoBehaviour, IDamageable
    {
        public enum Role { Citizen, Guard }

        static readonly List<Treefolk> active = new List<Treefolk>();
        public static IReadOnlyList<Treefolk> Active => active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll() => active.Clear();

        public Role Kind = Role.Citizen;
        [Tooltip("Class of this treefolk (filled in by the class system later; unused for now)")]
        public string ClassId = "";
        [Tooltip("Index shared with other machines in co-op (set by the builder)")]
        public int Index;
        public float MaxHealth = 30f;
        [Tooltip("Where a guard stands")]
        public Vector3 Post;

        public const float WalkSpeed = 0.8f, RunSpeed = 2.2f, HitDamage = 8f, HitReach = 1.4f, HitInterval = 1f;
        public const float AlarmSeconds = 6f, ColdDistance = 6f, GuardFollowDistance = 4f, NoticeRange = 25f;
        public const float CorpseSeconds = 300f;

        public float Health { get; private set; }
        public bool IsDead { get; private set; }
        public Knowledge Mind { get; } = new Knowledge();
        public TreefolkTribe Tribe { get; private set; }
        public bool Alarmed => Time.time < alarmedUntil;
        public bool Attacking { get; private set; }
        /// <summary>Client side: a copy that only follows the host.</summary>
        public bool IsReplica { get; set; }

        Rigidbody rb;
        float alarmedUntil, nextHit, nextIdle, diedAt;
        Vector3 goal;
        bool hasGoal;
        Vector3 replicaPos; float replicaYaw;
        static GUIStyle markStyle;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            Health = MaxHealth;
            if (Post == Vector3.zero) Post = transform.position;
            replicaPos = transform.position;
        }

        void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
            Tribe = GetComponentInParent<TreefolkTribe>();
        }

        void OnDisable() => active.Remove(this);

        /// <summary>Heard about (or saw) an offender. True if it was news; news makes it alarmed for a while.</summary>
        public bool Hear(object offender)
        {
            if (IsDead) return false;
            if (offender == null) { alarmedUntil = Time.time + AlarmSeconds; return true; }   // learned through talk: just alarm
            if (!Mind.Learn(offender)) return false;
            alarmedUntil = Time.time + AlarmSeconds;
            return true;
        }

        public void TakeDamage(float amount, SurvivalStats by)
        {
            if (amount <= 0f || IsDead) return;
            if (by != null) Evolve.Visuals.SlimeGelAnimator.Play(by.gameObject, Evolve.Visuals.GelMotion.Attack);
            if (IsReplica)
            {
                Evolve.Survival.NetWorld.ReportTreefolkHit?.Invoke(Index, amount);   // the host applies it
                return;
            }
            Health -= amount;
            bool killed = Health <= 0f;
            if (killed) Die();
            if (Tribe != null && by != null) Tribe.ReportAttack(this, by, killed);
        }

        void Die()
        {
            IsDead = true;
            diedAt = Time.time;
            Attacking = false;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 90f);   // falls over and stays as a corpse
            transform.position += Vector3.up * 0.2f;
        }

        /// <summary>Client: the host's state for this treefolk.</summary>
        public void SetReplicaState(Vector3 position, float yaw, bool alarmed, bool dead, bool attacking)
        {
            replicaPos = position; replicaYaw = yaw;
            if (alarmed) alarmedUntil = Time.time + 0.3f;
            Attacking = attacking;
            if (dead && !IsDead) Die();
        }

        void Update()
        {
            if (IsReplica)
            {
                if (IsDead) return;
                rb.MovePosition(Vector3.Lerp(transform.position, replicaPos, 1f - Mathf.Exp(-12f * Time.deltaTime)));
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, replicaYaw, 0f), 1f - Mathf.Exp(-10f * Time.deltaTime)));
                return;
            }
            if (IsDead)
            {
                if (Time.time - diedAt > CorpseSeconds) gameObject.SetActive(false);
                return;
            }
            Think();
        }

        void Think()
        {
            Attacking = false;
            Standing standing = Standing.Neutral;
            SurvivalStats target = null;
            if (Tribe != null) target = Tribe.NearestKnownTarget(this, NoticeRange, out standing);
            if (target != null)
            {
                Vector3 to = target.transform.position - transform.position; to.y = 0f;
                float d = to.magnitude;
                if (standing == Standing.Hostile)
                {
                    if (Kind == Role.Guard)
                    {
                        Attacking = true;
                        if (d > HitReach * 0.8f) Move(target.transform.position, RunSpeed);
                        else Face(to);
                        if (d <= HitReach && Time.time >= nextHit) { nextHit = Time.time + HitInterval; Strike(target, to); }
                    }
                    else Move(transform.position - to.normalized * 4f, RunSpeed);   // flee
                    return;
                }
                if (standing == Standing.Cold)
                {
                    if (Kind == Role.Guard && d > GuardFollowDistance + 1f) { Move(target.transform.position, WalkSpeed * 1.4f); return; }
                    if (Kind == Role.Citizen && d < ColdDistance) { Move(transform.position - to.normalized * 3f, WalkSpeed * 1.5f); return; }
                }
            }
            if (Alarmed && Tribe != null) { Move(Tribe.HallPosition, RunSpeed); return; }
            if (Kind == Role.Guard) { if ((Post - transform.position).sqrMagnitude > 0.5f) Move(Post, WalkSpeed); return; }
            Wander();
        }

        void Wander()
        {
            if (!hasGoal)
            {
                if (Time.time < nextIdle || Tribe == null) return;
                goal = Tribe.RandomHomePoint();
                hasGoal = true;
            }
            Vector3 to = goal - transform.position; to.y = 0f;
            if (to.magnitude < 0.6f) { hasGoal = false; nextIdle = Time.time + Random.Range(2f, 5f); return; }
            Move(goal, WalkSpeed);
        }

        void Move(Vector3 target, float speed)
        {
            Vector3 to = target - transform.position; to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            Vector3 next = transform.position + Vector3.ClampMagnitude(to, speed * Time.deltaTime);
            next.y = GroundY(next);
            rb.MovePosition(next);
            Face(to);
        }

        void Face(Vector3 dir)
        {
            if (dir.sqrMagnitude < 1e-4f) return;
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 1f - Mathf.Exp(-8f * Time.deltaTime)));
        }

        static float GroundY(Vector3 p)
        {
            var t = Terrain.activeTerrain;
            return t != null ? t.SampleHeight(p) + t.transform.position.y : p.y;
        }

        void Strike(SurvivalStats target, Vector3 to)
        {
            if (target.IsRemote) Evolve.Survival.NetWorld.BiteRemote?.Invoke(target, HitDamage, to.normalized * 2f);
            else target.State.Damage(HitDamage);
        }

        void OnGUI()
        {
            if (!Alarmed || IsDead || Event.current.type != EventType.Repaint) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 head = transform.position + Vector3.up * 1.8f;
            if ((head - cam.transform.position).sqrMagnitude > 40f * 40f) return;
            Vector3 s = cam.WorldToScreenPoint(head);
            if (s.z <= 0f) return;
            if (markStyle == null) markStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.85f, 0.3f) } };
            GUI.Label(new Rect(s.x - 15f, Screen.height - s.y - 15f, 30f, 30f), "!", markStyle);
        }
    }
}
