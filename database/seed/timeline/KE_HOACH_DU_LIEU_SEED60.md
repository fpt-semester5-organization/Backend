# Kế hoạch dữ liệu seed60 theo từng bảng (bản nháp để duyệt trước khi triển khai)

Cập nhật: 04/11/2026. Trạng thái: generator đã chạy được (14.019 dòng) nhưng CHƯA nạp thử trên SQL Server. Mọi ID mới dùng dải 10001+, email `@seed60.veggielife.vn`, nạp lại nhiều lần an toàn (xóa theo dải ID).

Ký hiệu nguồn: **[THẬT]** lấy từ sách/ảnh/video thật; **[SINH]** generator tự sinh theo luật nghiệp vụ; **[CHƯA NỐI]** đã có tài nguyên nhưng chưa gắn vào bảng.

## A. Nhóm danh mục & món ăn (dữ liệu thật)
| Bảng | Số dòng | Nguồn | Nội dung / quy tắc | Việc còn lại |
|---|---|---|---|---|
| Foods | 69 | [THẬT] sách "Tuyệt kỹ 500 công thức" (ảnh OK, không trùng, không mặn) | Mỗi dòng là 1 món (tên sách, gắn 1 danh mục). Dùng lại món cùng tên đã có trong DB, không tạo trùng. Không có cột ảnh. | Duyệt danh sách 69 tên |
| Recipes | 184 | [THẬT]+biến tấu | 1–3 công thức/món: bản gốc + bản giảm cân / giữ cân / tăng cân (300–420 / 450–620 / 680–900 kcal mỗi khẩu phần) + bản không cay (11) + **30 bản không hành tỏi / kiêng ngũ vị tân** (từ 30 trong 36 món có hành tỏi; đã bỏ các câu nhắc hành tỏi trong bước nấu). Ảnh = ảnh món trong sách (Cloudinary). Bước nấu = văn sách + ghi chú biến tấu. Trường phái: VEGAN/BUDDHIST, OVO, LACTO, LACTO_OVO theo nguyên liệu. Nguồn: SYSTEM 100, USER ~38, AI 15. Trạng thái: APPROVED 121, PENDING 15, DRAFT 10, REJECTED 3, ARCHIVED 1, QUARANTINED 3. | Duyệt luật biến tấu |
| RecipeIngredients | 1.775 | [THẬT]+[SINH] | Nguyên liệu tách từ sách; gram ước lượng từ chữ trong sách (món không ghi lượng dùng mức mặc định); biến tấu nhân hệ số. | Soát lượng gram vài món |
| Ingredients | 119 master + 10 bổ sung | Master có sẵn; bổ sung [THẬT] | Thêm: chanh, me chua, húng quế, rau thơm, dứa, xoài chín, ớt chuông, giá đỗ, giấm gạo, bột bắp. Ảnh: 31 nguyên liệu có ảnh tin cậy (Pexels→Cloudinary); ảnh độ tin cậy thấp bị bỏ. | Thiếu ảnh ~35 nguyên liệu (gia vị/nước chấm/đồ khô), củ hũ dừa, củ năng |

## B. Người dùng & sức khỏe [SINH]
| Bảng | Số dòng | Nội dung / quy tắc | Việc còn lại |
|---|---|---|---|
| Users | 150 | 1 ADMIN + MEMBER/VIP/đã khóa/đã xóa; mật khẩu băm; thời điểm tạo trải đều 60 ngày. **Avatar_Url đang NULL** | [CHƯA NỐI] gắn 150 trong 200 avatar đã upload (ưu tiên đúng giới tính theo tên) |
| HealthProfiles | 120 | Trường phái, giới tính, chiều cao/cân nặng, BMI/BMR/TDEE, mục tiêu | — |
| UserWeightHistory | 906 | Chuỗi cân nặng theo thời gian, hướng về cân mục tiêu | — |
| UserAllergies | 105 | 1–2 dị ứng/người (khoảng 55% người có hồ sơ) | — |
| Surveys | 60 | Khảo sát đầu vào | — |
| PaymentTransactions | 88 | Gói VIP ↔ giao dịch SUCCESS khớp thời hạn; có giao dịch lỗi/hết hạn | — |

## C. Cộng đồng
| Bảng | Số dòng | Nội dung / quy tắc | Việc còn lại |
|---|---|---|---|
| Posts | 175 | 12 bài video + bài thường; chia trạng thái duyệt; một phần gắn công thức. Ảnh bài viết là **URL giữ chỗ chưa có file thật** | [CHƯA NỐI] ảnh bài viết (dùng lại ảnh món sách) |
| Comments | 443 | Bình luận có/không trả lời, một số bị ẩn | — |
| Votes | 1.200 | Like/dislike trên bài/công thức; không trùng người-đối tượng | — |
| ViolationReports | 215 | Báo cáo vi phạm; số APPROVE khớp `Count_ViolationReports`; bậc xử phạt tăng dần | — |
| Videos | 6 | [THẬT] chỉ giữ 6 video thật đã upload (vid_01…vid_06), trạng thái READY, mỗi video gắn 1 bài viết và 1 công thức gần nghĩa nhất. Không còn video giả (PROCESSING/FAILED/BANNED); phần còn lại bạn tự xử lý thêm. | Soát lại tiêu đề 6 video |
| VideoTranscripts | 6 | Transcript + JSON công thức sinh từ bước nấu của công thức gắn kèm (chưa phải lời thoại thật trong video) | Thay bằng transcript thật nếu có phụ đề |

## D. Thực đơn & mua sắm [SINH]
| Bảng | Số dòng | Nội dung / quy tắc |
|---|---|---|
| MealPlans | 70 | ACTIVE/ARCHIVED/DRAFT/CANCELLED/TEMPLATE; mỗi người tối đa 1 kế hoạch ACTIVE |
| MealSlots | 1.552 | Bữa sáng/trưa/tối/phụ; chọn công thức gần mức calo mục tiêu; lọc theo trường phái (VEGAN/BUDDHIST không nhận món trứng/sữa; không hành tỏi cho người kiêng) |
| MealSlotSwapHistories | 146 | Lịch sử đổi món |
| ShoppingLists / ShoppingListItems | 18 / 252 | Danh sách đi chợ gọn: 12 kế hoạch ACTIVE + 5 ARCHIVED + 1 CANCELLED; mỗi danh sách tối đa 14 nguyên liệu chính, bỏ gia vị/đồ pantry |

## E. Tủ lạnh, quét sản phẩm [SINH]
| Bảng | Số dòng | Nội dung / quy tắc | Việc còn lại |
|---|---|---|---|
| FridgeSessions / FridgeInventory | 90 / 509 | Phiên quét tủ lạnh; tươi/không tươi. Ảnh tủ lạnh là URL giữ chỗ | [CHƯA NỐI] ảnh tủ lạnh (cần nguồn ảnh) |
| Products | 40 | Sản phẩm đóng gói, kết luận VEGAN/DOUBTFUL/NON_VEGAN. Ảnh là URL giữ chỗ | [CHƯA NỐI] ảnh sản phẩm |
| ProductAllergens / Product_ENumbers | 42 / 52 | Dị ứng, mã E của sản phẩm | — |
| UserScanHistory | 750 | Lịch sử quét mã, 80% camera | — |

## F. Quán ăn
| Bảng | Số dòng | Nội dung | Việc còn lại |
|---|---|---|---|
| VeganVenues | 26 | Quán chay (nguồn GOONG, tọa độ, đánh giá). **Thumbnail_Url chưa nối** | [CHƯA NỐI] 100 ảnh quán đã upload (chưa soát từng ảnh có món mặn không) |
| VenueCategories | 49 | Quán ↔ danh mục | — |

## G. AI & hệ thống [SINH]
| Bảng | Số dòng | Nội dung |
|---|---|---|
| ChatSessions / ChatMessages | 240 / ~2.200 | Hội thoại AI, câu trả lời gợi ý món từ các công thức thật |
| AIQuotaLogs | 1.448 | Hạn mức AI theo ngày; VIP nhiều hơn thường |
| AuditLogs | 935 | Hành động quản trị (duyệt công thức, xử lý báo cáo…) |

## Tổng hợp việc cần làm trước khi chạy thật
1. Bạn duyệt: danh sách 69 món, luật biến tấu, và việc có giữ trứng/sữa không (đã chốt giữ).
2. Nối tài nguyên đã có: avatar (Users), ảnh quán (VeganVenues), ảnh bài viết (Posts), 6 video + transcript.
3. Quyết định ảnh còn thiếu: nguyên liệu, sản phẩm, tủ lạnh.
4. Chạy `verify_timeline.sql` (55 kiểm tra) trên SQL Server thật rồi sửa lỗi; file này chưa từng chạy.
5. Soát lại cột Image_Url nào đang là URL giữ chỗ để tránh ảnh vỡ.
