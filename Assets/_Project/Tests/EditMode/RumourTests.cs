using System.Collections.Generic;
using Evolve.Civilization;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class RumourTests
    {
        static List<Knowledge> Minds(int n) { var l = new List<Knowledge>(); for (int i = 0; i < n; i++) l.Add(new Knowledge()); return l; }

        [Test] public void NewsTravelsOneGapPerRound()
        {
            var pos = new List<Vector3> { Vector3.zero, new Vector3(3f, 0f, 0f), new Vector3(6f, 0f, 0f), new Vector3(20f, 0f, 0f) };
            var minds = Minds(4);
            var talk = new List<bool> { true, true, true, true };
            minds[0].Learn("slime");
            Assert.AreEqual(1, Rumour.Spread(pos, minds, talk, ReputationRules.SpreadRadius));
            Assert.IsTrue(minds[1].Knows("slime")); Assert.IsFalse(minds[2].Knows("slime"));
            Rumour.Spread(pos, minds, talk, ReputationRules.SpreadRadius);
            Assert.IsTrue(minds[2].Knows("slime"));
            for (int i = 0; i < 5; i++) Rumour.Spread(pos, minds, talk, ReputationRules.SpreadRadius);
            Assert.IsFalse(minds[3].Knows("slime"), "too far away to hear");
        }

        [Test] public void OnlyThoseWhoCanTalkShare()
        {
            var pos = new List<Vector3> { Vector3.zero, new Vector3(2f, 0f, 0f) };
            var minds = Minds(2);
            minds[0].Learn("slime");
            Rumour.Spread(pos, minds, new List<bool> { false, true }, 4f);
            Assert.IsFalse(minds[1].Knows("slime"));
        }

        [Test] public void LearningTwiceIsNotNews()
        {
            var k = new Knowledge();
            Assert.IsTrue(k.Learn("slime"));
            Assert.IsFalse(k.Learn("slime"));
            Assert.AreEqual(1, k.Count);
        }
    }
}
