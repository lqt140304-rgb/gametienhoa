# Blender props kit (headless `bpy`)

Dựng vật thể rắn cho thế giới ngầm bằng Blender chạy dạng thư viện Python, xuất FBX (mét, Y lên) kèm mesh va chạm `<tên>_COL`, và ảnh xem trước.

```bash
python3 -m venv .venv-blender && .venv-blender/bin/pip install bpy   # bpy 5.3, một lần
.venv-blender/bin/python -I tools/blender/test_rocks.py            # kiểm tra (thoát khác 0 nếu hỏng)
.venv-blender/bin/python -I tools/blender/build_rocks.py \
  --out Assets/_Project/Models/Underroot --previews docs/art/previews
```

- `kit_common.py`: cảnh, xuất FBX, render xem trước, tạo mesh va chạm, vật liệu màu đỉnh.
- `build_rocks.py`: 6 tảng đá (0,3 / 0,6 / 1 / 1,6 / 2,4 / 3,2 m), 1.280 mặt, va chạm 80 mặt, rêu phủ mặt hướng lên.
- Lưu ý: phải `import bpy` trước `bmesh` và `mathutils`.
- Ảnh xem trước (3D Cycles CPU, 800×600) ở `docs/art/previews/`; hình trong Unity do chủ dự án chụp.
