using System.Collections.Generic;
using Evolve.Combat;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Ties the scorpion to its boss room (BossRoom_Scorpion, "Bãi Cát Tinh Thể"): the low boulders of the room (children named Prop_Pillar_*) stop the mound of Đào Chui, so a player can hide behind one
    /// (<see cref="ScorpionRoomRules"/>). Installed when the scorpion wakes (<see cref="ScorpionMoves"/>); outside a room nothing changes (it digs anywhere, <see cref="ScorpionArena"/>).
    /// </summary>
    public static class ScorpionRoom
    {
        const float RoomReach = 60f;

        public static void Install(Transform scorpion)
        {
            BossRoom room = null; float best = RoomReach;
            foreach (var r in Object.FindObjectsByType<BossRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(r.transform.position, scorpion.position);
                if (d < best) { best = d; room = r; }
            }
            if (room == null) return;
            var rocks = new List<ScorpionRock>();
            foreach (var t in room.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Prop_Pillar")) continue;
                var col = t.GetComponentInChildren<Collider>(true);
                Bounds b = col != null ? col.bounds : new Bounds(t.position, Vector3.one);
                rocks.Add(new ScorpionRock(b.center.x, b.center.z, Mathf.Max(b.extents.x, b.extents.z)));
            }
            ScorpionArena.CanBurrowAt = (x, z) => ScorpionRoomRules.CanDig(x, z, rocks);
            Debug.Log("[Bọ Cạp] Phòng " + room.name + ": " + rocks.Count + " tảng đá chặn đường đào của Đào Chui.");
        }

        public static void Uninstall() => ScorpionArena.Reset();
    }
}
