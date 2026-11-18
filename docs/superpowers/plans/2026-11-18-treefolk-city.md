# Treefolk City Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the Root Foot cave into a grand dwarf-style treefolk city spread over the whole area 02.

**Architecture:** `CityLayout` (pure, tested) holds every coordinate: hall, pillars, dais, cliff-block homes, workshops, road, statues, gate, gardens, posts. `LakeTreefolkCity` (editor tool, idempotent) builds meshes from it with the existing `RootMeshes`/`CaveMeshes`/`FloraMeshes`/`LakeGround` helpers; the old cave in `LakeLandmarks.RootCave` is replaced by the bigger hall. `LakeTreefolk` places 28 treefolk from the layout.

**Tech Stack:** Unity 6.6 URP, C# 9, NUnit EditMode.

**Spec:** `docs/superpowers/specs/2026-11-18-treefolk-city-design.md`

## Global Constraints
- Hall 58 × 44 m, 18 m high; 8 pillars; dais Ø 8 m with 3 steps and a petrified-wood throne; amber furnace at the far end.
- Cliff blocks N and S of the hall, 16–20 round-arched niches each (1 m wide, 1.6 m high), 3 floors, root stairs and balconies. 6 workshops (4 × 3 m niches) in a west cliff.
- Road 6 m wide, ~45 m, flagstones, 12 statues 2–3 m; gate arch of two giant roots, 10 m high.
- 28 treefolk: 4 guards (gate ×2, hall door, slit), 24 citizens by groups; atonement stone ≥ 25 m from any guard, outside the gate.
- Door 5.2 × 3.6 m east, slit 1.35 m wide × 1.1 m high north (unchanged widths). Niches are shallow (< 1.3 m): no slime enters houses.
- Everything built by code, idempotent, placed on real ground height; 64 m chunks + LOD; FPS not > 15 % lower near the city. No Unity here (user runs tests, sends screenshots).

## Review Focus
- Re-running the tool twice: no duplicate halls or treefolk.
- The slime must walk from the road through the door, around pillars and onto the dais without catching on colliders (pillar gaps ≥ 3 m).
- The slit stays exactly slime-wide after the hall grows.
- The tunnel 02 → 03 still leaves the enlarged hall cleanly (its start point moves).
- Treefolk posts and home points all lie on dry floor and inside/outside walls as intended.

---

### Task 1 (PR 1): CityLayout + Great Hall
**Files:** Create `Scripts/World/CityLayout.cs`, `Editor/LakeTreefolkCity.cs`; Modify `Scripts/World/ZoneLayout.cs` (`CaveRadii → (29, 22)`, `CaveHeight → 18`, `TunnelLine` start at the new hall edge), `Editor/LakeLandmarks.cs` (cave built from the new numbers; remove its own slit/door numbers in favour of `CityLayout`), `Editor/LakeRebuildAll.cs`; Test `Tests/EditMode/CityLayoutTests.cs`
**Produces:** `static class CityLayout { Vector2 HallCenter; Vector2[] Pillars (8); Vector2 DaisCenter; Vector2 ThroneAt; Vector2 FurnaceAt; Vector2 DoorAt; Vector2 SlitAt; static bool InsideHall(Vector2 p, float margin) }`
- [ ] Tests: 8 pillars inside the hall, ≥ 3 m apart and ≥ 3 m from the dais edge; dais, throne and furnace inside; door and slit on the hall wall; hall and tunnel line on dry land (`DepthAt == 0`); tunnel line starts outside the new hall.
- [ ] Implement layout; build hall (shell, door, slit blocks, outer roots), 8 pillars (`RootTube` Ø 1.4 m, collider), dais (3 stacked discs) + throne, furnace (amber glowing blob with `Spot` in the material name), amber lamps on pillars. Commit.

### Task 2 (PR 2): Cliff-block homes
**Consumes:** `CityLayout.HomeBlocks` → add `Vector2[] HomeBlocks (2)` and `Rect[] NicheFaces`.
- [ ] Tests: each block has 16–20 niches on 3 floors, each niche on dry land, ≥ 2 m apart, stair points connect every floor.
- [ ] Build two great root/earth masses (lumpy domes via `CaveMeshes.Shell`, with collider), carve round-arch niches (`RemoveTriangles` + dark inset mesh + warm lamp on half), root stairs (`RootTube` along a slope) and balconies (flat slabs with collider). Commit.

### Task 3 (PR 3): Workshops and statues-in-progress
- [ ] Tests: 6 workshop niches on the west face; each has a work-pile position and an unfinished-statue position.
- [ ] Build west cliff mass with 6 wide niches, piles of petrified wood and amber (`Blob` clusters), 6 half-carved statues (stacked Lathe/Blob shapes with a chisel scaffold of thin roots), anvil-like stumps. Commit.

### Task 4 (PR 4): Road, gate, statues, gardens
- [ ] Tests: road polyline ~45 m, 6 m wide, on dry land; 12 statue positions alternate on both sides every ~7 m ≥ 4 m from the road centre; gate legs 10 m apart (inside width 4.5 m).
- [ ] Build flagstone road (draped quads, `LakeGround.Drape`), 12 statues (people holding lamps: tapered column + head + raised arm + amber lamp), carved standing stones at junctions, the gate (two giant `RootTube`s arching 10 m high, colliders on the legs), moss/mushroom beds and two stone-rimmed shallow basins. Commit.

### Task 5 (PR 5): The people
**Files:** Modify `Editor/LakeTreefolk.cs`
- [ ] Tests: `CityLayout.GuardPosts` has 4 entries (2 gate, 1 hall door, 1 slit); `HomePoints` ≥ 20, each > 1 m from walls (checked against layout rects); atonement stone position ≥ 25 m from every guard post.
- [ ] Place 28 treefolk (4 guards, 24 citizens by group), home points from the layout (halls, workshops, road, gardens), hall point at the dais, door/slit gates unchanged, atonement stone outside the gate. Update `docs/PROJECT_STATUS.md`. Commit and open PR.
