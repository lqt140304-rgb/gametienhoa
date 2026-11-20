using System;
using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;
using UnityEngine.UI;

namespace Evolve.Traits
{
    /// <summary>
    /// The birth scene, bản 3 (spec 2026-11-20-birth-scene-system-collapse-design): a player with no core is taken out of the world to a separate stage
    /// (BirthStage) and pulled in over a failing connection. Everything is driven by the steps of BirthFlow and the instability curve: noise, tearing
    /// and shake (BirthGlitch, BirthFx), broken frames (BirthWindow), six gift scenes (BirthGiftScene), the fate cards that turn over by themselves
    /// (BirthCard), the name slam (BirthSlam) and the sound (BirthAudio, BirthVoice). What fate gave was decided by FateRoll before the first frame.
    /// Runs once per run; dying and respawning never plays it again. If anything fails the scene is dropped and the player just gets the result.
    /// </summary>
    public class BirthScene : MonoBehaviour
    {
        enum State { Waiting, Running, Done }

        const int CardCount = 7;   // face-down core cards in the heap (one of them is the fate card)

        State state;
        bool ranThisRun;
        PlayerTraits player;
        float nextFind;

        BirthFlow flow;
        int shownIndex = -1;
        float clock;
        FateResult result;
        string coreName, coreSummary;
        bool coreImplemented;
        Color coreOrb;
        string[] flawNames = new string[0], flawDescriptions = new string[0];
        bool[] flawImplemented = new bool[0];
        List<SlamPlan.Slam> slamPlan;

        BirthStage stage;
        BirthGlitch glitch;
        BirthFx fx;
        BirthAudio audio;
        BirthVoice voice;
        readonly List<(float at, float strength)> pulses = new List<(float, float)>();
        float lastHitAt = -10f, lastHitStrength;

        // what is on screen
        RectTransform view, cardsRoot;
        BirthWindow window, window2;
        BirthSlime slime;
        Image fadeOut;
        float fadeInStart = -10f;
        BirthGiftScene gift;
        BirthSlam slam;
        BirthCard[] coreCards = new BirthCard[0], flawCards = new BirthCard[0];
        Vector2[] cardTarget = new Vector2[0];
        float[] cardAngle = new float[0], cardFrom = new float[0];
        int fateSlot;
        int[] flawChosen = new int[0];
        List<Image> shards = new List<Image>(), streaks = new List<Image>();
        RectTransform sigil;
        Text countdown, banner;
        Image[] orbs = new Image[0];
        Image blob;
        bool[] dealt = new bool[0];
        bool impactDone;
        int lastTyped;
        float lastTick;

        // the frozen state while the scene plays (the same three flags the old scenes used), and the cursor
        SurvivalStats frozenStats;
        bool freezing, prevInvulnerable, prevExternalMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<BirthScene>() != null) return;
            var go = new GameObject("Birth Scene");
            DontDestroyOnLoad(go);
            go.AddComponent<BirthScene>();
        }

        void Awake()
        {
            voice = gameObject.AddComponent<BirthVoice>();
            audio = gameObject.AddComponent<BirthAudio>();
            audio.Init(voice);
        }

        void OnDisable() { Cleanup(); }

        void OnDestroy() { BirthUiKit.Release(); }

        // ---------------------------------------------------------------- flow

        void Update()
        {
            if (state == State.Waiting)
            {
                if (Time.unscaledTime < nextFind) return;
                nextFind = Time.unscaledTime + 0.5f;
                player = TraitNet.Local;
                if (player != null && flow == null && !ranThisRun && player.CoreId == null) Begin();
                return;
            }
            if (state != State.Running) return;
            try { TickRunning(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                Abort();   // never leave the player stuck behind a broken scene
            }
        }

        void Begin()
        {
            int seed = FateRoll.PlayerSeed(Environment.TickCount, 0);
            var r = FateRoll.Roll(seed, TraitCatalog.FateCards());
            if (!r.HasValue)
            {
                Debug.LogWarning("Cảnh ra đời: kho Lõi rỗng, bỏ qua (chạy Evolve → Create Traits).");
                ranThisRun = true;
                return;
            }
            result = r.Value;
            ReadResultNames();
            stage = BirthStage.Create();
            if (stage == null) { GiveResult(); ranThisRun = true; return; }   // no stage: skip the scene, keep the result
            try
            {
                glitch = new BirthGlitch(stage);
                fx = new BirthFx(stage);
                flow = new BirthFlow(BirthScript.BuildSteps());
                var giftNames = new List<string>();
                foreach (var g in BirthScript.Gifts) giftNames.Add(g.Name);
                slamPlan = SlamPlan.Build(coreName, result.Rarity, flawNames, giftNames);
                int slamIndex = flow.IndexOf(BirthStepKind.Slam);
                if (slamIndex >= 0) flow.Step(slamIndex).MinSeconds = SlamPlan.TotalSeconds(slamPlan);
                fateSlot = new System.Random(seed ^ 0x5bd1e995).Next(1, CardCount - 1);
                var rnd = new System.Random(seed ^ 0x2545F491);
                int flawCount = result.FlawIds != null ? result.FlawIds.Length : 0;
                flawChosen = PickDistinct(rnd, 3, Mathf.Clamp(flawCount, 1, 3));
                clock = 0f; shownIndex = -1; pulses.Clear();
                state = State.Running;
                Freeze();
                audio.SetHum(true);
            }
            catch (Exception e) { Debug.LogException(e); Abort(); }
        }

        static int[] PickDistinct(System.Random rnd, int of, int count)
        {
            var pool = new List<int>(); for (int i = 0; i < of; i++) pool.Add(i);
            var pick = new int[count];
            for (int i = 0; i < count; i++) { int k = rnd.Next(pool.Count); pick[i] = pool[k]; pool.RemoveAt(k); }
            return pick;
        }

        void ReadResultNames()
        {
            var so = TraitCatalog.Core(result.CoreId);
            if (so != null) { coreName = so.DisplayName; coreSummary = so.Summary; coreImplemented = so.Implemented; coreOrb = so.OrbColor; }
            else
            {
                coreName = result.CoreId; coreSummary = ""; coreImplemented = false; coreOrb = BirthUiKit.Cyan;
                foreach (var d in TraitSeed.Cores) if (d.Id == result.CoreId) { coreName = d.Name; coreSummary = d.Summary; coreImplemented = d.Implemented; }
            }
            var ids = result.FlawIds ?? new string[0];
            flawNames = new string[ids.Length]; flawDescriptions = new string[ids.Length]; flawImplemented = new bool[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                var f = TraitCatalog.Flaw(ids[i]);
                flawNames[i] = ids[i]; flawDescriptions[i] = ""; flawImplemented[i] = false;
                if (f != null) { flawNames[i] = f.DisplayName; flawDescriptions[i] = f.Description; flawImplemented[i] = f.Implemented; }
                else foreach (var d in TraitSeed.Flaws) if (d.Id == ids[i]) { flawNames[i] = d.Name; flawDescriptions[i] = d.Description; flawImplemented[i] = d.Implemented; }
            }
        }

        void GiveResult()
        {
            if (player == null) return;
            player.Set(result.CoreId, result.FlawIds);
            if (TraitNet.IsClient) TraitNet.ClientPick?.Invoke(result.CoreId, result.FlawIds);   // co-op: tell the host what fate gave
        }

        void Finish()
        {
            fadeInStart = Time.unscaledTime;
            GiveResult();
            Cleanup();
            ranThisRun = true;
            state = State.Done;
        }

        void Abort()
        {
            if (state == State.Running || state == State.Waiting) GiveResult();
            Cleanup();
            ranThisRun = true;
            state = State.Done;
        }

        void Cleanup()
        {
            if (voice != null) voice.Stop();
            if (audio != null) audio.StopAll();
            if (stage != null) { stage.Destroy(); stage = null; }
            view = null; cardsRoot = null; window = null; window2 = null; gift = null; slam = null; if (slime != null) { slime.Destroy(); slime = null; } coreCards = new BirthCard[0]; flawCards = new BirthCard[0];
            Unfreeze();
        }

        void Freeze()
        {
            if (freezing) return;
            var stats = player != null ? player.GetComponent<SurvivalStats>() : null;
            if (stats != null)
            {
                frozenStats = stats;
                prevInvulnerable = stats.State.Invulnerable;
                stats.State.Invulnerable = true;
                stats.Frozen = true;
            }
            prevExternalMenu = SurvivalHUD.ExternalMenuOpen;
            SurvivalHUD.ExternalMenuOpen = true;
            freezing = true;
        }

        void Unfreeze()
        {
            if (!freezing) return;
            freezing = false;
            if (frozenStats != null)
            {
                frozenStats.Frozen = false;
                frozenStats.State.Invulnerable = prevInvulnerable;
            }
            SurvivalHUD.ExternalMenuOpen = prevExternalMenu;
            frozenStats = null;
        }

        // ---------------------------------------------------------------- per frame

        void TickRunning()
        {
            float dt = Time.unscaledDeltaTime;
            clock += dt;
            flow.Tick(dt, voice.Busy);
            if (flow.Finished) { Finish(); return; }
            if (flow.Index != shownIndex) EnterStep();

            var step = flow.Current;
            float t = flow.StepTime;
            float t01 = Mathf.Clamp01(t / Mathf.Max(0.1f, step.MinSeconds));
            float inst = Instability.Value(Instability.BaseFor(step.Kind, step.GiftIndex, t01), pulses, clock);
            float hit = lastHitStrength * ShakeMath.Decay(clock - lastHitAt, 0.6f);
            glitch.Apply(inst, hit, clock);
            fx.Tick(clock, dt, inst);
            if (window != null) window.Tick(clock, inst);
            if (window2 != null) window2.Tick(clock, inst);
            TickStep(step, t, inst);
        }

        void Pulse(float strength)
        {
            // noise belongs to the opening only; later events are felt as shaking, with just a hint of glitch
            var k = flow != null && flow.Current != null ? flow.Current.Kind : BirthStepKind.Slam;
            pulses.Add((clock, k == BirthStepKind.Disconnect || k == BirthStepKind.Pull ? strength : strength * 0.2f));
        }

        void Hit(float strength) { lastHitAt = clock; lastHitStrength = strength; }

        void Type(string text, float t, float inst)
        {
            if (window == null) return;
            window.SetBody(text, t - 0.2f, 34f, inst, clock);
            if (window2 != null && flow.Current != null) window2.SetBody(Stream(flow.Current.Kind), t - 0.2f, 60f, inst, clock);
            int n = GlitchText.TypedCount(text, t - 0.2f, 34f);
            if (n > lastTyped && clock - lastTick > 0.09f) { audio.Play("type_tick", 0.5f); lastTick = clock; }
            lastTyped = n;
        }

        // ---------------------------------------------------------------- steps

        void EnterStep()
        {
            shownIndex = flow.Index;
            var step = flow.Current;
            if (view != null) Destroy(view.gameObject);
            window = null; window2 = null; gift = null; slam = null; sigil = null; if (slime != null) { slime.Destroy(); slime = null; } fadeOut = null; countdown = null; banner = null; blob = null;
            shards.Clear(); streaks.Clear(); orbs = new Image[0];
            impactDone = false; lastTyped = 0; flipped.Clear();
            view = BirthUiKit.Group(stage.Canvas, "View", Vector2.zero);
            fx.Clear();

            // voice
            switch (step.Kind)
            {
                case BirthStepKind.RevealCore: voice.Play(result.CoreId); break;
                case BirthStepKind.RevealFlaw:
                    voice.Stop();
                    foreach (var id in result.FlawIds ?? new string[0]) voice.Enqueue(id);
                    break;
                case BirthStepKind.Slam: voice.Stop(); break;
                default:
                    if (string.IsNullOrEmpty(step.VoiceId)) voice.Stop(); else voice.Play(step.VoiceId);
                    break;
            }

            var size = stage.CanvasSize;
            switch (step.Kind)
            {
                case BirthStepKind.Disconnect:
                    window = Win(new Vector2(0f, -70f), new Vector2(1180f, 430f), "[HỆ THỐNG]  CẢNH BÁO", BirthUiKit.Red);
                    banner = BirthUiKit.Label(view, "Banner", new Vector2(0f, size.y * 0.34f), new Vector2(size.x, 120f), "MẤT KẾT NỐI", 110, TextAnchor.MiddleCenter, BirthUiKit.Red, true);
                    for (int i = 0; i < 30; i++)
                        shards.Add(BirthUiKit.Box(view, "Shard" + i, new Vector2((BirthUiKit.Hash(i * 3) - 0.5f) * size.x, (BirthUiKit.Hash(i * 3 + 1) - 0.5f) * size.y),
                            new Vector2(10f + 50f * BirthUiKit.Hash(i * 3 + 2), 8f + 30f * BirthUiKit.Hash(i * 5)), BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.25f)));
                    audio.Play("collapse"); audio.Play("alarm", 0.7f); Pulse(0.3f); Hit(0.8f);
                    break;
                case BirthStepKind.Pull:
                    window = Win(new Vector2(0f, -360f), new Vector2(1180f, 230f), "[HỆ THỐNG]", BirthUiKit.Cyan);
                    for (int i = 0; i < 40; i++)
                        streaks.Add(BirthUiKit.Box(view, "Streak" + i, new Vector2((BirthUiKit.Hash(i * 2) - 0.5f) * size.x, 0f), new Vector2(3f, 80f + 160f * BirthUiKit.Hash(i * 7)), BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.4f)));
                    countdown = BirthUiKit.Label(view, "Count", new Vector2(0f, 40f), new Vector2(500f, 300f), "3", 260, TextAnchor.MiddleCenter, BirthUiKit.WithAlpha(Color.white, 0.8f), true);
                    audio.Play("pull"); Pulse(0.2f);
                    break;
                case BirthStepKind.Form:
                    window = Win(new Vector2(0f, -360f), new Vector2(1180f, 230f), "[HỆ THỐNG]", BirthUiKit.Cyan);
                    blob = BirthUiKit.Glow(view, "Blob", new Vector2(0f, 60f), 10f, new Color(0.7f, 0.55f, 1f, 0.95f));
                    BirthUiKit.Circle(view, "Ring", new Vector2(0f, 60f), 340f, BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.5f));
                    audio.Play("whoosh"); Pulse(0.15f);
                    break;
                case BirthStepKind.GiftIntro:
                    window = Win(new Vector2(0f, -360f), new Vector2(1180f, 230f), "[HỆ THỐNG]  ÂN ĐIỂN KHỞI SINH", BirthUiKit.Cyan);
                    blob = BirthUiKit.Glow(view, "Blob", new Vector2(0f, 60f), 200f, new Color(0.7f, 0.55f, 1f, 0.95f));
                    orbs = new Image[6];
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f - Mathf.PI / 2f;
                        orbs[i] = BirthUiKit.Glow(view, "Orb" + i, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 300f + new Vector2(0f, 60f), 90f, BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.15f));
                    }
                    audio.Play("whoosh");
                    break;
                case BirthStepKind.Gift:
                    window = Win(new Vector2(0f, -385f), new Vector2(1180f, 190f), "[HỆ THỐNG]  ÂN ĐIỂN " + (step.GiftIndex + 1) + "/6", BirthUiKit.Cyan, 30);
                    gift = BirthGiftScene.Create(step.GiftIndex, view, new Vector2(0f, 80f));
                    BirthUiKit.Label(view, "GiftName", new Vector2(0f, size.y * 0.37f), new Vector2(size.x, 110f), BirthScript.Gifts[step.GiftIndex].Name, 90, TextAnchor.MiddleCenter, Color.white, true);
                    audio.Play(step.VoiceId.Replace('.', '_')); Pulse(0.3f);
                    break;
                case BirthStepKind.Fate:
                    window = Win(new Vector2(0f, -360f), new Vector2(1180f, 230f), "[HỆ THỐNG]  SỐ MỆNH", BirthUiKit.Cyan);
                    sigil = MakeSigil(view, 300f, new Vector2(0f, 70f));
                    BirthUiKit.Label(view, "Core", new Vector2(-330f, 70f), new Vector2(340f, 70f), "LÕI", 60, TextAnchor.MiddleCenter, BirthUiKit.WithAlpha(Color.white, 0.8f), true);
                    BirthUiKit.Label(view, "Price", new Vector2(330f, 70f), new Vector2(380f, 70f), "CÁI GIÁ", 60, TextAnchor.MiddleCenter, BirthUiKit.WithAlpha(BirthUiKit.Red, 0.85f), true);
                    audio.Play("whoosh"); Pulse(0.2f);
                    break;
                case BirthStepKind.DealCore:
                    window = Win(new Vector2(0f, size.y * 0.36f), new Vector2(1180f, 190f), "[HỆ THỐNG]  LÕI", BirthUiKit.Cyan, 30);
                    BuildCoreCards();
                    break;
                case BirthStepKind.RevealCore:
                    window = Win(new Vector2(0f, -385f), new Vector2(1180f, 220f), "[HỆ THỐNG]  LÕI ĐỘC NHẤT", RarityColor(), 30);
                    break;
                case BirthStepKind.DealFlaw:
                    window = Win(new Vector2(0f, size.y * 0.36f), new Vector2(1180f, 190f), "[HỆ THỐNG]  KHUYẾT ĐIỂM", BirthUiKit.Red, 30);
                    BuildFlawCards();
                    break;
                case BirthStepKind.RevealFlaw:
                    window = Win(new Vector2(0f, -385f), new Vector2(1180f, 220f), "[HỆ THỐNG]  KHUYẾT ĐIỂM", BirthUiKit.Red, 30);
                    break;
                case BirthStepKind.Slam:
                    if (cardsRoot != null) { Destroy(cardsRoot.gameObject); cardsRoot = null; }
                    coreCards = new BirthCard[0]; flawCards = new BirthCard[0];
                    slam = new BirthSlam(view, slamPlan, size, result.Rarity);
                    slam.OnImpact = OnSlamImpact;
                    audio.Play("whoosh");
                    break;
                case BirthStepKind.Showcase:
                    BuildShowcase(size);
                    audio.Play("whoosh");
                    break;
                case BirthStepKind.Stabilize:
                    window = Win(new Vector2(0f, -40f), new Vector2(1100f, 280f), "[HỆ THỐNG]  KẾT NỐI", BirthUiKit.Cyan);
                    sigil = MakeSigil(view, 280f, new Vector2(0f, size.y * 0.27f));
                    audio.SetHum(false); audio.Play("stable_chime");
                    break;
            }
        }

        /// <summary>Two frameless text columns, the narration on the left and a parallel readout on the right (the frame arguments are no longer used).</summary>
        BirthWindow Win(Vector2 pos, Vector2 size, string title, Color accent, int bodySize = 34)
        {
            var cs = stage.CanvasSize;
            const float w = 430f, h = 720f;
            float x = cs.x / 2f - 50f - w / 2f;
            window2 = BirthWindow.CreateSide(view, new Vector2(x, 0f), new Vector2(w, h), "[ĐƯỜNG TRUYỀN]", accent, true, 24);
            return BirthWindow.CreateSide(view, new Vector2(-x, 0f), new Vector2(w, h), title, accent, false, Mathf.Min(bodySize, 36));
        }

        static string Stream(BirthStepKind kind)
        {
            switch (kind)
            {
                case BirthStepKind.Disconnect: return "> TÍN HIỆU ........ MẤT\n> GÓI TIN ......... 12%\n> ĐỒNG BỘ ......... LỖI\n> THỬ LẠI ......... 1/∞\n> LINH HỒN ........ KHÔNG XÁC ĐỊNH";
                case BirthStepKind.Pull: return "> ĐỊNH VỊ THẾ GIỚI .. OK\n> TÁI TẠO LINH HỒN .. 41%\n> KÉO VÀO ......... ĐANG CHẠY\n> ĐỘ TRỄ .......... CAO";
                case BirthStepKind.Form: return "> KẾT NỐI ......... THÀNH CÔNG\n> ĐỘ TRỄ .......... 8 ms\n> ĐỒNG BỘ ......... 100%\n> CHỦNG TỘC ....... SLIME";
                case BirthStepKind.GiftIntro:
                case BirthStepKind.Gift: return "> NẠP ÂN ĐIỂN ..... OK\n> BỘ NHỚ .......... ỔN ĐỊNH\n> NĂNG LỰC NỀN ..... 6";
                case BirthStepKind.Fate: return "> SỐ MỆNH ......... KHỞI ĐỘNG\n> LÕI ............. CHỜ\n> CÁI GIÁ ......... CHỜ";
                case BirthStepKind.DealCore:
                case BirthStepKind.RevealCore: return "> SỐ MỆNH ......... ĐANG QUAY\n> LÕI ............. ĐANG CHỌN\n> ĐỘ HIẾM .......... ?";
                case BirthStepKind.DealFlaw:
                case BirthStepKind.RevealFlaw: return "> SỐ MỆNH ......... ĐANG QUAY\n> CÁI GIÁ ......... ĐANG CHỌN";
                default: return "> TRẠNG THÁI ....... ỔN ĐỊNH";
            }
        }

        Color RarityColor() => result.Rarity == TraitRarity.Mythic ? BirthUiKit.Gold : result.Rarity == TraitRarity.Rare ? new Color(0.45f, 0.72f, 1f) : new Color(0.85f, 0.93f, 1f);

        string RarityName() => result.Rarity == TraitRarity.Mythic ? "CỰC HIẾM" : result.Rarity == TraitRarity.Rare ? "HIẾM" : "THƯỜNG";

        RectTransform MakeSigil(RectTransform parent, float radius, Vector2 pos)
        {
            var g = BirthUiKit.Group(parent, "Sigil", pos);
            BirthUiKit.Circle(g, "R1", Vector2.zero, radius * 2f, BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.7f));
            BirthUiKit.Circle(g, "R2", Vector2.zero, radius * 1.6f, BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.4f));
            BirthUiKit.Circle(g, "R3", Vector2.zero, radius * 0.8f, BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.6f));
            for (int k = 0; k < 2; k++)
            {
                var sq = BirthUiKit.Group(g, "Sq" + k, Vector2.zero);
                float side = radius * 1.2f;
                BirthUiKit.Box(sq, "T", new Vector2(0f, side / 2f), new Vector2(side, 3f), BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.6f));
                BirthUiKit.Box(sq, "B", new Vector2(0f, -side / 2f), new Vector2(side, 3f), BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.6f));
                BirthUiKit.Box(sq, "L", new Vector2(-side / 2f, 0f), new Vector2(3f, side), BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.6f));
                BirthUiKit.Box(sq, "R", new Vector2(side / 2f, 0f), new Vector2(3f, side), BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.6f));
                sq.localRotation = Quaternion.Euler(0f, 0f, k * 45f);
            }
            return g;
        }

        // ---- the cards

        void BuildCoreCards()
        {
            if (cardsRoot != null) Destroy(cardsRoot.gameObject);
            cardsRoot = BirthUiKit.Group(stage.Canvas, "Cards", Vector2.zero);
            coreCards = new BirthCard[CardCount]; cardTarget = new Vector2[CardCount]; cardAngle = new float[CardCount]; cardFrom = new float[CardCount];
            dealt = new bool[CardCount];
            for (int i = 0; i < CardCount; i++)
            {
                var c = BirthCard.Create(cardsRoot, "Card" + i, false);
                c.SetScale(0.6f);
                if (i == fateSlot) c.SetFace(coreName, coreSummary + (coreImplemented ? "" : "\n(chưa có hiệu ứng)"), RarityName(), RarityColor(), result.Rarity == TraitRarity.Common ? coreOrb : RarityColor());
                coreCards[i] = c;
                float ox = (i - (CardCount - 1) / 2f) * 150f;
                cardTarget[i] = new Vector2(ox + (BirthUiKit.Hash(i * 11) - 0.5f) * 70f, 20f + (BirthUiKit.Hash(i * 13) - 0.5f) * 140f);
                cardAngle[i] = (BirthUiKit.Hash(i * 17) - 0.5f) * 50f;
                cardFrom[i] = BirthUiKit.Hash(i * 19) * Mathf.PI * 2f;
                c.Root.gameObject.SetActive(false);
            }
        }

        void BuildFlawCards()
        {
            if (cardsRoot != null) Destroy(cardsRoot.gameObject);
            cardsRoot = BirthUiKit.Group(stage.Canvas, "Cards", Vector2.zero);
            flawCards = new BirthCard[3]; cardTarget = new Vector2[3]; cardAngle = new float[3]; cardFrom = new float[3];
            dealt = new bool[3];
            for (int i = 0; i < 3; i++)
            {
                var c = BirthCard.Create(cardsRoot, "Flaw" + i, true);
                c.SetScale(0.7f);
                int pos = System.Array.IndexOf(flawChosen, i);
                if (pos >= 0 && pos < flawNames.Length)
                    c.SetFace(flawNames[pos], flawDescriptions[pos] + (flawImplemented[pos] ? "" : "\n(chưa có hiệu ứng)"), "CÁI GIÁ", BirthUiKit.Red, BirthUiKit.Red);
                flawCards[i] = c;
                cardTarget[i] = new Vector2((i - 1) * 380f, 10f);
                cardAngle[i] = (BirthUiKit.Hash(i * 23) - 0.5f) * 14f;
                cardFrom[i] = BirthUiKit.Hash(i * 29) * Mathf.PI * 2f;
                c.Root.gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------------- animation of each step

        void TickStep(BirthStep step, float t, float inst)
        {
            var size = stage.CanvasSize;
            switch (step.Kind)
            {
                case BirthStepKind.Disconnect:
                    Type(step.Text + "\nMất tín hiệu…\nĐang cố kết nối lại…", t, inst);
                    banner.color = BirthUiKit.WithAlpha(BirthUiKit.Red, ((int)(clock * 5f) % 2 == 0) ? 0.95f : 0.35f);
                    FallShards(t);
                    break;
                case BirthStepKind.Pull:
                    Type(step.Text, t, inst);
                    for (int si = 0; si < streaks.Count; si++)
                    {
                        var s = streaks[si];
                        float y = size.y / 2f + 200f - Mathf.Repeat(clock * (900f + 600f * BirthUiKit.Hash(si * 7 + 1)) + BirthUiKit.Hash(si * 5 + 3) * size.y, size.y + 400f);
                        s.rectTransform.anchoredPosition = new Vector2(s.rectTransform.anchoredPosition.x, y);
                    }
                    countdown.text = Mathf.Max(1, 3 - (int)(t / 1.4f)).ToString();
                    countdown.color = BirthUiKit.WithAlpha(Color.white, 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(t * 4f)));
                    break;
                case BirthStepKind.Form:
                    Type(step.Text, t, inst);
                    blob.rectTransform.sizeDelta = Vector2.one * (320f * (flow.Index == flow.IndexOf(BirthStepKind.Form) ? BirthUiKit.Smooth(t / 3f) : 1f) * (1f + 0.04f * Mathf.Sin(clock * 6f)));
                    break;
                case BirthStepKind.GiftIntro:
                    Type(step.Text, t, inst);
                    for (int i = 0; i < orbs.Length; i++) orbs[i].color = BirthUiKit.WithAlpha(BirthUiKit.Cyan, t > 0.4f + i * 0.5f ? 0.95f : 0.15f);
                    blob.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(clock * 5f));
                    break;
                case BirthStepKind.Gift:
                    Type(step.Text, t, inst);
                    gift.Tick(t, clock);
                    break;
                case BirthStepKind.Fate:
                    Type(step.Text, t, inst);
                    sigil.localRotation = Quaternion.Euler(0f, 0f, clock * 12f);
                    break;
                case BirthStepKind.DealCore: DealCards(coreCards, t, step.Text, inst, size); break;
                case BirthStepKind.DealFlaw: DealCards(flawCards, t, step.Text, inst, size); break;
                case BirthStepKind.RevealCore: RevealCore(t, inst); break;
                case BirthStepKind.RevealFlaw: RevealFlaw(t, inst); break;
                case BirthStepKind.Slam: slam.Tick(t); break;
                case BirthStepKind.Showcase: TickShowcase(t, step.MinSeconds); break;
                case BirthStepKind.Stabilize:
                    Type("Kết nối ổn định.\n" + step.Text, t, inst);
                    sigil.localRotation = Quaternion.Euler(0f, 0f, clock * 6f);
                    break;
            }
            foreach (var c in coreCards) if (c != null) c.Tick(clock, inst);
            foreach (var c in flawCards) if (c != null) c.Tick(clock, inst);
        }

        // ---- the magazine cover: the primal slime alone, the camera (the slime's turn) circling it

        RectTransform[] coverItems = new RectTransform[0];
        Vector2[] coverTo = new Vector2[0];
        bool coverSlam;

        void BuildShowcase(Vector2 size)
        {
            var gold = BirthUiKit.Gold; var lilac = BirthUiKit.Cyan;
            var items = new List<RectTransform>(); var to = new List<Vector2>();
            RectTransform Add(RectTransform r, Vector2 target) { items.Add(r); to.Add(target); return r; }
            float lx = -size.x * 0.27f;
            Add(BirthUiKit.Label(view, "Issue", new Vector2(lx, size.y * 0.40f), new Vector2(900f, 50f), "SỐ 001  ·  NHẬT KÝ SINH TỒN  ·  NĂM THỨ 0", 26, TextAnchor.MiddleLeft, BirthUiKit.WithAlpha(lilac, 0.9f), true).rectTransform, new Vector2(lx, size.y * 0.40f));
            Add(BirthUiKit.Box(view, "Rule", new Vector2(lx, size.y * 0.355f), new Vector2(900f, 4f), BirthUiKit.WithAlpha(lilac, 0.8f)).rectTransform, new Vector2(lx, size.y * 0.355f));
            var title = BirthUiKit.Label(view, "TitleA", new Vector2(lx, size.y * 0.20f), new Vector2(1000f, 260f), "SLIME", 230, TextAnchor.MiddleLeft, Color.white, true);
            title.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
            Add(title.rectTransform, new Vector2(lx, size.y * 0.20f));
            var sub = BirthUiKit.Label(view, "TitleB", new Vector2(lx, size.y * 0.02f), new Vector2(1000f, 190f), "NGUYÊN THỦY", 120, TextAnchor.MiddleLeft, gold, true);
            sub.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
            Add(sub.rectTransform, new Vector2(lx, size.y * 0.02f));
            string flaws = string.Join("  +  ", flawNames);
            Add(BirthUiKit.Label(view, "Core", new Vector2(lx, -size.y * 0.16f), new Vector2(1000f, 60f), "LÕI  ·  " + coreName.ToUpperInvariant(), 40, TextAnchor.MiddleLeft, gold, true).rectTransform, new Vector2(lx, -size.y * 0.16f));
            Add(BirthUiKit.Label(view, "Flaw", new Vector2(lx, -size.y * 0.23f), new Vector2(1000f, 60f), "CÁI GIÁ  ·  " + flaws.ToUpperInvariant(), 34, TextAnchor.MiddleLeft, BirthUiKit.Red, true).rectTransform, new Vector2(lx, -size.y * 0.23f));
            Add(BirthUiKit.Label(view, "Gifts", new Vector2(lx, -size.y * 0.30f), new Vector2(1000f, 60f), "6 ÂN ĐIỂN KHỞI SINH", 30, TextAnchor.MiddleLeft, BirthUiKit.WithAlpha(Color.white, 0.8f)).rectTransform, new Vector2(lx, -size.y * 0.30f));
            Add(BirthUiKit.Label(view, "Tag", new Vector2(size.x * 0.28f, -size.y * 0.42f), new Vector2(700f, 44f), "CHƯA CÓ TÊN  ·  CHƯA CÓ MẮT  ·  CHƯA CÓ CHÂN", 22, TextAnchor.MiddleRight, BirthUiKit.WithAlpha(lilac, 0.7f)).rectTransform, new Vector2(size.x * 0.28f, -size.y * 0.42f));
            coverItems = items.ToArray(); coverTo = to.ToArray(); coverSlam = false;
            fadeOut = BirthUiKit.Box(view, "FadeOut", Vector2.zero, size * 1.2f, new Color(0f, 0f, 0f, 0f));
            fadeOut.transform.SetAsLastSibling();
            Color body = new Color(0.68f, 0.88f, 0.68f);
            try { body = Evolve.Visuals.SlimeColor.Current; } catch (Exception) { }
            float s = stage.Canvas.localScale.x;
            slime = BirthSlime.Create(stage.Root.transform, new Vector3(size.x * 0.22f * s, -size.y * 0.02f * s, stage.Canvas.localPosition.z - 0.5f), 270f * s, body);
        }

        void TickShowcase(float t, float total)
        {
            var size = stage.CanvasSize;
            for (int i = 0; i < coverItems.Length; i++)
            {
                float u = BirthUiKit.Smooth((t - 0.5f - i * 0.18f) / 0.7f);
                var r = coverItems[i];
                r.anchoredPosition = coverTo[i] + new Vector2(-(1f - u) * 500f, 0f);
                var g = r.GetComponent<Graphic>(); if (g != null) { var c = g.color; g.color = new Color(c.r, c.g, c.b, Mathf.Max(c.a * 0f, 1f) * u * (i == 1 ? 0.8f : 1f)); }
            }
            if (!coverSlam && t >= 1.3f) { coverSlam = true; fx.Flash(Color.white, 0.35f); Hit(0.5f); audio.Play("slam_big", 0.8f); }
            // the eyes open (a slow half-open, a blink, then wide), the view circles the slime
            float open = BirthUiKit.Smooth((t - 3.2f) / 1.0f) * 0.5f;
            if (t > 4.6f) open = Mathf.Lerp(open, 1f, BirthUiKit.Smooth((t - 4.6f) / 0.5f));
            float blink = (t > 6.6f && t < 6.8f) ? Mathf.Abs((t - 6.7f) / 0.1f) : 1f;
            slime.Tick(clock, 20f + t * 32f, open * blink, (t - 0.8f) / 0.8f);
            fadeOut.color = new Color(0f, 0f, 0f, BirthUiKit.Smooth((t - (total - 1.4f)) / 1.2f));
        }

        void FallShards(float t)
        {
            var size = stage.CanvasSize;
            for (int i = 0; i < shards.Count; i++)
            {
                var rt = shards[i].rectTransform;
                float fall = 40f + 160f * BirthUiKit.Hash(i * 3 + 4);
                float y = rt.anchoredPosition.y - fall * Time.unscaledDeltaTime * (1f + t);
                if (y < -size.y / 2f - 60f) y = size.y / 2f + 60f;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                rt.localRotation = Quaternion.Euler(0f, 0f, clock * (30f + 80f * BirthUiKit.Hash(i * 7)));
            }
        }

        /// <summary>Cards fly in from all around, one after another, then shuffle chaotically until the step ends.</summary>
        void DealCards(BirthCard[] cards, float t, string text, float inst, Vector2 size)
        {
            Type(text, t, inst);
            for (int i = 0; i < cards.Length; i++)
            {
                var c = cards[i];
                float start = 0.3f * i;
                if (t < start) continue;
                c.Root.gameObject.SetActive(true);
                if (!dealt[i]) { dealt[i] = true; audio.Play("card_deal", 0.8f); }
                float u = BirthUiKit.Smooth((t - start) / 0.6f);
                var from = new Vector2(Mathf.Cos(cardFrom[i]), Mathf.Sin(cardFrom[i])) * (size.x * 0.7f);
                var pos = Vector2.Lerp(from, cardTarget[i], u);
                float chaos = t > 2.6f ? 1f : 0f;   // after the deal the heap keeps shuffling
                pos += new Vector2(Mathf.Sin(clock * 3.1f + i * 1.9f), Mathf.Cos(clock * 2.3f + i * 2.7f)) * 34f * chaos;
                c.Root.anchoredPosition = pos;
                c.Root.localRotation = Quaternion.Euler(0f, 0f, cardAngle[i] * u + Mathf.Sin(clock * 2f + i) * 8f * chaos);
                c.SetFlip(0f);
            }
        }

        void RevealCore(float t, float inst)
        {
            var size = stage.CanvasSize;
            const float lift = 1.0f, flipEnd = 2.0f;
            for (int i = 0; i < coreCards.Length; i++)
            {
                var c = coreCards[i];
                c.Root.gameObject.SetActive(true);
                if (i == fateSlot)
                {
                    float u = BirthUiKit.Smooth(t / lift);
                    c.Root.anchoredPosition = Vector2.Lerp(cardTarget[i], new Vector2(0f, 60f), u);
                    c.Root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(cardAngle[i], 0f, u));
                    c.SetScale(Mathf.Lerp(0.6f, 1.0f, u));
                    c.SetFlip(BirthUiKit.Smooth((t - lift) / (flipEnd - lift)));
                }
                else
                {
                    float u = BirthUiKit.Smooth(t / 0.9f);
                    c.Root.anchoredPosition = cardTarget[i] + new Vector2(0f, -size.y * 0.9f * u);
                    c.Root.localRotation = Quaternion.Euler(0f, 0f, cardAngle[i] + 40f * u * (i % 2 == 0 ? 1f : -1f));
                    c.SetFlip(0f);
                }
            }
            if (!impactDone && t >= flipEnd)
            {
                impactDone = true;
                var col = RarityColor();
                bool mythic = result.Rarity == TraitRarity.Mythic;
                fx.Burst(new Vector2(0f, 60f), col, mythic ? 140 : 70, 900f);
                fx.Flash(mythic ? BirthUiKit.Gold : Color.white, mythic ? 0.8f : 0.4f);
                Pulse(mythic ? 0.45f : 0.3f); Hit(mythic ? 1.4f : 0.7f);
                audio.Play(mythic ? "core_mythic" : result.Rarity == TraitRarity.Rare ? "core_rare" : "core_common");
                audio.Play("card_flip", 0.8f);
            }
            string text = $"{coreName}   ·   {RarityName()}\n{coreSummary}" + (coreImplemented ? "" : "\n(chưa có hiệu ứng)");
            if (t >= flipEnd) Type(text, t - flipEnd + 0.2f, inst);
        }

        void RevealFlaw(float t, float inst)
        {
            var size = stage.CanvasSize;
            int n = flawChosen.Length;
            float[] flipAt = new float[n];
            for (int k = 0; k < n; k++) flipAt[k] = 1.0f + k * 2.0f;
            for (int i = 0; i < flawCards.Length; i++)
            {
                var c = flawCards[i];
                c.Root.gameObject.SetActive(true);
                int k = System.Array.IndexOf(flawChosen, i);
                if (k >= 0)
                {
                    float u = BirthUiKit.Smooth(t / 0.8f);
                    float x = n == 1 ? 0f : (k == 0 ? -250f : 250f);
                    c.Root.anchoredPosition = Vector2.Lerp(cardTarget[i], new Vector2(x, 40f), u);
                    c.Root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(cardAngle[i], 0f, u));
                    c.SetScale(Mathf.Lerp(0.7f, n == 1 ? 1.0f : 0.9f, u));
                    c.SetFlip(BirthUiKit.Smooth((t - flipAt[k]) / 1.0f));
                    if (t >= flipAt[k] + 1.0f && !flawFlipped(k)) MarkFlipped(k);
                }
                else
                {
                    float u = BirthUiKit.Smooth(t / 0.8f);
                    c.Root.anchoredPosition = cardTarget[i] + new Vector2(0f, -size.y * 0.9f * u);
                    c.SetFlip(0f);
                }
            }
            var sb = new System.Text.StringBuilder();
            for (int k = 0; k < n && k < flawNames.Length; k++)
                if (t >= flipAt[k] + 1.0f) sb.AppendLine(flawNames[k] + ": " + flawDescriptions[k] + (flawImplemented[k] ? "" : " (chưa có hiệu ứng)"));
            if (sb.Length > 0) Type(sb.ToString(), t - flipAt[0] - 1.0f + 0.2f, inst);
        }

        readonly HashSet<int> flipped = new HashSet<int>();

        bool flawFlipped(int k) => flipped.Contains(k);

        void MarkFlipped(int k)
        {
            flipped.Add(k);
            fx.Burst(new Vector2(flawChosen.Length == 1 ? 0f : (k == 0 ? -250f : 250f), 40f), BirthUiKit.Red, 70, 800f);
            fx.Flash(BirthUiKit.Red, 0.35f);
            Pulse(0.4f); Hit(0.8f);
            audio.Play("flaw_reveal"); audio.Play("alarm", 0.4f);
        }

        void OnSlamImpact(SlamPlan.Slam s, Vector2 pos, Color color)
        {
            bool big = s.Kind != SlamKind.Gift;
            fx.Burst(pos, color, big ? 90 : 28, big ? 1000f : 600f);
            fx.Flash(s.Shake >= 1.5f ? BirthUiKit.Gold : Color.white, Mathf.Clamp01(0.15f + 0.3f * s.Shake));
            Pulse(0.15f + 0.25f * s.Shake); Hit(s.Shake);
            audio.Play(s.Shake >= 0.8f ? "slam_big" : "slam_small");
            if (s.Kind == SlamKind.Core) voice.Play(result.CoreId);
            else if (s.Kind == SlamKind.Flaw)
            {
                int idx = System.Array.IndexOf(flawNames, s.Text);
                if (idx >= 0 && result.FlawIds != null && idx < result.FlawIds.Length) voice.Enqueue(result.FlawIds[idx]);
            }
        }

        // ---------------------------------------------------------------- input and the screen

        void OnGUI()
        {
            var e = Event.current;
            if (e.type == EventType.Repaint && Time.unscaledTime - fadeInStart < 1.6f)
            {
                // after the cover the game fades in from black, so the change into the world is smooth
                float a = 1f - BirthUiKit.Smooth((Time.unscaledTime - fadeInStart) / 1.6f);
                var prev = GUI.color; GUI.depth = -10001; GUI.color = new Color(0f, 0f, 0f, a);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = prev;
            }
            if (state != State.Running || stage == null) return;
            if (e.type == EventType.KeyDown && flow != null)
            {
                if (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { flow.Skip(); e.Use(); }
                else if (e.keyCode == KeyCode.Tab) { flow.SkipIntro(); e.Use(); }
            }
            if (e.type != EventType.Repaint || stage.Rt == null) return;
            GUI.depth = -10000;   // over every other IMGUI, so the game's HUD is hidden too
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), stage.Rt, ScaleMode.StretchToFill, false);
        }
    }
}
