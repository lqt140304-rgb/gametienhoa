using System;
using Evolve.Abilities;
using Evolve.Combat;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// A boss room, wired by the names of its children (see docs/BOSS_ROOM_CONTRACT.md): Boss_Wait, Activate_Zone, Door_Steel with Door_Open_Pos and Door_Closed_Pos, Axe_Anchor (the golem's room).
    /// The boss sleeps (inactive) at Boss_Wait. When every living player is inside Activate_Zone (<see cref="RoomRules.ShouldActivate"/>) a countdown runs, the steel door shuts (<see cref="DoorFsm"/>), and two
    /// seconds later the boss wakes. When the boss dies the door opens; when every player is dead the door opens and the boss goes back to sleep at full health. A missing child is reported once and skipped.
    /// shortcut: players dead while others fight are not counted again after respawn; one room wakes once per fight. Upgrade: per-player entry tracking when co-op rooms are tested.
    /// </summary>
    public class BossRoom : MonoBehaviour
    {
        const float CountdownSeconds = 2f, WakeDelayAfterDoor = 2f;

        [SerializeField, Tooltip("The boss; if empty the nearest boss to Boss_Wait is used.")] GameObject boss;
        Transform wait, doorSteel, doorOpen, doorClosed;
        Collider zone, bounds;
        readonly DoorFsm door = new DoorFsm();
        enum Stage { Idle, Counting, Closing, Fighting, Done }
        Stage stage; float stageTime;
        Predator pred;

        public event Action Activated;
        public Transform AxeAnchor { get; private set; }

        T Find<T>(string childName, bool required = true) where T : Component
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == childName) { var c = typeof(T) == typeof(Transform) ? t as T : t.GetComponent<T>(); if (c != null) return c; }
            if (required) Debug.LogWarning("[Phòng trùm] " + name + " thiếu " + childName + " (xem docs/BOSS_ROOM_CONTRACT.md).");   // Axe_Anchor is only for the golem's room
            return default;
        }

        void Start()
        {
            wait = Find<Transform>("Boss_Wait"); doorSteel = Find<Transform>("Door_Steel"); doorOpen = Find<Transform>("Door_Open_Pos"); doorClosed = Find<Transform>("Door_Closed_Pos");
            zone = Find<Collider>("Activate_Zone"); bounds = Find<Collider>("Room_Bounds"); AxeAnchor = Find<Transform>("Axe_Anchor", false);
            if (boss == null && wait != null)   // the boss standing nearest to the waiting place
            {
                float best = 12f;
                foreach (var c in FindObjectsByType<CreatureCombat>(FindObjectsInactive.Include, FindObjectsSortMode.None))   // a boss saved inactive in the scene counts too { float d = Vector3.Distance(c.transform.position, wait.position); if (d < best) { best = d; boss = c.gameObject; } }
            }
            if (boss != null)
            {
                CreatureTuning.Apply(boss, "BossRoom");   // a boss placed by hand gets the size a cave lord has (a golem placed in a room was unscaled); a spawned one is already tuned and is skipped
                pred = boss.GetComponent<Predator>(); if (wait != null) boss.transform.position = wait.position; boss.SetActive(false);
            }
            else Debug.LogWarning("[Phòng trùm] " + name + ": không tìm thấy trùm trong 12 m quanh Boss_Wait: đặt trùm vào phòng (với phòng Golem: menu Evolve → Boss Rooms → Place Golem In The Open Boss Room) hoặc kéo trùm vào ô Boss của BossRoom.");
            ApplyDoor();
            Debug.Log("[Phòng trùm] " + name + ": trùm=" + (boss != null ? boss.name : "KHÔNG THẤY") + ", Activate_Zone=" + (zone != null ? zone.GetType().Name : "KHÔNG THẤY") + ", Boss_Wait=" + (wait != null) + ", cửa=" + (doorSteel != null && doorOpen != null && doorClosed != null));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            door.Tick(dt); ApplyDoor();
            stageTime += dt;
            switch (stage)
            {
                case Stage.Idle:
                    if (zone != null && boss != null && Count(out int inside, out int alive))
                    {
                        if (inside != lastInside || alive != lastAlive) { lastInside = inside; lastAlive = alive; Debug.Log("[Phòng trùm] " + name + ": " + inside + " người chơi trong vùng kích hoạt / " + alive + " còn sống (cần bằng nhau, và ít nhất 1)."); }
                        if (RoomRules.ShouldActivate(inside, alive)) Go(Stage.Counting);
                    }
                    break;
                case Stage.Counting:
                    if (stageTime >= CountdownSeconds) { door.Close(); ProceduralSfx.PlayFile("Golem/golem_door", transform.position, 1f, ProceduralSfx.Kind.HoundThud); Go(Stage.Closing); }
                    break;
                case Stage.Closing:
                    if (door.State == DoorState.Closed && stageTime >= DoorFsm.CloseSeconds + WakeDelayAfterDoor) Wake();
                    break;
                case Stage.Fighting:
                    if (boss == null || !boss.activeSelf || (pred != null && pred.Health <= 0f)) { door.Open(); Go(Stage.Done); }
                    else if (Count(out _, out int living) && living == 0) { ResetBoss(); door.Open(); Go(Stage.Idle); }   // everyone died: the room sleeps again
                    break;
            }
        }

        int lastInside = -1, lastAlive = -1;

        void Go(Stage s) { stage = s; stageTime = 0f; Debug.Log("[Phòng trùm] " + name + " → " + s); }

        /// <summary>Players that are alive (everyone connected) and how many of them stand inside the activation zone.</summary>
        bool Count(out int inside, out int alive)
        {
            inside = alive = 0;
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.State.IsDead) continue;
                alive++;
                if (zone != null && Inside(p.transform.position)) inside++;
            }
            return true;
        }

        /// <summary>Is the point in the activation zone? A non-convex mesh collider cannot answer ClosestPoint, so its bounds are used.</summary>
        bool Inside(Vector3 pos)
        {
            if (zone is MeshCollider mc && !mc.convex) return zone.bounds.Contains(pos);
            return zone.ClosestPoint(pos) == pos;
        }

        void Wake()
        {
            boss.SetActive(true);
            ProceduralSfx.PlayFile("Golem/golem_awaken", boss.transform.position, 1f, ProceduralSfx.Kind.HoundRoar);
            ImpactFx.Thump(boss.transform.position, 0.5f, 1.5f, 40f);
            Activated?.Invoke();
            Go(Stage.Fighting);
        }

        void ResetBoss()
        {
            if (boss == null) return;
            if (pred != null) pred.Heal(pred.MaxHealth);
            if (wait != null) boss.transform.position = wait.position;
            boss.SetActive(false);
        }

        void ApplyDoor()
        {
            if (doorSteel == null || doorOpen == null || doorClosed == null) return;
            doorSteel.position = Vector3.Lerp(doorClosed.position, doorOpen.position, door.Openness);
        }
    }
}
