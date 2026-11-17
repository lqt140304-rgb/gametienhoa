using Unity.Netcode;
using UnityEngine;

namespace Evolve.Net
{
    /// <summary>
    /// What other players need to know about one player: where they are, how healthy, which organs.
    /// Sent about 10 times a second. Organs are identified by their asset name and sent with their level.
    /// </summary>
    public struct PlayerNetState
    {
        public const int MaxOrgans = 16;

        public string Name;
        public Vector3 Position;
        public float Yaw;
        public float Health, MaxHealth;
        public byte Lives;
        public bool Dead;
        public bool Lost; // corpse gone for good
        public Color32 BodyColor; // the slime colour chosen by this player (set by the sender)
        public string[] OrganNames;
        public byte[] OrganLevels;

        public void Write(ref FastBufferWriter w)
        {
            w.WriteValueSafe(Name ?? "");
            w.WriteValueSafe(Position.x);
            w.WriteValueSafe(Position.y);
            w.WriteValueSafe(Position.z);
            w.WriteValueSafe(Yaw);
            w.WriteValueSafe(Health);
            w.WriteValueSafe(MaxHealth);
            w.WriteValueSafe(Lives);
            w.WriteValueSafe(Dead);
            w.WriteValueSafe(Lost);
            w.WriteValueSafe(BodyColor.r);
            w.WriteValueSafe(BodyColor.g);
            w.WriteValueSafe(BodyColor.b);

            int n = OrganNames == null ? 0 : Mathf.Min(OrganNames.Length, MaxOrgans);
            w.WriteValueSafe((byte)n);
            for (int i = 0; i < n; i++)
            {
                w.WriteValueSafe(OrganNames[i] ?? "");
                w.WriteValueSafe(OrganLevels[i]);
            }
        }

        public static PlayerNetState Read(ref FastBufferReader r)
        {
            var s = new PlayerNetState();
            r.ReadValueSafe(out string name);
            r.ReadValueSafe(out float x);
            r.ReadValueSafe(out float y);
            r.ReadValueSafe(out float z);
            r.ReadValueSafe(out float yaw);
            r.ReadValueSafe(out float health);
            r.ReadValueSafe(out float maxHealth);
            r.ReadValueSafe(out byte lives);
            r.ReadValueSafe(out bool dead);
            r.ReadValueSafe(out bool lost);
            r.ReadValueSafe(out byte cr);
            r.ReadValueSafe(out byte cg);
            r.ReadValueSafe(out byte cb);
            r.ReadValueSafe(out byte count);

            s.Name = name;
            s.Position = new Vector3(x, y, z);
            s.Yaw = yaw;
            s.Health = health;
            s.MaxHealth = maxHealth;
            s.Lives = lives;
            s.Dead = dead;
            s.Lost = lost;
            s.BodyColor = new Color32(cr, cg, cb, 255);
            s.OrganNames = new string[count];
            s.OrganLevels = new byte[count];
            for (int i = 0; i < count; i++)
            {
                r.ReadValueSafe(out string organ);
                r.ReadValueSafe(out byte level);
                s.OrganNames[i] = organ;
                s.OrganLevels[i] = level;
            }
            return s;
        }
    }
}
