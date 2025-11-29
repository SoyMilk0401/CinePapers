import requests
import json

def test_lotte_endpoint(name, payload_dict):
    print(f"--- [{name}] 테스트 시작 ---")
    
    url = "https://www.lottecinema.co.kr/LCWS/Event/EventData.aspx"
    
    param_list_json = json.dumps(payload_dict)
    
    files = {
        'paramList': (None, param_list_json)
    }

    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36',
        'Referer': 'https://www.lottecinema.co.kr/NLCHS/Event/DetailList?code=20',
        'Origin': 'https://www.lottecinema.co.kr'
    }

    try:
        response = requests.post(url, files=files, headers=headers, timeout=10)
        
        print(f"Target URL: {url}")
        print(f"Status Code: {response.status_code}")
        
        if response.status_code == 200:
            try:
                data = response.json()
                print("응답 성공 (데이터 일부):")
                print(json.dumps(data, indent=2, ensure_ascii=False)[:500] + "\n... (생략)")
                
                if data.get("IsOK") == "true":
                    print(">> 결론: 정상 호출됨 (별도 보안 헤더 불필요)")
                else:
                    print(f">> 결론: 호출은 됐으나 내부 오류 발생 ({data.get('ResultMessage')})")
                    
            except json.JSONDecodeError:
                print("응답은 200이나 JSON 형식이 아님")
                print(response.text[:200])
        else:
            print("요청 실패")
            print(f"에러 메시지: {response.text[:200]}")
            
    except Exception as e:
        print(f"에러 발생: {e}")
    
    print("\n")

common_params = {
    "channelType": "HO",
    "osType": "W",
    "osVersion": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
    "MemberNo": "0"
}

list_payload = common_params.copy()
list_payload.update({
    "MethodName": "GetEventLists",
    "EventClassificationCode": "20",
    "SearchText": "",
    "CinemaID": "",
    "PageNo": 1,
    "PageSize": 15
})
test_lotte_endpoint("이벤트 리스트 (영화)", list_payload)

main_payload = common_params.copy()
main_payload.update({
    "MethodName": "GetEventSummaryLists"
})
test_lotte_endpoint("이벤트 메인 요약", main_payload)


search_payload = common_params.copy()
search_payload.update({
    "MethodName": "GetEventLists",
    "EventClassificationCode": "20",
    "SearchText": "",
    "CinemaID": "",
    "PageNo": 1,
    "PageSize": 15
})
test_lotte_endpoint("이벤트 검색 ()", search_payload)