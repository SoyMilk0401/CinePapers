import requests
from bs4 import BeautifulSoup
import json

def test_megabox_endpoint(name, category_code):
    print(f"--- [{name}] 테스트 시작 ---")
    
    url = "https://www.megabox.co.kr/on/oh/ohe/Event/eventMngDiv.do"
    
    payload = {
        "currentPage": "1",
        "recordCountPerPage": "10", 
        "eventStatCd": "ONG",
        "eventTitle": "",
        "eventDivCd": category_code,
        "eventTyCd": "",
        "orderReqCd": "ONGlist"
    }

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
            html_content = response.text
            soup = BeautifulSoup(html_content, 'html.parser')
            
            titles = soup.select("p.tit")
            
            if titles:
                print(f"응답 성공 (이벤트 {len(titles)}개 발견):")
                for i, title in enumerate(titles[:3]):
                    print(f"  {i+1}. {title.get_text(strip=True)}")
                print("  ... (생략)")
                print(">> 결론: 정상 호출됨 (HTML 파싱 필요)")
            else:
                print("응답은 200이나 이벤트 리스트를 찾을 수 없음.")
                print("응답 내용 일부:", html_content[:200])
        else:
            print("요청 실패")
            print(f"에러 메시지: {response.text[:200]}")
            
    except Exception as e:
        print(f"에러 발생: {e}")
    
    print("\n")

test_megabox_endpoint("전체 이벤트", "")

test_megabox_endpoint("영화 카테고리 (CED01)", "CED01")

test_megabox_endpoint("메가Pick 카테고리 (CED03)", "CED03")