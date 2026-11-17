using Evolve.Survival;
using UnityEngine;

namespace Evolve.Ecology
{
    /// <summary>
    /// Body left by a dead creature. Decays through Fresh -> Rotten -> Humus -> Gone (see CarcassDecay),
    /// changing colour with the stage. A local player touching it while Fresh/Rotten eats from it.
    /// Humus turns back into region food when it is Gone. Host only: Spawn does nothing on a client.
    /// Not network-synced yet. (Sickness / dirty water from Rotten belong to 0.5b/0.5d; they read <see cref="Stage"/>.)
    /// </summary>
    public class Carcass : MonoBehaviour
    {
        const float BiteAmount = 15f;
        const float BiteCooldown = 1f;

        static readonly Color FreshColor = new Color(0.45f, 0.08f, 0.08f);
        static readonly Color RottenColor = new Color(0.30f, 0.38f, 0.15f);
        static readonly Color HumusColor = new Color(0.30f, 0.20f, 0.10f);

        public CarcassDecay Decay { get; private set; } = new CarcassDecay();
        public CarcassStage Stage => Decay.Stage;

        Region region;
        Renderer rend;
        Material mat;
        CarcassStage shownStage = (CarcassStage)(-1);
        float nextBite;

        /// <summary>Create a carcass. Host / single player only: returns null on a client.</summary>
        public static Carcass Spawn(Vector3 pos, float size, Region region)
        {
            if (NetWorld.IsClient) return null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Carcass";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size, size * 0.4f, size);
            go.GetComponent<Collider>().isTrigger = true;
            var c = go.AddComponent<Carcass>();
            c.region = region;
            return c;
        }

        void Awake()
        {
            rend = GetComponent<Renderer>();
            if (rend != null) mat = rend.material; // one material instance per carcass
            ApplyColor();
        }

        void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }

        void Update()
        {
            Decay.Tick(Time.deltaTime / 60f);
            if (Decay.Stage == CarcassStage.Gone)
            {
                if (region != null) region.AddFood(Decay.HumusFoodReleased);
                Destroy(gameObject);
                return;
            }
            ApplyColor();
        }

        void ApplyColor()
        {
            if (mat == null || shownStage == Decay.Stage) return;
            shownStage = Decay.Stage;
            switch (shownStage)
            {
                case CarcassStage.Fresh: mat.color = FreshColor; break;
                case CarcassStage.Rotten: mat.color = RottenColor; break;
                default: mat.color = HumusColor; break;
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (Time.time < nextBite) return;
            if (Decay.Stage != CarcassStage.Fresh && Decay.Stage != CarcassStage.Rotten) return;
            var stats = other.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.State.IsDead || stats.IsRemote) return;
            nextBite = Time.time + BiteCooldown;
            NetWorld.Feed(stats, Decay.Eat(BiteAmount));
            if (Decay.FoodLeft <= 0.01f) Destroy(gameObject); // eaten up
        }
    }
}
