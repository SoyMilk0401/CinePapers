import requests
from bs4 import BeautifulSoup
import json

def test_megabox_endpoint(name, category_code):
    print(f"--- [{name}] 테스트 시작 ---")
    
    url = "https://www.megabox.co.kr/on/oh/ohe/Event/eventMngDiv.do"
    
    # 메가박스는 JSON Payload를 사용하여 요청합니다.
    # recordCountPerPage를 조절하여 한 번에 가져올 개수를 정할 수 있습니다.
    payload = {
        "currentPage": "1",
        "recordCountPerPage": "10", 
        "eventStatCd": "ONG", # 진행중인 이벤트 (ONG)
        "eventTitle": "",
        "eventDivCd": category_code, # 카테고리 코드
        "eventTyCd": "",
        "orderReqCd": "ONGlist"
    }

    # 헤더 설정: Content-Type과 X-Requested-With가 중요합니다.
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36',
        'Content-Type': 'application/json;charset=UTF-8',
        'X-Requested-With': 'XMLHttpRequest',
        'Origin': 'https://www.megabox.co.kr',
        'Referer': 'https://www.megabox.co.kr/event'
    }

    try:
        response = requests.post(url, json=payload, headers=headers, timeout=10)
        
        print(f"Target URL: {url}")
        print(f"Status Code: {response.status_code}")
        
        if response.status_code == 200:
            # 응답이 HTML 형식이므로 BeautifulSoup으로 파싱하여 확인
            html_content = response.text
            soup = BeautifulSoup(html_content, 'html.parser')
            
            # 이벤트 제목(.tit) 추출 시도
            titles = soup.select("p.tit")
            
            if titles:
                print(f"응답 성공 (이벤트 {len(titles)}개 발견):")
                for i, title in enumerate(titles[:3]): # 상위 3개만 출력
                    print(f"  {i+1}. {title.get_text(strip=True)}")
                print("  ... (생략)")
                print(">> 결론: 정상 호출됨 (HTML 파싱 필요)")
            else:
                # 데이터가 없거나 구조가 바뀐 경우
                print("응답은 200이나 이벤트 리스트를 찾을 수 없음.")
                print("응답 내용 일부:", html_content[:200])
        else:
            print("요청 실패")
            print(f"에러 메시지: {response.text[:200]}")
            
    except Exception as e:
        print(f"에러 발생: {e}")
    
    print("\n")

# 1. 전체 이벤트 (메인 페이지 등)
# category_code를 비우면 전체 리스트가 오는 것으로 추정됨 (또는 기본값)
test_megabox_endpoint("전체 이벤트", "")

# 2. 영화 카테고리 테스트
# 출처: [이벤트 영화 카테고리 요청 mega .txt] -> "eventDivCd":"CED01"
test_megabox_endpoint("영화 카테고리 (CED01)", "CED01")

# 3. 메가Pick 카테고리 테스트
# 출처: [이벤트 메가Pick 카테고리 요청 mega .txt] -> "eventDivCd":"CED03"
test_megabox_endpoint("메가Pick 카테고리 (CED03)", "CED03")