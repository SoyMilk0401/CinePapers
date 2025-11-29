import requests
import json

def test_cgv_endpoint(name, url, params):
    print(f"--- [{name}] 테스트 시작 ---")
    print(f"Target URL: {url}")
    
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36',
        'Referer': 'https://cgv.co.kr/',
        'Accept': 'application/json',
        'Accept-Language': 'ko-KR'
    }

    try:
        response = requests.get(url, params=params, headers=headers, timeout=10)
        
        print(f"Status Code: {response.status_code}")
        
        if response.status_code == 200:
            try:
                data = response.json()
                print("응답 성공 (데이터 일부):")
                print(json.dumps(data, indent=2, ensure_ascii=False)[:500] + "\n... (생략)")
                print(">> 결론: x-signature 헤더 없이 호출 가능함")
            except json.JSONDecodeError:
                print("응답은 200이나 JSON 형식이 아님 (HTML 등일 수 있음)")
        else:
            print("요청 실패")
            print(f"에러 메시지: {response.text[:200]}")
            print(">> 결론: x-signature 등의 보안 헤더가 필수일 가능성 높음")
            
    except Exception as e:
        print(f"에러 발생: {e}")
    
    print("\n")

search_url = 'https://api.cgv.co.kr/tme/more/itgrSrch/searchItgrSrchAll'
search_params = {
    'coCd': 'A420',
    'swrd': '주토피아',
    'lmtSrchYn': 'Y'
}
test_cgv_endpoint("통합 검색", search_url, search_params)

event_list_url = 'https://event.cgv.co.kr/evt/evt/evt/searchEvtListForPage'
event_list_params = {
    'coCd': 'A420',
    'evntCtgryLclsCd': '01',
    'sscnsChoiYn': 'N',
    'expnYn': 'N',
    'expoChnlCd': '01',
    'startRow': '0',
    'listCount': '10'
}
test_cgv_endpoint("이벤트 리스트", event_list_url, event_list_params)

category_url = 'https://api.cgv.co.kr/com/bznsCom/user/searchComcdValList'
category_params = {
    'coCd': 'A420',
    'comCd': 'EVNT_CTGRY_LCLS_CD',
    'useYn': 'Y'
}
test_cgv_endpoint("이벤트 카테고리 코드", category_url, category_params)