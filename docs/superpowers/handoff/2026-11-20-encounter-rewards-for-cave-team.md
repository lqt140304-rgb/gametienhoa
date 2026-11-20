# Cho bên làm hang: phần thưởng kỳ ngộ gọi thế nào

Gọi từ code kỳ ngộ khi điều kiện đạt (một lần mỗi vòng, do bạn kiểm). Tất cả nằm ở `Evolve.Traits.EncounterRewards` (`Scripts/Traits/EncounterRewards.cs`), chạy trên máy người nhận: truyền người chơi cục bộ `EncounterRewards.LocalPlayer`. Người chơi từ xa bị bỏ qua (mỗi máy tự nhận phần của mình).

| Hàm | Làm gì | Trả về | Dùng cho kỳ ngộ |
|---|---|---|---|
| `GrantGene(player, int minRarity)` (0 Thường, 1 Hiếm, 2 Quý, 3 Huyền thoại) | Dạy một gene chưa biết, độ hiếm tối thiểu. | Tên gene hoặc `null` (hết gene thì cho khối) | Hợp Âm Tinh Thể (`GeneRarity.Rare`), Cầu Vồng Trong Thân |
| `GrantUniqueGene(player, "gene_la")` | Dạy gene riêng của kỳ ngộ (tài sản trong `Resources/EncounterGenes`, tạo bằng `Evolve → Create Encounter Genes`). | Tên hoặc `null` | Quả Trứng Còn Sống (`gene_la`), Bữa Tiệc Cổ (`gene_loai_co`) |
| `AddMass(player, kg)` | Cộng khối. | `bool` | Bữa Tiệc Cổ |
| `RerollFlaw(player)` | Rút lại một Khuyết Điểm (không ra lại đúng cái cũ, có thể tệ hơn; co-op tự báo máy chủ). | `bool` | Vòng Nấm |
| `AddHeatResistance(player, độ)` | Kháng nhiệt cộng thêm suốt vòng (tối đa +20 °C). | `bool` | Tôi Luyện |
| `RefillStats(player)` / `CureDiseases(player)` | Hồi đầy / hết bệnh. | `bool` | Hơi Thở Của Rêu, Tia Nắng Đầu Tiên (no nhẹ: dùng `Feed` có sẵn) |
| `BoostCore(player)` | Lõi mạnh lên một chút (+15%, tối đa 3 lần mỗi vòng). | `bool` | Ký Ức Đá |
| `UnlockColor(player, Color)` (màu truyền vào bị bỏ qua: mở khóa kiểu **Cầu Vồng**; `UnlockFinish(player, id)` nếu cần id khác) | Mở khóa màu **Cầu Vồng** cho slime (lưu lâu dài, bật ở menu F3). | `bool` (mới) | Cầu Vồng Trong Thân |
| `GiveStomachItems(player, string[] tên món)` (`GiveItems` nếu cần khối và dinh dưỡng riêng) | Bỏ đồ vào Túi. | số món vừa | Túi Của Kẻ Đi Trước |
| `Say("câu tiếng Việt")` | Tiếng Vọng nói một câu. | | mọi kỳ ngộ |

Lưu ý:
- `GrantGene` lấy gene từ các tài sản organ nằm trong thư mục `Resources` (kỹ năng hang ở `Resources/CaveSkills`); gene để ngoài `Resources` không được chọn.
- Màu Cầu Vồng: slime đổi sắc liên tục; người chơi khác trong co-op chỉ thấy một màu đứng yên (màu lúc gửi).
- Hai gene riêng là **bản tạm**, chủ dự án chưa duyệt nội dung.

Cầu nối `Evolve.Maze.EncounterRewardBridge` của bên làm hang gọi các hàm này qua reflection **chỉ theo tên và số tham số**, nên mỗi hàm trên chỉ có đúng một bản với đúng kiểu tham số. Có test giữ điều này (`Tests/EditMode/EncounterRewardBridgeCompatTests.cs`): đừng thêm hàm trùng tên và trùng số tham số.
