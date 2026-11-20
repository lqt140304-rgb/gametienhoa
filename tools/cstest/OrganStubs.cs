// Stand-ins for the organ data assets (the real ones carry Unity-only fields).
using System.Collections.Generic;
using UnityEngine;
namespace Evolve.Organs
{
    public enum OrganSlot { Eyes, Legs, Skin, Back }
    public class OrganSO : ScriptableObject { public string DisplayName; public OrganSlot Slot; public OrganTier Tier = OrganTier.C; }
    public class DropTableSO : ScriptableObject { public List<OrganSO> Organs = new List<OrganSO>(); }
}
