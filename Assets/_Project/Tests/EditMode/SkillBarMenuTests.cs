using System.Reflection;
using Evolve.Abilities;
using Evolve.Organs;
using Evolve.Survival;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SkillBarMenuTests
    {
        [Test]
        public void ClearedSkillStaysInLibraryUntilAssignedAgain()
        {
            var player = new GameObject("HUD test");
            var ability = ScriptableObject.CreateInstance<SpikeBurstAbility>();
            var organ = ScriptableObject.CreateInstance<OrganSO>();
            try
            {
                var stats = player.AddComponent<SurvivalStats>();
                var host = player.AddComponent<OrganHost>();
                var bar = player.AddComponent<SkillBar>();
                // EditMode does not guarantee Awake; these component references are all that Sync needs.
                typeof(SkillBar).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(bar,host);
                organ.GrantedAbility=ability;organ.GrowthExpRequired=0;organ.AbilityUnlockLevel=1;
                host.TryAttach(organ);
                var sync=typeof(SkillBar).GetMethod("Sync",BindingFlags.Instance|BindingFlags.NonPublic);
                sync.Invoke(bar,null);Assert.AreEqual(organ,bar.GetSlot(0));
                bar.Clear(0);sync.Invoke(bar,null);Assert.AreEqual(-1,bar.SlotOf(organ));
                bar.Assign(3,organ);sync.Invoke(bar,null);Assert.AreEqual(organ,bar.GetSlot(3));
            }
            finally { Object.DestroyImmediate(player);Object.DestroyImmediate(organ);Object.DestroyImmediate(ability); }
        }
    }
}
