using System;
using System.Linq;
using System.Reflection;
using Evolve.Traits;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    /// <summary>
    /// The cave team's EncounterRewardBridge calls EncounterRewards through reflection: it takes the FIRST public static method with the same name and
    /// number of parameters and invokes it (a wrong type is swallowed, and the encounter silently falls back). So each of these calls must match
    /// exactly one method with exactly these parameter types.
    /// </summary>
    public class EncounterRewardBridgeCompatTests
    {
        static void Expect(string name, params Type[] types)
        {
            var same = typeof(EncounterRewards).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == name && m.GetParameters().Length == types.Length).ToList();
            Assert.AreEqual(1, same.Count, name + ": exactly one method with " + types.Length + " parameters (the bridge takes the first it finds)");
            CollectionAssert.AreEqual(types, same[0].GetParameters().Select(p => p.ParameterType).ToArray(), name + " parameter types");
        }

        [Test] public void Say() => Expect("Say", typeof(string));
        [Test] public void RefillStats() => Expect("RefillStats", typeof(GameObject));
        [Test] public void UnlockColor() => Expect("UnlockColor", typeof(GameObject), typeof(Color));
        [Test] public void GrantGene() => Expect("GrantGene", typeof(GameObject), typeof(int));
        [Test] public void GrantUniqueGene() => Expect("GrantUniqueGene", typeof(GameObject), typeof(string));
        [Test] public void CureDiseases() => Expect("CureDiseases", typeof(GameObject));
        [Test] public void RerollFlaw() => Expect("RerollFlaw", typeof(GameObject), typeof(int));
        [Test] public void AddHeatResistance() => Expect("AddHeatResistance", typeof(GameObject), typeof(float));
        [Test] public void GiveStomachItems() => Expect("GiveStomachItems", typeof(GameObject), typeof(string[]));
    }
}
