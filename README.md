# PROJECT EVOLVE

Game sinh tồn tiến hóa 3D Low Poly, co-op 4–8 người. Người chơi bắt đầu là một slime nguyên sinh và tiến hóa mở bằng cách thu thập cơ quan.

- Thiết kế chi tiết: [`docs/GDD.md`](docs/GDD.md)
- Dựng scene prototype: [`docs/PROTOTYPE_01_SETUP.md`](docs/PROTOTYPE_01_SETUP.md)
- Lộ trình: [`docs/ROADMAP.md`](docs/ROADMAP.md)
- **Cơ chế và kỹ năng của slime (bản mới nhất):** [`docs/SLIME_MECHANICS.md`](docs/SLIME_MECHANICS.md)
- Trạng thái từng mục và danh sách thử tay: [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md), [`docs/TEST_CHECKLIST_UNITY.md`](docs/TEST_CHECKLIST_UNITY.md)
- **Trạng thái:** đang thiết kế, chưa có project Unity.

## Công nghệ

Unity 6 + URP (ghi đúng phiên bản Editor vào đây khi tạo project), C#, Blender, Shader Graph, Netcode for GameObjects.

## Thiết lập repo

1. Cài [Git LFS](https://git-lfs.com) rồi chạy `git lfs install`. File `.blend`, `.fbx`, ảnh và âm thanh được lưu qua LFS (xem `.gitattributes`).
2. Trong Unity, vào **Project Settings → Editor**, đặt *Asset Serialization = Force Text* và *Version Control = Visible Meta Files*.
3. `.gitignore` đã loại trừ `Library/`, `Temp/`, `Logs/`, `UserSettings/`… Không commit các thư mục này.

## Cấu trúc thư mục dự kiến

```
Assets/_Project/
  Scripts/            # C#: Core, Survival, Abilities, Organs, Networking
  Art/                # Models, Materials, Shaders
  Prefabs/
  ScriptableObjects/  # Dữ liệu Dị Năng, cơ quan
  Scenes/             # Prototype_01
```

## Quy ước

- Làm trên branch `feature/<tên>`, merge vào `main` qua pull request.
- Class C# đặt tên PascalCase, mỗi file một class; ScriptableObject có hậu tố `SO`, ví dụ `AbilitySO`.
