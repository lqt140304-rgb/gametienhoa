using System.Collections.Generic;
using System.Linq;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class TraitRoomTests
    {
        static List<string> Cores(int n) => Enumerable.Range(0, n).Select(i => "core." + i).ToList();

        [Test]
        public void JoinGivesThreeDistinctAndNoOverlapWhenEnoughCores()
        {
            var room = new TraitRoom(5, Cores(24));
            var seen = new HashSet<string>();
            for (int p = 1; p <= 8; p++)
            {
                var hand = room.Join(p);
                Assert.AreEqual(3, hand.Length);
                Assert.AreEqual(3, hand.Distinct().Count());
                foreach (var c in hand) Assert.IsTrue(seen.Add(c), "card dealt twice: " + c);
            }
        }

        [Test]
        public void RoomFirstConfirmWins()
        {
            var room = new TraitRoom(1, Cores(12));
            var hand = room.Join(1);
            var result = room.Confirm(1, hand[0]);
            Assert.AreEqual(ConfirmOutcome.Accepted, result.Outcome);
            Assert.IsTrue(room.IsLocked(hand[0]));
            Assert.AreEqual(1, room.OwnerOf(hand[0]));
        }

        [Test]
        public void RoomSecondGetsReplacementNeverDuplicate()
        {
            var room = new TraitRoom(2, Cores(4));   // 4 cores, two hands of 3: they must overlap
            var a = room.Join(1);
            var b = room.Join(2);
            string shared = a.Intersect(b).First();
            Assert.AreEqual(ConfirmOutcome.Accepted, room.Confirm(1, shared).Outcome);
            var second = room.Confirm(2, shared);
            Assert.AreEqual(ConfirmOutcome.TakenReplaced, second.Outcome);
            CollectionAssert.DoesNotContain(second.NewHand, shared);
            Assert.AreEqual(3, second.NewHand.Length);
            Assert.AreEqual(3, second.NewHand.Distinct().Count());
            Assert.AreEqual(1, room.OwnerOf(shared));
        }

        [Test]
        public void RoomHandShrinksWhenNothingFreeToReplaceWith()
        {
            var room = new TraitRoom(3, Cores(3));
            var a = room.Join(1);
            var b = room.Join(2);
            room.Confirm(1, a[0]);
            var second = room.Confirm(2, a[0]);
            Assert.AreEqual(ConfirmOutcome.TakenReplaced, second.Outcome);
            Assert.AreEqual(2, second.NewHand.Length);
        }

        [Test]
        public void RoomLeaveReleasesCore()
        {
            var room = new TraitRoom(4, Cores(12));
            var hand = room.Join(1);
            room.Confirm(1, hand[0]);
            room.Leave(1);
            Assert.IsFalse(room.IsLocked(hand[0]));
            Assert.IsNull(room.OwnerOf(hand[0]));
            Assert.AreEqual(0, room.Hand(1).Length);
        }

        [Test]
        public void RoomLateJoinGetsHandFromUnlocked()
        {
            var room = new TraitRoom(6, Cores(6));
            var taken = new List<string>();
            for (int p = 1; p <= 2; p++)
            {
                var hand = room.Join(p);
                room.Confirm(p, hand[0]);
                taken.Add(hand[0]);
            }
            var late = room.Join(9);
            Assert.AreEqual(3, late.Length);
            foreach (var c in taken) CollectionAssert.DoesNotContain(late, c);
        }

        [Test]
        public void RoomConfirmInvalid()
        {
            var room = new TraitRoom(7, Cores(12));
            var hand = room.Join(1);
            Assert.AreEqual(ConfirmOutcome.Invalid, room.Confirm(1, "core.not_in_hand").Outcome);
            Assert.AreEqual(ConfirmOutcome.Invalid, room.Confirm(99, hand[0]).Outcome);   // never joined
            Assert.AreEqual(ConfirmOutcome.Invalid, room.Confirm(1, null).Outcome);
            room.Confirm(1, hand[0]);
            Assert.AreEqual(ConfirmOutcome.Invalid, room.Confirm(1, hand[1]).Outcome);    // already confirmed
        }

        [Test]
        public void RoomEmptyStockAndSolo()
        {
            Assert.AreEqual(0, new TraitRoom(1, new List<string>()).Join(1).Length);
            Assert.AreEqual(0, new TraitRoom(1, null).Join(1).Length);
            Assert.AreEqual(3, new TraitRoom(1, Cores(12)).Join(1).Length);
        }

        [Test]
        public void ClaimLocksAFreeCoreAndRefusesTakenOrUnknown()
        {
            var room = new TraitRoom(1, Cores(4));
            Assert.IsTrue(room.Claim(1, "core.0"));
            Assert.IsTrue(room.IsLocked("core.0"));
            Assert.AreEqual(1, room.OwnerOf("core.0"));
            Assert.IsFalse(room.Claim(2, "core.0"));        // taken
            Assert.IsFalse(room.Claim(2, "core.nope"));     // not in stock
            Assert.IsFalse(room.Claim(1, "core.1"));        // the player already has one
            Assert.IsFalse(room.Claim(3, null));
        }

        [Test]
        public void ClaimedCoreMakesALaterConfirmGetAReplacement()
        {
            var room = new TraitRoom(2, Cores(4));
            var hand = room.Join(2);
            string target = hand[0];
            Assert.IsTrue(room.Claim(1, target));           // player 1 already had this core before the room existed
            var result = room.Confirm(2, target);
            Assert.AreEqual(ConfirmOutcome.TakenReplaced, result.Outcome);
            CollectionAssert.DoesNotContain(result.NewHand, target);
        }

        [Test]
        public void RoomJoinTwiceReturnsSameHand()
        {
            var room = new TraitRoom(8, Cores(12));
            CollectionAssert.AreEqual(room.Join(1), room.Join(1));
        }
    }
}
