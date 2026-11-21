using System.Collections.Generic;
using Evolve.Ecology;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Swallow
{
    /// <summary>
    /// Nuốt Chửng + Túi Không Đáy + Giải Mã on the player.
    /// Small prey is swallowed alive when caught (Prey calls TrySwallowAlive). A carcass is swallowed on F:
    /// whole if small enough, otherwise F is held to dissolve it first. Each frame the stomach ticks; finished
    /// analyses go to the AnalysisBook (and Tiếng Vọng), finished digestion feeds the slime and adds mass.
    /// The stomach empties on death; the book is kept.
    /// </summary>
    [RequireComponent(typeof(SurvivalStats))]
    public class Swallower : MonoBehaviour
    {
        readonly Stomach stomach = new Stomach();
        readonly AnalysisBook book = new AnalysisBook();
        readonly List<StomachItem> analyzed = new List<StomachItem>();
        readonly List<StomachItem> digested = new List<StomachItem>();

        SurvivalStats stats;
        SlimeBody body;
        Carcass touching;
        Evolve.Traits.PlayerTraits traitsComp;
        float dissolve;

        public Stomach Stomach => stomach;
        public AnalysisBook Book => book;
        /// <summary>The carcass the slime is touching (null if none).</summary>
        public Carcass Touching => touching;
        /// <summary>0..1 while F is held on a carcass too big to swallow whole; -1 otherwise.</summary>
        public float DissolveProgress => touching != null && dissolve > 0f ? dissolve : -1f;
        float Mass => body != null ? body.Mass : MassRules.StartMass;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            body = GetComponent<SlimeBody>();
        }

        void OnEnable() { if (stats != null) stats.State.Died += OnDied; }
        void OnDisable() { if (stats != null) stats.State.Died -= OnDied; }

        void OnDied()
        {
            // Dạ Dày Bất Diệt: with lives left, the first two deaths keep what is in the stomach.
            var traits = GetComponent<Evolve.Traits.PlayerTraits>();
            bool keeps = Evolve.Traits.DeathLossRules.KeepsStomach(traits != null && traits.HasCore(Evolve.Traits.DeathLossRules.CoreId), stats.State.Lives);
            if (!keeps) stomach.Clear();
            touching = null;
            dissolve = 0f;
        }

        /// <summary>Swallow a creature caught alive. False when it does not fit (stomach full or too big).</summary>
        public bool TrySwallowAlive(string label, float mass, float nutrition, FoodKind food, List<Gene> genes)
        {
            if (stats.State.IsDead || !MassRules.CanSwallowWhole(Mass, mass)) return false;
            var item = new StomachItem { Label = label, Mass = mass, Nutrition = nutrition * AnMonNutrition(), Food = food, Genes = genes ?? new List<Gene>(), Quality = AnalysisRules.Fresh };
            if (!stomach.TryAdd(item)) return false;
            Evolve.Visuals.SlimeGelAnimator.Play(gameObject, Evolve.Visuals.GelMotion.Gulp);
            Evolve.Audio.SlimeSfx.Play("slime_swallow");
            return true;
        }

        /// <summary>Swallow a carcass at once whatever its size: the end of a latch fight, digested in place and as fresh as it gets.</summary>
        public bool SwallowCarcassNow(Carcass c) =>
            c != null && SwallowCarcass(c, c.GetComponent<CarcassSample>());

        public void Touch(Carcass c)
        {
            if (touching == c) return;
            touching = c;
            dissolve = 0f;
        }

        public void Untouch(Carcass c)
        {
            if (touching != c) return;
            touching = null;
            dissolve = 0f;
        }

        /// <summary>Spit an item out: it lands at the slime's feet as a carcass (already-analysed genes are not in it any more).</summary>
        public void Spit(int index)
        {
            if (!SwallowRules.CanSpit(NetWorld.IsClient)) { Echo.Say("«Không thể nhả ra khi đang là khách trong co-op.»"); return; }
            var it = stomach.Spit(index);
            if (it == null) return;
            var pos = transform.position + transform.forward * 1.2f;
            pos.y = Evolve.World.GroundHeight.At(pos.x, pos.z, 0f) + 0.2f;
            var c = Carcass.Spawn(pos, Mathf.Clamp(0.4f + it.Mass * 0.07f, 0.4f, 1.5f), null);
            if (c == null) return;
            c.ResetFood(it.Nutrition);
            var s = c.gameObject.AddComponent<CarcassSample>();
            s.Genes = it.Genes ?? new List<Gene>();
            s.Mass = it.Mass;
            s.Label = it.Label;
            s.QualityCap = it.Quality;
            Evolve.Visuals.SlimeGelAnimator.Play(gameObject, Evolve.Visuals.GelMotion.Release);
            Evolve.Audio.SlimeSfx.Play("slime_spit");
        }

        void Update()
        {
            if (stats.IsRemote) return;
            if (stats.State.IsDead) { Evolve.Audio.SlimeSfx.SetLoop("slime_dissolve", false); return; }
            if (traitsComp == null) traitsComp = GetComponent<Evolve.Traits.PlayerTraits>();
            // Dòng Máu Tham Lam: rarer genes come easier.
            book.RareGeneBonus = traitsComp != null && traitsComp.HasCore(Evolve.Traits.LuckRules.CoreId)
                ? Evolve.Traits.EncounterRewardRules.BoostMultiplier(Evolve.Traits.TraitTuning.ThamLamFactor, traitsComp.CoreBoost) : 1f;
            TickStomach(Time.deltaTime);
            UpdateCarcass(Time.deltaTime);
            Evolve.Audio.SlimeSfx.SetLoop("slime_dissolve", dissolve > 0.01f);   // a bubbling sound while a big carcass is being dissolved
        }

        // Nhân Ăn Mòn: carcasses dissolve faster and everything swallowed gives more.
        bool HasAnMon()
        {
            if (traitsComp == null) traitsComp = GetComponent<Evolve.Traits.PlayerTraits>();
            return traitsComp != null && traitsComp.HasCore(Evolve.Traits.EnvTraitRules.AnMon);
        }
        float CoreBoost() => traitsComp != null ? traitsComp.CoreBoost : 1f;
        float AnMonNutrition() => Evolve.Traits.EncounterRewardRules.BoostMultiplier(Evolve.Traits.EnvTraitRules.AnMonNutritionMultiplier(HasAnMon()), CoreBoost());
        float AnMonDissolve() => Evolve.Traits.EncounterRewardRules.BoostMultiplier(Evolve.Traits.EnvTraitRules.AnMonDissolveMultiplier(HasAnMon()), CoreBoost());

        void TickStomach(float dt)
        {
            analyzed.Clear();
            digested.Clear();
            stomach.Tick(dt, stats.State.InShelter, analyzed, digested);
            foreach (var it in analyzed)
            {
                var results = book.Submit(it.Genes, it.Quality, () => Random.value);
                bool showName = AnalysisRules.ShowsName(book.DecoderLevel);
                foreach (var r in results) Echo.Say(Echo.LearnedLine(r, showName));
                it.Genes = new List<Gene>();   // analysed once: spitting it out and swallowing again teaches nothing more
            }
            foreach (var it in digested)
            {
                NetWorld.Feed(stats, it.Nutrition, FoodProfiles.For(it.Food));
                if (body != null) body.AddMass(it.Mass * MassRules.DigestMassShare);
            }
        }

        void UpdateCarcass(float dt)
        {
            if (touching == null) { dissolve = 0f; return; }
            if (SurvivalHUD.BlocksInput(gameObject)) return;
            var sample = touching.GetComponent<CarcassSample>();
            float mass = sample != null ? sample.Mass : 5f;
            if (MassRules.CanSwallowWhole(Mass, mass))
            {
                if (KeyPressed.F()) SwallowCarcass(touching, sample);
                return;
            }
            if (!KeyPressed.FHeld()) { dissolve = 0f; return; }
            dissolve += dt * AnMonDissolve() / Mathf.Max(0.1f, MassRules.DissolveSeconds(Mass, mass));
            if (dissolve >= 1f) SwallowCarcass(touching, sample);
        }

        bool SwallowCarcass(Carcass c, CarcassSample sample)
        {
            dissolve = 0f;
            float quality = SwallowRules.CarcassQuality(c.Stage, c.Decay.ElapsedMinutes);
            if (quality <= 0f) return false;
            if (sample != null) quality = Mathf.Min(quality, sample.QualityCap);
            var item = new StomachItem
            {
                Label = sample != null ? sample.Label : "Xác",
                Mass = sample != null ? sample.Mass : 5f,
                Nutrition = c.Decay.FoodLeft * AnMonNutrition(),
                Food = FoodKind.PredatorMeat,
                Genes = sample != null ? new List<Gene>(sample.Genes) : new List<Gene>(),
                Quality = quality
            };
            if (!stomach.TryAdd(item)) { Echo.Say("«Túi Không Đáy đã đầy.»"); return false; }
            if (c.Stage == CarcassStage.Rotten) stats.State.TryInfect(DiseaseKind.RottenGel, DiseaseRules.RottenBiteChance);
            Evolve.Visuals.SlimeGelAnimator.Play(gameObject, Evolve.Visuals.GelMotion.Gulp);
            Evolve.Audio.SlimeSfx.Play("slime_swallow");
            touching = null;
            Destroy(c.gameObject);
            return true;
        }
    }
}
