using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Civilization
{
    /// <summary>
    /// A band of treefolk (spec 2026-11-17-treefolk-grudge-design): what it thinks of each player (<see cref="Ledger"/>),
    /// how news of an attack spreads from one member to the next (once a second), the root walls that close the cave to a
    /// player it hates, and the standing shown on screen near the cave. Runs on the host; a co-op client only mirrors it.
    /// The members are the Treefolk children of this object.
    /// </summary>
    public class TreefolkTribe : MonoBehaviour
    {
        static readonly List<TreefolkTribe> all = new List<TreefolkTribe>();
        public static IReadOnlyList<TreefolkTribe> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll() => all.Clear();

        public int TribeId;
        public string DisplayName = "Người cây";
        public Transform Hall;
        public Transform[] HomePoints;
        [Tooltip("Root walls that close the cave's door and slit to a hated player")]
        public GameObject[] Gates;
        public Vector3 CaveCenter;
        public float GateRadius = 40f;
        public float ShowStandingRadius = 50f;

        public TribeLedger Ledger { get; } = new TribeLedger();
        public bool GatesClosed { get; private set; }
        public Vector3 HallPosition => Hall != null ? Hall.position : transform.position;
        /// <summary>Client side: the host's reputation value for this machine's player.</summary>
        public float ClientValue { get; set; }
        public bool IsReplica { get; set; }

        readonly List<SurvivalStats> players = new List<SurvivalStats>();
        float nextPlayers, nextTalk, messageUntil;
        string message;
        Standing? lastShown;
        static GUIStyle labelStyle, messageStyle;

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() => all.Remove(this);

        public IReadOnlyList<SurvivalStats> Players => players;

        public SurvivalStats LocalPlayer
        {
            get
            {
                foreach (var p in players) if (p != null && !p.IsRemote) return p;
                return null;
            }
        }

        public Vector3 RandomHomePoint()
        {
            if (HomePoints == null || HomePoints.Length == 0) return HallPosition;
            var t = HomePoints[Random.Range(0, HomePoints.Length)];
            return t != null ? t.position : HallPosition;
        }

        /// <summary>This machine's player's standing with the tribe.</summary>
        public Standing LocalStanding()
        {
            if (IsReplica) return ReputationRules.StandingOf(ClientValue);
            var me = LocalPlayer;
            return me != null ? Ledger.StandingOf(me) : Standing.Neutral;
        }

        /// <summary>The nearest player within range that this member knows about and dislikes (Cold or Hostile).</summary>
        public SurvivalStats NearestKnownTarget(Treefolk member, float range, out Standing standing)
        {
            standing = Standing.Neutral;
            SurvivalStats best = null;
            float bestD = range * range;
            foreach (var p in players)
            {
                if (p == null || p.State.IsDead || !member.Mind.Knows(p)) continue;
                var s = Ledger.StandingOf(p);
                if (s != Standing.Hostile && s != Standing.Cold) continue;
                float d = (p.transform.position - member.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; standing = s; }
            }
            return best;
        }

        /// <summary>A member was hit (or killed) by a player: the ledger changes, the victim and every witness learn who did it.</summary>
        public void ReportAttack(Treefolk victim, SurvivalStats by, bool killed)
        {
            if (IsReplica || by == null) return;
            RefreshPlayers();
            var companions = new List<object>();
            foreach (var p in players)
                if (p != null && p != by && (p.transform.position - by.transform.position).sqrMagnitude <= ReputationRules.CompanionRadius * ReputationRules.CompanionRadius)
                    companions.Add(p);
            Ledger.Offend(by, killed ? ReputationRules.KillPenalty : ReputationRules.HitPenalty, companions);

            victim.Mind.Learn(by);   // the dead remember too: their body tells whoever comes by
            foreach (var c in companions) victim.Mind.Learn(c);
            if (!victim.IsDead) victim.Hear(by);
            foreach (var t in Treefolk.Active)
            {
                if (t == null || t == victim || t.Tribe != this || t.IsDead) continue;
                if ((t.transform.position - victim.transform.position).sqrMagnitude > ReputationRules.WitnessRadius * ReputationRules.WitnessRadius) continue;
                if (!CanSee(t.transform.position, victim.transform.position)) continue;
                t.Hear(by);
                foreach (var c in companions) t.Hear(c);
            }
        }

        /// <summary>An offering at the stone (host side). False if it changed nothing.</summary>
        public bool Offer(SurvivalStats by) => !IsReplica && by != null && Ledger.Offer(by);

        static bool CanSee(Vector3 from, Vector3 to)
        {
            Vector3 a = from + Vector3.up * 1.1f, b = to + Vector3.up * 0.8f;
            if (!Physics.Linecast(a, b, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponentInParent<Treefolk>() != null || hit.collider.GetComponentInParent<SurvivalStats>() != null;
        }

        void RefreshPlayers()
        {
            players.Clear();
            players.AddRange(FindObjectsByType<SurvivalStats>());
            nextPlayers = Time.time + 1f;
        }

        void Update()
        {
            if (Time.time >= nextPlayers) RefreshPlayers();
            if (!IsReplica)
            {
                if (Time.time >= nextTalk) { nextTalk = Time.time + 1f; Talk(); }
                bool close = false;
                foreach (var p in players)
                    if (p != null && Ledger.StandingOf(p) == Standing.Hostile && (p.transform.position - CaveCenter).sqrMagnitude <= GateRadius * GateRadius)
                        close = true;
                SetGates(close);
            }
            var now = LocalStanding();
            if (lastShown.HasValue && now != lastShown.Value)
            {
                message = now < lastShown.Value ? $"{DisplayName} giờ coi bạn là: {ReputationRules.Name(now)}" : $"{DisplayName} đã nguôi giận: {ReputationRules.Name(now)}";
                messageUntil = Time.time + 4f;
            }
            lastShown = now;
        }

        /// <summary>One round of talk: the living pass news to each other, a body tells anyone who comes near it.</summary>
        void Talk()
        {
            var positions = new List<Vector3>();
            var minds = new List<Knowledge>();
            var canTalk = new List<bool>();
            var members = new List<Treefolk>();
            foreach (var t in Treefolk.Active)
            {
                if (t == null || t.Tribe != this) continue;
                members.Add(t);
                positions.Add(t.transform.position);
                minds.Add(t.Mind);
                canTalk.Add(!t.IsDead);
            }
            var before = new int[members.Count];
            for (int i = 0; i < members.Count; i++) before[i] = members[i].Mind.Count;
            Rumour.Spread(positions, minds, canTalk, ReputationRules.SpreadRadius);
            foreach (var body in members)
            {
                if (!body.IsDead || body.Mind.Count == 0) continue;
                foreach (var t in members)
                {
                    if (t.IsDead || (t.transform.position - body.transform.position).sqrMagnitude > ReputationRules.CorpseRadius * ReputationRules.CorpseRadius) continue;
                    foreach (var o in body.Mind.Offenders) t.Hear(o);
                }
            }
            // whoever just learned through talk is alarmed too (Hear already alarmed those taught by a body)
            for (int i = 0; i < members.Count; i++)
                if (!members[i].IsDead && members[i].Mind.Count > before[i]) members[i].Hear(null);
        }

        /// <summary>Open or close the root walls. Closing pushes any local player standing in a doorway out of it.</summary>
        public void SetGates(bool closed)
        {
            if (closed == GatesClosed) return;
            GatesClosed = closed;
            if (Gates == null) return;
            foreach (var g in Gates) if (g != null) g.SetActive(closed);
            if (!closed) return;
            var me = LocalPlayer;
            if (me == null) return;
            Physics.SyncTransforms();
            foreach (var g in Gates)
            {
                var col = g != null ? g.GetComponent<Collider>() : null;
                if (col == null) continue;
                var bounds = col.bounds;
                bounds.Expand(1f);
                if (!bounds.Contains(me.transform.position)) continue;
                Vector3 outward = me.transform.position - CaveCenter; outward.y = 0f;   // out of the doorway, away from the cave
                var controller = me.GetComponent<Evolve.Player.SlimeController>();
                if (controller != null) controller.Teleport(me.transform.position + outward.normalized * (bounds.extents.magnitude + 1f) + Vector3.up * 0.3f);
            }
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            var me = LocalPlayer;
            if (me == null) return;
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.95f, 0.9f, 0.75f) } };
                messageStyle = new GUIStyle(labelStyle) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.7f, 0.35f) } };
            }
            if ((me.transform.position - CaveCenter).sqrMagnitude <= ShowStandingRadius * ShowStandingRadius)
                GUI.Label(new Rect(Screen.width * 0.5f - 200f, 70f, 400f, 24f), $"{DisplayName}: {ReputationRules.Name(LocalStanding())}", labelStyle);
            if (Time.time < messageUntil && !string.IsNullOrEmpty(message))
                GUI.Label(new Rect(Screen.width * 0.5f - 300f, 100f, 600f, 30f), message, messageStyle);
        }

        /// <summary>Show a short message under the standing (used by the atonement stone).</summary>
        public void Say(string text) { message = text; messageUntil = Time.time + 3f; }
    }
}
