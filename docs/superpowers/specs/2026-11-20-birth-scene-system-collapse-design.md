# Cảnh Ra Đời bản 3: đường truyền sụp đổ

**Ngày:** 2026-11-20
**Trạng thái:** đã thảo luận với chủ dự án; chờ duyệt bản viết này.
**Thay thế:** phần trình bày và cách chơi của `2026-11-19-birth-scene-design.md` (mục 5 và 8). Giữ nguyên luật rút số mệnh (`FateRoll`), 12 Lõi và Khuyết Điểm, hai Lõi làm lại, co-op, 37 file giọng Chi.

## 1. Ý định và lý do

Người chơi là một người **bị bắt vào thế giới này làm slime** qua một **đường truyền không ổn định**: hỗn loạn, sụp đổ, khẩn trương, rồi dần ổn định. Chủ dự án xem cảnh hiện có (vẽ bằng khung chữ nhật bo góc, một tông xanh, không âm thanh ngoài giọng đọc) và thấy **tẻ nhạt, thiếu màu mè**. **Hình ảnh là ưu tiên cao**, sau đó là âm thanh.

## 2. Mục tiêu, không phải mục tiêu, điều tránh

**Mục tiêu**
- Cảm giác **mất kết nối**: màn hình nhiễu, lệch màu, xé sọc, méo, rung; mức hỗn loạn giảm dần đến lúc kết nối ổn định.
- **Khung mới**: không vuông, không tĩnh; đoạn viền rời nhau, lệch, nhấp nháy, vỡ rồi liền lại theo độ ổn định.
- **Số mệnh tự động**: không còn người chơi chọn bài. Bài bay vào hỗn loạn, tự lật Lõi rồi Khuyết Điểm.
- **Sáu Ân Điển, mỗi cái một cảnh riêng**, đơn giản (một hình và một chuyển động).
- **Đóng dấu tên**: sau khi mọi thứ định xong, tên Lõi, Khuyết Điểm và sáu Ân Điển đập vào màn hình thật to, mỗi cái một góc nghiêng, kèm rung màn hình và tiếng va.
- **Âm thanh**: tiếng chữ chạy, âm nền, báo động, lật bài, nổ sáng khi nhận Lõi, tiếng riêng cho từng Ân Điển, tiếng va khi đóng dấu.

**Không phải mục tiêu** (để sau)
- Video Veo làm nền (chỉ chừa **chỗ cắm**, mục 5.9). Dị Tượng. Hiệu ứng thật cho Phân Thân và các Lõi còn "chưa có hiệu ứng".
- Chọn bài bằng chuột (bỏ hẳn).

**Điều tránh:** màn hình đen hay tím nếu shader lỗi; **người chơi bị kẹt** trong cảnh; lệch hình giữa các máy co-op (kết quả luôn do hạt giống); cảnh quá dài làm phát ngán; nhiễu mạnh đến mức không đọc được chữ quan trọng.

## 3. Ràng buộc

- Unity 6.6, **URP 17**, `com.unity.ugui 2.6` (có; **không có TextMeshPro**: dùng `Text` cũ với `LegacyRuntime.ttf`, font đang dùng cho IMGUI đã hiện được tiếng Việt). Dự án đã dùng hậu kỳ (Volume, bloom, chỉnh màu) nên URP hậu kỳ bật được cho camera.
- **Không có Unity trong môi trường làm việc của người viết kế hoạch:** luật thuần phải chạy được bằng `tools/cstest/run.sh`; phần Unity chỉ kiểm được khi chủ dự án mở. Vì thế **dùng các hiệu ứng hậu kỳ có sẵn của URP** (không viết shader toàn màn hình riêng) để tránh màn hình tím.
- Volume toàn cục `BrightnessSetting` (độ sáng, ưu tiên 100) và hệ nhìn đơn sắc của slime nằm ở layer Default: **cảnh dùng layer riêng** cho Volume của nó để không bị chúng làm hỏng màu.
- Không có file hiệu ứng âm thanh nào trong dự án. **Khóa ElevenLabs hiện không có quyền `sound_generation`**, nên âm thanh được **tổng hợp bằng code**; chừa chỗ để thả file thật vào sau.
- Giọng Chi (37 file) có sẵn ở `Resources/Voice/Echo/<id>.mp3`, tạo bằng `tools/voice/generate_echo_voice.sh` từ `BirthScript` (một nguồn). Lời mới cần giọng mới: chạy lại script.
- Mọi chữ trong game bằng tiếng Việt.

## 4. Các hướng dựng đã cân nhắc

1. **A. Vẽ như hiện tại (IMGUI), làm kỹ hơn.** Ít rủi ro nhưng không có méo hình thật, độ sáng tỏa, hạt thật: trần chất lượng thấp, không đủ cho "sụp đổ hỗn loạn".
2. **B. Dựng thành một lớp hình thật** *(chủ dự án chọn)*: sân khấu riêng với camera riêng, hậu kỳ URP thật (lệch màu, méo thấu kính, hạt nhiễu, tỏa sáng, vignette), hệ hạt thật, giao diện UGUI. Trần cao nhất; công lớn nhất; rủi ro cao ở lần chạy đầu vì chưa thấy được hình.
3. **C. Video Veo làm nền** cho sáu Ân Điển và phần mở đầu: hình đẹp, ít code, nhưng nặng file, phải khớp thời gian, mỗi clip phải tạo lại nếu chưa ưng. **Giữ làm bước sau** qua chỗ cắm.

## 5. Thiết kế

### 5.1 Sân khấu riêng (`BirthStage`)
- Một đối tượng gốc đặt rất xa thế giới (ví dụ y = 20000), chứa: **camera riêng**, **Volume hậu kỳ riêng**, một **Canvas UGUI World Space** và các hệ hạt. Camera: `depth` cao hơn mọi camera, xóa nền bằng màu đặc, `far` nhỏ (khoảng 300), nên **không thấy thế giới** và camera chính cũng không thấy sân khấu. Slime, hang, HUD biến mất khỏi mắt người chơi mà không cần che bằng một tấm đen.
- URP: camera riêng bật `renderPostProcessing`; `Volume` toàn cục của cảnh đặt ở **layer riêng** và camera cảnh chỉ nhận layer đó (`volumeLayerMask`), nên độ sáng và hệ nhìn đơn sắc không ảnh hưởng. Khi cảnh xong: **tắt và hủy** camera, Volume, đối tượng gốc (không để lại gì).
- Vật liệu: chỉ dùng shader có sẵn của URP (Unlit, Particles/Unlit, UI Default). Texture tạo bằng code (đĩa mờ, hạt, nhiễu).
- **Không bao giờ kẹt:** nếu dựng sân khấu ném lỗi, ghi cảnh báo, **bỏ cảnh và giao số mệnh ngay** (đã rút bằng hạt giống), trả lại các cờ đóng băng và con trỏ.
- Đóng băng slime, bất tử, ẩn HUD: như bản 2 (`SurvivalStats.Frozen`, `Invulnerable`, `SurvivalHUD.ExternalMenuOpen`); trả lại giá trị cũ khi xong.

### 5.2 Độ ổn định đường truyền (`Instability`, thuần)
- Một hàm 0..1 của thời gian cảnh cộng các **cú xung** từ sự kiện (một cú đập tên, một cảnh báo, một lần Ân Điển hiện). 0 = ổn định hoàn toàn, 1 = hỗn loạn tối đa.
- Đường nền: bắt đầu khoảng **0,9**, giảm từng bậc qua các đoạn, **về 0 ở đoạn cuối**. Mỗi xung cộng thêm rồi tắt dần trong vài trăm mili giây.
- Điều khiển (mỗi hiệu ứng có hệ số riêng, kẹp trong khoảng an toàn để chữ vẫn đọc được): lệch màu, méo thấu kính, hạt nhiễu, vignette, **rung camera** (`ShakeMath`, thuần), độ vỡ và nhấp nháy của khung, tần suất và độ dày của dải xé ngang, độ giật của chữ.

### 5.3 Khung mới ("khung vỡ")
- Khung không còn là một khối chữ nhật bo góc. Mỗi khung gồm **các đoạn viền riêng lẻ** (cạnh, góc cắt vát, bracket), thân kính mờ có **đường quét và nhiễu**. Mỗi đoạn có dịch chuyển, nhấp nháy, và thỉnh thoảng **tách rời** theo độ ổn định; khi độ ổn định giảm các đoạn liền lại, viền sáng đều.
- Cửa sổ có nhiều cỡ và nghiêng nhẹ khác nhau khi hỗn loạn (tối đa vài độ), về thẳng khi ổn định.
- Chữ gõ từng ký tự, kèm tiếng gõ; khi nhiễu mạnh, một số ký tự bị thay bằng ký tự lạ rồi sửa lại.

### 5.4 Nhịp cảnh (khoảng 100–120 giây; bấm Space/Enter sang đoạn kế sau 1,5 giây, Tab nhảy tới chia bài)
| # | Đoạn | Nội dung | Ổn định |
|---|---|---|---|
| 1 | Mất kết nối | Nhiễu dữ dội, cảnh báo đỏ, "đường truyền không ổn định…", thế giới cũ vỡ thành mảnh | 0,9–1 |
| 2 | Kéo vào | Một dòng dữ liệu bị kéo xuống, đếm ngược ngắn | 0,9 |
| 3 | Hình thành | Dữ liệu hỏng tụ thành một khối slime, "Chủng tộc: Slime… chưa có mắt, chưa có chân" | 0,75 |
| 4–9 | **Sáu Ân Điển** | Mỗi cái một cảnh riêng (5.5), ~7 giây, mỗi lần hiện có một xung nhiễu | 0,7 → 0,5 |
| 10 | Số mệnh | Vòng phù văn lớn, hai bóng bài | 0,5 |
| 11 | Chia bài Lõi | Bài bay vào lộn xộn, xáo, **tự lật** Lõi (đã định sẵn), nổ sáng theo độ hiếm | 0,6 |
| 12 | Chia bài Khuyết Điểm | Ba bài đỏ sẫm, **tự lật**, báo động đỏ | 0,6 |
| 13 | **Đóng dấu tên** | 5.6 | xung mạnh mỗi cú |
| 14 | Ổn định | Nhiễu tan, màn hình lặng, "Kết nối ổn định. Hãy sống sót." | → 0 |

Mỗi đoạn có thời gian tối thiểu để đọc kịp và **chờ giọng đọc xong** (cộng 0,35 giây) mới sang đoạn sau, như bản 2 (`BirthFlow`). Lõi nặng có **hai** Khuyết Điểm: ở đoạn 12 và 13 chúng lật và đập **lần lượt**, mỗi cái một cú.

### 5.5 Sáu cảnh Ân Điển (mỗi cái riêng, đơn giản: một hình vẽ bằng hình cơ bản + một chuyển động + một tiếng + lời đọc có sẵn)
1. **Túi Không Đáy:** một túi tối hình giọt mở ra, các hạt sáng chảy vào, túi phồng rồi thu lại.
2. **Nuốt Chửng:** khối slime phồng ra bao một viên cầu nhỏ rồi ép lại.
3. **Giải Mã:** các cột ký hiệu chạy xuống, dần xếp thành một chuỗi xoắn sáng.
4. **Biến Hình:** hình khối đổi liên tục giữa tròn, nhọn, dài rồi về tròn.
5. **Sóng Dò:** các vòng sóng lan ra, chạm vài chấm (vật) và sáng lên.
6. **Phân Thân:** một khối nhỏ tách khỏi khối lớn, trôi một chút, rồi nhập lại. Có dòng **"(sắp có)"** vì chưa có hiệu ứng thật.

### 5.6 Cảnh đóng dấu tên
- Thứ tự: **Lõi**, **Khuyết Điểm**, rồi **sáu Ân Điển** liên tiếp nhanh.
- Mỗi tên là một khối chữ **rất to**, chiếm phần lớn bề ngang màn hình, **nghiêng khác nhau**: Lõi nghiêng trái (khoảng −7°), Khuyết Điểm nghiêng phải (khoảng +6°), sáu Ân Điển nhỏ hơn, xen kẽ trái phải, góc từ ±4° đến ±9°.
- Mỗi cú: chữ **bay từ rất lớn (3×) vào, đập, nảy nhẹ rồi nằm yên**; tại lúc chạm: **rung màn hình** (mạnh nhất ở Lõi và Khuyết Điểm, nhỏ dần với các Ân Điển), chớp trắng ngắn, một xung độ ổn định, mảnh vỡ và tia sáng, **tiếng va** trầm, và **giọng đọc tên** (clip `core.<id>`, `flaw.<id>`, `gift.*` có sẵn).
- Màu: Lõi theo độ hiếm (trắng, xanh, vàng; **Cực hiếm** có chớp vàng toàn màn hình và rung gấp đôi), Khuyết Điểm đỏ, Ân Điển xanh lam.
- Các tên **ở lại trên màn hình** xếp chồng nhau lệch góc cho tới hết đoạn, rồi cùng tan khi chuyển sang đoạn ổn định.

### 5.7 Âm thanh (`SfxSynth` thuần, `BirthAudio`)
- Âm tổng hợp bằng code từ dạng sóng (sin, nhiễu, lọc, bao hình), tạo thành `AudioClip` lúc bắt đầu cảnh. Danh sách: tiếng gõ chữ, nhiễu (xé), chuông báo động, ù nền (lặp, có nhịp chậm), whoosh, bài bay và lật, nổ sáng khi nhận Lõi (theo độ hiếm), thud và báo động khi nhận Khuyết Điểm, tiếng va đóng dấu, chime khi ổn định, và **sáu tiếng riêng** cho sáu Ân Điển (nuốt, giải mã, biến hình, sóng, tách...).
- Nếu có file cùng tên trong `Resources/Sfx/Birth/<tên>` thì **dùng file thay cho âm tổng hợp** (để sau này thả file thật vào, ví dụ làm bằng ElevenLabs khi có quyền).
- Trộn: giọng đọc luôn nổi lên; âm nền hạ xuống khi có giọng; âm lượng tổng chỉnh được bằng một hằng số.

### 5.8 Lời mới
Thêm vào `BirthScript` (nguồn duy nhất, sinh giọng bằng `tools/voice/generate_echo_voice.sh`): `echo.warn` («Cảnh báo. Đường truyền không ổn định.»), `echo.pull` («Đang kéo linh hồn vào thế giới mới.»), `echo.stable` («Kết nối ổn định. Hãy sống sót.»). Các câu cũ giữ nguyên trừ những câu nói về việc chọn bài (`echo.core`, `echo.flaw`) được viết lại cho cảnh tự động.

### 5.9 Chỗ cắm video (sau này)
Mỗi cảnh Ân Điển và đoạn mở đầu có thể được thay bằng một clip trong `Resources/Video/Birth/<tên>` phát bằng `VideoPlayer` lên một mặt phẳng trong sân khấu; nếu không có clip thì dùng cảnh vẽ bằng code. Không làm trong lần này, chỉ chừa tên và chỗ.

### 5.10 Co-op, kết quả
Không đổi: số mệnh rút bằng `FateRoll.Roll(seed, pool)` **trước khi** cảnh chạy; cảnh chỉ trình diễn kết quả đó (mỗi người tự rút, client báo bằng `evolve.tr.pick`). Bỏ hẳn bộ bài 8 lá và việc chọn: `FateRoll.DealDeck`, `ShuffleOptions`, các bước `PickCore/PickFlaw` bị gỡ.

## 6. Chia chặng (rủi ro cao nên chia nhỏ)
- **Chặng 1 (nền):** sân khấu và hậu kỳ, độ ổn định và rung, khung vỡ, chữ gõ, nhịp cảnh mới, bài tự động, cảnh đóng dấu, âm tổng hợp và lời mới. Có thể chơi trọn cảnh (các cảnh Ân Điển tạm dùng một tấm giữ chỗ).
- **Chặng 2:** sáu cảnh Ân Điển riêng.
- Sau mỗi chặng chủ dự án mở Unity xem và báo lỗi (ảnh chụp), rồi chỉnh.

## 7. Quyết định do người viết tự đặt (chủ dự án xem lại)
- Âm thanh tổng hợp bằng code (không file), có chỗ thả file thay.
- Dùng hậu kỳ có sẵn của URP, không viết shader toàn màn hình riêng.
- Sân khấu đặt xa bằng khoảng cách thay vì dùng layer để che.
- Thời lượng khoảng 100–120 giây; cảnh Ân Điển ~7 giây mỗi cái.
- Các góc nghiêng và thứ tự đóng dấu ở 5.6; "Cực hiếm" rung gấp đôi.
- Thêm ba câu thoại ở 5.8.
- Phân Thân có cảnh nhưng ghi "(sắp có)".

## 8. Để người làm kế hoạch quyết
- Cách chia lớp và tên file (đề xuất: thuần `Instability`, `ShakeMath`, `SfxSynth`, `SlamPlan`, `BirthFlow` bản 3; Unity `BirthStage`, `BirthGlitch`, `BirthFrames`, `BirthCards`, `BirthSlam`, `BirthGifts`, `BirthAudio`).
- Chi tiết số của hiệu ứng (hệ số lệch màu, méo, hạt; độ dài từng xung) trong khoảng an toàn ở 5.2.
- Cách vẽ hình của sáu cảnh Ân Điển từ hình cơ bản và hạt.
- Cách tạo khung vỡ bằng đối tượng UGUI (số đoạn, cách lệch).
