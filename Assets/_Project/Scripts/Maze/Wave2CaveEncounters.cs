using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>Kỳ ngộ Hang Rêu Thở: Hơi Thở Của Rêu (thảm rêu sáng tối theo nhịp, nằm yên lúc thở ra -> hồi đầy & khỏi bệnh).</summary>
    [RequireComponent(typeof(Collider))]
    public class BreathingMossEncounter : MonoBehaviour
    {
        [SerializeField] Light pulseLight;
        [SerializeField] Renderer mossPadRenderer;
        [SerializeField] float stillSpeedThreshold = 0.20f;

        readonly CaveEncounterWave2Rules.BreathingMossState state = new CaveEncounterWave2Rules.BreathingMossState();
        Vector3 lastPlayerPos;
        bool hasLastPos;
        bool playerOnMoss;
        GameObject currentPlayer;

        public void Configure(Light lightComp, Renderer padRend)
        {
            pulseLight = lightComp;
            mossPadRenderer = padRend;
        }

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var body = other.GetComponentInParent<SlimeBody>();
            if (body == null) return;
            playerOnMoss = true;
            currentPlayer = body.gameObject;
            lastPlayerPos = body.transform.position;
            hasLastPos = true;
        }

        void OnTriggerExit(Collider other)
        {
            var body = other.GetComponentInParent<SlimeBody>();
            if (body != null && body.gameObject == currentPlayer)
            {
                playerOnMoss = false;
                currentPlayer = null;
                hasLastPos = false;
            }
        }

        void Update()
        {
            var phase = CaveEncounterWave2Rules.BreathingMossState.EvaluatePhase(Time.time);
            float pulse = phase == BreathingMossPhase.Exhale
                ? 0.65f + 0.35f * Mathf.Sin(Time.time * 4f)
                : 0.20f + 0.15f * Mathf.Sin(Time.time * 1.5f);

            if (pulseLight != null)
                pulseLight.intensity = Mathf.Lerp(6f, 26f, pulse);

            if (state.Completed || !playerOnMoss || currentPlayer == null) return;

            float dt = Mathf.Max(0.001f, Time.deltaTime);
            Vector3 p = currentPlayer.transform.position;
            float speed = hasLastPos ? (p - lastPlayerPos).magnitude / dt : 99f;
            lastPlayerPos = p;
            hasLastPos = true;

            bool isStill = speed <= stillSpeedThreshold;
            if (state.TryComplete(Time.time, true, isStill))
            {
                EncounterRewardBridge.RefillStats(currentPlayer, 100f);
                EncounterRewardBridge.CureDiseases(currentPlayer);
                EncounterRewardBridge.Say("Thảm rêu thở ra một luồng khí ấm áp, mọi vết thương và bệnh tật đều tan biến.");
            }
        }
    }

    /// <summary>Kỳ ngộ Tổ Bỏ Hoang: Quả Trứng Còn Sống (nuốt trứng trong khe, vài phút sau nở trong Túi cho gene lạ).</summary>
    public class LivingEggEncounter : MonoBehaviour
    {
        readonly CaveEncounterWave2Rules.LivingEggState state = new CaveEncounterWave2Rules.LivingEggState();
        GameObject carrierPlayer;

        public void OnEggSwallowed(GameObject player, GameObject eggVisual)
        {
            if (!state.TrySwallowEgg()) return;
            carrierPlayer = player;
            if (eggVisual != null) eggVisual.SetActive(false);
            EncounterRewardBridge.Say("Quả trứng còn ấm đang đập khẽ bên trong Túi của ngươi...");
        }

        void Update()
        {
            if (carrierPlayer == null) return;
            if (state.AdvanceIncubation(Time.deltaTime))
            {
                EncounterRewardBridge.GrantUniqueGene(carrierPlayer, "TrungLa");
                EncounterRewardBridge.Say("Vỏ trứng nứt vỡ bên trong cơ thể — một dòng gene cổ lạ hòa vào máu ngươi!");
            }
        }
    }

    [RequireComponent(typeof(Collider))]
    public class LivingEggPickup : MonoBehaviour
    {
        [SerializeField] LivingEggEncounter owner;

        public void Configure(LivingEggEncounter encounter) => owner = encounter;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (owner == null) return;
            var body = other.GetComponentInParent<SlimeBody>();
            if (body != null) owner.OnEggSwallowed(body.gameObject, gameObject);
        }
    }
}
