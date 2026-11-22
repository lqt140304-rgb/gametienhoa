using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// The Unity side of the golem's axe form (the rules are pure: <see cref="AxeFsm"/>, <see cref="FetchFsm"/>, <see cref="PullRules"/>, <see cref="PushAura"/>, <see cref="ShockwaveRules"/>):
    /// the axe stuck in the heart stone at the room's <c>Axe_Anchor</c> (the map's prop if it has one, else its own; with no anchor in the scene a test one is made 8 m ahead of the golem),
    /// the walk to it and the pull in three struggling heaves (whoever comes near is thrown back), the axe in its hand, the throw and where it lands, and the Bổ Đất shock wave.
    /// CreatureCombat calls <see cref="Tick"/> every physics step and <see cref="OnStrike"/> when a move of the axe starts its strike. Nothing here has been run in Unity yet.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(80)]   // after GolemRigFx (70): the axe is put in the hand where the arm ended up this frame, not where the animation left it (it used to sit at the hips)
    public class GolemAxe : MonoBehaviour
    {
        const float WalkSpeed = 3.2f, WaveSpeed = 14f, WaveLength = 14f, WaveWidth = 2.4f, WaveDepth = 1.2f, BladeYaw = -90f;   // BladeYaw: the blade of the FBX points along +X; turns it to the front (check in Unity)
        static readonly Color Dust = new Color(0.58f, 0.52f, 0.45f);

        CreatureCombat combat;
        readonly AxeFsm fsm = new AxeFsm();
        readonly PushAura aura = new PushAura();
        FetchFsm fetch;
        MoveDef fetchMove;
        GameObject planted, held;          // the axe stuck in the ground / in the golem's hand (or in flight)
        Vector3 plantedBase; Quaternion plantedRot;
        Transform rightHand, grip;
        GolemRigFx rig;
        int heaveSeen = -1;
        float swatT = 9f;
        // the shock wave in progress
        bool waving; float waveT; Vector3 waveOrigin, waveDir; MoveDef waveMove;
        readonly System.Collections.Generic.HashSet<SurvivalStats> waveHit = new System.Collections.Generic.HashSet<SurvivalStats>();

        public AxeFsm Fsm => fsm;
        public MoveForm Form => fsm.Form;
        public bool Fetching => fetch != null && fetch.Stage != FetchStage.Done;
        public FetchStage FetchStageNow => fetch != null ? fetch.Stage : FetchStage.Done;
        /// <summary>Seconds into the pull (0 before it starts).</summary>
        public float FetchPullSeconds => fetch != null ? fetch.PullProgress01 * PullRules.AxeSeconds : 0f;
        /// <summary>Seconds since a push-away swat began (large when none).</summary>
        public float SwatTime => swatT;

        void Awake()
        {
            combat = GetComponent<CreatureCombat>();
            fetchMove = new MoveDef { Name = "Rút Rìu", Shape = MoveShape.Fetch, Windup = 0.3f, Active = 0.1f, Recover = 0.1f, MinRange = 0f, MaxRange = 99f, Armored = true, Anim = "AxePull" };
        }

        void Start() => PlaceInitialAxe();

        // ---------- the axe in the room ----------

        void PlaceInitialAxe()
        {
            var anchor = GameObject.Find("Axe_Anchor");
            if (anchor != null)
            {
                plantedBase = anchor.transform.position; plantedRot = anchor.transform.rotation;
                foreach (Transform c in anchor.transform) if (c.name.Contains("GolemAxe")) { planted = c.gameObject; break; }   // the map's own prop
                if (planted == null) planted = SpawnVisual("GolemAxe_Planted", plantedBase, plantedRot);
            }
            else   // no room (a test arena, a cave): the axe stands 8 m ahead of where the golem woke
            {
                Vector3 p = transform.position + transform.forward * 8f;
                plantedBase = new Vector3(p.x, Evolve.World.GroundHeight.At(p.x, p.z, p.y), p.z); plantedRot = Quaternion.Euler(0f, transform.eulerAngles.y + 180f, 0f);
                planted = SpawnVisual("GolemAxe_Planted", plantedBase, plantedRot);
            }
        }

        /// <summary>The prefab from Resources/Props (made by the menu Evolve → Build Golem Axe Prefabs), or a stand-in of plain shapes when it does not exist yet.</summary>
        static GameObject SpawnVisual(string prefabName, Vector3 pos, Quaternion rot)
        {
            var prefab = Resources.Load<GameObject>("Props/" + prefabName);
            if (prefab != null) { var go = Instantiate(prefab, pos, rot); foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c); return go; }
            var root = new GameObject(prefabName + " (stand-in)"); root.transform.SetPositionAndRotation(pos, rot);
            var mat = SkillFx.Gel(new Color(0.25f, 0.26f, 0.29f), 1f, 0.2f, 0.8f);
            Part(root.transform, PrimitiveType.Cylinder, new Vector3(0.14f, 1.45f, 0.14f), new Vector3(0f, 1.6f, 0f), Quaternion.identity, mat);   // the handle: 2.9 m up from the stone
            Part(root.transform, PrimitiveType.Cube, new Vector3(0.12f, 1.1f, 0.9f), new Vector3(0f, 0.4f, 0f), Quaternion.identity, mat);          // the blade, in the stone
            if (prefabName == "GolemAxe") { var g = new GameObject("Grip"); g.transform.SetParent(root.transform, false); g.transform.localPosition = new Vector3(0f, 0.8f, 0f); }
            return root;
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 scale, Vector3 pos, Quaternion rot, Material mat)
        {
            var p = GameObject.CreatePrimitive(type); Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent, false); p.transform.localScale = scale; p.transform.localPosition = pos; p.transform.localRotation = rot;
            p.GetComponent<MeshRenderer>().sharedMaterial = mat; p.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------- every physics step ----------

        /// <summary>Called by CreatureCombat before the scheduler moves on: starts a fetch when the axe says so, and runs the one in progress, the push-away and the shock wave.</summary>
        public void Tick()
        {
            float dt = Time.fixedDeltaTime;
            fsm.Tick(dt); swatT += dt;
            if (waving) TickWave(dt);
            if (fetch == null)
            {
                if (fsm.ShouldFetch(combat.HealthFraction, combat.IsIdle, combat.PhaseRoarPending) && combat.TryForce(fetchMove, fetchMove.Windup))
                {
                    float dist = new Vector3(plantedBase.x - transform.position.x, 0f, plantedBase.z - transform.position.z).magnitude;
                    fetch = new FetchFsm(PullRules.AxeSeconds, FetchFsm.WalkTimeoutFor(dist, WalkSpeed)); heaveSeen = -1;   // a far axe gets a longer walk: it used to give up after 4 s (13 m) and pull from afar
                    Debug.Log("[Rìu] Golem đi lấy rìu: cách " + dist.ToString("0.0") + " m");
                    GolemFx.Point(transform.position);   // a growl as it turns toward the axe
                }
                return;
            }
            Vector3 to = new Vector3(plantedBase.x - transform.position.x, 0f, plantedBase.z - transform.position.z);
            float reach = combat.BodyRadius + 1.3f;
            bool arrived = to.magnitude <= reach;
            if (fetch.Stage == FetchStage.Walk && !arrived && to.sqrMagnitude > 1e-4f) { combat.FaceTargetNow(); combat.MoveBody(to.normalized, WalkSpeed); }   // it walks to the axe, eyes on the player
            var e = fetch.Update(dt, arrived);
            if (fetch.Stage == FetchStage.Pull) { combat.FaceTargetNow(); Pull(); PushNearby(); }
            if (e == FetchEvent.Done) FinishPull();
        }

        void Pull()
        {
            float p = fetch.PullProgress01;
            float lift = PullRules.Lift(p, PullRules.AxeHeaves), strain = PullRules.Strain(p, PullRules.AxeHeaves);
            if (planted != null)
            {
                planted.transform.position = plantedBase + Vector3.up * (0.9f * lift) + planted.transform.right * (Mathf.Sin(Time.time * 45f) * 0.05f * strain);   // the axe shudders and rises out of the stone
            }
            int heave = Mathf.Min(PullRules.AxeHeaves - 1, (int)(p * PullRules.AxeHeaves)); float f = p * PullRules.AxeHeaves - heave;
            if (f > 0.4f && heave > heaveSeen)
            {
                heaveSeen = heave;
                SkillFx.Dust(plantedBase, 1.6f, Dust, 20);
                ProceduralSfx.PlayFile("Golem/golem_strain", transform.position, 1f, ProceduralSfx.Kind.HoundGrowlShort);
                ImpactFx.Thump(plantedBase, 0.12f + 0.08f * heave, 0.35f, 22f);   // each heave shakes the floor a little more
            }
        }

        void FinishPull()
        {
            fsm.Pulled();
            if (planted != null) { Destroy(planted); planted = null; }
            if (rightHand == null) foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "RightHand") { rightHand = t; break; }
            held = SpawnVisual("GolemAxe", rightHand != null ? rightHand.position : transform.position, transform.rotation);   // born in the hand, not at the feet
            Debug.Log("[Rìu] Golem rút xong rìu, cầm trong tay (bàn tay tìm thấy: " + (rightHand != null) + ", điểm nắm Grip: " + (held.transform.Find("Grip") != null) + ")");
            grip = held.transform.Find("Grip");
            SkillFx.Burst(plantedBase + Vector3.up * 0.4f, new Color(1f, 0.7f, 0.3f), 30, 5f, 0.12f, 0.9f, 0.6f);
            SkillFx.Ring(plantedBase + Vector3.up * 0.1f, 5f, new Color(1f, 0.8f, 0.5f), 0.7f, 0.1f);
            ImpactFx.Thump(plantedBase, 0.4f, 0.9f, 30f);
            ProceduralSfx.PlayFile("Golem/golem_roar", transform.position, 0.8f, ProceduralSfx.Kind.HoundRoar);
            fetch = null;
        }

        /// <summary>While it strains, whoever comes within 4 m is thrown back, hurt a little and stunned a second (each player once per 1.5 s): the pull is a moment for ranged attacks only.</summary>
        void PushNearby()
        {
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                Vector3 away = p.transform.position - transform.position; away.y = 0f;
                if (!aura.TryPush(p.GetEntityId().GetHashCode(), away.magnitude, Time.time)) continue;
                swatT = 0f;
                if (away.sqrMagnitude < 1e-4f) away = transform.forward;
                p.State.Damage(PushAura.Damage * combat.DamageScale);
                Impulse.Kick(p.GetComponent<Rigidbody>(), away.normalized * PushAura.Force + Vector3.up * PushAura.Force * 0.45f);
                if (p.TryGetComponent(out Evolve.Player.SlimeController sc)) sc.FreezeFor(PushAura.Freeze);
                ProceduralSfx.PlayFile("Golem/golem_punch_hit", transform.position, 1f, ProceduralSfx.Kind.HoundThud);
                SkillFx.Burst(p.transform.position, Dust, 12, 4f, 0.1f, 0.6f, 0.5f);
            }
        }

        // ---------- the strike of the axe's moves ----------

        public void OnStrike(MoveDef m)
        {
            if (m.Shape == MoveShape.AxeThrow) Throw(m);
            else if (m.Shape == MoveShape.Shockwave) BeginWave(m);
        }

        void Throw(MoveDef m)
        {
            if (fsm.Place != AxePlace.Held || held == null) return;
            Vector3 to = combat.AimPoint(m.FlightSeconds) + Vector3.up * 0.2f;
            fsm.Thrown();
            var axe = held; held = null; grip = null;
            if (rightHand != null) axe.transform.position = rightHand.position;   // it leaves from the hand
            Debug.Log("[Rìu] ném rìu từ " + axe.transform.position.ToString("0.0") + " tới " + to.ToString("0.0"));
            AxeFlight.Launch(axe, to, m.FlightSeconds, m.Damage * combat.DamageScale, OnLanded);
            ProceduralSfx.PlayFile("Golem/golem_hurl", transform.position, 1f, ProceduralSfx.Kind.HoundLunge);
        }

        void OnLanded(Vector3 point, bool reachable, GameObject flyer)
        {
            if (flyer != null) Destroy(flyer);
            var landing = fsm.Landed(reachable);
            Debug.Log("[Rìu] rìu hạ cánh tại " + point.ToString("0.0") + ", tới được: " + reachable);
            Vector3 at = point;
            if (landing == AxeLanding.AtGolem) at = transform.position + transform.forward * 1.5f;   // out of reach: it falls at the golem's feet and the fight goes on
            plantedBase = new Vector3(at.x, Evolve.World.GroundHeight.At(at.x, at.z, at.y), at.z); plantedRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            planted = SpawnVisual("GolemAxe_Planted", plantedBase, plantedRot);
            SkillFx.Dust(plantedBase, 2f, Dust, 30); SkillFx.Burst(plantedBase + Vector3.up * 0.3f, Dust, 16, 4f, 0.1f, 0.8f, 0.6f);
            ImpactFx.Thump(plantedBase, 0.3f, 0.7f, 26f);
            ProceduralSfx.PlayFile("Golem/golem_land", plantedBase, 1f, ProceduralSfx.Kind.HoundThud);
        }

        void BeginWave(MoveDef m)
        {
            waving = true; waveT = 0f; waveMove = m; waveHit.Clear();
            waveOrigin = transform.position; waveDir = combat.StrikeDirection;
            ImpactFx.Thump(waveOrigin, 0.45f, 1f, 30f);
        }

        void TickWave(float dt)
        {
            waveT += dt;
            float front = ShockwaveRules.Front(WaveSpeed, waveT, WaveLength);
            Vector3 at = waveOrigin + waveDir * front;
            SkillFx.Dust(at, 1.2f, Dust, 6); if (Random.value < 0.5f) SkillFx.Burst(at + Vector3.up * 0.2f, Dust, 4, 3f, 0.1f, 0.6f, 0.7f);   // a trail of cracks and chips runs along the floor
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead || waveHit.Contains(p)) continue;
                Vector3 pos = p.transform.position;
                if (Evolve.World.GroundHeight.OffsetOf(pos) > 1.0f) continue;   // jumping over the wave dodges it
                if (!ShockwaveRules.Hits(waveOrigin.x, waveOrigin.z, waveDir.x, waveDir.z, WaveLength, WaveWidth, front, WaveDepth, pos.x, pos.z)) continue;
                waveHit.Add(p);
                p.State.Damage(waveMove.Damage * combat.DamageScale);
                Impulse.Kick(p.GetComponent<Rigidbody>(), (Vector3.Cross(waveDir, Vector3.up) * (Random.value < 0.5f ? 1f : -1f) + Vector3.up * 0.6f) * waveMove.Force);
                if (p.TryGetComponent(out Evolve.Player.SlimeController sc)) sc.FreezeFor(0.4f);
            }
            if (front >= WaveLength && waveT * WaveSpeed >= WaveLength + 2f) waving = false;
        }

        // ---------- the axe in the golem's hand ----------

        void LateUpdate()
        {
            if (held == null || grip == null) return;
            if (rightHand == null) foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "RightHand") { rightHand = t; break; }
            if (rightHand == null) return;
            // the haft leans back when the arms are up and forward when they come down (from the pose of the arms)
            if (rig == null) TryGetComponent(out rig);
            float raise = rig != null ? rig.CurrentPose.Raise : 0f;
            float lean = Mathf.Lerp(70f, -25f, Mathf.InverseLerp(-20f, 165f, raise));
            held.transform.rotation = Quaternion.AngleAxis(lean, transform.right) * transform.rotation * Quaternion.Euler(0f, BladeYaw, 0f);
            held.transform.position += rightHand.position - grip.position;
        }

        /// <summary>Stunned, killed or switched off: the fetch ends, the visuals stay where they are (the axe is dropped where the golem stands if it held it).</summary>
        public void Abort()
        {
            if (fetch != null) { fetch.Abort(); fetch = null; }
            waving = false;
        }

        void OnDestroy()
        {
            if (held != null) Destroy(held);
            if (planted != null && planted.name.Contains("stand-in")) Destroy(planted);
        }
    }
}
