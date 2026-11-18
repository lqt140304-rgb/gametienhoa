# Treefolk Grudge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A band of placeholder treefolk lives in the Root Foot cave; hurting one makes the tribe hold a grudge that spreads creature-to-creature, guards turn on you and the cave closes; offerings at a carved stone atone up to "cold".

**Architecture:** Pure rules (`Reputation`, `Rumour`) hold the numbers and the spreading, tested in EditMode. `Treefolk` (one creature) and `TreefolkTribe` (the band: ledger, spreading tick, gates) run on the host; `TreefolkSync` mirrors them to co-op clients. An `IDamageable` interface lets spikes hit treefolk like prey/predators. An editor tool places the band, gates and the stone.

**Tech Stack:** Unity 6.6 URP, C# 9, NGO 2.x named messages, NUnit EditMode.

**Spec:** `docs/superpowers/specs/2026-11-17-treefolk-grudge-design.md`

## Global Constraints
- Reputation per tribe per player in −100…+100, start 0. Standings: Hostile ≤ −50, Cold −50…−10, Neutral −10…+30, Friendly +30…+70, Member ≥ +70.
- Hit −15, kill −60; companions within 15 m get 1/4. No decay over time.
- Witnesses: the victim and treefolk within 20 m with line of sight learn at once. Spread: a knowing treefolk passes it to one within 4 m (≈1 s); corpses tell anyone within 6 m. Whole band knows in ≈1–2 min.
- A treefolk acts on the tribe's value for a player only once it knows; before that, as Neutral.
- Atonement stone ~25 m outside the wide door; offerings raise reputation, never above −10.
- 10–14 treefolk, 1–1.5 m tall, 2–3 guards at the door and the slit; a `ClassId` string on each (unused).
- Hostile: guards attack on sight, citizens flee, door and slit are blocked by roots. Cold: citizens keep away, guards follow at a distance.
- C# 9, host-authoritative co-op, no Unity here (the user runs the tests and sends screenshots).

## Review Focus
- A player who is Hostile and stands in the doorway when the roots close: they must be pushed out, never trapped inside the blocker.
- Two players, one Hostile, one Neutral, near the cave: the gates close only while the hostile one is near; the neutral one is never attacked.
- Killing the last treefolk in sight: the corpse still spreads the grudge to others who come by.
- Offering at the stone while Neutral or above: nothing changes (no farming reputation upward).
- Co-op client hitting a treefolk: the hit is counted once, on the host.

---

### Task 1: Reputation rules
**Files:** Create `Scripts/Civilization/Reputation.cs`; Test `Tests/EditMode/ReputationTests.cs`
**Produces:** `enum Standing { Hostile, Cold, Neutral, Friendly, Member }`; `static class ReputationRules` with consts `HitPenalty = -15f, KillPenalty = -60f, CompanionShare = 0.25f, CompanionRadius = 15f, AtonementCap = -10f, WitnessRadius = 20f, SpreadRadius = 4f, CorpseRadius = 6f, OfferingValue = 12f` and `static Standing StandingOf(float value)`; `class TribeLedger { float Get(object player); void Offend(object offender, float penalty, IEnumerable<object> companions); bool Offer(object player); }` (keys are any object: the player's `SurvivalStats` on the host).
- [ ] Tests: `StandingOf(-50)==Hostile`, `(-49.9)==Cold`, `(-10)==Neutral`... boundaries as in Global Constraints; `Offend(p, Hit)` → −15, companions −3.75; value clamps at −100; `Offer` from −80 raises by 12, never above −10, returns false and changes nothing at ≥ −10.
- [ ] Implement; commit.

### Task 2: Rumour spreading
**Files:** Create `Scripts/Civilization/Rumour.cs`; Test `Tests/EditMode/RumourTests.cs`
**Produces:** `class Knowledge { bool Knows(object offender); bool Learn(object offender); IEnumerable<object> Offenders }`; `static class Rumour { static int Spread(IList<Vector3> positions, IList<Knowledge> minds, IList<bool> canTalk, float radius) }` — one pass: every pair within `radius` where one knows and the other can talk shares; returns how many learned. Called about once a second.
- [ ] Tests: three in a 3 m chain, first knows → after 2 passes all know; one 10 m away never learns with radius 4; a corpse (canTalk true, mind knows) teaches within 6 m; learning twice returns false.
- [ ] Implement; commit.

### Task 3: Damageable + treefolk creature
**Files:** Create `Scripts/Creatures/IDamageable.cs`, `Scripts/Civilization/Treefolk.cs`, `Scripts/Visuals/TreefolkMesh.cs`; Modify `Scripts/Abilities/SpikeProjectile.cs`, `Scripts/Creatures/Prey.cs`, `Scripts/Creatures/Predator.cs` (implement the interface)
**Produces:** `interface IDamageable { void TakeDamage(float amount, SurvivalStats by); }`; `class Treefolk : MonoBehaviour, IDamageable` with `public enum Role { Citizen, Guard }`, `Role`, `string ClassId`, `float Health`, `bool IsDead`, `Knowledge Mind`, `TreefolkTribe Tribe`, `bool Alarmed`, `static IReadOnlyList<Treefolk> Active`; `TreefolkMesh.Build(int seed, float height) -> Mesh` (bark trunk, root feet, moss crown).
- Behaviour (host only): wander between the tribe's home points; when it learns about an offender: alarmed for 6 s (runs at 2× to the hall point, a small "!" above it); toward a known player by standing: Hostile → guard chases and hits (8 damage per hit, 1 s apart, reach 1.4 m) / citizen flees; Cold → citizen keeps ≥ 6 m away, guard follows at ~4 m; otherwise normal. Max health 30; dead treefolk lie down and stay as a corpse (still in `Active` with `IsDead`) for 5 min.
- SpikeProjectile hits any `IDamageable` (replace the two type checks).
- [ ] Test (EditMode): `TreefolkMesh.Build(1, 1.3f)` bounds height 1.2–1.45 m, no NaN.
- [ ] Implement; commit.

### Task 4: The tribe: ledger, spreading, gates, stone
**Files:** Create `Scripts/Civilization/TreefolkTribe.cs`, `Scripts/Civilization/AtonementStone.cs`
**Consumes:** Tasks 1–3.
- `TreefolkTribe`: owns `TribeLedger`, members, home points, hall point, gate objects. `ReportAttack(Treefolk victim, SurvivalStats by, bool killed)`: ledger.Offend (companions = other players within 15 m), victim + witnesses (20 m, raycast line of sight) learn. Every 1 s: `Rumour.Spread` (living within 4 m, corpses within 6 m). Gates (root walls at the door and the slit) are active while any Hostile player is within 40 m of the cave centre; when they close, any player inside a gate's bounds is moved 2 m outward.
- `AtonementStone`: a trigger; a player inside it pressing E (or the interact key used elsewhere) spends 25 hunger for `ledger.Offer`; message on screen with the new standing; 3 s between offerings.
- Standing shown on screen while within 50 m of the cave ("Người cây: Thù / Lạnh nhạt / Bình thường").
- [ ] Commit.

### Task 5: Editor tool + co-op
**Files:** Create `Editor/LakeTreefolk.cs` (menu "Evolve/Dress Lake: Treefolk", added to Rebuild Lake), `Scripts/Net/TreefolkSync.cs`; Modify `Editor/LakeRebuildAll.cs`, `Scripts/Net/CoopManager.cs` (register messages)
- Tool: 12 treefolk (3 guards: 2 at the door, 1 at the slit), home points round the cave floor, hall point at the cave centre, root-wall gates fitted to the door (5.2 × 3.6 m) and slit, the stone 25 m east of the door on dry ground.
- `TreefolkSync`: host → clients every 0.1 s "evolve.tf" (index, position, yaw, flags alarmed/dead/attacking); clients disable treefolk brains and follow; client spike hits on treefolk → "evolve.tfhit" (index, damage) → host applies with the client's avatar stats as the attacker.
- [ ] Commit; push; PR; user runs Rebuild Lake + Test Runner.
