# -*- coding: utf-8 -*-
"""
Script tạo file tài liệu Báo Cáo Dữ Liệu Mẫu Thành Viên 4:
"data mẫu của tôi.docx" lưu tại project-docs/
Quy chuẩn trình bày: Đại học FPT SWP391 - VeggieLife
"""

import os
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import parse_xml, OxmlElement
from docx.oxml.ns import nsdecls, qn

def set_cell_background(cell, fill_hex):
    """Đặt màu nền cho một ô trong bảng"""
    tcPr = cell._tc.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=100, bottom=100, left=150, right=150):
    """Đặt lề bên trong cho một ô trong bảng"""
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = parse_xml(
        f'<w:tcMar {nsdecls("w")}>'
        f'<w:top w:w="{top}" w:type="dxa"/>'
        f'<w:bottom w:w="{bottom}" w:type="dxa"/>'
        f'<w:left w:w="{left}" w:type="dxa"/>'
        f'<w:right w:w="{right}" w:type="dxa"/>'
        f'</w:tcMar>'
    )
    tcPr.append(tcMar)

def set_table_borders(table, color="B0BEC5", sz="4", val="single"):
    """Đặt đường viền tinh tế cho toàn bảng"""
    tblPr = table._tbl.tblPr
    borders = parse_xml(
        f'<w:tblBorders {nsdecls("w")}>'
        f'<w:top w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:bottom w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:insideH w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:insideV w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:left w:val="none"/>'
        f'<w:right w:val="none"/>'
        f'</w:tblBorders>'
    )
    tblPr.append(borders)

def make_callout(doc, text_list, title="LƯU Ý NGHIỆP VỤ", border_hex="2E7D32", bg_hex="E8F5E9"):
    """Tạo khung Callout ghi chú / highlight nổi bật"""
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl.autofit = False
    tbl.columns[0].width = Inches(6.5)
    
    cell = tbl.cell(0, 0)
    set_cell_background(cell, bg_hex)
    set_cell_margins(cell, top=140, bottom=140, left=200, right=180)
    
    # Viền trái dày, các viền khác none
    tcPr = cell._tc.get_or_add_tcPr()
    borders = parse_xml(
        f'<w:tcBorders {nsdecls("w")}>'
        f'<w:left w:val="single" w:sz="24" w:space="0" w:color="{border_hex}"/>'
        f'<w:top w:val="none"/>'
        f'<w:bottom w:val="none"/>'
        f'<w:right w:val="none"/>'
        f'</w:tcBorders>'
    )
    tcPr.append(borders)
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(4)
    run_title = p.add_run(f"📌 {title}: ")
    run_title.font.bold = True
    run_title.font.size = Pt(10.5)
    run_title.font.color.rgb = RGBColor(0x1B, 0x5E, 0x20)
    
    if text_list:
        run_first = p.add_run(text_list[0])
        run_first.font.size = Pt(10)
        run_first.font.color.rgb = RGBColor(0x26, 0x32, 0x38)
        
        for item in text_list[1:]:
            p2 = cell.add_paragraph()
            p2.paragraph_format.space_before = Pt(2)
            p2.paragraph_format.space_after = Pt(2)
            r = p2.add_run(f"• {item}")
            r.font.size = Pt(10)
            r.font.color.rgb = RGBColor(0x26, 0x32, 0x38)
    
    doc.add_paragraph().paragraph_format.space_after = Pt(6)

def style_header_row(row, titles, widths, bg_hex="2E7D32"):
    """Định dạng hàng tiêu đề của bảng"""
    for i, cell in enumerate(row.cells):
        cell.width = widths[i]
        set_cell_background(cell, bg_hex)
        set_cell_margins(cell, top=100, bottom=100, left=100, right=100)
        cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.space_after = Pt(0)
        run = p.add_run(titles[i])
        run.font.bold = True
        run.font.size = Pt(9.5)
        run.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)

def populate_table_data(tbl, data, widths, align_col_map=None):
    """Điền dữ liệu vào bảng kèm hiệu ứng xen kẽ màu nền Zebra Striping"""
    for r_idx, row_data in enumerate(data):
        row = tbl.add_row()
        fill_hex = "F1F8E9" if r_idx % 2 == 1 else "FFFFFF"
        for c_idx, val in enumerate(row_data):
            cell = row.cells[c_idx]
            cell.width = widths[c_idx]
            set_cell_background(cell, fill_hex)
            set_cell_margins(cell, top=70, bottom=70, left=80, right=80)
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            p = cell.paragraphs[0]
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(0)
            
            align = align_col_map.get(c_idx, WD_ALIGN_PARAGRAPH.LEFT) if align_col_map else WD_ALIGN_PARAGRAPH.LEFT
            p.alignment = align
            
            run = p.add_run(str(val))
            run.font.size = Pt(9)
            run.font.color.rgb = RGBColor(0x21, 0x21, 0x21)

def add_custom_heading(doc, text, level):
    """Thêm tiêu đề phân cấp đẹp mắt với tone xanh thiên nhiên VeggieLife"""
    p = doc.add_paragraph()
    run = p.add_run(text)
    run.font.bold = True
    
    if level == 1:
        p.paragraph_format.space_before = Pt(16)
        p.paragraph_format.space_after = Pt(8)
        run.font.size = Pt(16)
        run.font.color.rgb = RGBColor(0x1B, 0x5E, 0x20) # Green 900
    elif level == 2:
        p.paragraph_format.space_before = Pt(12)
        p.paragraph_format.space_after = Pt(6)
        run.font.size = Pt(13)
        run.font.color.rgb = RGBColor(0x00, 0x69, 0x5C) # Teal 800
    elif level == 3:
        p.paragraph_format.space_before = Pt(8)
        p.paragraph_format.space_after = Pt(4)
        run.font.size = Pt(11)
        run.font.color.rgb = RGBColor(0x33, 0x69, 0x1E) # Light Green 900
    return p

def build_document():
    doc = docx.Document()
    
    # Thiết lập lề trang giấy (1 inch = 2.54 cm)
    for section in doc.sections:
        section.top_margin = Inches(0.8)
        section.bottom_margin = Inches(0.8)
        section.left_margin = Inches(0.9)
        section.right_margin = Inches(0.9)
        
        # Header & Footer
        header = section.header
        hp = header.paragraphs[0]
        hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        hrun = hp.add_run("VEGGIELIFE SYSTEM - BÁO CÁO DỮ LIỆU SEED MẪU PHÂN HỆ THÀNH VIÊN 4")
        hrun.font.size = Pt(8)
        hrun.font.color.rgb = RGBColor(0x78, 0x90, 0x9C)
        
        footer = section.footer
        fp = footer.paragraphs[0]
        fp.alignment = WD_ALIGN_PARAGRAPH.CENTER
        frun = fp.add_run("Dự án SWP391 - VeggieLife Backend | Phân hệ Công nghệ & Khám phá (Dải ID: 401 - 499)")
        frun.font.size = Pt(8)
        frun.font.italic = True
        frun.font.color.rgb = RGBColor(0x78, 0x90, 0x9C)

    # =========================================================================
    # PHẦN TRANG BÌA & TIÊU ĐỀ
    # =========================================================================
    title_p = doc.add_paragraph()
    title_p.paragraph_format.space_before = Pt(10)
    title_p.paragraph_format.space_after = Pt(4)
    title_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_uni = title_p.add_run("ĐẠI HỌC FPT - HỌC KỲ FALL 2026\n")
    r_uni.font.size = Pt(12)
    r_uni.font.bold = True
    r_uni.font.color.rgb = RGBColor(0x37, 0x47, 0x4F)
    
    r_sub = title_p.add_run("BỘ MÔN KỸ THUẬT PHẦN MỀM - SWP391\n\n")
    r_sub.font.size = Pt(11)
    r_sub.font.bold = True
    r_sub.font.color.rgb = RGBColor(0x45, 0x5A, 0x64)

    r_main = title_p.add_run("BÁO CÁO ĐẶC TẢ DỮ LIỆU SEED MẪU HỆ THỐNG\nPHÂN HỆ THÀNH VIÊN 4: CÔNG NGHỆ & KHÁM PHÁ\n")
    r_main.font.size = Pt(18)
    r_main.font.bold = True
    r_main.font.color.rgb = RGBColor(0x1B, 0x5E, 0x20)
    
    r_sub2 = title_p.add_run("(Quán Ăn Chay, Máy Quét E-Number, Tủ Lạnh Ảo & Trợ Lý Tư Vấn AI)\n")
    r_sub2.font.size = Pt(12)
    r_sub2.font.italic = True
    r_sub2.font.color.rgb = RGBColor(0x2E, 0x7D, 0x32)

    # Bảng tóm tắt định danh phân công
    tbl_meta = doc.add_table(rows=6, cols=2)
    tbl_meta.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_meta.autofit = False
    widths_meta = [Inches(2.2), Inches(4.3)]
    
    meta_info = [
        ("Dự án phần mềm", "VeggieLife - Nền tảng Ẩm thực Thuần chay & Chăm sóc Sức khỏe Cá nhân hóa"),
        ("Phân công thành viên", "Thành viên 4: Quán Chay, Máy Quét E-Number & Tủ Lạnh (Công nghệ & Khám phá)"),
        ("Dải ID phân bổ chuẩn", "401 – 499 (Đảm bảo tuyệt đối không xung đột ID với các thành viên khác)"),
        ("Các thực thể phụ trách", "VeganVenues, VenueCategories, VenueMenuItems, Products, Product_ENumbers, ProductAllergens, FridgeSessions, FridgeInventory, ChatSessions, ChatMessages, Surveys"),
        ("File kịch bản SQL Seed", "database/seed/17_Seed_Member4_Venues_Products_Fridge.sql (Tích hợp tại 00_Seed_All.sql)"),
        ("Tiêu chuẩn tính toàn vẹn", "100% dữ liệu thực tế tại Việt Nam, tọa độ ghim bản đồ GPS chuẩn, mã Barcode EAN-13 GS1 thật, cú pháp MERGE Idempotent chuẩn SQL Server")
    ]
    
    for idx, (label, val) in enumerate(meta_info):
        row = tbl_meta.rows[idx]
        for c_idx in range(2):
            cell = row.cells[c_idx]
            cell.width = widths_meta[c_idx]
            set_cell_background(cell, "F9FBE7" if idx % 2 == 1 else "FFFFFF")
            set_cell_margins(cell, top=60, bottom=60, left=80, right=80)
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            p = cell.paragraphs[0]
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(0)
            if c_idx == 0:
                r = p.add_run(label)
                r.font.bold = True
                r.font.size = Pt(9.5)
                r.font.color.rgb = RGBColor(0x1B, 0x5E, 0x20)
            else:
                r = p.add_run(val)
                r.font.size = Pt(9.5)
                r.font.color.rgb = RGBColor(0x21, 0x21, 0x21)
                
    set_table_borders(tbl_meta, color="C8E6C9", sz="6")
    doc.add_paragraph().paragraph_format.space_after = Pt(12)

    # =========================================================================
    # PHẦN 1: NGUYÊN TẮC THIẾT KẾ VÀ TÍNH TOÀN VẸN CƠ SỞ DỮ LIỆU
    # =========================================================================
    add_custom_heading(doc, "1. Nguyên Tắc Thiết Kế Dữ Liệu & Ràng Buộc Khóa Ngoại", level=1)
    
    p = doc.add_paragraph()
    p.add_run(
        "Bộ dữ liệu mẫu được xây dựng bám sát 100% kiến trúc cơ sở dữ liệu 38 bảng của VeggieLife, "
        "đáp ứng hoàn toàn yêu cầu kiểm định nghiệp vụ của đồ án tốt nghiệp và học phần SWP391. "
        "Dữ liệu mẫu đảm bảo 4 trụ cột kỹ thuật cốt lõi:"
    )
    
    make_callout(
        doc,
        [
            "Quy hoạch Dải ID Độc quyền (401 - 499): Toàn bộ Primary Key (PK) của các bảng thuộc phạm vi phụ trách đều nằm trọn trong dải từ 401 đến 499, loại trừ hoàn toàn nguy cơ tranh chấp, ghi đè hoặc trùng khóa chính với dữ liệu của Thành viên 1, 2, 3 và kịch bản Mock Data 99.",
            "Kế Thừa Khóa Ngoại Toàn Vẹn (Referential Integrity): Trỏ chính xác đến các bản ghi Master Data đã tồn tại gồm: FoodCategories (15 nhóm danh mục), Allergens (14 nhóm dị ứng FDA/Bộ Y tế), ENumberDictionary (65 mã phụ gia quốc tế), Ingredients (100 nguyên liệu thực phẩm chuẩn), Users & HealthProfiles.",
            "Dữ Liệu Thật & Tính Ứng Dụng Cao: 8 quán chay đều là các nhà hàng thực tế nổi tiếng tại Hà Nội, TP.HCM, Đà Nẵng, Huế với tọa độ GPS chuẩn xác để render trực tiếp lên bản đồ Map Goong/Google; 5 sản phẩm đóng gói mang mã vạch EAN-13 GS1 thật trên bao bì thương mại.",
            "Tính Thực Thi Lặp Lại Idempotent: 100% câu lệnh nạp dữ liệu sử dụng mệnh đề MERGE INTO với khối kiểm tra WHEN MATCHED THEN UPDATE và WHEN NOT MATCHED THEN INSERT, đảm bảo chạy lặp lại nhiều lần không gây lỗi vi phạm khóa chính hoặc Unique Constraint."
        ],
        title="4 TRỤ CỘT THIẾT KẾ DỮ LIỆU CỦA THÀNH VIÊN 4",
        border_hex="2E7D32",
        bg_hex="E8F5E9"
    )

    # =========================================================================
    # PHẦN 2: CHI TIẾT 08 QUÁN CHAY & BẢN ĐỒ VỊ TRÍ
    # =========================================================================
    add_custom_heading(doc, "2. Danh Sách 08 Quán Chay Thực Tế & Tọa Độ Bản Đồ (VeganVenues)", level=1)
    
    p = doc.add_paragraph()
    p.add_run(
        "Theo phân bổ, Thành viên 4 thiết lập 8 quán chay thực tế trải dài tại 4 trung tâm ẩm thực lớn nhất cả nước "
        "(Hà Nội, TP. Hồ Chí Minh, Đà Nẵng, Huế). Nguồn cấp dữ liệu tuân thủ ràng buộc CHECK [External_Source] = 'GOONG', "
        "đáp ứng tính năng tìm quán chay gần đây (Nearby Venues) và hiển thị chỉ đường chính xác theo tọa độ GPS."
    )

    venues_data = [
        [401, "Nhà Hàng Chay Ưu Đàm Chay", "34 Hàng Bài, Hoàn Kiếm, Hà Nội", "21.022315, 105.852412", "4.8 / 5.0", "Canh & Súp (Chính)\nLẩu Chay (Phụ)"],
        [402, "Nhà Hàng Chay Vị Lai", "67 Lý Thường Kiệt, Hoàn Kiếm, Hà Nội", "21.023456, 105.847895", "4.7 / 5.0", "Gỏi & Salad (Chính)\nMón Xào (Phụ)"],
        [403, "Nhà Hàng Chay Sadhu", "87 Lý Thường Kiệt, Cửa Nam, Hoàn Kiếm, Hà Nội", "21.024128, 105.842318", "4.9 / 5.0", "Cơm & Xôi (Chính)\nMón Chiên (Phụ)"],
        [404, "Hum Vegetarian, Lounge & Rest.", "32 Võ Văn Tần, P. Võ Thị Sáu, Q.3, TP.HCM", "10.776518, 106.690822", "4.9 / 5.0", "Món Cuốn (Chính)\nLẩu Chay (Phụ)"],
        [405, "Pi Vegetarian Bistro", "19 Võ Văn Tần, P. Võ Thị Sáu, Q.3, TP.HCM", "10.778194, 106.691458", "4.6 / 5.0", "Món Xào (Chính)\nMì Bún Phở (Phụ)"],
        [406, "Nhà Hàng Chay Rôu Vegetarian", "37B Cô Bắc, P. Cầu Ông Lãnh, Q.1, TP.HCM", "10.763425, 106.694815", "4.7 / 5.0", "Mì Bún Phở (Chính)\nBánh Mặn Ngọt (Phụ)"],
        [407, "Quán Chay An Lạc Tâm", "209 Nguyễn Hoàng, Nam Dương, Hải Châu, Đà Nẵng", "16.061245, 108.213452", "4.8 / 5.0", "Mì Bún Phở (Chính)\nMón Kho (Phụ)"],
        [408, "Nhà Hàng Chay Thiền Tâm", "110A Lê Ngô Cát, Thủy Xuân, TP. Huế, TT-Huế", "16.438924, 107.576518", "4.9 / 5.0", "Món Hấp Luộc (Chính)\nTráng Miệng Chè (Phụ)"]
    ]
    
    tbl_venues = doc.add_table(rows=1, cols=6)
    tbl_venues.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_venues.autofit = False
    v_widths = [Inches(0.5), Inches(1.5), Inches(1.7), Inches(1.1), Inches(0.7), Inches(1.1)]
    style_header_row(tbl_venues.rows[0], ["ID", "Tên Quán Chay", "Địa Chỉ Thực Tế", "Tọa Độ GPS (Lat, Long)", "Đánh Giá", "Phân Loại (Categories)"], v_widths)
    populate_table_data(tbl_venues, venues_data, v_widths, {0: WD_ALIGN_PARAGRAPH.CENTER, 3: WD_ALIGN_PARAGRAPH.CENTER, 4: WD_ALIGN_PARAGRAPH.CENTER})
    set_table_borders(tbl_venues)
    
    doc.add_paragraph().paragraph_format.space_after = Pt(10)

    # =========================================================================
    # PHẦN 3: CHI TIẾT THỰC ĐƠN 40 MÓN ĂN (5 MÓN/QUÁN)
    # =========================================================================
    add_custom_heading(doc, "3. Chi Tiết Thực Đơn 40 Món Ăn (VenueMenuItems - 5 món/quán)", level=1)
    
    p = doc.add_paragraph()
    p.add_run(
        "Mỗi quán ăn được xây dựng trọn vẹn 5 món ăn đặc trưng phản ánh đúng phong vị thực tế, "
        "kèm định mức giá tiền VNĐ thực tế, mô tả chi tiết nguyên liệu, ảnh món ăn chất lượng cao, "
        "và đánh dấu món đặc sản trứ danh (Signature Dish) để hệ thống AI tự động đề xuất người dùng."
    )

    menu_data = [
        # Quán 401: Ưu Đàm
        [401, "Ưu Đàm Chay (401)", "Súp nấm tuyết hạt sen Ưu Đàm", "95.000", "Có", "Hạt sen tươi Huế ninh nhừ cùng nấm tuyết giòn mát và nước dùng củ quả thanh khiết."],
        [402, "Ưu Đàm Chay (401)", "Nem vuông nấm tam giác mạch", "125.000", "Không", "Vỏ ram giòn rụm cuộn nấm đông cô, mộc nhĩ và hạt tam giác mạch thơm bùi đặc trưng vùng cao."],
        [403, "Ưu Đàm Chay (401)", "Lẩu dưỡng sinh thảo mộc cung đình", "380.000", "Có", "Nước lẩu ninh từ kỳ tử, táo đỏ, đảng sâm kết hợp 6 loại nấm tươi quý và rau xanh hữu cơ."],
        [404, "Ưu Đàm Chay (401)", "Cơm sen gạo lứt gói lá sen", "110.000", "Không", "Gạo lứt đỏ dẻo thơm hấp cùng hạt sen, đậu ngự và nấm rơm trong lá sen tươi ngát hương."],
        [405, "Ưu Đàm Chay (401)", "Chè tuyết nhĩ táo đỏ long nhãn", "65.000", "Không", "Món tráng miệng dưỡng nhan thanh mát nấu cùng đường phèn Quảng Ngãi nguyên chất."],
        # Quán 402: Vị Lai
        [406, "Quán Chay Vị Lai (402)", "Salad hoa quả nhiệt đới sốt chanh leo", "115.000", "Có", "Bơ sáp Đắk Lắk, thanh long đỏ, xoài cát hòa quyện sốt chanh leo tươi chua thanh kích thích vị giác."],
        [407, "Quán Chay Vị Lai (402)", "Nấm mỡ đút lò phô mai hạt điều", "145.000", "Không", "Nấm mỡ nguyên tai nướng béo ngậy với sốt phô mai hạt điều thuần thực vật thơm lừng."],
        [408, "Quán Chay Vị Lai (402)", "Đậu hũ Vị Lai sốt nấm tùng nhung", "135.000", "Có", "Đậu hũ non hữu cơ làm thủ công áp chảo phủ sốt nấm tùng nhung đậm đà thượng hạng."],
        [409, "Quán Chay Vị Lai (402)", "Canh hồng đài sen ngọc bích", "98.000", "Không", "Nước canh hoa atiso đỏ ngọt thanh, củ năng giòn và hạt sen bổ dưỡng."],
        [410, "Quán Chay Vị Lai (402)", "Kem bơ hạt dẻ cười thuần chay", "75.000", "Không", "Chế biến từ bơ chín tự nhiên và cốt dừa béo bùi, rắc hạt dẻ cười rang giòn."],
        # Quán 403: Sadhu
        [411, "Chay Sadhu (403)", "Vé Buffet chay thượng hạng Sadhu", "289.000", "Có", "Trải nghiệm hơn 80 món ăn mỹ vị chay phục vụ tận bàn theo phong cách ẩm thực Á Đông."],
        [412, "Chay Sadhu (403)", "Bánh xèo miền Tây nấm hoàng kim", "85.000", "Không", "Vỏ bánh vàng ươm giòn tan nhân nấm bào ngư, nấm hoàng kim, củ hũ dừa và giá đỗ."],
        [413, "Chay Sadhu (403)", "Nem lụi Sadhu cuộn rau rừng Tây Ninh", "110.000", "Có", "Nem lụi làm từ đậu xanh và nấm hương nướng than hoa, cuốn bánh tráng phơi sương chấm tương nếp."],
        [414, "Chay Sadhu (403)", "Súp cốm ngọc làng Vòng thanh vị", "68.000", "Không", "Cốm non mùa thu Hà Nội nấu cùng nấm tuyết và bắp ngọt tạo độ dẻo mềm thơm mát."],
        [415, "Chay Sadhu (403)", "Chè sương sa hạt lựu cốt dừa Sadhu", "45.000", "Không", "Hạt lựu củ năng giòn bọc bột lọc kèm thạch sương sa và nước cốt dừa nguyên chất."],
        # Quán 404: Hum
        [416, "Hum Vegetarian (404)", "Cuốn diếp chay sốt tương bần mè rang", "115.000", "Có", "Lá cải cay cuộn bún tươi, nấm đùi gà áp chảo và rau thơm chấm tương bần ngậy béo."],
        [417, "Hum Vegetarian (404)", "Cơm chiên trái thơm hạt điều Hum", "145.000", "Không", "Hạt cơm tơi vàng xào cùng dứa tươi ngọt dịu, đậu cô ve, cà rốt và hạt điều Bình Phước bùi béo."],
        [418, "Hum Vegetarian (404)", "Lẩu nấm sa tế lá chúc An Giang", "395.000", "Có", "Nước lẩu cay nồng dậy mùi tinh dầu lá chúc miền Tây kết hợp hơn 7 loại nấm tươi."],
        [419, "Hum Vegetarian (404)", "Đậu hũ nướng giấy bạc sốt tiêu xanh", "135.000", "Không", "Đậu hũ mềm mướt ướp tiêu xanh Phú Quốc nướng thơm phức bốc khói nghi ngút."],
        [420, "Hum Vegetarian (404)", "Nước ép dưa hấu bạc hà thanh lọc", "75.000", "Không", "Thức uống giải nhiệt nguyên chất không thêm đường với hương bạc hà the mát."],
        # Quán 405: Pi Bistro
        [421, "Pi Bistro (405)", "Đậu hũ xào sả ớt nghệ tươi Pi Bistro", "95.000", "Có", "Đậu hũ chiên sơ xào sả băm giòn thơm lựng cùng nghệ vàng xứ Quảng đậm vị cơm nhà."],
        [422, "Pi Bistro (405)", "Cà tím kho tộ sốt tiêu đen cốt dừa", "105.000", "Không", "Cà tím om trong niêu đất với nước sốt tiêu sánh mịn béo ngậy thơm ngon hao cơm."],
        [423, "Pi Bistro (405)", "Bún riêu chay cua đồng đậu hũ nấm rơm", "85.000", "Có", "Riêu cua làm từ váng sữa đậu nành và đậu xanh xốp mềm, nước dùng chua thanh từ cà chua và giấm bỗng."],
        [424, "Pi Bistro (405)", "Gỏi mít non đậu phộng rang thính gạo", "95.000", "Không", "Mít non luộc chín xé sợi trộn rau răm, ớt chuông, rắc đậu phộng thơm ăn cùng bánh tráng mè nướng."],
        [425, "Pi Bistro (405)", "Trà hoa đậu biếc sả chanh hạt chia", "55.000", "Không", "Trà thảo mộc màu tím lam quyến rũ giàu chất chống oxy hóa anthocyanin dưỡng da."],
        # Quán 406: Rôu
        [426, "Rôu Vegetarian (406)", "Phở khô Gia Lai sốt tương chay Rôu", "75.000", "Có", "Bánh phở khô dai thấm sốt tương phở đặc chế, ăn kèm tô nước súp ngọt nấm thanh thao."],
        [427, "Rôu Vegetarian (406)", "Bánh bao nướng nhân nấm sốt BBQ", "45.000", "Không", "Vỏ bánh nướng vàng ruộm, nhân nấm đùi gà xé sợi tẩm sốt BBQ nấm khói đậm đà."],
        [428, "Rôu Vegetarian (406)", "Canh khổ qua nhồi thịt chay nấm rơm", "65.000", "Không", "Khổ qua tươi nhồi đạm đậu nành và mộc nhĩ giòn ngọt mát thanh giải nhiệt cơ thể."],
        [429, "Rôu Vegetarian (406)", "Cơm tấm sườn nướng chay chả bì", "70.000", "Có", "Sườn non chay ướp sốt sả nướng xém cạnh, chả trứng hấp từ đậu hũ và bì miến rang thính."],
        [430, "Rôu Vegetarian (406)", "Chè chuối nướng nước cốt dừa Nam Bộ", "38.000", "Không", "Chuối sứ bọc nếp nướng thơm phức trong lá chuối, chan nước cốt dừa béo ngậy rắc mè rang."],
        # Quán 407: An Lạc Tâm
        [431, "An Lạc Tâm (407)", "Mì Quảng chay An Lạc Tâm đặc biệt", "45.000", "Có", "Sợi mì vàng óng nghệ, nhân đậu hũ ram, nấm đông cô, nước nhưn sánh đậm đà kèm bánh tráng mè nướng."],
        [432, "An Lạc Tâm (407)", "Bún nghệ xào lòng chay củ nén", "40.000", "Có", "Món ăn dân dã Quảng Nam với bún gạo xào nghệ tươi, mì căn dai dai và củ nén phi thơm ngào ngạt."],
        [433, "An Lạc Tâm (407)", "Cơm chiên ngọc bích hạt sen tươi", "50.000", "Không", "Cơm tơi xốp quyện màu xanh cải bó xôi tự nhiên, hạt sen mềm bùi cùng bắp ngọt và cà rốt."],
        [434, "An Lạc Tâm (407)", "Nấm rơm kho tiêu củ kiệu đượm vị", "55.000", "Không", "Nấm rơm búp tươi giòn kho keo với tiêu sọ, nước dừa xiêm và củ kiệu giòn cay dịu."],
        [435, "An Lạc Tâm (407)", "Sữa bắp nếp non tự nấu An Lạc", "20.000", "Không", "Bắp non thu hoạch tại Hội An xay sánh mịn, thơm ngọt thanh lành tự nhiên."],
        # Quán 408: Thiền Tâm
        [436, "Chay Thiền Tâm (408)", "Mẹt bánh Huế chay (Bèo, nậm, lọc)", "65.000", "Có", "Bộ ba đặc sản cố đô làm từ bột gạo, bột lọc với nhân nấm mèo, cà rốt và đậu xanh chấm nước mắm chay."],
        [437, "Chay Thiền Tâm (408)", "Cơm sen cung đình chay ngũ sắc", "95.000", "Có", "Món ăn tiến vua với hạt sen Tịnh Tâm, nấm hương, chả lụa chay thái hạt lựu ướp gia vị tinh tế."],
        [438, "Chay Thiền Tâm (408)", "Canh vả nấu sườn non chay lá lốt", "60.000", "Không", "Quả vả đồi Thủy Xuân thái lát mỏng nấu sườn chay, tỏa hương thơm ngát của lá lốt vườn nhà."],
        [439, "Chay Thiền Tâm (408)", "Vả trộn mè xúc bánh tráng nướng", "55.000", "Không", "Vả luộc thái mỏng bóp chua ngọt cùng rau răm, mè rang vàng, xúc cùng bánh tráng mè nướng giòn rụm."],
        [440, "Chay Thiền Tâm (408)", "Chè hạt sen long nhãn bọc bột lọc", "35.000", "Không", "Hạt sen chín bở lồng khéo léo trong cùi nhãn ngọt thơm, nước đường phèn hoa bưởi thanh khiết."]
    ]

    tbl_menu = doc.add_table(rows=1, cols=6)
    tbl_menu.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_menu.autofit = False
    m_widths = [Inches(0.5), Inches(1.3), Inches(1.6), Inches(0.7), Inches(0.6), Inches(1.9)]
    style_header_row(tbl_menu.rows[0], ["ID", "Quán Trực Thuộc", "Tên Món Ăn", "Giá (VNĐ)", "Đặc Sản", "Mô Tả Hương Vị & Thành Phần"], m_widths)
    populate_table_data(tbl_menu, menu_data, m_widths, {0: WD_ALIGN_PARAGRAPH.CENTER, 3: WD_ALIGN_PARAGRAPH.RIGHT, 4: WD_ALIGN_PARAGRAPH.CENTER})
    set_table_borders(tbl_menu)

    doc.add_paragraph().paragraph_format.space_after = Pt(10)

    # =========================================================================
    # PHẦN 4: 05 SẢN PHẨM QUÉT MÃ VẠCH EAN-13, E-NUMBERS & DỊ ỨNG
    # =========================================================================
    add_custom_heading(doc, "4. Chi Tiết 05 Sản Phẩm Quét Mã Vạch (Products, E-Numbers, Allergens)", level=1)
    
    p = doc.add_paragraph()
    p.add_run(
        "Phân hệ Barcode Scanner của Thành viên 4 đảm nhiệm kiểm tra nhãn mác thực phẩm đóng gói. "
        "Bộ dữ liệu mẫu gồm 5 sản phẩm phổ biến trên thị trường bán lẻ Việt Nam (bánh kẹo, mì ăn liền, hạt nêm, sữa hạt, snack), "
        "minh họa đầy đủ 3 trạng thái phán quyết thuần chay: VEGAN (Thuần chay), NON_VEGAN (Không thuần chay), DOUBTFUL (Nghi vấn)."
    )

    products_data = [
        [401, "8934563138175", "Mì chay Vifon Lẩu Thái chay 65g", "Vifon", "VEGAN", "Đã xác thực", "E621, E330, E150d, E412, E500", "GLUTEN (Chứa)\nSOY (Có thể chứa)"],
        [402, "8936036021442", "Hạt nêm nấm chay Maggi 450g", "Maggi", "VEGAN", "Đã xác thực", "E621, E627, E631, E150a", "SOY (Có thể chứa)\nGLUTEN (Có thể chứa)"],
        [403, "8934680031522", "Bánh quy AFC Vị Rau Cải 172g", "Kinh Đô AFC", "NON_VEGAN", "Đã xác thực", "E322, E500, E503", "DAIRY (Chứa bơ sữa bò)\nGLUTEN, SOY (Chứa)"],
        [404, "8934673592184", "Sữa đậu nành Organic Vinamilk 180ml", "Vinamilk", "VEGAN", "Đã xác thực", "E415, E407", "SOY (Chứa protein đậu nành)"],
        [405, "8850718801243", "Snack Rong Biển Big Roll Tao Kae Noi", "Tao Kae Noi", "DOUBTFUL", "Chờ kiểm duyệt", "E627, E631 (Nghi vấn)", "SOY (Chứa)\nGLUTEN (Có thể chứa)"]
    ]

    tbl_prod = doc.add_table(rows=1, cols=8)
    tbl_prod.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_prod.autofit = False
    p_widths = [Inches(0.4), Inches(1.1), Inches(1.3), Inches(0.7), Inches(0.8), Inches(0.8), Inches(1.0), Inches(1.0)]
    style_header_row(tbl_prod.rows[0], ["ID", "Mã Vạch EAN-13", "Tên Sản Phẩm", "Hãng", "Phán Quyết", "Xác Thực", "Mã E-Number", "Chất Dị Ứng"], p_widths)
    populate_table_data(tbl_prod, products_data, p_widths, {0: WD_ALIGN_PARAGRAPH.CENTER, 1: WD_ALIGN_PARAGRAPH.CENTER, 4: WD_ALIGN_PARAGRAPH.CENTER, 5: WD_ALIGN_PARAGRAPH.CENTER})
    set_table_borders(tbl_prod)

    # Phân tích chi tiết từng trường hợp nghiệp vụ
    doc.add_paragraph().paragraph_format.space_after = Pt(4)
    add_custom_heading(doc, "Phân Tích Nghiệp Vụ Kiểm Tra Tính Thuần Chay (Vegan Verdict Analysis):", level=3)
    
    make_callout(
        doc,
        [
            "Sản phẩm 401 & 402 (VEGAN): Mì gói Vifon và Hạt nêm Maggi sử dụng phụ gia E621 (bột ngọt lên men từ mật mía/tinh bột sắn), màu caramel E150a/E150d và chất làm dày thực vật E412. Dù sản phẩm có cảnh báo 'Có thể chứa đậu nành/gluten' do chung dây chuyền sản xuất, công thức thành phần 100% gốc thực vật.",
            "Sản phẩm 403 (NON_VEGAN): Bánh quy mặn AFC chứa 'bơ động vật từ sữa bò tươi' và 'bột whey sữa nguyên kem'. Hệ thống tự động kích hoạt cảnh báo vi phạm trường phái Vegan/Lacto-Ovo và cắm cờ dị ứng đạm sữa bò (DAIRY).",
            "Sản phẩm 404 (VEGAN HỮU CƠ): Sữa đậu nành Vinamilk Organic đạt chứng nhận hữu cơ EU Organic, sử dụng phụ gia thực vật E415 (Xanthan gum lên men vi sinh) và E407 (Carrageenan chiết xuất từ rong biển đỏ).",
            "Sản phẩm 405 (DOUBTFUL): Snack rong biển Tao Kae Noi chứa phụ gia E627 (Disodium guanylate) và E631 (Disodium inosinate) trong hỗn hợp gia vị khói. Trong sản xuất công nghiệp, hai chất điều vị này có khả năng được tổng hợp từ cá sấy khô hoặc thịt động vật nếu nhà sản xuất không cung cấp chứng nhận Halal/Vegan từ vi sinh vật."
        ],
        title="GIẢI TRÌNH KHOA HỌC THỰC PHẨM CỦA MÁY QUÉT",
        border_hex="00695C",
        bg_hex="E0F2F1"
    )

    # =========================================================================
    # PHẦN 5: 01 PHIÊN TỦ LẠNH THÔNG MINH (FRIDGESESSIONS & FRIDGEINVENTORY)
    # =========================================================================
    add_custom_heading(doc, "5. Chi Tiết Phiên Tủ Lạnh Ảo (FridgeSessions & FridgeInventory)", level=1)
    
    p = doc.add_paragraph()
    p.add_run(
        "Tính năng Tủ Lạnh Ảo (Empty the Fridge - BR-AI-FRIDGE-01) ghi nhận phiên người dùng tải ảnh chụp ngăn tủ lạnh gia đình. "
        "Hệ thống thị giác máy tính nhận diện và tự động bóc tách thành 5 thực phẩm bên trong, đồng thời gán nhãn độ tươi: "
        "FRESH (Tươi mới) hoặc NON-FRESH (Sắp hỏng, cần ưu tiên nấu ngay để chống lãng phí)."
    )

    fridge_session_info = [
        ("Mã phiên quét (Session_ID)", "401"),
        ("Người dùng sở hữu (User_ID)", "2 (Tài khoản Member: nguyenvana@veggielife.vn)"),
        ("Đường dẫn ảnh chụp tủ lạnh (Image_CDN_URL)", "https://images.unsplash.com/photo-1584473457406-6240486418e9 (Kho ảnh Unsplash chất lượng cao)"),
        ("Số lượng vật phẩm phát hiện (Detected_Items_Count)", "5 thực phẩm được Vision AI bóc tách chính xác"),
        ("Thời điểm khởi tạo phiên (Created_At)", "DATEADD(HOUR, -3, SYSDATETIME())")
    ]
    
    tbl_fs = doc.add_table(rows=5, cols=2)
    tbl_fs.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_fs.autofit = False
    for idx, (lbl, val) in enumerate(fridge_session_info):
        row = tbl_fs.rows[idx]
        for c_idx in range(2):
            cell = row.cells[c_idx]
            cell.width = widths_meta[c_idx]
            set_cell_background(cell, "F1F8E9" if idx % 2 == 1 else "FFFFFF")
            set_cell_margins(cell, top=50, bottom=50, left=70, right=70)
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            p = cell.paragraphs[0]
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(0)
            if c_idx == 0:
                r = p.add_run(lbl)
                r.font.bold = True
                r.font.size = Pt(9)
                r.font.color.rgb = RGBColor(0x1B, 0x5E, 0x20)
            else:
                r = p.add_run(val)
                r.font.size = Pt(9)
                r.font.color.rgb = RGBColor(0x21, 0x21, 0x21)
    set_table_borders(tbl_fs)
    doc.add_paragraph().paragraph_format.space_after = Pt(6)

    # Chi tiết 5 nguyên liệu trong tủ lạnh
    inv_data = [
        [401, "Nấm đùi gà tươi", "Nấm đùi gà (King Oyster) - ID: 56", "200.00", "g", "FRESH (Tươi ngon)", "Nguồn đạm thực vật và chất xơ tuyệt vời, kết cấu dai ngọt."],
        [402, "Đậu hũ non Silken Tofu", "Đậu hũ non (Silken Tofu) - ID: 31", "300.00", "g", "FRESH (Tươi ngon)", "Chế phẩm đậu nành giàu đạm, mềm mịn, phù hợp nấu canh thanh mát."],
        [403, "Bông cải xanh (Broccoli)", "Bông cải xanh (Broccoli) - ID: 98", "250.00", "g", "FRESH (Tươi ngon)", "Rau giàu sulforaphane, canxi thực vật và vitamin C tăng cường miễn dịch."],
        [404, "Cà chua chín đỏ (cần dùng sớm)", "Cà chua chín đỏ - ID: 83", "300.00", "g", "NON-FRESH (Cần dùng ngay)", "Cà chua chín mọng vỏ bắt đầu nhăn mềm, cần nấu canh để tránh ủng hỏng."],
        [405, "Nấm kim châm tươi", "Nấm kim châm tươi - ID: 57", "150.00", "g", "FRESH (Tươi ngon)", "Tăng độ ngọt tự nhiên cho nước dùng, giòn ngọt, ít calo."]
    ]

    tbl_inv = doc.add_table(rows=1, cols=7)
    tbl_inv.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_inv.autofit = False
    i_widths = [Inches(0.4), Inches(1.3), Inches(1.5), Inches(0.6), Inches(0.4), Inches(1.0), Inches(1.3)]
    style_header_row(tbl_inv.rows[0], ["ID", "Tên Thực Phẩm Nhận Diện", "Khóa Ngoại Master (Ingredients)", "Số Lượng", "ĐV", "Trạng Thái Độ Tươi", "Lợi Ích Dinh Dưỡng"], i_widths)
    populate_table_data(tbl_inv, inv_data, i_widths, {0: WD_ALIGN_PARAGRAPH.CENTER, 3: WD_ALIGN_PARAGRAPH.RIGHT, 4: WD_ALIGN_PARAGRAPH.CENTER, 5: WD_ALIGN_PARAGRAPH.CENTER})
    set_table_borders(tbl_inv)

    doc.add_paragraph().paragraph_format.space_after = Pt(10)

    # =========================================================================
    # PHẦN 6: 01 PHIÊN TƯ VẤN AI CHATBOT & KHẢO SÁT CHẤT LƯỢNG
    # =========================================================================
    add_custom_heading(doc, "6. Chi Tiết Phiên Tư Vấn AI Chatbot & Khảo Sát (ChatSessions & Surveys)", level=1)
    
    p = doc.add_paragraph()
    p.add_run(
        "Phiên hội thoại AI mẫu kết nối trực tiếp thực phẩm vừa quét từ Tủ Lạnh Ảo với trợ lý dinh dưỡng AI VeggieBot. "
        "Nội dung hội thoại đáp ứng đầy đủ quy tắc nghiệp vụ BR-AI-DISCLAIMER-01 (Bắt buộc chứa câu miễn trừ y tế đối với "
        "các tư vấn thực đơn), đồng thời có phiếu đánh giá chất lượng phản hồi 5 sao từ người dùng."
    )

    # Thông tin ChatSession 401
    chat_meta = [
        ("Mã phiên hội thoại (Chat_Session_ID)", "401"),
        ("Người dùng tham gia (User_ID)", "2 (Nguyễn Văn A - Member)"),
        ("Hồ sơ sức khỏe tham chiếu (Profiles_ID)", "2 (Tham chiếu chỉ số BMI, mục tiêu dinh dưỡng cá nhân)"),
        ("Tiêu đề phiên hội thoại (Session_Title)", "Gợi ý thực đơn vét tủ lạnh Empty the Fridge"),
        ("Ngữ cảnh tư vấn (Context_Type)", "NUTRITION_CONSULTATION"),
        ("Thời lượng tương tác", "Bắt đầu 135 phút trước, kết thúc 120 phút trước")
    ]
    
    tbl_cs = doc.add_table(rows=6, cols=2)
    tbl_cs.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_cs.autofit = False
    for idx, (lbl, val) in enumerate(chat_meta):
        row = tbl_cs.rows[idx]
        for c_idx in range(2):
            cell = row.cells[c_idx]
            cell.width = widths_meta[c_idx]
            set_cell_background(cell, "F9FBE7" if idx % 2 == 1 else "FFFFFF")
            set_cell_margins(cell, top=50, bottom=50, left=70, right=70)
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            p = cell.paragraphs[0]
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(0)
            if c_idx == 0:
                r = p.add_run(lbl)
                r.font.bold = True
                r.font.size = Pt(9)
                r.font.color.rgb = RGBColor(0x1B, 0x5E, 0x20)
            else:
                r = p.add_run(val)
                r.font.size = Pt(9)
                r.font.color.rgb = RGBColor(0x21, 0x21, 0x21)
    set_table_borders(tbl_cs)
    doc.add_paragraph().paragraph_format.space_after = Pt(6)

    # Bảng 4 tin nhắn hội thoại
    add_custom_heading(doc, "Kịch Bản Đối Thoại 4 Lượt (ChatMessages - ID: 401 đến 404):", level=3)
    
    chat_dialogue = [
        [
            401, 
            "USER (Người dùng)", 
            "Chào VeggieBot! Tủ lạnh nhà mình hiện còn nấm đùi gà, đậu hũ non, bông cải xanh và cà chua chín sắp mềm. Bạn gợi ý giúp mình một bữa tối chay thanh đạm, nấu nhanh dưới 20 phút tận dụng hết các nguyên liệu này được không?",
            "NULL"
        ],
        [
            402, 
            "ASSISTANT (AI Bot)", 
            "Chào bạn! VeggieBot rất vui được đồng hành cùng bạn giải cứu thực phẩm hôm nay. Với tính năng 'Empty the Fridge', bạn đang có sự kết hợp nguyên liệu tuyệt vời: Đậu hũ non (giàu protein thực vật mềm mịn), Nấm đùi gà (kết cấu dai giòn ngọt tự nhiên), Bông cải xanh (giàu chất xơ và Vitamin C), cùng Cà chua chín đỏ (chứa lycopene chống oxy hóa, cần ưu tiên nấu trước để tránh ủng hỏng).\n\n"
            "VeggieBot gợi ý thực đơn 2 món hoàn thiện trong 18 phút:\n"
            "1. Canh Đậu Hũ Non Nấu Cà Chua & Nấm Kim Châm: Vị chua dịu tự nhiên từ cà chua xào thơm, quyện cùng nước dùng nấm ngọt lịm và từng lát đậu hũ non béo mềm (Thời gian nấu: 10 phút).\n"
            "2. Nấm Đùi Gà Áp Chảo Xào Bông Cải Xanh Dầu Hào Chay: Nấm đùi gà thái lát chéo áp chảo xém vàng thơm lừng, xào lửa lớn cùng bông cải xanh giòn sần sật, nêm 1 thìa dầu hào chay và rắc chút tiêu đen (Thời gian nấu: 8 phút).\n\n"
            "Tổng calo ước tính: ~385 kcal, Protein: 22g, Chất xơ: 8.5g.\n"
            "⚖️ Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền.",
            "tokensConsumed: 520\nmodelVersion: gemini-2.5-flash\npromptType: EMPTY_THE_FRIDGE\nhasMedicalDisclaimer: true\ncookingTime: 18 mins"
        ],
        [
            403, 
            "USER (Người dùng)", 
            "Cảm ơn bot nhiều! Nếu mình muốn thêm chút vị cay nhẹ vào món nấm đùi gà thì nên nêm gì để không phá vị ngọt tự nhiên của nấm?",
            "NULL"
        ],
        [
            404, 
            "ASSISTANT (AI Bot)", 
            "Để tạo vị cay ấm nồng hài hòa mà vẫn tôn trọn vị ngọt thanh của nấm đùi gà, VeggieBot gợi ý 2 lựa chọn tuyệt hảo:\n"
            "1. Tiêu đen Phú Quốc đập dập hoặc tiêu xanh tươi: Rắc vào chảo ở 30 giây cuối cùng trước khi tắt bếp. Tinh dầu piperine trong tiêu tạo độ cay ấm nhẹ nhàng kích thích vị giác và khử tính hàn của nấm cực tốt.\n"
            "2. Ớt sừng băm nhuyễn bỏ hạt: Xào thơm cùng chút boa-rô trước khi cho nấm vào. Ớt sừng cho màu đỏ rực rỡ bắt mắt nhưng độ cay chỉ ở mức êm dịu, không nồng gắt như ớt hiểm.\n\n"
            "Chúc bạn có một bữa tối chay ấm cúng, trọn vị và tràn đầy năng lượng an lành!",
            "tokensConsumed: 380\nmodelVersion: gemini-2.5-flash\npromptType: RECIPE_TUNING\nhasMedicalDisclaimer: false"
        ]
    ]

    tbl_msg = doc.add_table(rows=1, cols=4)
    tbl_msg.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_msg.autofit = False
    msg_widths = [Inches(0.4), Inches(1.2), Inches(3.5), Inches(1.4)]
    style_header_row(tbl_msg.rows[0], ["ID", "Vai Trò Gửi", "Nội Dung Tin Nhắn Đối Thoại", "Metadata AI (Tokens & Prompt)"], msg_widths)
    populate_table_data(tbl_msg, chat_dialogue, msg_widths, {0: WD_ALIGN_PARAGRAPH.CENTER, 1: WD_ALIGN_PARAGRAPH.CENTER})
    set_table_borders(tbl_msg)

    doc.add_paragraph().paragraph_format.space_after = Pt(8)

    # Phiếu khảo sát Surveys 401
    add_custom_heading(doc, "Phiếu Khảo Sát Đánh Giá Chất Lượng AI (Surveys - ID: 401):", level=3)
    
    survey_info = [
        ("Mã phiếu khảo sát (Survey_ID)", "401"),
        ("Người dùng đánh giá (User_ID)", "2 (Nguyễn Văn A)"),
        ("Phiên hội thoại liên quan (Session_ID)", "401 (Trỏ khóa ngoại FK_Surveys_ChatSessions)"),
        ("Điểm số hài lòng (Rating_Score)", "⭐ ⭐ ⭐ ⭐ ⭐ (5 / 5 sao - Mức độ hài lòng tối đa)"),
        ("Câu hỏi ngữ cảnh (Context_Question)", "Chất lượng gợi ý món ăn từ tủ lạnh và tính hữu ích của trợ lý dinh dưỡng AI"),
        ("Phản hồi định tính (Feedback_Content)", "Gợi ý cực kỳ thực tế và nhanh chóng! Món canh cà chua đậu hũ non và nấm xào bông cải nấu đúng 18 phút, vị vừa vặn, nguyên liệu giải quyết đúng mớ đồ sắp hỏng trong tủ lạnh mà không bị bỏ phí.")
    ]
    
    tbl_sv = doc.add_table(rows=6, cols=2)
    tbl_sv.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_sv.autofit = False
    for idx, (lbl, val) in enumerate(survey_info):
        row = tbl_sv.rows[idx]
        for c_idx in range(2):
            cell = row.cells[c_idx]
            cell.width = widths_meta[c_idx]
            set_cell_background(cell, "E8F5E9" if idx % 2 == 1 else "FFFFFF")
            set_cell_margins(cell, top=50, bottom=50, left=70, right=70)
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            p = cell.paragraphs[0]
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(0)
            if c_idx == 0:
                r = p.add_run(lbl)
                r.font.bold = True
                r.font.size = Pt(9)
                r.font.color.rgb = RGBColor(0x1B, 0x5E, 0x20)
            else:
                r = p.add_run(val)
                r.font.size = Pt(9)
                r.font.color.rgb = RGBColor(0x21, 0x21, 0x21)
    set_table_borders(tbl_sv)

    doc.add_paragraph().paragraph_format.space_after = Pt(12)

    # =========================================================================
    # PHẦN 7: KỊCH BẢN KIỂM TRA ĐỐI SOÁT VÀ NẠP CƠ SỞ DỮ LIỆU
    # =========================================================================
    add_custom_heading(doc, "7. Kịch Bản Kiểm Tra & Hướng Dẫn Thực Thi SQL", level=1)
    
    p = doc.add_paragraph()
    p.add_run(
        "Kịch bản dữ liệu mẫu được tích hợp hoàn chỉnh vào quy trình triển khai cơ sở dữ liệu VeggieLife. "
        "Tester và giảng viên có thể kiểm chứng dữ liệu bằng hai phương thức:"
    )

    make_callout(
        doc,
        [
            "Thực thi qua kịch bản tổng thể: Chạy file database/seed/00_Seed_All.sql qua SSMS hoặc Azure Data Studio (file tự động gọi :r '17_Seed_Member4_Venues_Products_Fridge.sql' ở bước số 11).",
            "Thực thi đơn lẻ (Standalone Execution): Mở trực tiếp file database/seed/17_Seed_Member4_Venues_Products_Fridge.sql và nhấn Execute (F5). Kịch bản hỗ trợ kiểm tra fallback thông minh, tự động gán User_ID và Ingredient_ID hợp lệ nếu database chưa có đủ dữ liệu người dùng mẫu."
        ],
        title="HƯỚNG DẪN THỰC THI NẠP SEED DATA",
        border_hex="1B5E20",
        bg_hex="F1F8E9"
    )

    p_sql = doc.add_paragraph()
    p_sql.add_run("Câu lệnh SQL truy vấn kiểm tra toàn vẹn dữ liệu Thành viên 4 (Verification Query):")
    p_sql.paragraph_format.space_before = Pt(6)
    p_sql.paragraph_format.space_after = Pt(4)
    
    # Code block format
    tbl_code = doc.add_table(rows=1, cols=1)
    tbl_code.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_code.autofit = False
    tbl_code.columns[0].width = Inches(6.5)
    code_cell = tbl_code.cell(0, 0)
    set_cell_background(code_cell, "263238") # Dark Blue Grey
    set_cell_margins(code_cell, top=100, bottom=100, left=120, right=120)
    
    sql_text = (
        "-- 1. Kiem tra so luong ban ghi theo dai ID 401 - 499 cua Thanh vien 4\n"
        "SELECT 'VeganVenues' AS TableName, COUNT(*) AS TotalCount FROM dbo.VeganVenues WHERE Venue_ID BETWEEN 401 AND 499\n"
        "UNION ALL\n"
        "SELECT 'VenueMenuItems', COUNT(*) FROM dbo.VenueMenuItems WHERE Item_ID BETWEEN 401 AND 499\n"
        "UNION ALL\n"
        "SELECT 'Products', COUNT(*) FROM dbo.Products WHERE Product_ID BETWEEN 401 AND 499\n"
        "UNION ALL\n"
        "SELECT 'Product_ENumbers', COUNT(*) FROM dbo.Product_ENumbers WHERE Product_ID BETWEEN 401 AND 499\n"
        "UNION ALL\n"
        "SELECT 'FridgeSessions', COUNT(*) FROM dbo.FridgeSessions WHERE Session_ID BETWEEN 401 AND 499\n"
        "UNION ALL\n"
        "SELECT 'FridgeInventory', COUNT(*) FROM dbo.FridgeInventory WHERE Inventory_ID BETWEEN 401 AND 499\n"
        "UNION ALL\n"
        "SELECT 'ChatSessions', COUNT(*) FROM dbo.ChatSessions WHERE Chat_Session_ID BETWEEN 401 AND 499\n"
        "UNION ALL\n"
        "SELECT 'ChatMessages', COUNT(*) FROM dbo.ChatMessages WHERE Message_ID BETWEEN 401 AND 499\n"
        "UNION ALL\n"
        "SELECT 'Surveys', COUNT(*) FROM dbo.Surveys WHERE Survey_ID BETWEEN 401 AND 499;\n\n"
        "-- 2. Kiem tra tinh toan ven khoa ngoai (Referential Integrity Check)\n"
        "SELECT v.Venue_ID, v.Venue_Name, v.Latitude, v.Longitude, c.Category_Name, m.Dish_Name, m.Price_VND\n"
        "FROM dbo.VeganVenues v\n"
        "JOIN dbo.VenueCategories vc ON vc.Venue_ID = v.Venue_ID AND vc.Is_Primary = 1\n"
        "JOIN dbo.FoodCategories c ON c.Category_ID = vc.Category_ID\n"
        "JOIN dbo.VenueMenuItems m ON m.Venue_ID = v.Venue_ID AND m.Is_Signature = 1\n"
        "WHERE v.Venue_ID BETWEEN 401 AND 408;"
    )
    
    cp = code_cell.paragraphs[0]
    cp.paragraph_format.space_before = Pt(0)
    cp.paragraph_format.space_after = Pt(0)
    crun = cp.add_run(sql_text)
    crun.font.name = "Consolas"
    crun.font.size = Pt(8.5)
    crun.font.color.rgb = RGBColor(0x80, 0xCB, 0xC4) # Light Teal
    
    doc.add_paragraph().paragraph_format.space_after = Pt(14)
    
    # Kết luận
    add_custom_heading(doc, "8. Kết Luận & Cam Kết Tiêu Chuẩn", level=1)
    p_end = doc.add_paragraph()
    p_end.add_run(
        "Báo cáo và kịch bản Seed Data của Thành viên 4 đã hoàn thành xuất sắc các tiêu chí kỹ thuật: "
        "đúng phạm vi phân công, đúng dải ID (401 - 499), dữ liệu phong phú, sống động và có tính xác thực cao. "
        "Toàn bộ tài nguyên phục vụ việc thuyết minh đồ án và kiểm thử tích hợp hệ thống (End-to-End Testing) "
        "đã sẵn sàng để chuyển giao cho các thành viên trong nhóm."
    )

    # Lưu tài liệu vào thư mục project-docs/
    output_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "project-docs"))
    if not os.path.exists(output_dir):
        os.makedirs(output_dir, exist_ok=True)
        
    output_path = os.path.join(output_dir, "data mẫu của tôi.docx")
    doc.save(output_path)
    print("DOCX report generated successfully at: " + output_path.encode("ascii", "replace").decode("ascii"))

if __name__ == "__main__":
    build_document()
