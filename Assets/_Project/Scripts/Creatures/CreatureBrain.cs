using System.Collections.Generic;
using Evolve.Ecology;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// The scene behaviour of a cave creature (ecosystem B2), twice a second: a hunter looks for the prey its species eats (CaveEcologySeed) and goes after it
    /// when hungry, scavengers go to carcasses and eat them, Chó Xương guards a big carcass and drives off everyone, hunters avoid or fight each other,
    /// and the slime's size decides who hunts it and who runs from it. The choices are CreatureBehaviorRules (tested); this carries them out through
    /// Predator.Intent / Prey.Goal. Put it on a Predator or a Prey prefab; the species is its prefab name (the display name) or <see cref="speciesId"/>.
    /// Runs on the host only. <see cref="Enabled"/> switches all of it off (the god menu flips it): then the old behaviour runs.
    /// shortcut: hunters look for prey by scanning every Prey/Predator twice a second (fine for the few dozen embodied creatures the cap allows).
    /// </summary>
    [DisallowMultipleComponent]
    public class CreatureBrain : MonoBehaviour
    {
        public static bool Enabled = true;

        [Tooltip("CaveEcologySeed species id (e.g. coc_ho_gia). Empty = look up by this prefab's name")]
        [SerializeField] string speciesId;

        const float ThinkSeconds = 0.5f, EatReach = 1.8f, HunterBite = 8f, ScavengerBite = 2f;
        const float SlimeSeen = 10f, GuardRadius = 6f, GuardMinSize = 1.2f, GuardLook = 15f, ThreatNear = 6f;

        Predator pred;
        Prey prey;
        string id;
        readonly HashSet<string> dietIds = new HashSet<string>();
        bool scavenger, guardian;
        float nextThink;

        void Awake()
        {
            pred = GetComponent<Predator>();
            prey = GetComponent<Prey>();
            id = !string.IsNullOrEmpty(speciesId) ? speciesId : CaveEcologySeed.IdByName(name);
            if (id == null || (pred == null && prey == null)) { enabled = false; return; }
            foreach (var n in CaveEcologySeed.DietSpeciesNames(id)) { var d = CaveEcologySeed.IdByName(n); if (d != null) dietIds.Add(d); }
            scavenger = CaveEcologySeed.EatsCarcass(id);
            guardian = id == CaveEcologySeed.GuardianId;
            nextThink = Time.time + Random.value * ThinkSeconds;
        }

        void OnEnable() { if (pred != null) pred.TargetFilter = SlimeFilter; }

        void OnDisable()
        {
            if (pred != null) { pred.TargetFilter = null; pred.Intent = default; }
            if (prey != null) prey.Goal = null;
        }

        static float MassOf(SurvivalStats p) => p.TryGetComponent(out Evolve.Swallow.SlimeBody body) ? body.Mass : 10f;   // a remote avatar has no body here: a typical slime
        static float Flat(Vector3 a, Vector3 b) { a.y = b.y; return Vector3.Distance(a, b); }

        bool SlimeFilter(SurvivalStats p) => !Enabled || CreatureBehaviorRules.HunterToSlime(MassOf(p), pred.BodyMass) == SlimeAttitude.Hunt;

        bool IsDiet(Component c) { var cid = CaveEcologySeed.IdByName(c.name); return cid != null && dietIds.Contains(cid); }

        void Update()
        {
            if (Time.time < nextThink) return;
            nextThink = Time.time + ThinkSeconds * Random.Range(0.9f, 1.2f);
            if (pred != null)
            {
                if (pred.IsReplica) return;
                if (!Enabled) { pred.Intent = default; return; }
                ThinkHunter();
            }
            else if (prey != null)
            {
                if (prey.IsReplica) return;
                if (!Enabled) { prey.Goal = null; return; }
                ThinkScavengingPrey();
            }
        }

        static Carcass NearestEdible(Vector3 pos, float range, float minSize, out float dist)
        {
            Carcass best = null; dist = float.MaxValue;
            foreach (var c in Carcass.Active)
            {
                if (c == null || !c.Edible || c.Size < minSize) continue;
                float d = Flat(pos, c.transform.position);
                if (d < dist && d <= range) { dist = d; best = c; }
            }
            return best;
        }

        void ThinkHunter()
        {
            Vector3 pos = transform.position;
            float myMass = Mathf.Max(0.1f, pred.BodyMass);
            var inp = new HunterInput
            {
                Hunger = pred.Hunger01, Scavenger = scavenger, Guardian = guardian, PlayerDist = float.MaxValue, PreyDist = float.MaxValue,
                CarcassDist = float.MaxValue, GuardedCarcassDist = float.MaxValue, RivalDist = float.MaxValue, RivalRatio = 1f,
            };

            SurvivalStats player = null;
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.State.IsDead) continue;
                float d = Flat(pos, p.transform.position);
                if (d < inp.PlayerDist) { inp.PlayerDist = d; player = p; }
            }
            if (player != null && inp.PlayerDist <= SlimeSeen) { inp.PlayerSeen = true; inp.Attitude = CreatureBehaviorRules.HunterToSlime(MassOf(player), myMass); }

            Transform preyT = null, rivalT = null;
            foreach (var c in Prey.Active)
            {
                if (c == null || !IsDiet(c)) continue;
                float d = Flat(pos, c.transform.position);
                if (d < inp.PreyDist && d <= CreatureBehaviorRules.PreySense) { inp.PreyDist = d; preyT = c.transform; inp.HasPrey = true; }
            }
            foreach (var h in Predator.Active)
            {
                if (h == null || h == pred) continue;
                float d = Flat(pos, h.transform.position);
                if (IsDiet(h)) { if (d < inp.PreyDist && d <= CreatureBehaviorRules.PreySense) { inp.PreyDist = d; preyT = h.transform; inp.HasPrey = true; } }   // a lord eats hunters
                else if (d < inp.RivalDist) { inp.RivalDist = d; rivalT = h.transform; inp.RivalRatio = h.BodyMass / myMass; inp.HasRival = true; }
            }

            var seek = scavenger ? NearestEdible(pos, CreatureBehaviorRules.ScavengeRange, 0f, out float seekDist) : null;
            if (seek != null) { inp.HasCarcass = true; inp.CarcassDist = seekDist; }
            Carcass guarded = null; Transform intruder = null; bool intruderIsPlayer = false;
            if (guardian)
            {
                guarded = NearestEdible(pos, GuardLook, GuardMinSize, out float gd);
                if (guarded != null)
                {
                    inp.HasGuardedCarcass = true; inp.GuardedCarcassDist = gd;
                    Vector3 cp = guarded.transform.position;
                    float best = GuardRadius;
                    foreach (var p in SurvivalStats.All)
                        if (p != null && !p.State.IsDead && Flat(cp, p.transform.position) <= best) { best = Flat(cp, p.transform.position); intruder = p.transform; intruderIsPlayer = true; }
                    foreach (var c in Prey.Active)
                        if (c != null && Flat(cp, c.transform.position) <= best) { best = Flat(cp, c.transform.position); intruder = c.transform; intruderIsPlayer = false; }
                    foreach (var h in Predator.Active)
                        if (h != null && h != pred && Flat(cp, h.transform.position) <= best) { best = Flat(cp, h.transform.position); intruder = h.transform; intruderIsPlayer = false; }
                    inp.IntruderNear = intruder != null;
                }
            }

            var mode = CreatureBehaviorRules.DecideHunter(inp);
            switch (mode)
            {
                case HunterMode.Wander: pred.Intent = default; break;
                case HunterMode.ChasePlayer: pred.Intent = default; pred.Provoke(player); break;   // the old chase takes over
                case HunterMode.ChasePrey: pred.Intent = PredatorIntent.ChaseOf(preyT, true); break;
                case HunterMode.ToCarcass:
                    var goal = guardian ? guarded : seek;
                    pred.Intent = goal != null ? PredatorIntent.GoToPoint(goal.transform.position, 1.2f) : default;
                    TryEat(goal, pos);
                    break;
                case HunterMode.Guard: pred.Intent = PredatorIntent.Hold(); TryEat(guarded, pos); break;
                case HunterMode.ChaseIntruder:
                    if (intruderIsPlayer) { pred.Intent = default; if (intruder != null && intruder.TryGetComponent(out SurvivalStats s)) pred.Provoke(s); }
                    else pred.Intent = PredatorIntent.ChaseOf(intruder, false);
                    break;
                case HunterMode.AvoidPlayer: pred.Intent = PredatorIntent.AvoidOf(player.transform); break;
                case HunterMode.AvoidRival: pred.Intent = PredatorIntent.AvoidOf(rivalT); break;
                case HunterMode.FightRival: pred.Intent = PredatorIntent.ChaseOf(rivalT, false); break;
            }
        }

        void TryEat(Carcass c, Vector3 pos)
        {
            if (c == null || pred.Hunger01 < CreatureBehaviorRules.HungerToScavenge || Flat(pos, c.transform.position) > EatReach) return;
            if (c.EatBy(HunterBite) > 0f) pred.NoteAte();
        }

        void ThinkScavengingPrey()
        {
            if (!scavenger) { prey.Goal = null; return; }
            Vector3 pos = transform.position;
            var c = NearestEdible(pos, CreatureBehaviorRules.ScavengeRange, 0f, out float d);
            bool threat = false;
            foreach (var p in SurvivalStats.All) if (p != null && !p.State.IsDead && Flat(pos, p.transform.position) <= ThreatNear) { threat = true; break; }
            if (!threat) foreach (var h in Predator.Active) if (h != null && Flat(pos, h.transform.position) <= ThreatNear) { threat = true; break; }
            if (CreatureBehaviorRules.PreyScavenges(c != null, d, threat))
            {
                prey.Goal = c.transform.position;
                if (d <= EatReach) c.EatBy(ScavengerBite);
            }
            else prey.Goal = null;
        }
    }
}
