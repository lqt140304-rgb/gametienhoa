using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Tells the grass shader who is walking through it: every frame, the walkers nearest to the camera (slimes, local and remote, and the creatures)
    /// are written to the global array <c>_EvolvePushers</c> (x, y, z = position, w = radius). Blades within the radius lean away and duck.
    /// Created by itself when the game starts.
    /// </summary>
    public class GrassPushers : MonoBehaviour
    {
        static readonly int PushersId = Shader.PropertyToID("_EvolvePushers");
        readonly List<Vector4> candidates = new List<Vector4>();
        readonly List<Vector4> chosen = new List<Vector4>();
        readonly Vector4[] array = new Vector4[GrassMath.MaxPushers];
        Camera cam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindAnyObjectByType<GrassPushers>() != null) return;
            var go = new GameObject("Grass Pushers");
            go.AddComponent<GrassPushers>();
            DontDestroyOnLoad(go);
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            candidates.Clear();
            foreach (var s in SurvivalStats.All) if (s != null) candidates.Add(Pusher(s.transform.position, 1.7f));
            foreach (var p in Prey.Active) if (p != null) candidates.Add(Pusher(p.transform.position, 1.3f));
            foreach (var p in Predator.Active) if (p != null) candidates.Add(Pusher(p.transform.position, 1.9f));
            GrassMath.SelectPushers(candidates, cam.transform.position, chosen);
            for (int i = 0; i < array.Length; i++) array[i] = i < chosen.Count ? chosen[i] : Vector4.zero;
            Shader.SetGlobalVectorArray(PushersId, array);
        }

        static Vector4 Pusher(Vector3 position, float radius) => new Vector4(position.x, position.y, position.z, radius);

        void OnDisable()
        {
            for (int i = 0; i < array.Length; i++) array[i] = Vector4.zero;
            Shader.SetGlobalVectorArray(PushersId, array);
        }
    }
}
