# Slime gel 3D Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans (native, one session).

**Goal:** Thay quả cầu slime và cơ quan primitive bằng slime gel đa giác trong mờ có lõi hổ phách, 3 nhóm cơ quan gel liền thân.
**Architecture:** Mesh/lõi sinh bằng C# thuần ở runtime (`Scripts/Visuals`), prefab cơ quan và material dựng bằng builder Editor (`Editor/GelVisuals.cs`), gọi từ `PrototypeSceneBuilder`.
**Tech Stack:** Unity 6.6, URP Lit trong mờ, EditMode tests.
**Spec:** `docs/superpowers/specs/2026-11-17-gel-slime-art-design.md`

## Global Constraints
- Không shader HLSL tùy chỉnh; không `GetInstanceID()`; không `FindObjectsByType` có tham số sort.
- Giữ ổ gắn `OrganSocket` và `OrganSO.VisualPrefab` (ghi đè prefab cùng đường dẫn để giữ GUID).
- Vật liệu lưu thành asset (không tạo Material tạm trong scene đã lưu); lõi/gân dựng lúc chạy.

## Review Focus
1. Prefab/Material đã tồn tại: dựng lại phải ghi đè, không nhân đôi → builder idempotent.
2. Remote avatar không có asset: dùng `SlimeMeshBuilder.Build()` lúc chạy.
3. Slime dẹt hơn: ổ gắn mắt/chân/lưng phải nằm trong/ngay trên thân (đặt vị trí theo hình dẹt).

### Task 1: Mesh thân + lõi (runtime)
- Create `Scripts/Visuals/SlimeMeshBuilder.cs`: `public static Mesh Build(int seed = 7)`, `public static Mesh Cone(int sides, float radius, float height)`.
- Create `Scripts/Visuals/SlimeGelCore.cs`: MonoBehaviour dựng lõi + gân lúc `Awake`, đập theo máu.
- Test `Tests/EditMode/SlimeMeshBuilderTests.cs`: 80 tam giác, đáy y = -0.45, đỉnh trong [0.2,0.35], flat shading (đỉnh không dùng chung), `Cone` có đỉnh cao đúng.
### Task 2: Builder Editor
- Create `Editor/GelVisuals.cs`: `GelMaterial`, `StyleBody(GameObject visual)`, `RebuildOrganPrefabs()`, menu **Evolve → Rebuild Slime Visuals**.
- Modify `PrototypeSceneBuilder.CreatePlayer` (gọi `StyleBody`) và `CreateSharedAssets` (gọi `RebuildOrganPrefabs`).
### Task 3: Nảy + co-op
- Modify `SlimeController.AnimateVisual` (bè ra khi đáp, căng khi trên không).
- Modify `RemoteAvatar.Build` dùng mesh gel + `SlimeGelCore`.
### Task 4: Tài liệu + PR
- Cập nhật ROADMAP, PROJECT_STATUS; PR; chủ dự án chạy test và menu, chụp màn hình.
