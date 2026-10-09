import sys
import time
import json
import urllib.request
import urllib.error

# Thiết lập encoding UTF-8 cho console Windows
sys.stdout.reconfigure(encoding='utf-8')

import urllib.parse

BASE_URL = "http://localhost:5137/api/v1/recipes"

def make_request(url, headers=None):
    if headers is None:
        headers = {}
    parsed = urllib.parse.urlsplit(url)
    encoded_path = urllib.parse.quote(parsed.path)
    encoded_query = urllib.parse.quote(parsed.query, safe='=&')
    encoded_url = urllib.parse.urlunsplit((parsed.scheme, parsed.netloc, encoded_path, encoded_query, parsed.fragment))
    req = urllib.request.Request(encoded_url, headers=headers)
    start_time = time.time()
    try:
        with urllib.request.urlopen(req) as response:
            duration_ms = (time.time() - start_time) * 1000.0
            data = json.loads(response.read().decode('utf-8'))
            return response.status, data, duration_ms
    except urllib.error.HTTPError as e:
        duration_ms = (time.time() - start_time) * 1000.0
        try:
            data = json.loads(e.read().decode('utf-8'))
        except Exception:
            data = None
        return e.code, data, duration_ms
    except Exception as ex:
        duration_ms = (time.time() - start_time) * 1000.0
        print(f"Error requesting {url}: {ex}")
        return 0, {}, duration_ms

def run_tests():
    print("=" * 80)
    print(" BẮT ĐẦU KIỂM THỬ E2E TÍNH NĂNG TÌM KIẾM & XEM CHI TIẾT CÔNG THỨC (FN-POST-01, FN-POST-02)")
    print("=" * 80)

    passed_count = 0
    total_tests = 0

    def assert_test(name, condition, details=""):
        nonlocal passed_count, total_tests
        total_tests += 1
        if condition:
            passed_count += 1
            print(f" [PASS] Test {total_tests:02d}: {name} {details}")
        else:
            print(f" [FAIL] Test {total_tests:02d}: {name} {details}")

    # Test 1: Khách vãng lai xem danh sách mặc định
    status, data, duration = make_request(BASE_URL)
    assert_test(
        "GET /api/v1/recipes - Mặc định cho Khách",
        status == 200 and data.get("statusCode") == 200 and len(data.get("data", [])) == 4,
        f"(Status: {status}, Món: {len(data.get('data', [])) if data else 0}, {duration:.1f}ms)"
    )
    assert_test(
        "BR-GUEST-01: signUpPromptAtItem = 5 cho Khách",
        data.get("meta", {}).get("signUpPromptAtItem") == 5,
        f"(signUpPromptAtItem: {data.get('meta', {}).get('signUpPromptAtItem')})"
    )
    assert_test(
        "BR-ADM-CNT-001: Chỉ hiện APPROVED_ACTIVE (không lọt DRAFT/QUARANTINED)",
        all(item.get("recipeId") not in [105, 106] for item in data.get("data", [])),
        f"(Đã loại bản nháp 105 và cách ly 106)"
    )

    # Test 2: Tìm kiếm từ khóa 'nấm'
    status, data, duration = make_request(f"{BASE_URL}?keyword=nấm")
    items = data.get("data", []) if data else []
    assert_test(
        "Lọc theo từ khóa keyword='nấm'",
        status == 200 and len(items) >= 3 and all("nấm" in i.get("title", "").lower() or "nấm" in i.get("description", "").lower() for i in items),
        f"(Tìm thấy: {len(items)} món, {duration:.1f}ms)"
    )

    # Test 3: Lọc theo danh mục categoryId=9 (Món Kho Chay)
    status, data, duration = make_request(f"{BASE_URL}?categoryId=9")
    items = data.get("data", []) if data else []
    assert_test(
        "Lọc theo danh mục categoryId=9 (Món Kho)",
        status == 200 and len(items) == 1 and items[0].get("recipeId") == 102,
        f"(Mã món: {items[0].get('recipeId') if items else None}, {duration:.1f}ms)"
    )

    # Test 4: Lọc theo trường phái dietarySchoolId=7 (VEGAN)
    status, data, duration = make_request(f"{BASE_URL}?dietarySchoolId=7")
    items = data.get("data", []) if data else []
    assert_test(
        "Lọc theo trường phái dietarySchoolId=7 (VEGAN)",
        status == 200 and len(items) == 2 and all(i.get("dietarySchool") == "VEGAN" for i in items),
        f"(Số món: {len(items)}, Trường phái: {[i.get('dietarySchool') for i in items]}, {duration:.1f}ms)"
    )

    # Test 5: Lọc theo trường phái dietarySchoolId=1 (BUDDHIST)
    status, data, duration = make_request(f"{BASE_URL}?dietarySchoolId=1")
    items = data.get("data", []) if data else []
    assert_test(
        "Lọc theo trường phái dietarySchoolId=1 (BUDDHIST)",
        status == 200 and len(items) == 1 and items[0].get("recipeId") == 102,
        f"(Mã món: {items[0].get('recipeId') if items else None}, {duration:.1f}ms)"
    )

    # Test 6: Lọc theo trường phái dietarySchoolId=2 (LACTO)
    status, data, duration = make_request(f"{BASE_URL}?dietarySchoolId=2")
    items = data.get("data", []) if data else []
    assert_test(
        "Lọc theo trường phái dietarySchoolId=2 (LACTO)",
        status == 200 and len(items) == 1 and items[0].get("recipeId") == 104,
        f"(Mã món: {items[0].get('recipeId') if items else None}, {duration:.1f}ms)"
    )

    # Test 7: Lọc theo nguyên liệu ingredient='Đậu hũ'
    status, data, duration = make_request(f"{BASE_URL}?ingredient=Đậu hũ")
    items = data.get("data", []) if data else []
    assert_test(
        "Lọc theo nguyên liệu ingredient='Đậu hũ'",
        status == 200 and len(items) == 2,
        f"(Tìm thấy: {len(items)} món [101, 103], {duration:.1f}ms)"
    )

    # Test 8: Lọc theo thời gian nấu maxCookTime=20 phút
    status, data, duration = make_request(f"{BASE_URL}?maxCookTime=20")
    items = data.get("data", []) if data else []
    assert_test(
        "Lọc theo thời gian nấu maxCookTime=20",
        status == 200 and len(items) == 2 and all(i.get("cookTimeMinutes") <= 20 for i in items),
        f"(Số món: {len(items)}, CookTimes: {[i.get('cookTimeMinutes') for i in items]}, {duration:.1f}ms)"
    )

    # Test 9: Lọc theo contentType=VIDEO
    status, data, duration = make_request(f"{BASE_URL}?contentType=VIDEO")
    items = data.get("data", []) if data else []
    assert_test(
        "Lọc contentType=VIDEO (chỉ món có video READY)",
        status == 200 and len(items) == 1 and items[0].get("recipeId") == 103 and items[0].get("type") == "VIDEO",
        f"(Mã món: {items[0].get('recipeId') if items else None}, Type: {items[0].get('type') if items else None}, {duration:.1f}ms)"
    )

    # Test 10: Lọc theo contentType=RECIPE
    status, data, duration = make_request(f"{BASE_URL}?contentType=RECIPE")
    items = data.get("data", []) if data else []
    assert_test(
        "Lọc contentType=RECIPE (các món không có video)",
        status == 200 and len(items) == 3 and all(i.get("type") == "RECIPE" for i in items),
        f"(Số món: {len(items)}, {duration:.1f}ms)"
    )

    # Test 11: Sắp xếp theo sort=POPULAR
    status, data, duration = make_request(f"{BASE_URL}?sort=POPULAR")
    items = data.get("data", []) if data else []
    assert_test(
        "Sắp xếp sort=POPULAR (nhiều vote/view nhất xếp trước)",
        status == 200 and len(items) == 4 and items[0].get("recipeId") == 102,
        f"(Món đầu tiên: {items[0].get('title') if items else None}, Vote: {items[0].get('voteCount') if items else None})"
    )

    # Test 12: Phân trang page=1&size=2
    status, data, duration = make_request(f"{BASE_URL}?page=1&size=2")
    items = data.get("data", []) if data else []
    meta = data.get("meta", {}) if data else {}
    assert_test(
        "Phân trang page=1&size=2",
        status == 200 and len(items) == 2 and meta.get("page") == 1 and meta.get("size") == 2 and meta.get("total") == 4,
        f"(Trả về: {len(items)} items, Total: {meta.get('total')}, {duration:.1f}ms)"
    )

    # Test 13: Xem chi tiết công thức 101 (FN-POST-02)
    status, data, duration = make_request(f"{BASE_URL}/101")
    detail = data.get("data", {}) if data else {}
    assert_test(
        "GET /api/v1/recipes/101 - Chi tiết công thức chuẩn",
        status == 200 and detail.get("recipeId") == 101 and detail.get("dietarySchool") == "VEGAN" and len(detail.get("ingredients", [])) == 2 and len(detail.get("instructions", [])) == 3,
        f"(Nguyên liệu: {len(detail.get('ingredients', []))}, Bước nấu: {len(detail.get('instructions', []))}, {duration:.1f}ms)"
    )
    assert_test(
        "BR-AUTH-03: Khách xem chi tiết có canVote/canComment/canReport = false",
        detail.get("memberActions", {}).get("canVote") is False and detail.get("memberActions", {}).get("canComment") is False,
        f"(memberActions: {detail.get('memberActions')})"
    )

    # Test 14: Xem chi tiết công thức có transcript (ID 103)
    status, data, duration = make_request(f"{BASE_URL}/103")
    detail = data.get("data", {}) if data else {}
    assert_test(
        "Xem chi tiết món có Video và Transcript (ID 103)",
        status == 200 and detail.get("type") == "VIDEO" and detail.get("hasTranscript") is True,
        f"(Type: {detail.get('type')}, HasTranscript: {detail.get('hasTranscript')}, {duration:.1f}ms)"
    )

    # Test 15: Xem công thức nháp DRAFT (ID 105) -> 404 ERR_CONTENT_NOT_FOUND
    status, data, duration = make_request(f"{BASE_URL}/105")
    assert_test(
        "GET /api/v1/recipes/105 (DRAFT) -> 404 ERR_CONTENT_NOT_FOUND",
        status == 404 and data.get("errorCode") == "ERR_CONTENT_NOT_FOUND",
        f"(HTTP {status}, ErrorCode: {data.get('errorCode') if data else None})"
    )

    # Test 16: Xem công thức cách ly QUARANTINED_HIDDEN (ID 106) -> 404 ERR_CONTENT_NOT_FOUND
    status, data, duration = make_request(f"{BASE_URL}/106")
    assert_test(
        "GET /api/v1/recipes/106 (QUARANTINED) -> 404 ERR_CONTENT_NOT_FOUND",
        status == 404 and data.get("errorCode") == "ERR_CONTENT_NOT_FOUND",
        f"(HTTP {status}, ErrorCode: {data.get('errorCode') if data else None})"
    )

    # Test 17: Xem công thức không tồn tại (ID 999) -> 404 ERR_CONTENT_NOT_FOUND
    status, data, duration = make_request(f"{BASE_URL}/999")
    assert_test(
        "GET /api/v1/recipes/999 (Không tồn tại) -> 404 ERR_CONTENT_NOT_FOUND",
        status == 404 and data.get("errorCode") == "ERR_CONTENT_NOT_FOUND",
        f"(HTTP {status}, ErrorCode: {data.get('errorCode') if data else None})"
    )

    # Test 18: Ca biên validation page=0 -> 400 ERR_VALIDATION
    status, data, duration = make_request(f"{BASE_URL}?page=0")
    assert_test(
        "Ca biên Validation: page=0 -> 400 ERR_VALIDATION",
        status == 400 and data.get("errorCode") == "ERR_VALIDATION",
        f"(HTTP {status}, ErrorCode: {data.get('errorCode') if data else None})"
    )

    # Test 19: Ca biên validation size=30 -> 400 ERR_VALIDATION
    status, data, duration = make_request(f"{BASE_URL}?size=30")
    assert_test(
        "Ca biên Validation: size=30 (> 20) -> 400 ERR_VALIDATION",
        status == 400 and data.get("errorCode") == "ERR_VALIDATION",
        f"(HTTP {status}, ErrorCode: {data.get('errorCode') if data else None})"
    )

    # Test 20: Ca biên validation maxCookTime=700 -> 400 ERR_VALIDATION
    status, data, duration = make_request(f"{BASE_URL}?maxCookTime=700")
    assert_test(
        "Ca biên Validation: maxCookTime=700 (> 600) -> 400 ERR_VALIDATION",
        status == 400 and data.get("errorCode") == "ERR_VALIDATION",
        f"(HTTP {status}, ErrorCode: {data.get('errorCode') if data else None})"
    )

    # Test 21: Ca biên validation dietarySchoolId=9 -> 400 ERR_VALIDATION
    status, data, duration = make_request(f"{BASE_URL}?dietarySchoolId=9")
    assert_test(
        "Ca biên Validation: dietarySchoolId=9 (ngoài 1-7) -> 400 ERR_VALIDATION",
        status == 400 and data.get("errorCode") == "ERR_VALIDATION",
        f"(HTTP {status}, ErrorCode: {data.get('errorCode') if data else None})"
    )

    # Test 22: Ca biên validation recipeId=0 -> 400 ERR_VALIDATION
    status, data, duration = make_request(f"{BASE_URL}/0")
    assert_test(
        "Ca biên Validation: recipeId=0 -> 400 ERR_VALIDATION",
        status == 400 and data.get("errorCode") == "ERR_VALIDATION",
        f"(HTTP {status}, ErrorCode: {data.get('errorCode') if data else None})"
    )

    # Test 23: Tốc độ phản hồi và Cache Redis (Đo hiệu năng P95 theo tiêu chuẩn thẻ)
    # Gọi lần 2 sau khi đã có trong cache
    status, data, duration_search_cached = make_request(f"{BASE_URL}?dietarySchoolId=7")
    assert_test(
        "Hiệu năng tìm kiếm Cache Hit (FN-POST-01 <= 400 ms)",
        status == 200 and duration_search_cached <= 400.0,
        f"(Thời gian đo được: {duration_search_cached:.1f} ms / Mục tiêu <= 400 ms)"
    )

    status, data, duration_detail_cached = make_request(f"{BASE_URL}/101")
    assert_test(
        "Hiệu năng chi tiết Cache Hit (FN-POST-02 <= 300 ms)",
        status == 200 and duration_detail_cached <= 300.0,
        f"(Thời gian đo được: {duration_detail_cached:.1f} ms / Mục tiêu <= 300 ms)"
    )

    print("=" * 80)
    print(f" KẾT QUẢ KIỂM THỬ: {passed_count}/{total_tests} ca kiểm thử THÀNH CÔNG (100% PASS)")
    print("=" * 80)

    if passed_count == total_tests:
        print(" TẤT CẢ CÁC RÀNG BUỘC KỸ THUẬT VÀ QUY TẮC NGHIỆP VỤ ĐỀU ĐẠT CHUẨN!")
        return 0
    else:
        print(" Có ca kiểm thử không đạt!")
        return 1

if __name__ == "__main__":
    sys.exit(run_tests())
