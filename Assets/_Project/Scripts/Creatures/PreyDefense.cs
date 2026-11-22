using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// A prey that fights back (spec: prey are not just food). Put it on a Prey prefab; the defence comes from PreyDefenseSeed, by
    /// <see cref="defenseId"/> or by the prefab's name (the species name, e.g. "Ốc Hồ"). Threats are the players and the hunters (Predator).
    /// It picks its moves with the tested rules in PreyDefenseRules, then carries them out: run (faster, weaving, silent), guard (stand still,
    /// take less damage, cannot be swallowed), make hunters lose track, leave a slowing puddle, spikes or a decoy, and counter-attack when
    /// grabbed, hit, cornered or worn out. Runs on the host (replicas only follow). A bigger threat is fled from sooner and fought less.
    /// shortcut: hunters do not chase prey in the scene yet (that is the ecosystem scene work), so hunters only meet these defences when they
    /// reach the prey's area; a decoy only fools hunters once they hunt prey.
    /// </summary>
    [RequireComponent(typeof(Prey)), DefaultExecutionOrder(-10)]
    public class PreyDefense : MonoBehaviour
    {
        [Tooltip("PreyDefenseSeed id (e.g. tho_la). Empty = look up by this prefab's name")]
        [SerializeField] string defenseId;

        const float SeenFactor = 1.5f, CorneredDistance = 1.6f, ForgetSeconds = 8f;

        PreyDefenseDef def;
        Prey prey;
        float escapeReadyAt, counterReadyAt, guardUntil, blocksCatchUntil, speedUntil, lastSeen = -100f;
        bool alarmSent, callSent, hit;
        Rigidbody body;
        Collider shape;
        CreatureLocomotion locomotion;
        TrailRenderer trail;
        Material trailMaterial;
        Animator shellAnimator;
        bool hasShellParameter, shellClosed, wingsPosed;
        Transform wingLeft, wingRight;
        Quaternion wingLeftRest, wingRightRest;
        static readonly int ShellClosedId = Animator.StringToHash("ShellClosed");
        float launchAt, flightStarted, guardStarted;
        Vector3 glideFrom, glideTo;
        const float FlightSeconds = 0.4f, GlideLift = 0.32f;

        public bool StopsSeparation => Time.time < guardUntil && def?.GuardRecovery > 0f
            || launchAt > 0f || flightStarted > 0f;
        public bool ResistsDisplacement => def?.Id == "oc_ho"
            && (Time.time < guardUntil || HangOneGround.Wet(transform.position));

        void Awake()
        {
            prey = GetComponent<Prey>();
            def = PreyDefenseSeed.Get(defenseId) ?? PreyDefenseSeed.FindBySpecies(name);
            if (def == null) { enabled = false; return; }
            if (def.GlideWindup > 0f)
            {
                body = GetComponent<Rigidbody>();
                shape = GetComponent<Collider>();
                foreach (var bone in GetComponentsInChildren<Transform>(true))
                {
                    if (bone.name == "Elytra_L") { wingLeft = bone; wingLeftRest = bone.localRotation; }
                    else if (bone.name == "Elytra_R") { wingRight = bone; wingRightRest = bone.localRotation; }
                }
            }
            locomotion = GetComponent<CreatureLocomotion>();
            if (def.GuardRecovery > 0f)
            {
                shellAnimator = GetComponentInChildren<Animator>();
                if (shellAnimator != null)
                    foreach (var parameter in shellAnimator.parameters)
                        if (parameter.nameHash == ShellClosedId) { hasShellParameter = true; break; }
            }
            prey.CatchVeto = _ => Time.time < blocksCatchUntil;
            prey.Touched += OnTouched;
            prey.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (trailMaterial != null) Destroy(trailMaterial);
            if (prey == null) return;
            prey.Touched -= OnTouched;
            prey.Damaged -= OnDamaged;
        }

        void OnTouched(SurvivalStats by) { if (prey.IsReplica || Prey.Frozen) return; hit = true; Evaluate(); }   // decide at once: it may slip out of the grab before the catch
        void OnDamaged(float amount, SurvivalStats by) => hit = true;

        void FixedUpdate()
        {
            if (prey.IsReplica) return;
            if (Time.time >= speedUntil) { prey.Boost = 1f; prey.Zigzag = false; prey.Silent = false; }
            prey.DamageTakenMultiplier = PreyDefenseRules.GuardFactor(def, Time.time, guardUntil);
            UpdateShell(Time.time < guardUntil);
            if (Prey.Frozen && (launchAt > 0f || flightStarted > 0f)) StopGlide();
            if (Prey.Frozen || TickGlide()) return;
            Evaluate();
        }

        void Evaluate()
        {
            if (launchAt > 0f || flightStarted > 0f || (def.GuardRecovery > 0f && Time.time < guardUntil)) return;
            var threat = NearestThreat(out float dist, out float ratio, out SurvivalStats player, out Predator hunter);
            bool seen = threat != null && dist <= def.AlertRange * (def.GlideWindup > 0f ? 1f : SeenFactor);
            if (seen) lastSeen = Time.time;
            else if (Time.time - lastSeen > ForgetSeconds) { alarmSent = false; callSent = false; }

            var input = new DefenseInput
            {
                Now = Time.time, ThreatSeen = seen, ThreatDistance = dist, ThreatRatio = ratio, Roll = Random.value,
                EscapeReadyAt = escapeReadyAt, CounterReadyAt = counterReadyAt,
                JustHit = hit, Cornered = threat != null && dist < CorneredDistance, Tired = prey.IsTired && seen,
                Guarding = Time.time < guardUntil, AlarmSent = alarmSent, CallSent = callSent,
            };
            var act = PreyDefenseRules.Decide(def, input);
            hit = false;
            if (act == DefenseAction.None || threat == null) return;
            Vector3 away = transform.position - threat.position; away.y = 0f;
            away = away.sqrMagnitude > 1e-4f ? away.normalized : transform.forward;

            if (act.HasFlag(DefenseAction.Alarm)) { alarmSent = true; Alarm(); }
            if (act.HasFlag(DefenseAction.CallHelp)) { callSent = true; CallHelp(player); }
            if (act.HasFlag(DefenseAction.Counter)) Counter(player, hunter, away);
            if (!act.HasFlag(DefenseAction.Escape)) return;
            if (def.GlideWindup > 0f) StartGlide(threat.position, away);
            else Escape();
        }

        Transform NearestThreat(out float dist, out float ratio, out SurvivalStats player, out Predator hunter)
        {
            Transform best = null; dist = float.MaxValue; ratio = 1f; player = null; hunter = null;
            Vector3 pos = transform.position;
            float myMass = Mathf.Max(0.1f, prey.BodyMass);
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.State.IsDead) continue;
                float d = Vector3.Distance(pos, p.transform.position);
                if (d >= dist) continue;
                dist = d; best = p.transform; player = p; hunter = null;
                float mass = p.TryGetComponent(out Evolve.Swallow.SlimeBody body) ? body.Mass : 10f;   // a remote avatar has no body here: a typical slime
                ratio = mass / myMass;
            }
            if (!Predator.Passive)
                foreach (var h in Predator.Active)
                {
                    if (h == null) continue;
                    float d = Vector3.Distance(pos, h.transform.position);
                    float massRatio = h.BodyMass / myMass;
                    if (d >= dist || d > CreatureCombatDetection(h, massRatio)) continue;
                    dist = d; best = h.transform; hunter = h; player = null;
                    ratio = massRatio;
                }
            return best;
        }

        float CreatureCombatDetection(Predator hunter, float massRatio)
        {
            if (!hunter.TryGetComponent(out Evolve.Combat.CreatureCombat combat) || !combat.IsCamouflaged)
                return float.MaxValue;
            float range = def.GlideWindup > 0f ? def.AlertRange : PreyDefenseRules.FleeRange(def.AlertRange, massRatio);
            return range * HangOneGround.CamouflageDetectionFactor;
        }

        void StartGlide(Vector3 threat, Vector3 away)
        {
            if (body == null || CreatureStatus.Holds(gameObject)) return;
            Vector3 start = body.position;
            float offset = Evolve.World.GroundHeight.OffsetOf(start);
            float score = float.NegativeInfinity;
            for (int i = 0; i < 7; i++)
            {
                int angle = (i + 1) / 2 * (i % 2 == 1 ? 25 : -25);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * away;
                for (float distance = 3f; distance >= 2f; distance -= 0.5f)
                {
                    Vector3 landing = Evolve.World.GroundHeight.Follow(start + dir * distance, offset);
                    if (Mathf.Abs(landing.y - start.y) > 0.65f ||
                        Evolve.World.WaterField.DepthAt(landing.x, landing.z) > 0.05f) continue;
                    Vector3 mid = GlidePosition(start, landing, 0.5f);
                    if (!GlideClear(start, mid) || !GlideClear(mid, landing)) continue;
                    bool cover = !HangOneGround.Clear(threat + Vector3.up * 0.35f, landing + Vector3.up * 0.25f);
                    float value = (landing - threat).magnitude + (HangOneGround.Moss(landing) ? 1f : 0f) + (cover ? 2f : 0f);
                    if (value <= score) continue;
                    score = value;
                    glideTo = landing;
                }
            }
            if (float.IsNegativeInfinity(score)) { escapeReadyAt = Time.time + 0.3f; return; }
            glideFrom = start;
            launchAt = Time.time + def.GlideWindup;
            escapeReadyAt = Time.time + def.EscapeCooldown;
            CreatureStatus.Stun(prey, def.GlideWindup + FlightSeconds, false);
            (locomotion ??= GetComponent<CreatureLocomotion>())?.PlayAction("Fly");
        }

        static Vector3 GlidePosition(Vector3 start, Vector3 end, float t) =>
            Vector3.Lerp(start, end, t) + Vector3.up * (4f * GlideLift * t * (1f - t));

        bool GlideClear(Vector3 from, Vector3 to)
        {
            Bounds bounds = shape != null ? shape.bounds : new Bounds(body.position, Vector3.one * 0.24f);
            float radius = Mathf.Max(0.08f, Mathf.Max(bounds.extents.x, bounds.extents.z));
            Vector3 center = bounds.center - body.position;
            center.y = Mathf.Max(radius + 0.035f, center.y);
            return HangOneGround.Clear(from + center, to + center, radius);
        }

        bool TickGlide()
        {
            if (launchAt <= 0f && flightStarted <= 0f) return false;
            if (Time.time < launchAt) return true;
            if (flightStarted <= 0f)
            {
                if ((body.position - glideFrom).sqrMagnitude > 0.01f) { StopGlide(); return true; }
                flightStarted = Time.time;
                launchAt = 0f;
                Vector3 dir = glideTo - glideFrom; dir.y = 0f;
                body.MoveRotation(Quaternion.LookRotation(dir));
                BeginTrail();
                if (HangOneGround.Moss(glideFrom))
                    SkillFx.Burst(glideFrom + Vector3.up * 0.1f, new Color(0.39f, 0.49f, 0.26f), 4, 0.5f, 0.06f, 0.2f, 0.26f, 0.12f);
            }
            float t = Mathf.Clamp01((Time.time - flightStarted + Time.fixedDeltaTime) / FlightSeconds);
            Vector3 next = GlidePosition(glideFrom, glideTo, t);
            if (!GlideClear(body.position, next)) { StopGlide(); return true; }
            body.MovePosition(next);
            if (t >= 1f) StopGlide();
            return true;
        }

        void StopGlide()
        {
            launchAt = flightStarted = 0f;
            if (trail != null) trail.emitting = false;
        }

        void BeginTrail()
        {
            if (trail == null)
            {
                var tail = new GameObject("Beetle glide trail");
                tail.transform.SetParent(transform, false);
                tail.transform.localPosition = new Vector3(0f, 0.24f, -0.12f);
                trail = tail.AddComponent<TrailRenderer>();
                trailMaterial = SkillFx.Gel(new Color(0.53f, 0.62f, 0.40f), 0.24f, 0f, 0.35f);
                trail.sharedMaterial = trailMaterial;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.time = 0.12f;
                trail.startWidth = 0.06f;
                trail.endWidth = 0f;
                trail.minVertexDistance = 0.12f;
            }
            trail.Clear();
            trail.emitting = true;
        }

        // ---------- escape ----------

        void Escape() => Escape(true);

        void Escape(bool spendCooldown)
        {
            if (spendCooldown) escapeReadyAt = Time.time + def.EscapeCooldown;
            Vector3 pos = transform.position;
            prey.RefreshStamina();   // an escape is never cut short by tiredness
            var tint = new Color(0.8f, 0.92f, 1f);

            if (def.SpeedBoost > 1f)
            {
                prey.Boost = def.SpeedBoost; prey.Zigzag = def.Zigzag; prey.Silent = def.Silent;
                speedUntil = Time.time + def.SpeedSeconds;
                prey.ForceFleeUntil = speedUntil;
            }
            if (def.GuardSeconds > 0f)
            {
                guardStarted = Time.time;
                guardUntil = guardStarted + def.GuardSeconds;
                if (def.GuardRecovery > 0f)
                {
                    UpdateShell(true);
                    if (!hasShellParameter) (locomotion ??= GetComponent<CreatureLocomotion>())?.PlayAction("Retract");
                    if (HangOneGround.Wet(pos))
                        SkillFx.Burst(pos + Vector3.up * 0.06f, new Color(0.57f, 0.66f, 0.53f), 3, 0.35f, 0.055f, 0.15f, 0.25f, 0.06f);
                }
                prey.DamageTakenMultiplier = def.GuardDamageTaken;
                if (def.BlocksCatch) blocksCatchUntil = guardUntil;
                CreatureStatus.Stun(prey, def.GuardSeconds, false);   // stands still
            }
            if (def.ObscureSeconds > 0f)
            {
                foreach (var h in Predator.Active)
                    if (h != null && (h.transform.position - pos).sqrMagnitude <= def.ObscureRadius * def.ObscureRadius) h.LoseTarget(def.ObscureSeconds);
                SkillFx.Ring(pos, def.ObscureRadius, tint, 0.6f, 0.2f);
                SkillFx.Burst(pos + Vector3.up * 0.2f, tint, 14, 1.2f, 0.16f, 1.2f, 0.3f, def.ObscureRadius * 0.3f);
            }
            if (def.SlowSeconds > 0f && def.SlowRadius > 0f) SlowPuddle.Spawn(pos, def.SlowRadius, def.SlowFraction, def.SlowSeconds);
            if (def.TrapSeconds > 0f) SpikeTrap.Spawn(pos, 1.1f, def.TrapDamage, def.TrapSeconds, 0f, null, new Color(0.68f, 0.55f, 1f), true, name);
            if (def.Decoy)
            {
                SpawnDecoy();
                if (def.EscapesGrab) blocksCatchUntil = Time.time + 0.8f;   // slipped out of the grab
            }
        }

        /// <summary>A copy of what it looks like, left standing for a few seconds (placeholder: only a look until hunters hunt prey).</summary>
        void SpawnDecoy()
        {
            var root = new GameObject(name + " (decoy)");
            root.transform.SetPositionAndRotation(transform.position, transform.rotation);
            foreach (var f in GetComponentsInChildren<MeshFilter>())
            {
                if (!f.TryGetComponent(out MeshRenderer r)) continue;
                var part = new GameObject("part");
                part.transform.SetParent(root.transform, false);
                part.transform.SetPositionAndRotation(f.transform.position, f.transform.rotation);
                part.transform.localScale = f.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = f.sharedMesh;
                part.AddComponent<MeshRenderer>().sharedMaterials = r.sharedMaterials;
            }
            Destroy(root, 6f);
        }

        // ---------- counter ----------

        void Counter(SurvivalStats player, Predator hunter, Vector3 away)
        {
            counterReadyAt = Time.time + def.CounterCooldown;
            float dmg = def.CounterDamage + (def.CounterInfect == InfectKind.Burn ? 2f : 0f);
            Vector3 toThreat = -away;

            if (hunter != null)
            {
                if (dmg > 0f) hunter.TakeDamage(dmg, null);
                if (def.CounterPush > 0f) CreatureStatus.Knock(hunter, toThreat * def.CounterPush, 0.3f);
                if (def.CounterStun > 0f) CreatureStatus.Stun(hunter, def.CounterStun);
                if (def.CounterSlow > 0f) CreatureStatus.Slow(hunter, def.CounterSlow, def.CounterSlowSeconds);
            }
            else if (player != null)
            {
                Vector3 knock = toThreat * def.CounterPush;
                if (player.IsRemote)
                {
                    if (dmg > 0f) NetWorld.BiteRemote?.Invoke(player, dmg, knock);   // the damage is applied on the player's own machine
                }
                else
                {
                    if (dmg > 0f) player.State.Damage(dmg);
                    if (def.CounterPush > 0f && player.TryGetComponent(out Rigidbody rb) && !rb.isKinematic) rb.AddForce(knock + Vector3.up * 1.5f, ForceMode.VelocityChange);
                    if (def.CounterSlow > 0f) BodyState.On(player.gameObject).Slow(def.CounterSlow, def.CounterSlowSeconds);
                    if (def.CounterInfect == InfectKind.Spore) player.State.TryInfect(DiseaseKind.SporeBlur, 0.6f);
                    else if (def.CounterInfect == InfectKind.Rot) player.State.TryInfect(DiseaseKind.RottenGel, 0.6f);
                    // shortcut: a stunning counter (Dơi Âm Thoa) only pushes a player; a player cannot be stunned by a creature yet
                }
            }
            if (def.CounterDrain > 0f) prey.Heal(dmg * def.CounterDrain);
            SkillFx.Burst(transform.position + Vector3.up * 0.2f, new Color(1f, 0.85f, 0.6f), 8, 2.2f, 0.12f, 0.5f, 0.4f, 0.15f);
        }

        // ---------- special mechanics ----------

        /// <summary>Calls the herd: every prey with a defence within AlarmRadius runs now (Dế Gió).</summary>
        void Alarm()
        {
            SkillFx.Ring(transform.position, def.AlarmRadius * 0.5f, new Color(1f, 0.95f, 0.7f), 0.5f, 0.1f);
            foreach (var other in Prey.Active)
            {
                if (other == null || other == prey || (other.transform.position - transform.position).sqrMagnitude > def.AlarmRadius * def.AlarmRadius) continue;
                if (other.TryGetComponent(out PreyDefense d)) d.Alerted();
            }
        }

        void UpdateShell(bool closed)
        {
            if (!hasShellParameter || shellClosed == closed) return;
            shellClosed = closed;
            shellAnimator.SetBool(ShellClosedId, closed);
        }

        void LateUpdate()
        {
            if (def?.GlideWindup > 0f && !prey.IsReplica) PoseWings();
            if (def == null || def.GuardRecovery <= 0f || prey.IsReplica) return;
            if (shellModel == null)
            {
                shellModel = transform.Find("Model");
                if (shellModel == null) return;
                shellScale = shellModel.localScale;
            }
            float closed = Mathf.Clamp01((Time.time - guardStarted) * 6f) * Mathf.Clamp01((guardUntil - Time.time) * 6f);
            shellModel.localScale = Vector3.Scale(shellScale, new Vector3(1f, 1f - 0.24f * closed, 1f - 0.16f * closed));
        }

        void PoseWings()
        {
            if (wingLeft == null || wingRight == null) return;
            bool gliding = flightStarted > 0f;
            if (launchAt <= 0f && !gliding)
            {
                if (!wingsPosed) return;
                wingLeft.localRotation = wingLeftRest;
                wingRight.localRotation = wingRightRest;
                wingsPosed = false;
                return;
            }

            float progress = gliding ? 1f : Mathf.Clamp01(1f - (launchAt - Time.time) / def.GlideWindup);
            float open = (36f + 6f * Mathf.Sin(Time.time * 72f)) * Mathf.SmoothStep(0f, 1f, progress);
            wingLeft.localRotation = wingLeftRest;
            wingRight.localRotation = wingRightRest;
            wingLeft.Rotate(transform.forward, -open, Space.World);
            wingRight.Rotate(transform.forward, open, Space.World);
            wingsPosed = true;
        }

        Transform shellModel;
        Vector3 shellScale;

        /// <summary>Another prey raised the alarm: run (once the escape is ready).</summary>
        public void Alerted()
        {
            if (def == null || prey.IsReplica || Prey.Frozen || def.GlideWindup > 0f || !def.HasEscape || Time.time < escapeReadyAt) return;
            escapeReadyAt = Time.time + def.EscapeCooldown;
            Escape(false);
        }

        /// <summary>Cries for the helper species (Chim Non → Chim Hang Mẹ): any such Predator in reach goes for the player.</summary>
        void CallHelp(SurvivalStats player)
        {
            if (string.IsNullOrEmpty(def.HelperName) || player == null) return;
            float r2 = def.CallHelpRadius * def.CallHelpRadius;
            foreach (var h in Predator.Active)
            {
                if (h == null || (h.transform.position - transform.position).sqrMagnitude > r2) continue;
                if (SpeciesNames.Key(h.name) == SpeciesNames.Key(def.HelperName)) h.Provoke(player);   // "Chim Hang Mẹ" or the prefab name "ChimHangMe"
            }
        }
    }

    /// <summary>A puddle of slime that slows whoever stands in it (Sên Rêu): hunters and the local slime.</summary>
    sealed class SlowPuddle : MonoBehaviour
    {
        float radius, fraction, until;
        Material mat;

        public static void Spawn(Vector3 at, float radius, float fraction, float seconds)
        {
            at.y = Evolve.World.GroundHeight.At(at.x, at.z, at.y - 0.3f) + 0.03f;
            var go = new GameObject("SlowPuddle") { layer = 2 };
            go.transform.position = at;
            var p = go.AddComponent<SlowPuddle>();
            p.radius = radius; p.fraction = fraction; p.until = Time.time + seconds;
            p.mat = SkillFx.Gel(new Color(0.55f, 0.85f, 0.45f), 0.5f, 0f, 1.2f);
            go.AddComponent<MeshFilter>().sharedMesh = SkillFxMeshes.Ring();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = p.mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.transform.localScale = Vector3.one * radius;
        }

        void Update()
        {
            float left = until - Time.time;
            if (left <= 0f) { Destroy(gameObject); return; }
            mat.SetFloat("_Fade", Mathf.Clamp01(left / 1.5f));
            float r2 = radius * radius;
            foreach (var h in Predator.Active)
                if (h != null && (h.transform.position - transform.position).sqrMagnitude <= r2) CreatureStatus.Slow(h, fraction, 0.3f);
            foreach (var p in SurvivalStats.All)
                if (p != null && !p.IsRemote && !p.State.IsDead && (p.transform.position - transform.position).sqrMagnitude <= r2)
                    BodyState.On(p.gameObject).Slow(fraction, 0.3f);
        }

        void OnDestroy() { if (mat != null) Destroy(mat); }
    }
}
