using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Player;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Net
{
    /// <summary>
    /// A ghost of another player: a coloured slime that follows the position it was last told, wears the same
    /// organs (via OrganHost) and carries a name and health for the labels. It has no physics, but it does carry
    /// a SurvivalStats marked IsRemote (numbers come from the network): on the host, creatures chase it, bite it
    /// and get caught by it by distance, and the results are sent to the real player.
    /// </summary>
    public class RemoteAvatar : MonoBehaviour
    {
        static Dictionary<string, OrganSO> organLookup;

        public ulong ClientId { get; private set; }
        public string DisplayName { get; private set; } = "";
        public float Health { get; private set; }
        public float MaxHealth { get; private set; } = 100f;
        public byte Lives { get; private set; }
        public bool Dead { get; private set; }
        public bool Lost { get; private set; }
        public float LastSeen { get; private set; }
        public SurvivalStats Stats { get; private set; }

        Vector3 previousPosition;
        float lastApplyTime;
        bool hasPrevious;
        Transform body;
        Evolve.Visuals.SlimeGelTint tint;
        OrganHost organs;
        Vector3 targetPosition;
        float targetYaw;
        string organKey = "";
        bool placed;

        public static RemoteAvatar Create(ulong clientId, string name)
        {
            var go = new GameObject($"Remote Player {clientId}");
            var avatar = go.AddComponent<RemoteAvatar>();
            avatar.Build(clientId, name);
            return avatar;
        }

        void Build(ulong clientId, string name)
        {
            ClientId = clientId;
            DisplayName = name;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<MeshFilter>().sharedMesh = Evolve.Visuals.SlimeMeshBuilder.Build();
            visual.AddComponent<Evolve.Visuals.SlimeGelCore>();
            visual.transform.SetParent(transform, false);
            body = visual.transform;

            // The same gel material as the local slime; each player's own colour comes from SlimeGelTint.
            var renderer = visual.GetComponent<Renderer>();
            var shared = FindSlimeMaterial();
            if (shared != null) renderer.sharedMaterial = shared;
            tint = visual.GetComponent<Evolve.Visuals.SlimeGelTint>();


            // The sockets must exist before OrganHost, which looks for them when it wakes up.
            foreach (OrganSlot slot in System.Enum.GetValues(typeof(OrganSlot)))
                AddSocket(body, slot, Evolve.Visuals.SlimeSockets.Position(slot));
            Stats = gameObject.AddComponent<SurvivalStats>();
            Stats.IsRemote = true;
            Stats.RemoteClientId = clientId;
            organs = gameObject.AddComponent<OrganHost>();
        }

        static void AddSocket(Transform parent, OrganSlot slot, Vector3 localPosition)
        {
            var go = new GameObject("Socket_" + slot);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<OrganSocket>().Slot = slot;
        }

        static Material FindSlimeMaterial()
        {
            var controller = FindAnyObjectByType<SlimeController>();
            if (controller == null) return null;
            var r = controller.GetComponentInChildren<Renderer>();
            return r != null ? r.sharedMaterial : null;
        }

        /// <summary>Take in a new state from the network.</summary>
        public void Apply(PlayerNetState s, float now)
        {
            LastSeen = now;
            if (!string.IsNullOrEmpty(s.Name)) DisplayName = s.Name;
            Health = s.Health;
            MaxHealth = s.MaxHealth > 0f ? s.MaxHealth : 100f;
            Lives = s.Lives;
            Dead = s.Dead;
            if (tint != null && s.PartColors != null && s.PartColors.Length == 18)
            {
                var colors = new Color[Evolve.Visuals.SlimeColor.PartCount];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = BodyFeedback.SickTint(new Color32(s.PartColors[i * 3], s.PartColors[i * 3 + 1], s.PartColors[i * 3 + 2], 255), s.Diseases); // a sick player looks sick
                tint.SetColors(colors);
            }
            Lost = s.Lost;

            // Mirror the numbers into the ghost's stats so creatures can react to it.
            var st = Stats.State;
            st.MaxHealth = MaxHealth;
            st.Health = Dead ? 0f : Mathf.Max(0.01f, Health);
            st.Lives = Lives;
            st.SetDiseaseMask(s.Diseases); // the ghost shows (and can pass on) the other player's diseases
            if (hasPrevious && now - lastApplyTime > 0.01f)
                Stats.IsMoving = (s.Position - previousPosition).magnitude / (now - lastApplyTime) > 0.3f;
            previousPosition = s.Position;
            lastApplyTime = now;
            hasPrevious = true;

            targetPosition = s.Position;
            targetYaw = s.Yaw;
            if (!placed)
            {
                transform.position = targetPosition;
                body.rotation = Quaternion.Euler(0f, targetYaw, 0f);
                placed = true;
            }
            MirrorOrgans(s.OrganNames, s.OrganLevels);
            body.localScale = Lost ? Vector3.zero : Dead ? new Vector3(1.1f, 0.25f, 1.1f) : Vector3.one;
        }

        void Update()
        {
            if (!placed) return;
            float k = 1f - Mathf.Exp(-14f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, k);
            body.rotation = Quaternion.Slerp(body.rotation, Quaternion.Euler(0f, targetYaw, 0f), k);
        }

        void MirrorOrgans(string[] names, byte[] levels)
        {
            string key = names == null ? "" : string.Join(",", names) + "|" + (levels == null ? "" : string.Join(",", levels));
            if (key == organKey) return;
            organKey = key;

            var wanted = new List<OrganSO>();
            if (names != null)
                for (int i = 0; i < names.Length; i++)
                {
                    var organ = FindOrgan(names[i]);
                    if (organ == null) continue;
                    wanted.Add(organ);
                    organs.MirrorOrgan(organ, levels[i]);
                }
            foreach (var have in organs.OrganList)
                if (!wanted.Contains(have)) organs.Detach(have.Slot);
        }

        internal static OrganSO FindOrgan(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (organLookup == null || !organLookup.ContainsKey(name))
            {
                organLookup = new Dictionary<string, OrganSO>();
                foreach (var o in Resources.FindObjectsOfTypeAll<OrganSO>()) organLookup[o.name] = o;
            }
            return organLookup.TryGetValue(name, out var found) ? found : null;
        }
    }
}
