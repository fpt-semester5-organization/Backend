# -*- coding: utf-8 -*-
"""Dữ liệu tĩnh cho generator seed60: món chay, nguyên liệu bổ sung, sản phẩm, quán chay, mẫu văn bản.
LƯU Ý: công thức ở đây là DỮ LIỆU MẪU do generator tạo (Source='SYSTEM'), định lượng cân đối theo mục tiêu
calo; sẽ được thay bằng công thức trích từ sách/nguồn mở khi bạn gửi tài liệu (Data/cookbooks)."""

# ---------- Nguyên liệu bổ sung (chèn dải ID 10001+, bỏ qua nếu tên đã tồn tại) ----------
# key -> (tên, stall, kcal/100g, allium, food_group)
SUPP = {
 "coconut": ("Nước cốt dừa", "CONDIMENTS", 197.0, 0, "PLANT"),
 "chuoi":   ("Chuối chín", "PRODUCE", 89.0, 0, "PLANT"),
 "bo_sap":  ("Bơ sáp", "PRODUCE", 160.0, 0, "PLANT"),
 "du_du":   ("Đu đủ xanh bào sợi", "PRODUCE", 43.0, 0, "PLANT"),
 "rong_bien": ("Rong biển khô (Nori)", "DRY_GOODS", 306.0, 0, "PLANT"),
 "muoi":    ("Muối tinh", "CONDIMENTS", 0.0, 0, "PLANT"),
 "duong":   ("Đường cát trắng", "CONDIMENTS", 387.0, 0, "PLANT"),
 "bot_cari": ("Bột cà ri chay", "CONDIMENTS", 325.0, 0, "PLANT"),
 "bot_gao": ("Bột gạo", "DRY_GOODS", 366.0, 0, "PLANT"),
 "banh_pho": ("Bánh phở tươi", "DRY_GOODS", 109.0, 0, "PLANT"),
 "mien":    ("Miến dong khô", "DRY_GOODS", 330.0, 0, "PLANT"),
 "gao_nep": ("Gạo nếp", "DRY_GOODS", 350.0, 0, "PLANT"),
 "mi":      ("Mì sợi khô chay (không trứng)", "DRY_GOODS", 350.0, 0, "PLANT"),
}

# key -> chỉ số (1-based) trong Data/tools/ingredients.txt (= thứ tự 04_Seed_Ingredients.sql)
K = dict(dauhu=1, dauhu_chien=2, dauhu_non=3, hukyt=4, hukyk=5, tempeh=6, mican=7, mican_chien=8, chalua=9,
 suon=10, bo_chay=11, ga_chay=12, dau_nanh=13, dau_xanh=14, dau_do=15, dau_den=16, dau_ga=17, dau_lang=18,
 dau_trang=19, dau_ha=20, nam_rom=21, nam_bn=22, nam_bnx=23, nam_dg=24, nam_kc=25, nam_huong=26, nam_huong_k=27,
 moc_nhi=28, nam_tuyet=29, nam_mo=30, carot=33, cu_cai=34, bi_do=35, khoai_tay=36, khoai_lang=37, khoai_mon=38,
 cu_sen=39, bap=41, kho_qua=43, bau=45, bi_xanh=46, ca_chua=47, dua_leo=48, ca_tim=49, su_su=50, dau_que=51,
 dau_bap=53, mang=54, hoa_chuoi=56, hat_sen=84, ngo_sen=57, broccoli=58, bap_cai=60, cai_thia=62, cai_ngot=63, cai_xanh=64,
 rau_muong=65, rau_ngot=66, xa_lach=69, ngo_ri=72, rau_ram=74, hanh_la=77, hanh_tim=78, hanh_tay=79, toi=80,
 dau_phong=86, hat_dieu=87, oc_cho=89, me_trang=90, me_den=91, gao=92, gao_lut=93, yen_mach=94, bun=96,
 banh_trang=97, bot_mi=98, bot_nang=99, nuoc_tuong=100, nuoc_mam=101, dau_hao=102, tuong_hot=103, hat_nem=106,
 dau_oil=108, dau_me=109, tieu=110, gung=111, nghe=112, sa=113, ot=114)

C_RICE="Món Cơm & Xôi Chay"; C_NOODLE="Món Mì, Bún & Phở Chay"; C_KHO="Món Kho Chay Đậm Đà"
C_XAO="Món Xào Chay Thanh Vị"; C_SOUP="Món Canh & Súp Bổ Dưỡng"; C_SALAD="Món Gỏi & Salad Tươi Mát"
C_FRY="Món Chiên & Rán Giòn Rụm"; C_STEAM="Món Hấp & Luộc Dưỡng Sinh"; C_HOT="Món Lẩu Chay Gia Đình"
C_GRILL="Món Nướng Chay"; C_ROLL="Món Cuốn Chay"; C_SWEET="Món Tráng Miệng & Chè"
C_DRINK="Đồ Uống & Trà Dinh Dưỡng"; C_BAKE="Bánh Mặn & Bánh Ngọt Chay"

# slug: (category, meals, prep, cook, servings, difficulty, method, "key:grams[o] ...")
D = {
"tofu_tomato_sauce":("%s"%C_KHO,"LD",10,15,2,"EASY","braise","dauhu:300 ca_chua:300 hanh_la:15 dau_oil:10 nuoc_tuong:15 duong:5 muoi:3 tieu:1"),
"goi_cuon_chay":(C_ROLL,"LS",25,5,2,"MEDIUM","roll","banh_trang:40 bun:80 dauhu_chien:100 xa_lach:80 dua_leo:60 carot:50 rau_ram:15 tuong_hot:20 dau_phong:20"),
"canh_nam_rau_cu":(C_SOUP,"LD",10,20,2,"EASY","soup","nam_rom:100 nam_bn:100 carot:100 cai_ngot:150 ngo_ri:5 nuoc_tuong:10 muoi:3 hat_nem:5"),
"canh_bi_dao_nam":(C_SOUP,"LD",10,20,2,"EASY","soup","bi_xanh:300 nam_bn:100 nam_huong:40 hanh_la:10 muoi:3 hat_nem:5"),
"goi_ngo_sen_chay":(C_SALAD,"L",20,5,2,"MEDIUM","salad","ngo_sen:150 carot:60 dauhu_chien:100 rau_ram:15 dau_phong:25 me_trang:10 nuoc_mam:20 duong:8 ot:5:o"),
"canh_chua_chay":(C_SOUP,"LD",15,20,2,"MEDIUM","soup","ca_chua:150 dauhu_chien:100 dau_bap:100 nam_rom:80 sa:10 ot:5 duong:8 muoi:3"),
"dau_hu_hap_nam":(C_STEAM,"LD",15,20,2,"EASY","steam","dauhu_non:300 nam_huong:60 nam_kc:60 gung:10 nuoc_tuong:15 dau_me:5 tieu:1 hanh_la:10:o"),
"rau_muong_xao":(C_XAO,"LD",5,5,2,"EASY","stirfry","rau_muong:400 toi:15 dau_oil:10 nuoc_tuong:10 muoi:2"),
"chao_yen_mach_rau_cu":(C_RICE,"B",10,20,1,"EASY","porridge","yen_mach:80 carot:60 nam_huong:40 hanh_la:10:o nuoc_tuong:10 muoi:2"),
"sup_bi_do":(C_SOUP,"BD",10,25,2,"EASY","soup","bi_do:400 khoai_tay:100 hanh_tay:50 dau_oil:8 muoi:3 tieu:1 hat_sen:20"),
"nom_du_du_chay":(C_SALAD,"LS",20,0,2,"EASY","salad","du_du:300 carot:60 dauhu_chien:100 rau_ram:15 dau_phong:25 nuoc_mam:20 duong:10 ot:5"),
"canh_rau_ngot":(C_SOUP,"LD",10,10,2,"EASY","soup","rau_ngot:200 nam_rom:100 hat_nem:5 muoi:3 nuoc_tuong:5"),
"ca_tim_nuong":(C_GRILL,"D",10,25,2,"MEDIUM","grill","ca_tim:400 hanh_la:20 dau_oil:10 nuoc_mam:20 gung:5 duong:8 dau_phong:20"),
"mang_xao_nam":(C_XAO,"LD",15,15,2,"EASY","stirfry","mang:250 nam_rom:100 nam_dg:80 dau_oil:10 nuoc_tuong:15 tieu:1 muoi:2"),
"canh_kho_qua_chay":(C_SOUP,"LD",25,25,2,"MEDIUM","soup","kho_qua:300 nam_huong:40 moc_nhi:8 dauhu:100 hat_nem:5 muoi:3 carot:40"),
"salad_rau_cu_dau_phu":(C_SALAD,"LD",15,5,2,"EASY","salad","xa_lach:100 dua_leo:100 ca_chua:100 dauhu:150 carot:50 dau_me:10 me_trang:10 nuoc_tuong:15 dau_oil:10"),
"bun_gao_lut_rau_cu":(C_NOODLE,"L",15,10,1,"EASY","noodle","bun:150 carot:60 cai_ngot:100 dauhu_chien:100 nam_bn:80 nuoc_tuong:20 dau_oil:10 me_trang:8"),
"rau_cu_luoc_kho_quet":(C_STEAM,"LD",15,20,2,"MEDIUM","steam","carot:100 su_su:120 dau_que:100 bap_cai:100 dau_bap:80 nuoc_tuong:20 duong:10 dau_oil:15 hanh_tim:20 ot:5:o tieu:1"),
"dau_bap_luoc":(C_STEAM,"LS",5,8,2,"EASY","steam","dau_bap:300 me_trang:15 muoi:5 dau_me:5"),
"canh_cai_nam_rom":(C_SOUP,"LD",10,10,2,"EASY","soup","cai_xanh:200 nam_rom:120 gung:5 hat_nem:5 muoi:3"),
"com_chien_chay":(C_RICE,"LD",10,10,2,"EASY","stirfry","gao:250 dauhu_chien:100 carot:60 dau_ha:60 bap:60 hanh_la:20 dau_oil:20 nuoc_tuong:15 hat_nem:5"),
"bun_rieu_chay":(C_NOODLE,"BL",30,30,2,"MEDIUM","noodle","bun:200 ca_chua:300 dauhu:200 dauhu_chien:100 nam_rom:100 hanh_la:20 dau_oil:15 muoi:5 rau_muong:100"),
"pho_chay":(C_NOODLE,"BL",20,90,2,"HARD","noodle","banh_pho:250 nam_huong:60 nam_dg:100 dauhu_chien:100 hanh_tay:80 gung:30 cu_cai:100 carot:80 hanh_la:20 ngo_ri:10 hat_nem:8 muoi:5"),
"bun_hue_chay":(C_NOODLE,"BL",25,60,2,"HARD","noodle","bun:250 mican:100 dauhu_chien:100 sa:40 ot:15 ca_chua:100 hoa_chuoi:100 nam_rom:80 dau_oil:15 muoi:5 nuoc_mam:15"),
"mi_quang_chay":(C_NOODLE,"BL",25,30,2,"MEDIUM","noodle","banh_pho:250 nghe:10 dauhu_chien:120 dau_phong:40 bap_cai:100 nam_rom:100 ca_chua:100 dau_oil:15 me_trang:10 banh_trang:20 hat_nem:5 muoi:3"),
"hu_tieu_chay":(C_NOODLE,"BL",20,45,2,"MEDIUM","noodle","banh_pho:250 cu_cai:150 carot:80 nam_huong:50 dauhu_chien:100 cai_ngot:80 hanh_la:15 hat_nem:8 nuoc_tuong:10 muoi:4"),
"banh_xeo_chay":(C_FRY,"LD",30,20,2,"HARD","fry","bot_gao:150 nghe:5 coconut:100 dauhu:150 nam_bn:100 hanh_la:20 dau_oil:40 xa_lach:100 nuoc_mam:25 duong:15 ot:5"),
"com_tam_chay":(C_RICE,"LD",30,25,2,"MEDIUM","rice","gao:250 suon:100 chalua:80 dua_leo:80 ca_chua:60 hanh_la:20 dau_oil:20 nuoc_mam:30 duong:15 toi:10 ot:5:o tieu:1"),
"banh_canh_chay":(C_NOODLE,"LD",15,30,2,"MEDIUM","noodle","bot_gao:150 bot_nang:50 nam_rom:100 dauhu_chien:100 carot:80 ngo_ri:5 hanh_la:10 nuoc_tuong:15 muoi:5 dau_oil:10"),
"dau_hu_kho_nam":(C_KHO,"LD",10,25,2,"EASY","braise","dauhu_chien:250 nam_rom:150 nuoc_tuong:25 duong:10 tieu:2 hanh_tim:15 dau_oil:10 ot:5:o"),
"ca_ri_chay":(C_KHO,"LD",20,40,2,"MEDIUM","braise","khoai_lang:200 khoai_tay:150 carot:100 dauhu_chien:150 bot_cari:15 coconut:150 sa:20 ot:5 hanh_tay:60 dau_oil:15 muoi:4 duong:5"),
"cha_gio_chay":(C_FRY,"SD",40,20,3,"MEDIUM","fry","banh_trang:80 khoai_mon:150 carot:80 mien:40 moc_nhi:10 nam_huong:40 dauhu:100 dau_oil:120 hat_nem:5 tieu:2 muoi:3 hanh_la:15"),
"banh_mi_chay":(C_BAKE,"BL",15,10,2,"EASY","sandwich","bot_mi:160 chalua:100 dua_leo:80 carot:40 ngo_ri:10 nuoc_tuong:15 dau_oil:10 tuong_hot:20 ot:3:o"),
"xoi_dau_xanh":(C_RICE,"B",20,40,2,"MEDIUM","sticky","dau_xanh:100 gao_nep:250 hanh_tim:20 dau_oil:15 muoi:3 coconut:50:o"),
"com_gao_lut_muoi_me":(C_RICE,"LD",5,45,2,"EASY","rice","gao_lut:250 me_trang:15 me_den:10 muoi:5"),
"mi_xao_chay":(C_XAO,"LD",15,10,2,"EASY","stirfry","mi:200 carot:60 broccoli:80 nam_bn:80 dauhu_chien:100 hanh_tay:40 nuoc_tuong:20 dau_hao:20 dau_oil:15 toi:10"),
"lau_nam_chay":(C_HOT,"D",25,30,4,"MEDIUM","hotpot","nam_rom:120 nam_bn:120 nam_kc:100 nam_dg:100 dauhu:200 cai_thia:150 carot:80 bap_cai:100 bun:200 gung:20 hat_nem:8 muoi:5 ca_chua:150"),
"bun_mang_chay":(C_NOODLE,"L",20,60,2,"MEDIUM","noodle","bun:250 mang:200 dauhu_chien:100 nam_huong:40 sa:20 rau_ram:15 hat_nem:8 nuoc_tuong:15 muoi:4 ot:5:o"),
"bun_moc_chay":(C_NOODLE,"BL",30,45,2,"HARD","noodle","bun:250 chalua:120 nam_huong:40 moc_nhi:8 dauhu:150 ca_chua:100 hanh_la:15 hanh_tay:60 dau_oil:10 hat_nem:8 muoi:5"),
"banh_cuon_chay":(C_BAKE,"B",40,20,2,"HARD","steam","bot_gao:150 bot_nang:30 moc_nhi:10 nam_huong:60 dauhu:100 hanh_tim:20 dau_oil:20 nuoc_mam:25 duong:10 ot:3:o dua_leo:60"),
"com_nieu_nam_chay":(C_RICE,"LD",15,35,2,"MEDIUM","rice","gao:250 nam_rom:100 nam_dg:100 nam_huong:40 carot:50 nuoc_tuong:15 dau_oil:10 hat_nem:5 tieu:1 muoi:3"),
"dau_hu_chien_gion":(C_FRY,"SD",10,10,2,"EASY","fry","dauhu:350 bot_nang:40 dau_oil:80 muoi:3 nuoc_tuong:20 duong:8 ot:5:o"),
"tempeh_xao":(C_XAO,"LD",10,12,2,"EASY","stirfry","tempeh:250 sa:20 ot:8 hanh_tay:60 nuoc_tuong:20 dau_oil:20 duong:8 dau_phong:30"),
"com_dau_den_dau_phong":(C_RICE,"LD",10,50,2,"EASY","rice","gao:200 dau_den:80 dau_phong:60 me_den:10 dau_me:10 muoi:3 nuoc_tuong:15"),
"chao_dau_do":(C_RICE,"BD",10,60,2,"EASY","porridge","gao:100 dau_do:100 dau_phong:40 hat_dieu:30 duong:15 muoi:2"),
"sua_dau_nanh_oc_cho":(C_DRINK,"BS",15,20,2,"EASY","drink","dau_nanh:150 oc_cho:40 duong:20 hat_dieu:30 muoi:1"),
"dau_hu_nhoi_nam":(C_STEAM,"LD",25,25,2,"MEDIUM","steam","dauhu:350 nam_huong:60 nam_rom:80 carot:50 hanh_la:15 hat_nem:5 tieu:1 dau_me:10 nuoc_tuong:20 moc_nhi:8"),
"cha_dau_hu":(C_FRY,"LD",20,15,2,"MEDIUM","fry","dauhu:400 bot_nang:40 nam_huong:40 carot:60 hanh_la:20 dau_oil:60 hat_nem:5 tieu:1 muoi:3"),
"banh_bao_chay":(C_BAKE,"BS",40,20,3,"HARD","bake","bot_mi:300 nam_huong:60 moc_nhi:8 mien:30 carot:60 dauhu:100 hanh_tay:40 dau_oil:20 duong:25 muoi:3 hat_nem:5"),
"xoi_dau_phong":(C_RICE,"BS",15,40,2,"EASY","sticky","gao_nep:250 dau_phong:100 me_trang:15 duong:25 muoi:3 coconut:80"),
"khoai_lang_dau_hu":(C_XAO,"LD",10,20,2,"EASY","stirfry","khoai_lang:350 dauhu:250 dau_oil:20 nuoc_tuong:20 tieu:2 me_trang:10 hanh_la:15"),
"burger_dau_den":(C_BAKE,"LD",30,20,2,"MEDIUM","sandwich","dau_den:150 bot_mi:150 hanh_tay:50 ca_chua:80 xa_lach:40 dau_oil:20 nuoc_tuong:10 carot:50 tieu:1 muoi:3"),
"mi_can_xao":(C_XAO,"LD",10,12,2,"EASY","stirfry","mican:300 hanh_tay:60 ot:5 sa:20 broccoli:100 dau_oil:20 nuoc_tuong:25 duong:8 dau_phong:20"),
"goi_dau_phu_lac":(C_SALAD,"LS",20,5,2,"EASY","salad","dauhu_chien:200 dau_phong:80 carot:60 xa_lach:80 rau_ram:15 nuoc_mam:20 duong:10 me_trang:10 ot:3:o"),
"com_cuon_rong_bien":(C_ROLL,"LS",30,25,2,"MEDIUM","roll","gao:220 rong_bien:15 dauhu_chien:120 carot:60 dua_leo:80 me_trang:10 nuoc_tuong:15 duong:10 muoi:3"),
"sinh_to_chuoi_bo":(C_DRINK,"BS",5,0,1,"EASY","drink","chuoi:200 bo_sap:120 dau_phong:30 coconut:80 duong:10"),
"che_dau_xanh_nuoc_cot_dua":(C_SWEET,"S",15,40,3,"EASY","sweet","dau_xanh:150 coconut:150 duong:60 bot_nang:15 muoi:1"),
"banh_gio_chay":(C_BAKE,"BS",40,45,3,"HARD","steam","bot_gao:250 bot_nang:50 nam_huong:60 moc_nhi:8 dauhu:100 hanh_tim:30 dau_oil:30 hat_nem:5 tieu:2 muoi:3"),
"dau_hu_sot_nam_com_trang":(C_RICE,"LD",10,20,2,"EASY","rice","gao:250 dauhu_chien:250 nam_rom:100 nam_huong:40 nuoc_tuong:20 dau_hao:20 bot_nang:10 dau_oil:20 hanh_la:15 tieu:1 duong:8"),
"hu_tieu_xao_dau_hu":(C_XAO,"LD",15,10,2,"EASY","stirfry","banh_pho:250 dauhu_chien:150 cai_ngot:100 carot:60 nam_rom:80 nuoc_tuong:20 dau_hao:15 dau_oil:20 toi:10 tieu:1"),
}
# nhãn mục tiêu (từ Data/tools/dishes.json)
GOAL = {}
LOSE = ["tofu_tomato_sauce","goi_cuon_chay","canh_nam_rau_cu","canh_bi_dao_nam","goi_ngo_sen_chay","canh_chua_chay","dau_hu_hap_nam","rau_muong_xao","chao_yen_mach_rau_cu","sup_bi_do","nom_du_du_chay","canh_rau_ngot","ca_tim_nuong","mang_xao_nam","canh_kho_qua_chay","salad_rau_cu_dau_phu","bun_gao_lut_rau_cu","rau_cu_luoc_kho_quet","dau_bap_luoc","canh_cai_nam_rom"]
GAIN = ["dau_hu_chien_gion","tempeh_xao","com_dau_den_dau_phong","chao_dau_do","sua_dau_nanh_oc_cho","dau_hu_nhoi_nam","cha_dau_hu","banh_bao_chay","xoi_dau_phong","khoai_lang_dau_hu","burger_dau_den","mi_can_xao","goi_dau_phu_lac","com_cuon_rong_bien","sinh_to_chuoi_bo","che_dau_xanh_nuoc_cot_dua","banh_gio_chay","dau_hu_sot_nam_com_trang","hu_tieu_xao_dau_hu"]
for s in D:
    GOAL[s] = "LOSE_WEIGHT" if s in LOSE else ("GAIN_MUSCLE" if s in GAIN else "MAINTAIN")
GOAL_LABEL = {"LOSE_WEIGHT": "giảm cân", "MAINTAIN": "giữ cân", "GAIN_MUSCLE": "tăng cân"}
KCAL_BAND = {"LOSE_WEIGHT": (280, 400), "MAINTAIN": (450, 620), "GAIN_MUSCLE": (650, 880)}
NAME_VI = {
"tofu_tomato_sauce":"Đậu hũ sốt cà chua","goi_cuon_chay":"Gỏi cuốn chay","canh_nam_rau_cu":"Canh nấm rau củ","canh_bi_dao_nam":"Canh bí đao nấu nấm","goi_ngo_sen_chay":"Gỏi ngó sen chay","canh_chua_chay":"Canh chua chay","dau_hu_hap_nam":"Đậu hũ hấp nấm","rau_muong_xao":"Rau muống xào chay","chao_yen_mach_rau_cu":"Cháo yến mạch rau củ","sup_bi_do":"Súp bí đỏ","nom_du_du_chay":"Nộm đu đủ chay","canh_rau_ngot":"Canh rau ngót nấm","ca_tim_nuong":"Cà tím nướng mỡ hành chay","mang_xao_nam":"Măng xào nấm","canh_kho_qua_chay":"Canh khổ qua nhồi chay","salad_rau_cu_dau_phu":"Salad rau củ đậu phụ","bun_gao_lut_rau_cu":"Bún gạo lứt rau củ","rau_cu_luoc_kho_quet":"Rau củ luộc chấm kho quẹt chay","dau_bap_luoc":"Đậu bắp luộc chấm muối mè","canh_cai_nam_rom":"Canh cải nấm rơm","com_chien_chay":"Cơm chiên chay","bun_rieu_chay":"Bún riêu chay","pho_chay":"Phở chay","bun_hue_chay":"Bún Huế chay","mi_quang_chay":"Mì Quảng chay","hu_tieu_chay":"Hủ tiếu chay","banh_xeo_chay":"Bánh xèo chay","com_tam_chay":"Cơm tấm chay","banh_canh_chay":"Bánh canh chay","dau_hu_kho_nam":"Đậu hũ kho nấm","ca_ri_chay":"Cà ri chay","cha_gio_chay":"Chả giò chay","banh_mi_chay":"Bánh mì chay","xoi_dau_xanh":"Xôi đậu xanh","com_gao_lut_muoi_me":"Cơm gạo lứt muối mè","mi_xao_chay":"Mì xào rau củ chay","lau_nam_chay":"Lẩu nấm chay","bun_mang_chay":"Bún măng chay","bun_moc_chay":"Bún mọc chay","banh_cuon_chay":"Bánh cuốn nhân nấm chay","com_nieu_nam_chay":"Cơm niêu nấm chay","dau_hu_chien_gion":"Đậu hũ chiên giòn","tempeh_xao":"Tempeh xào sả ớt","com_dau_den_dau_phong":"Cơm đậu đen đậu phộng","chao_dau_do":"Cháo đậu đỏ","sua_dau_nanh_oc_cho":"Sữa đậu nành óc chó","dau_hu_nhoi_nam":"Đậu hũ nhồi nấm hấp","cha_dau_hu":"Chả đậu hũ chiên","banh_bao_chay":"Bánh bao chay","xoi_dau_phong":"Xôi đậu phộng","khoai_lang_dau_hu":"Khoai lang áp chảo đậu hũ","burger_dau_den":"Burger đậu đen","mi_can_xao":"Mì căn xào chay","goi_dau_phu_lac":"Gỏi đậu phụ lạc rang","com_cuon_rong_bien":"Cơm cuộn rong biển chay","sinh_to_chuoi_bo":"Sinh tố chuối bơ đậu phộng","che_dau_xanh_nuoc_cot_dua":"Chè đậu xanh nước cốt dừa","banh_gio_chay":"Bánh giò chay","dau_hu_sot_nam_com_trang":"Cơm trắng đậu hũ sốt nấm","hu_tieu_xao_dau_hu":"Hủ tiếu xào đậu hũ"}

STEP_TPL = {
 "braise": ["Sơ chế {m1}, {m2}; cắt miếng vừa ăn.","Phi thơm gia vị với dầu ăn, cho {m1} vào áp chảo nhẹ.","Cho {m2} cùng gia vị, rim/kho lửa nhỏ 10–15 phút đến khi nước sốt sánh lại.","Nêm nếm lại cho vừa miệng, dọn nóng với cơm."],
 "soup": ["Rửa sạch, sơ chế {m1}, {m2}.","Đun sôi nước, cho các nguyên liệu cứng vào nấu trước.","Cho {m2} và rau vào, nêm gia vị chay, nấu thêm 3–5 phút.","Tắt bếp, rắc rau thơm, dùng nóng."],
 "stirfry": ["Sơ chế, cắt nhỏ {m1} và các nguyên liệu đi kèm.","Làm nóng dầu, phi thơm gia vị rồi cho {m1} vào xào lửa lớn.","Thêm {m2}, nêm nước tương/hạt nêm chay, đảo đều 3–5 phút.","Tắt bếp, rắc tiêu, dọn ngay khi còn nóng."],
 "steam": ["Sơ chế {m1}, {m2}; ướp gia vị 10 phút.","Xếp vào đĩa/khay chịu nhiệt.","Hấp cách thủy đến khi chín mềm.","Rưới nước sốt hoặc chấm kèm gia vị, dùng nóng."],
 "fry": ["Chuẩn bị, thấm khô/trộn {m1} cùng gia vị và bột.","Làm nóng dầu ở mức vừa.","Chiên {m1} đến khi vàng giòn hai mặt, vớt ra giấy thấm dầu.","Dọn kèm rau sống và nước chấm chay."],
 "grill": ["Sơ chế {m1}, quét dầu và gia vị.","Nướng lò/than ở nhiệt vừa đến khi chín mềm, xém nhẹ.","Chuẩn bị phần sốt/topping, rưới lên khi dọn."],
 "salad": ["Sơ chế, cắt sợi/lát {m1}, {m2}.","Trộn nước chấm chay (nước mắm chay, đường, ớt).","Trộn đều các nguyên liệu với nước chấm, rắc đậu phộng/mè rang.","Dùng ngay để rau giữ độ giòn."],
 "roll": ["Chuẩn bị {m1}, {m2} và rau sống.","Nhúng/làm mềm vỏ cuốn, xếp nguyên liệu lên.","Cuốn chặt tay, xếp ra đĩa.","Dùng kèm nước chấm chay."],
 "noodle": ["Hầm/nấu nước dùng chay từ rau củ và nấm.","Chần {m1} và nguyên liệu ăn kèm.","Xếp vào tô, chan nước dùng nóng.","Thêm rau thơm, gia vị, dùng ngay."],
 "rice": ["Vo gạo, nấu cùng nước theo tỉ lệ thích hợp.","Chuẩn bị phần ăn kèm từ {m2}.","Xới cơm ra tô, đặt phần ăn kèm lên trên.","Dùng nóng."],
 "porridge": ["Nấu {m1} với nước đến khi nhừ.","Cho {m2} đã sơ chế vào nấu chung.","Nêm nhạt, khuấy đều đến độ sánh mong muốn.","Dùng nóng."],
 "sticky": ["Ngâm nguyên liệu khô qua đêm, để ráo.","Hấp cách thủy đến khi chín dẻo.","Trộn gia vị, phần nhân/topping.","Dùng khi còn ấm."],
 "sandwich": ["Chuẩn bị {m1}, rau củ và nước sốt.","Nướng/làm nóng phần vỏ bánh.","Xếp nhân và rau lên bánh, thêm sốt.","Dùng ngay."],
 "bake": ["Nhào bột, ủ nghỉ theo hướng dẫn.","Chuẩn bị nhân từ {m1}, {m2} xào chín, để nguội.","Chia bột, gói nhân, tạo hình.","Hấp/nướng đến khi chín, dùng nóng."],
 "hotpot": ["Hầm nước lẩu chay từ rau củ, nấm.","Bày {m1}, rau và bún ra đĩa.","Bắc nồi lẩu lên bếp, nhúng nguyên liệu theo thứ tự.","Dùng nóng kèm nước chấm chay."],
 "drink": ["Ngâm/sơ chế {m1}.","Xay nhuyễn cùng {m2} và nước.","Lọc/nêm vị, đun sôi hoặc làm lạnh tùy món.","Dùng ngay."],
 "sweet": ["Ngâm và nấu {m1} đến khi chín mềm.","Nấu phần nước đường/nước cốt dừa.","Phối hợp, nêm vị ngọt vừa phải.","Dùng nóng hoặc lạnh."],
}

# ---------- văn bản mẫu ----------
HO = ["Nguyễn","Trần","Lê","Phạm","Hoàng","Huỳnh","Phan","Vũ","Võ","Đặng","Bùi","Đỗ","Hồ","Ngô","Dương","Lý"]
DEM_NAM = ["Văn","Hữu","Quang","Minh","Đức","Thanh","Anh","Gia","Tuấn","Hoàng"]
DEM_NU = ["Thị","Ngọc","Thu","Mai","Phương","Khánh","Bảo","Thanh","Diễm","Hồng"]
TEN_NAM = ["Hùng","Dũng","Khoa","Nam","Phúc","Long","Tâm","Bình","Sơn","Khải","Đạt","Huy","Tín","Thịnh","Việt","Lộc"]
TEN_NU = ["Lan","Hương","Linh","Trang","Thảo","Ngân","Vy","Hà","Mai","Chi","Yến","Nhi","Quỳnh","Trâm","Oanh","An"]

POST_TITLES = ["Chia sẻ cách nấu {d} ngon tại nhà","Mình vừa thử {d} – kết quả bất ngờ!","Thực đơn tuần này của mình có {d}","Hỏi mẹo làm {d} không bị nhạt","{d}: phiên bản ít dầu mỡ cho người ăn kiêng","Bữa tối nhanh gọn với {d}","Review quán chay mới mở gần nhà","Mẹo đi chợ chay tiết kiệm cho cả tuần","Hành trình ăn chay 30 ngày của mình","Cách bảo quản nấm và rau củ trong tủ lạnh"]
POST_BODIES = ["Hôm nay mình thử nấu {d} theo cách đơn giản: sơ chế sạch, nêm vừa miệng và nấu lửa nhỏ. Cả nhà ăn rất hợp khẩu vị, bạn nào đã thử thì chia sẻ thêm nhé!","Mình ăn chay được vài tuần và thấy {d} là món dễ làm, no lâu mà không ngán. Mình dùng đậu hũ và nấm để đủ đạm, kèm nhiều rau xanh.","Ai có mẹo làm {d} đậm vị hơn mà không dùng nhiều bột nêm không? Mình đang theo dõi calo nên muốn món nhẹ nhàng, dễ tiêu.","Cuối tuần rảnh nên mình chuẩn bị sẵn nguyên liệu cho {d}. Chia nhỏ từng phần, bảo quản ngăn mát, nấu 15 phút là xong bữa.","Chia sẻ lại kinh nghiệm: chọn đậu hũ tươi, nấm còn nguyên mũ, rau củ theo mùa thì {d} ngon và rẻ hơn hẳn."]
COMMENT_TXT = ["Cảm ơn bạn đã chia sẻ, mình sẽ thử cuối tuần này!","Món này nhìn hấp dẫn quá, cho mình xin định lượng nhé.","Mình làm theo rồi, cả nhà khen ngon.","Bạn có thể thay đậu hũ bằng tempeh được không?","Tuyệt vời, đúng món mình đang tìm cho thực đơn giảm cân.","Mình ăn chay trường nên bỏ hành tỏi, vẫn ngon bạn nhé.","Công thức rõ ràng, dễ làm, 5 sao!","Nấu xong mình thấy hơi nhạt, chắc cần thêm chút nước tương.","Hay quá, mình đã lưu lại để làm tối nay.","Cho mình hỏi món này bao nhiêu calo một phần vậy bạn?","Mình thêm chút nấm hương, thơm hơn nhiều.","Ảnh đẹp quá, bạn dùng nguyên liệu mua ở đâu vậy?"]
SPAM_TXT = ["Mua thực phẩm chức năng giảm cân siêu tốc, inbox ngay để nhận ưu đãi!!!","Kiếm tiền online tại nhà 500k/ngày, liên hệ zalo ngay","Link khuyến mãi cực sốc, bấm vào để nhận quà miễn phí"]
HATE_TXT = ["Mấy người ăn chay đúng là lố bịch, đáng bị chê cười.","Bài viết nhảm nhí, người viết chắc không có não.","Cái đồ dở hơi, đăng bài làm gì cho mệt."]
MEAT_POST = ["Công thức chay đặc biệt: thêm chút thịt băm cho đậm vị","Chả giò chay… nhưng nhân tôm thịt cho ngon","Nước lèo phở chay hầm xương cho ngọt"]
LABEL_POST = ["Sản phẩm này ghi 'thuần chay' nhưng có gelatin?","Bánh nhãn thuần chay nhưng thành phần có trứng sữa"]

CHAT_QA = [
 ("Mình ăn chay thì nên bổ sung đạm từ nguồn nào?","Bạn có thể lấy đạm từ đậu hũ, tempeh, các loại đậu (đậu nành, đậu lăng, đậu gà), hạt và ngũ cốc nguyên hạt. Kết hợp nhiều nguồn trong ngày giúp đủ các axit amin cần thiết."),
 ("Gợi ý thực đơn giảm cân cho một ngày ăn chay?","Bạn có thể ăn sáng với cháo yến mạch rau củ, trưa với cơm gạo lứt và đậu hũ sốt cà chua, tối với canh nấm rau củ. Tổng năng lượng nên bám theo mục tiêu calo trong hồ sơ của bạn."),
 ("Mình bị dị ứng đậu phộng thì nên tránh món nào?","Bạn nên tránh các món có đậu phộng rang, bơ đậu phộng, nước chấm có lạc và kiểm tra nhãn sản phẩm. Hãy khai báo dị ứng trong hồ sơ để hệ thống lọc công thức phù hợp."),
 ("Ăn chay trường có được ăn hành tỏi không?","Theo trường phái Phật giáo, ngũ vị tân (hành, tỏi, hẹ, kiệu, boa-rô) thường được kiêng. Bạn có thể bật tùy chọn không ngũ vị tân để nhận công thức phù hợp."),
 ("Tăng cân khi ăn chay bằng cách nào?","Hãy tăng khẩu phần năng lượng bằng các món giàu đạm và chất béo tốt như đậu hũ chiên, tempeh, hạt, khoai lang, kết hợp tập luyện kháng lực và ăn thêm bữa phụ."),
 ("Đậu hũ và tempeh khác nhau thế nào?","Đậu hũ làm từ sữa đậu nành đông tụ, mềm và nhẹ. Tempeh làm từ đậu nành lên men nên đặc, nhiều chất xơ và có vị bùi, thường giàu đạm hơn trên cùng khối lượng."),
 ("Tủ lạnh mình có nấm và cà chua, nấu gì nhanh?","Bạn có thể nấu canh nấm cà chua hoặc đậu hũ sốt cà chua nấm trong khoảng 15–20 phút, ăn kèm cơm gạo lứt cho đủ chất."),
 ("Mình nên ăn bao nhiêu calo mỗi ngày?","Con số phụ thuộc vào chiều cao, cân nặng, tuổi và mục tiêu. Bạn có thể xem mức calo mục tiêu trong hồ sơ sức khỏe; hệ thống tính từ BMR và hệ số vận động của bạn."),
]
SURVEY_Q = ["Bạn có hài lòng với câu trả lời của trợ lý dinh dưỡng AI?","Gợi ý thực đơn có phù hợp với mục tiêu của bạn không?","Bạn đánh giá thế nào về tính năng quét tủ lạnh?"]
SURVEY_FB = ["Gợi ý hữu ích, dễ làm theo.","Cần thêm nhiều món miền Trung.","Trả lời nhanh và dễ hiểu.","Một số món hơi nhiều dầu so với mục tiêu giảm cân.","Rất tiện, mong có thêm công thức cho người ăn chay trường.",None,None]

SWAP_REASONS = ["Không có nguyên liệu","Muốn đổi vị","Giảm calo","Tăng đạm","Món cũ đã ăn hôm trước","Không hợp khẩu vị"]
PLAN_NAMES = {"LOSE_WEIGHT":"Thực đơn giảm cân","MAINTAIN":"Thực đơn giữ cân","GAIN_MUSCLE":"Thực đơn tăng cân"}

# ---------- Sản phẩm (barcode sinh có check digit) ----------
# (tên, thương hiệu, thành phần, [E-number], [(allergen code, presence)])
PRODUCTS = [
 ("Mì ăn liền chay vị nấm","Lá Xanh","Bột mì, dầu cọ, muối, đường, bột nấm hương, chất điều vị (E621), chất chống oxy hóa (E330)",["E621","E330"],[("GLUTEN","CONTAINS"),("SOY","MAY_CONTAIN")]),
 ("Nước tương đậu nành lên men tự nhiên","Thiện Tâm","Nước, đậu nành, muối, đường, chất bảo quản (E211)",["E211"],[("SOY","CONTAINS"),("GLUTEN","MAY_CONTAIN")]),
 ("Sữa đậu nành nguyên chất","An Nhiên","Nước, đậu nành, đường, muối, chất ổn định (E415)",["E415"],[("SOY","CONTAINS")]),
 ("Đậu hũ non đóng hộp","Hoa Sen","Nước, đậu nành, chất đông tụ (E516)",["E516"],[("SOY","CONTAINS")]),
 ("Bánh quy yến mạch hạt điều","Lá Xanh","Bột mì, yến mạch, hạt điều, đường, dầu thực vật, bột nở (E500)",["E500"],[("GLUTEN","CONTAINS"),("TREE_NUT","CONTAINS")]),
 ("Kẹo dẻo trái cây","Vui Vẻ","Đường, siro glucose, gelatin (E441), chất tạo màu (E120), axit (E330)",["E441","E120","E330"],[]),
 ("Bánh bông lan trứng sữa","Ngọt Lành","Bột mì, trứng, đường, sữa, chất nhũ hóa (E471), bột nở (E500)",["E471","E500"],[("EGG","CONTAINS"),("DAIRY","CONTAINS"),("GLUTEN","CONTAINS")]),
 ("Sô cô la đen 70%","Cacao Việt","Bột cacao, đường, bơ cacao, chất nhũ hóa lecithin hướng dương (E322)",["E322"],[("DAIRY","MAY_CONTAIN")]),
 ("Sốt mayonnaise thuần chay","Thiện Tâm","Dầu hướng dương, nước, giấm, đường, muối, chất làm đặc (E412), chất bảo quản (E202)",["E412","E202"],[("MUSTARD","MAY_CONTAIN")]),
 ("Chả lụa chay","An Nhiên","Đậu nành, bột mì, dầu thực vật, gia vị, chất điều vị (E621), chất ổn định (E407)",["E621","E407"],[("SOY","CONTAINS"),("GLUTEN","CONTAINS")]),
 ("Nước ép cam không đường","Vui Vẻ","Nước cam ép, vitamin C (E300), chất điều chỉnh độ acid (E330)",["E300","E330"],[]),
 ("Snack khoai tây vị phô mai","Ngọt Lành","Khoai tây, dầu cọ, bột phô mai, muối, chất điều vị (E627, E631), hương liệu",["E627","E631"],[("DAIRY","CONTAINS")]),
 ("Bột ngũ cốc dinh dưỡng","Hạt Vàng","Yến mạch, gạo lứt, đậu nành, óc chó, mè đen",[],[("SOY","CONTAINS"),("TREE_NUT","CONTAINS"),("SESAME","CONTAINS")]),
 ("Bánh gạo vị rong biển","Lá Xanh","Gạo, rong biển, dầu thực vật, muối, đường, chất điều vị (E621)",["E621"],[("SOY","MAY_CONTAIN")]),
 ("Thạch rau câu dừa","Hoa Sen","Nước cốt dừa, đường, bột rau câu (E406), chất ổn định (E410)",["E406","E410"],[]),
 ("Kem que vị vani","Mát Lạnh","Sữa, đường, kem, chất ổn định (E412), hương vani, chất tạo màu (E160a)",["E412","E160a"],[("DAIRY","CONTAINS")]),
 ("Tương ớt chay","Thiện Tâm","Ớt, đường, muối, giấm, chất bảo quản (E211), chất tạo đặc (E415)",["E211","E415"],[]),
 ("Bánh mì sandwich nguyên cám","Hạt Vàng","Bột mì nguyên cám, nước, men, đường, muối, chất xử lý bột (E300), chất nhũ hóa (E472e)",["E300","E472e"],[("GLUTEN","CONTAINS"),("SOY","MAY_CONTAIN")]),
 ("Nước mắm chay dứa nấm","An Nhiên","Nước, dứa, nấm rơm, muối, đường",[],[]),
 ("Hạt điều rang muối","Hạt Vàng","Hạt điều, muối, dầu thực vật",[],[("TREE_NUT","CONTAINS")]),
 ("Sữa chua trái cây","Mát Lạnh","Sữa, men sữa chua, đường, trái cây, chất làm đặc (E440)",["E440"],[("DAIRY","CONTAINS")]),
 ("Bột nêm nấm hương chay","Lá Xanh","Muối, đường, bột nấm hương, chất điều vị (E621, E627), tinh bột",["E621","E627"],[]),
 ("Dầu ăn đậu nành","An Nhiên","Dầu đậu nành tinh luyện, chất chống oxy hóa (E300)",["E300"],[("SOY","MAY_CONTAIN")]),
 ("Bánh quy bơ","Ngọt Lành","Bột mì, bơ, đường, trứng, bột nở (E500), hương vani",["E500"],[("GLUTEN","CONTAINS"),("DAIRY","CONTAINS"),("EGG","CONTAINS")]),
 ("Trà gạo lứt đậu đen","Hạt Vàng","Gạo lứt, đậu đen rang",[],[]),
 ("Nước dừa tươi đóng hộp","Mát Lạnh","Nước dừa, vitamin C (E300)",["E300"],[]),
 ("Kẹo gừng","Vui Vẻ","Đường, siro, gừng, chất tạo màu (E150a), chất phủ bóng (E904)",["E150a","E904"],[]),
 ("Phô mai thuần chay hạt điều","Cacao Việt","Hạt điều, dầu dừa, men dinh dưỡng, muối, chất làm đặc (E412)",["E412"],[("TREE_NUT","CONTAINS")]),
 ("Bánh bao chay nhân nấm","An Nhiên","Bột mì, nấm hương, mộc nhĩ, miến, dầu thực vật, bột nở (E500), chất điều vị (E621)",["E500","E621"],[("GLUTEN","CONTAINS"),("SOY","MAY_CONTAIN")]),
 ("Đậu phộng rang tỏi ớt","Hạt Vàng","Đậu phộng, bột mì, tỏi, ớt, muối, chất điều vị (E621)",["E621"],[("PEANUT","CONTAINS"),("GLUTEN","CONTAINS")]),
 ("Bột cà ri chay","Thiện Tâm","Nghệ, ngò, thì là, ớt, quế, chất tạo màu (E100)",["E100"],[("MUSTARD","MAY_CONTAIN"),("CELERY","MAY_CONTAIN")]),
 ("Bánh tráng cuốn","Hoa Sen","Bột gạo, bột năng, muối, nước",[],[]),
 ("Sữa hạt óc chó","An Nhiên","Nước, óc chó, đường, chất ổn định (E412)",["E412"],[("TREE_NUT","CONTAINS")]),
 ("Kẹo marshmallow","Ngọt Lành","Đường, siro glucose, gelatin (E441), tinh bột bắp, hương vani",["E441"],[]),
 ("Bánh quy mè đen","Hạt Vàng","Bột mì, mè đen, dầu thực vật, đường, bột nở (E500)",["E500"],[("GLUTEN","CONTAINS"),("SESAME","CONTAINS")]),
 ("Nước tương chay vị nấm","Thiện Tâm","Nước, đậu nành, nấm hương, muối, chất điều vị (E631)",["E631"],[("SOY","CONTAINS")]),
 ("Bột pudding vani","Ngọt Lành","Đường, tinh bột bắp, bột sữa, chất tạo màu (E160a), chất nhũ hóa (E471)",["E160a","E471"],[("DAIRY","CONTAINS")]),
 ("Miến dong chay","Lá Xanh","Tinh bột dong riềng",[],[]),
 ("Mứt dâu tằm","Vui Vẻ","Dâu tằm, đường, chất làm đặc pectin (E440), chất tạo màu (E163)",["E440","E163"],[]),
 ("Bánh snack rong biển chiên","Lá Xanh","Rong biển, bột gạo, dầu thực vật, đường, muối, chất điều vị (E621, E627)",["E621","E627"],[("SESAME","MAY_CONTAIN")]),
]

# ---------- Quán chay (dữ liệu giả lập, không phải doanh nghiệp thật) ----------
# (tên, địa chỉ, lat, lng, rating, giờ mở, giá, tiện ích, fully_vegan, [category names])
VENUES = [
 ("Chay An Nhiên Quận 1","12 Nguyễn Thị Minh Khai, Quận 1, TP. Hồ Chí Minh",10.7867,106.6987,4.6,"07:00 - 21:30","40.000đ - 120.000đ",["Wifi","Máy lạnh"],1,[C_RICE,C_NOODLE]),
 ("Quán Chay Hoa Sen Phú Nhuận","85 Phan Xích Long, Phú Nhuận, TP. Hồ Chí Minh",10.8004,106.6869,4.4,"06:30 - 21:00","35.000đ - 90.000đ",["Chỗ đỗ xe máy"],1,[C_NOODLE,C_KHO]),
 ("Thiện Tâm Vegetarian Bình Thạnh","47 Điện Biên Phủ, Bình Thạnh, TP. Hồ Chí Minh",10.7992,106.7103,4.5,"10:00 - 21:30","60.000đ - 180.000đ",["Wifi","Máy lạnh","Phòng riêng"],1,[C_HOT,C_XAO]),
 ("Lá Xanh Kitchen Quận 3","220 Võ Văn Tần, Quận 3, TP. Hồ Chí Minh",10.7746,106.6862,4.7,"08:00 - 22:00","70.000đ - 200.000đ",["Wifi","Máy lạnh","Thanh toán thẻ"],1,[C_SALAD,C_GRILL]),
 ("Cơm Chay Vạn Hạnh Quận 10","310 Sư Vạn Hạnh, Quận 10, TP. Hồ Chí Minh",10.7716,106.6700,4.3,"06:00 - 14:00","25.000đ - 60.000đ",["Chỗ đỗ xe máy"],1,[C_RICE]),
 ("Bếp Chay Tân Bình","95 Hoàng Văn Thụ, Tân Bình, TP. Hồ Chí Minh",10.7993,106.6564,4.2,"10:00 - 20:30","45.000đ - 110.000đ",["Máy lạnh"],1,[C_KHO,C_SOUP]),
 ("Chay Ngọc Hoa Gò Vấp","18 Quang Trung, Gò Vấp, TP. Hồ Chí Minh",10.8329,106.6665,4.1,"09:00 - 21:00","30.000đ - 80.000đ",["Chỗ đỗ xe máy","Giao hàng"],1,[C_NOODLE,C_FRY]),
 ("Quán Chay Sala Thủ Đức","66 Võ Văn Ngân, Thủ Đức, TP. Hồ Chí Minh",10.8505,106.7586,4.5,"09:30 - 21:30","50.000đ - 150.000đ",["Wifi","Sân vườn"],1,[C_BAKE,C_SWEET]),
 ("Vegan Garden Quận 7","101 Nguyễn Thị Thập, Quận 7, TP. Hồ Chí Minh",10.7387,106.7219,4.6,"10:00 - 22:00","80.000đ - 220.000đ",["Wifi","Máy lạnh","Chỗ đỗ ô tô"],1,[C_SALAD,C_DRINK]),
 ("Chay Tịnh Tâm Quận 5","29 Nguyễn Trãi, Quận 5, TP. Hồ Chí Minh",10.7545,106.6672,4.0,"06:00 - 19:00","25.000đ - 70.000đ",["Chỗ đỗ xe máy"],1,[C_RICE,C_STEAM]),
 ("Chay Tâm An Hoàn Kiếm","14 Hàng Bài, Hoàn Kiếm, Hà Nội",21.0234,105.8525,4.6,"07:00 - 21:30","45.000đ - 130.000đ",["Wifi","Máy lạnh"],1,[C_NOODLE,C_RICE]),
 ("Quán Chay Liên Hoa Ba Đình","55 Kim Mã, Ba Đình, Hà Nội",21.0316,105.8128,4.4,"08:00 - 21:00","40.000đ - 100.000đ",["Chỗ đỗ xe máy"],1,[C_KHO,C_XAO]),
 ("Bếp Chay Hà Thành","72 Tây Sơn, Đống Đa, Hà Nội",21.0098,105.8226,4.3,"10:00 - 21:30","50.000đ - 140.000đ",["Máy lạnh","Giao hàng"],1,[C_HOT,C_SOUP]),
 ("Lotus Vegetarian Cầu Giấy","8 Trần Thái Tông, Cầu Giấy, Hà Nội",21.0345,105.7897,4.7,"09:00 - 22:00","70.000đ - 190.000đ",["Wifi","Máy lạnh","Thanh toán thẻ"],1,[C_SALAD,C_GRILL]),
 ("Chay Phố Cổ","31 Hàng Gai, Hoàn Kiếm, Hà Nội",21.0309,105.8508,4.2,"08:00 - 20:30","35.000đ - 95.000đ",["Chỗ ngồi ngoài trời"],1,[C_NOODLE]),
 ("Cơm Chay Thanh Xuân","120 Nguyễn Trãi, Thanh Xuân, Hà Nội",20.9944,105.8132,4.1,"06:30 - 20:00","30.000đ - 75.000đ",["Chỗ đỗ xe máy"],1,[C_RICE,C_KHO]),
 ("Chay Tâm Đức Hai Bà Trưng","19 Bạch Mai, Hai Bà Trưng, Hà Nội",21.0058,105.8505,4.4,"07:30 - 21:00","40.000đ - 110.000đ",["Máy lạnh"],1,[C_STEAM,C_SOUP]),
 ("Quán Chay Hương Sen Hải Châu","45 Lê Duẩn, Hải Châu, Đà Nẵng",16.0678,108.2208,4.5,"07:00 - 21:00","35.000đ - 100.000đ",["Máy lạnh","Chỗ đỗ xe máy"],1,[C_NOODLE,C_RICE]),
 ("Chay Biển Xanh Sơn Trà","88 Võ Nguyên Giáp, Sơn Trà, Đà Nẵng",16.0544,108.2476,4.3,"09:00 - 21:30","50.000đ - 150.000đ",["Wifi","Gần biển"],1,[C_SALAD,C_BAKE]),
 ("Bếp Chay Ngũ Hành Sơn","12 Huỳnh Thúc Kháng, Ngũ Hành Sơn, Đà Nẵng",16.0214,108.2481,4.1,"08:00 - 20:30","30.000đ - 90.000đ",["Chỗ đỗ ô tô"],1,[C_KHO]),
 ("Cơm Chay Liên Hoa Huế","25 Lê Lợi, TP. Huế",16.4637,107.5909,4.6,"06:30 - 20:30","30.000đ - 100.000đ",["Sân vườn"],1,[C_RICE,C_NOODLE]),
 ("Quán Chay Tịnh Gia Huế","14 Phan Bội Châu, TP. Huế",16.4600,107.5857,4.4,"07:00 - 20:00","35.000đ - 95.000đ",["Chỗ đỗ xe máy"],1,[C_NOODLE,C_FRY]),
 ("Chay Tâm Giao Ninh Kiều","30 Hòa Bình, Ninh Kiều, Cần Thơ",10.0337,105.7878,4.3,"07:00 - 21:00","30.000đ - 90.000đ",["Máy lạnh"],1,[C_RICE,C_SOUP]),
 ("Quán Chay Miệt Vườn Cái Răng","77 Võ Nguyên Giáp, Cái Răng, Cần Thơ",10.0125,105.7469,4.2,"08:00 - 20:30","30.000đ - 85.000đ",["Sân vườn","Chỗ đỗ ô tô"],0,[C_KHO,C_GRILL]),
 ("Nhà Hàng Chay Bình Dân Quận 4","60 Khánh Hội, Quận 4, TP. Hồ Chí Minh",10.7578,106.7027,4.0,"10:00 - 20:00","40.000đ - 100.000đ",["Máy lạnh"],0,[C_STEAM,C_HOT]),
 ("Buffet Chay Sen Vàng Hà Nội","5 Láng Hạ, Đống Đa, Hà Nội",21.0187,105.8123,4.5,"10:30 - 21:30","199.000đ buffet",["Buffet","Máy lạnh","Phòng riêng"],1,[C_HOT,C_SWEET]),
]
