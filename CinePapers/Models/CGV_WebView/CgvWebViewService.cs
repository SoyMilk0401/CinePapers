using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using CinePapers.Models.Common;
using System.Linq;

namespace CinePapers.Models.CGV_WebView
{
    public class CgvWebViewService : ICinemaService
    {
        private WebView2 _webView;
        private bool _isInitialized = false;
        private TaskCompletionSource<bool> _navigationTask;

        public string CinemaName => "CGV";

        public CgvWebViewService()
        {
            _webView = new WebView2();
            _webView.Size = new System.Drawing.Size(800, 600);
        }

        public async Task InitializeAsync(Control parent)
        {
            if (_isInitialized) return;

            parent.Controls.Add(_webView);

            _webView.BringToFront();

            await _webView.EnsureCoreWebView2Async();

            _webView.NavigationCompleted += (s, e) =>
            {
                _navigationTask?.TrySetResult(e.IsSuccess);
            };

            _isInitialized = true;
        }

        public Dictionary<string, string> GetCategories()
        {
            return new Dictionary<string, string>
            {
                { "SPECIAL", "01" },
                { "영화", "03" },
                { "극장", "04" },
                { "제휴", "05" },
                { "멤버십/CLUB", "07" },
            };
        }

        private async Task NavigateAndWaitAsync(string url)
        {
            if (!_isInitialized) throw new Exception("WebView가 초기화되지 않았습니다.");

            // 이미 해당 도메인에 있다면 이동하지 않음 (불필요한 리로딩 방지)
            //if (_webView.Source != null && _webView.Source.ToString().StartsWith(url))
            //{
            //    return;
            //}

            _navigationTask = new TaskCompletionSource<bool>();
            _webView.CoreWebView2.Navigate(url);
            await _navigationTask.Task;
        }

        private async Task<T> ExecuteJsAsync<T>(string script)
        {
            try
            {
                string jsonResult = await _webView.ExecuteScriptAsync(script);
                if (jsonResult == "null") return default;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using (var doc = JsonDocument.Parse(jsonResult))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.String)
                    {
                        string innerJson = doc.RootElement.GetString();
                        return JsonSerializer.Deserialize<T>(innerJson, options);
                    }
                    else
                    {
                        return JsonSerializer.Deserialize<T>(jsonResult, options);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JS Error: {ex.Message}");
                return default;
            }
        }

        // 1. 이벤트 리스트 조회 (API 활용)
        public async Task<List<CinemaEventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = "")
        {
            // 검색어가 있으면 기존 검색 로직 사용
            if (!string.IsNullOrEmpty(searchText))
            {
                return await SearchCgvEventsAsync(searchText);
            }

            // [수정] 메인 페이지에서 API 호출을 위해 cgv.co.kr로 이동
            await NavigateAndWaitAsync("https://cgv.co.kr/");

            // 페이지 번호를 기반으로 startRow 계산 (한 페이지당 10개 기준)
            int listCount = 10;
            int startRow = (pageNo - 1) * listCount;

            // JS fetch를 이용해 API 직접 호출
            string script = $@"
                (async function() {{
                    try {{
                        var url = 'https://event.cgv.co.kr/evt/evt/evt/searchEvtListForPage?coCd=A420&evntCtgryLclsCd={categoryCode}&sscnsChoiYn=N&expnYn=N&expoChnlCd=01&startRow={startRow}&listCount={listCount}';
                        
                        var response = await fetch(url, {{
                            method: 'GET',
                            headers: {{
                                'Accept': 'application/json'
                            }}
                        }});
                        
                        var data = await response.json();
                        var list = [];

                        if (data && data.data && data.data.list) {{
                            data.data.list.forEach(function(e) {{
                                // 이미지 경로 조합 (기본 도메인 + 경로 + 파일명)
                                var imgUrl = '';
                                if (e.lagBanrPhyscFilePathnm && e.lagBanrPhyscFnm) {{
                                    imgUrl = 'https://img.cgv.co.kr/' + e.lagBanrPhyscFilePathnm + '/' + e.lagBanrPhyscFnm;
                                }}

                                // 날짜 포맷 정리 (YYYY-MM-DD)
                                var period = '';
                                if (e.evntStartDt && e.evntEndDt) {{
                                    period = e.evntStartDt.split(' ')[0] + ' ~ ' + e.evntEndDt.split(' ')[0];
                                }}

                                list.push({{
                                    EventId: e.evntNo,
                                    Title: e.evntNm,
                                    ImageUrl: imgUrl,
                                    DatePeriod: period,
                                    // PC용 상세 페이지 URL 조합
                                    DetailUrl: 'https://cgv.co.kr/evt/eventDetail?evntNo=' + e.evntNo
                                }});
                            }});
                        }}
                        return JSON.stringify(list);
                    }} catch (e) {{
                        return JSON.stringify([]);
                    }}
                }})();
            ";

            var list = await ExecuteJsAsync<List<CinemaEventItem>>(script);

            System.Diagnostics.Debug.WriteLine(list);

            return list ?? new List<CinemaEventItem>();
        }

        private async Task<List<CinemaEventItem>> SearchCgvEventsAsync(string keyword)
        {
            // [수정] 실제 검색 페이지로 이동 (요청 헤더/쿠키 환경 조성)
            string searchPageUrl = "https://cgv.co.kr/tme/itgrSrch";
            await NavigateAndWaitAsync(searchPageUrl);

            string script = $@"
                (async function() {{
                    try {{
                        // API 호출 URL 구성
                        var url = 'https://api.cgv.co.kr/tme/more/itgrSrch/searchItgrSrchAll?coCd=A420&swrd={keyword}&lmtSrchYn=Y';
                        
                        var response = await fetch(url, {{
                            method: 'GET',
                            headers: {{ 
                                'Accept': 'application/json' 
                            }}
                        }});
                        
                        var data = await response.json();
                        var list = [];
                        
                        // 1. 영화 검색 결과 매핑 (atktPsblMovInfo)
                        if (data.data && data.data.atktPsblMovInfo && data.data.atktPsblMovInfo.atktPsblMovLst) {{
                            data.data.atktPsblMovInfo.atktPsblMovLst.forEach(m => {{
                                var imgUrl = '';
                                // 응답의 path와 파일명을 조합 (예: /cgvpomsfilm/... + filename.jpg)
                                if (m.path && m.imageBasFnm) {{
                                    imgUrl = 'https://img.cgv.co.kr' + m.path + m.imageBasFnm;
                                }}

                                list.push({{
                                    EventId: m.movNo,
                                    Title: m.movNm,
                                    ImageUrl: imgUrl,
                                    DatePeriod: '개봉: ' + m.rlsYmd,
                                    DetailUrl: 'http://m.cgv.co.kr/WebApp/MovieV4/movieDetail.aspx?MovieIdx=' + m.movNo
                                }});
                            }});
                        }}

                        // 2. 이벤트 검색 결과 매핑 (evntInfo) - 검색어와 관련된 이벤트도 함께 표시
                        if (data.data && data.data.evntInfo && data.data.evntInfo.evntLst) {{
                            data.data.evntInfo.evntLst.forEach(e => {{
                                var imgUrl = '';
                                if (e.lagBanrPhyscFilePathnm && e.lagBanrPhyscFnm) {{
                                    imgUrl = 'https://img.cgv.co.kr/' + e.lagBanrPhyscFilePathnm + '/' + e.lagBanrPhyscFnm;
                                }}

                                var period = '';
                                if (e.evntStartDt && e.evntEndDt) {{
                                    period = e.evntStartDt.split(' ')[0] + ' ~ ' + e.evntEndDt.split(' ')[0];
                                }}

                                list.push({{
                                    EventId: e.evntNo,
                                    Title: e.evntNm,
                                    ImageUrl: imgUrl,
                                    DatePeriod: period,
                                    DetailUrl: 'https://cgv.co.kr/evt/eventDetail?evntNo=' + e.evntNo
                                }});
                            }});
                        }}

                        return JSON.stringify(list);
                    }} catch (e) {{
                        return JSON.stringify([]);
                    }}
                }})();
            ";

            return await ExecuteJsAsync<List<CinemaEventItem>>(script);
        }

        // 3. 이벤트 상세 조회
        public async Task<CinemaEventDetail> GetEventDetailAsync(string eventId)
        {
            // 모바일 페이지가 파싱하기 더 쉬운 구조이므로 유지하거나, 필요 시 PC URL로 변경 가능
            // 요청하신 URL: https://cgv.co.kr/evt/eventDetail?evntNo=...
            // 하지만 본문 이미지 추출 등은 모바일 DOM 구조가 단순한 경우가 많아 기존 모바일 로직을 유지하는 것을 추천합니다.
            // 만약 PC 페이지로 변경하고 싶다면 아래 URL을 사용하고 DOM 파싱 로직을 수정해야 합니다.

            string url = $"http://m.cgv.co.kr/WebApp/EventNotiV4/EventDetail.aspx?seq={eventId}";
            await NavigateAndWaitAsync(url);

            string script = @"
                (function() {
                    var title = document.querySelector('.tit_info') ? document.querySelector('.tit_info').innerText : '';
                    var date = document.querySelector('.date') ? document.querySelector('.date').innerText : '';
                    var imgs = [];
                    
                    var contentImgs = document.querySelectorAll('.sect-event-detail img');
                    contentImgs.forEach(img => imgs.push(img.src));

                    return JSON.stringify({
                        Title: title,
                        DatePeriod: date,
                        ImageUrls: imgs,
                        OriginalEventId: '',
                        HasStockCheck: false 
                    });
                })();
            ";

            return await ExecuteJsAsync<CinemaEventDetail>(script);
        }

        public async Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId)
        {
            return new List<CinemaStockItem>();
        }

        public string GetStockStatusText(int stockCount) => $"{stockCount}";
    }
}