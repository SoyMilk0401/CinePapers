using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using CinePapers.Models.Common;
using System.Diagnostics;

namespace CinePapers.Models.CGV_WebView
{
    public class CgvWebViewService : ICinemaService
    {
        public string CinemaName => "CGV";

        private WebView2 _webView;
        private Form _hostingForm;
        private TaskCompletionSource<string> _responseTcs;
        private bool _isInitialized = false;
        private bool _isHooked = false;

        // Webpack Hooking 스크립트
        private const string WebpackHookScript = @"
            (function() {
                if (window.cgvParamBuilder && window.cgvFetcher) return;

                window.webpackChunk_N_E = window.webpackChunk_N_E || [];
                window.webpackChunk_N_E.push([
                    [9999], 
                    {
                        9999: (e, t, n) => {
                            window.cgvRequire = n;
                            try {
                                if(n.m[97207]) window.cgvParamBuilder = n(97207);
                                if(n.m[74189]) window.cgvFetcher = n(74189);
                                
                                if(window.cgvParamBuilder && window.cgvFetcher) {
                                    window.chrome.webview.postMessage(JSON.stringify({ type: 'log', message: 'Webpack hooking success.' }));
                                }
                            } catch (err) {
                                window.chrome.webview.postMessage(JSON.stringify({ type: 'error', message: 'Hook error: ' + err.message }));
                            }
                        }
                    },
                    e => e(9999)
                ]);
            })();
        ";

        public CgvWebViewService()
        {
            // 실사용을 위해 폼을 숨김 처리
            _hostingForm = new Form
            {
                Width = 0,
                Height = 0,
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Minimized,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(-32000, -32000)
            };

            _webView = new WebView2 { Dock = DockStyle.Fill };
            _hostingForm.Controls.Add(_webView);

            _hostingForm.Show();
            _hostingForm.Hide();

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                // CORS 해제 옵션 적용
                var options = new CoreWebView2EnvironmentOptions("--disable-web-security --disable-features=IsolateOrigins,site-per-process");
                var env = await CoreWebView2Environment.CreateAsync(null, null, options);

                await _webView.EnsureCoreWebView2Async(env);

                _webView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36";

                _webView.NavigationCompleted += WebView_NavigationCompleted;
                _webView.WebMessageReceived += WebView_WebMessageReceived;

                _webView.CoreWebView2.Navigate("https://cgv.co.kr");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CGV Service] Init Error: {ex.Message}");
            }
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;
            await _webView.ExecuteScriptAsync(WebpackHookScript);
            _isHooked = true;
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.TryGetWebMessageAsString();
                var msg = JObject.Parse(json);

                if (msg["type"] != null && (msg["type"].ToString() == "log" || msg["type"].ToString() == "error"))
                {
                    return;
                }

                if (_responseTcs != null && !_responseTcs.Task.IsCompleted)
                {
                    _responseTcs.SetResult(json);
                }
            }
            catch (Exception ex)
            {
                if (_responseTcs != null && !_responseTcs.Task.IsCompleted)
                    _responseTcs.SetException(ex);
            }
        }

        // 이벤트 리스트 조회 (검색 로직 통합)
        public async Task<List<CinemaEventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = "")
        {
            if (!await WaitForHookingAsync()) return new List<CinemaEventItem>();

            // 검색어가 있는 경우 검색 로직 실행
            if (!string.IsNullOrEmpty(searchText))
            {
                // 검색 API는 페이지네이션을 다른 방식으로 처리하거나 전체를 주는 경우가 많아
                // 1페이지 요청일 때만 검색을 수행하도록 처리 (필요시 수정 가능)
                if (pageNo > 1) return new List<CinemaEventItem>();

                return await FetchCgvSearchList(searchText);
            }

            return await FetchCgvEventList(categoryCode, pageNo);
        }

        public async Task<CinemaEventDetail> GetEventDetailAsync(string eventId)
        {
            if (!await WaitForHookingAsync()) return null;
            return await FetchCgvEventDetail(eventId);
        }

        private async Task<bool> WaitForHookingAsync()
        {
            int retry = 0;
            while ((!_isInitialized || !_isHooked) && retry < 40)
            {
                await Task.Delay(500);
                retry++;
            }
            if (!_isHooked) Debug.WriteLine("[CGV Service] 후킹 준비 안됨 (Timeout)");
            return _isHooked;
        }

        // [API] 일반 이벤트 리스트 호출
        private async Task<List<CinemaEventItem>> FetchCgvEventList(string categoryCode, int pageNo)
        {
            int startRow = (pageNo - 1) * 10;
            _responseTcs = new TaskCompletionSource<string>();

            string script = $@"
                (async function() {{
                    async function waitForModules() {{
                        for (let i = 0; i < 50; i++) {{
                            if (window.cgvRequire && !window.cgvParamBuilder && window.cgvRequire.m[97207]) window.cgvParamBuilder = window.cgvRequire(97207);
                            if (window.cgvRequire && !window.cgvFetcher && window.cgvRequire.m[74189]) window.cgvFetcher = window.cgvRequire(74189);
                            if (window.cgvParamBuilder && window.cgvFetcher) return true;
                            await new Promise(r => setTimeout(r, 100));
                        }}
                        return false;
                    }}

                    try {{
                        if (!(await waitForModules())) {{
                            window.chrome.webview.postMessage(JSON.stringify({{ type: 'error', message: 'Module Load Timeout' }}));
                            return;
                        }}

                        var params = {{
                            'coCd': 'A420',
                            'evntCtgryLclsCd': '{categoryCode}',
                            'sscnsChoiYn': 'N',
                            'expnYn': 'N',
                            'expoChnlCd': '01',
                            'startRow': '{startRow}',
                            'listCount': '10'
                        }};

                        var signedQuery = window.cgvParamBuilder.n(params);
                        var url = 'https://event.cgv.co.kr/evt/evt/evt/searchEvtListForPage' + signedQuery;
                        
                        var response = await window.cgvFetcher.Z(url);
                        if (!response.ok) throw new Error('Network response was not ok: ' + response.status);
                        var jsonBody = await response.json();
                        
                        window.chrome.webview.postMessage(JSON.stringify(jsonBody));

                    }} catch (e) {{
                        window.chrome.webview.postMessage(JSON.stringify({{ type: 'error', message: e.message }}));
                    }}
                }})();
            ";

            try
            {
                await _webView.ExecuteScriptAsync(script);
                var jsonResult = await Task.WhenAny(_responseTcs.Task, Task.Delay(10000)) == _responseTcs.Task ? await _responseTcs.Task : null;

                if (string.IsNullOrEmpty(jsonResult)) return new List<CinemaEventItem>();
                return ParseCgvListJson(jsonResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CGV Service] List Fetch Error: {ex.Message}");
                return new List<CinemaEventItem>();
            }
        }

        // [API] 검색 API 호출 (서명 적용)
        private async Task<List<CinemaEventItem>> FetchCgvSearchList(string searchText)
        {
            _responseTcs = new TaskCompletionSource<string>();

            // 검색어 그대로 전달 (내부적으로 인코딩 될 수 있음, 필요시 encodeURIComponent 사용)
            string script = $@"
                (async function() {{
                    try {{
                        // 모듈 확인 (이미 로드된 상태라고 가정)
                        if (!window.cgvParamBuilder || !window.cgvFetcher) {{
                             window.chrome.webview.postMessage(JSON.stringify({{ type: 'error', message: 'Modules missing for search' }}));
                             return;
                        }}

                        var params = {{
                            'coCd': 'A420',
                            'swrd': '{searchText}', 
                            'lmtSrchYn': 'Y'
                        }};

                        // 서명 생성
                        var signedQuery = window.cgvParamBuilder.n(params);
                        
                        // 검색 API URL (도메인 api.cgv.co.kr 주의)
                        var url = 'https://api.cgv.co.kr/tme/more/itgrSrch/searchItgrSrchAll' + signedQuery;
                        
                        // cgvFetcher를 이용해 요청 (헤더 자동 처리)
                        var response = await window.cgvFetcher.Z(url);
                        
                        if (!response.ok) throw new Error('Network response was not ok: ' + response.status);
                        var jsonBody = await response.json();
                        
                        window.chrome.webview.postMessage(JSON.stringify(jsonBody));

                    }} catch (e) {{
                        window.chrome.webview.postMessage(JSON.stringify({{ type: 'error', message: e.message }}));
                    }}
                }})();
            ";

            try
            {
                await _webView.ExecuteScriptAsync(script);
                var jsonResult = await Task.WhenAny(_responseTcs.Task, Task.Delay(10000)) == _responseTcs.Task ? await _responseTcs.Task : null;

                if (string.IsNullOrEmpty(jsonResult)) return new List<CinemaEventItem>();
                return ParseCgvSearchJson(jsonResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CGV Service] Search Fetch Error: {ex.Message}");
                return new List<CinemaEventItem>();
            }
        }

        // [API] 이벤트 디테일 호출
        private async Task<CinemaEventDetail> FetchCgvEventDetail(string eventId)
        {
            _responseTcs = new TaskCompletionSource<string>();

            string script = $@"
                (async function() {{
                    try {{
                        if (!window.cgvParamBuilder || !window.cgvFetcher) {{
                             window.chrome.webview.postMessage(JSON.stringify({{ type: 'error', message: 'Modules missing for detail' }}));
                             return;
                        }}

                        var params = {{
                            'coCd': 'A420',
                            'evntNo': '{eventId}',
                            'expoChnlCd': '01',
                            'previewYn': 'N',
                            'expnYn': 'N'
                        }};

                        var signedQuery = window.cgvParamBuilder.n(params);
                        var url = 'https://event.cgv.co.kr/evt/evt/evtDtl/searchEvtDtl' + signedQuery;
                        
                        var response = await window.cgvFetcher.Z(url);
                        if (!response.ok) throw new Error('Network response was not ok: ' + response.status);
                        var jsonBody = await response.json();
                        
                        window.chrome.webview.postMessage(JSON.stringify(jsonBody));

                    }} catch (e) {{
                        window.chrome.webview.postMessage(JSON.stringify({{ type: 'error', message: e.message }}));
                    }}
                }})();
            ";

            try
            {
                await _webView.ExecuteScriptAsync(script);
                var jsonResult = await Task.WhenAny(_responseTcs.Task, Task.Delay(10000)) == _responseTcs.Task ? await _responseTcs.Task : null;

                if (string.IsNullOrEmpty(jsonResult)) return null;
                return ParseCgvDetailJson(jsonResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CGV Service] Detail Fetch Error: {ex.Message}");
                return null;
            }
        }

        // 리스트 파싱
        private List<CinemaEventItem> ParseCgvListJson(string jsonResult)
        {
            try
            {
                var json = JObject.Parse(jsonResult);
                if (json["statusCode"]?.ToString() != "0") return new List<CinemaEventItem>();

                var list = json["data"]?["list"];
                if (list == null) return new List<CinemaEventItem>();

                return list.Select(item => new CinemaEventItem
                {
                    EventId = item["evntNo"]?.ToString(),
                    Title = item["evntNm"]?.ToString(),
                    ImageUrl = GetImageUrl(item, "mduBanrPhyscFilePathnm", "mduBanrPhyscFnm"),
                    DatePeriod = $"{item["evntStartDt"]} ~ {item["evntEndDt"]}"
                }).ToList();
            }
            catch
            {
                return new List<CinemaEventItem>();
            }
        }

        // [신규] 검색 결과 파싱 (사용자가 제공한 JSON 구조 반영)
        private List<CinemaEventItem> ParseCgvSearchJson(string jsonResult)
        {
            try
            {
                var json = JObject.Parse(jsonResult);

                // 검색 결과는 statusCode 0, data -> evntInfo -> evntLst 구조
                if (json["statusCode"]?.ToString() != "0") return new List<CinemaEventItem>();

                var evntInfo = json["data"]?["evntInfo"];
                if (evntInfo == null) return new List<CinemaEventItem>();

                var eventList = evntInfo["evntLst"];
                if (eventList == null) return new List<CinemaEventItem>();

                return eventList.Select(item => new CinemaEventItem
                {
                    EventId = item["evntNo"]?.ToString(),
                    Title = item["evntNm"]?.ToString(),
                    // 검색 결과 JSON의 이미지 필드는 일반 리스트와 이름이 동일함
                    ImageUrl = GetImageUrl(item, "mduBanrPhyscFilePathnm", "mduBanrPhyscFnm"),
                    DatePeriod = $"{item["evntStartDt"]} ~ {item["evntEndDt"]}"
                }).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CGV Search Parse Error] {ex.Message}");
                return new List<CinemaEventItem>();
            }
        }

        // 디테일 파싱
        private CinemaEventDetail ParseCgvDetailJson(string jsonResult)
        {
            try
            {
                var json = JObject.Parse(jsonResult);
                if (json["statusCode"]?.ToString() != "0") return null;

                var data = json["data"];
                if (data == null) return null;

                var detail = new CinemaEventDetail
                {
                    OriginalEventId = data["evntNo"]?.ToString(),
                    Title = data["evntNm"]?.ToString(),
                    DatePeriod = $"{data["evntStartDt"]} ~ {data["evntEndDt"]}"
                };

                string htmlContent = data["evntHtmlCont"]?.ToString();

                if (!string.IsNullOrEmpty(htmlContent))
                {
                    if (!htmlContent.Trim().StartsWith("<") && !htmlContent.Contains(" "))
                    {
                        try
                        {
                            byte[] decodedBytes = Convert.FromBase64String(htmlContent);
                            htmlContent = System.Text.Encoding.UTF8.GetString(decodedBytes);
                        }
                        catch { }
                    }

                    var htmlImages = ExtractImagesFromHtml(htmlContent);
                    if (htmlImages.Count > 0)
                    {
                        detail.ImageUrls.AddRange(htmlImages);
                    }
                }

                if (detail.ImageUrls.Count == 0)
                {
                    string imgUrl = GetImageUrl(data, "evntImfilePhyscFilePathnm", "evntImfilePhyscFnm");
                    if (!string.IsNullOrEmpty(imgUrl))
                    {
                        detail.ImageUrls.Add(imgUrl);
                    }
                }

                return detail;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CGV Detail Parse Error] {ex.Message}");
                return null;
            }
        }

        private List<string> ExtractImagesFromHtml(string html)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(html)) return list;

            try
            {
                html = System.Net.WebUtility.HtmlDecode(html);

                var regex = new System.Text.RegularExpressions.Regex(
                    @"<img\s+[^>]*\bsrc\s*=\s*[""'](?<url>[^""']+)[""']",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

                var matches = regex.Matches(html);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    string src = match.Groups["url"].Value;

                    if (!string.IsNullOrEmpty(src))
                    {
                        src = src.Trim();
                        if (src.StartsWith("/")) src = "https://www.cgv.co.kr" + src;
                        if (!list.Contains(src)) list.Add(src);
                    }
                }
            }
            catch { }
            return list;
        }

        private string GetImageUrl(JToken item, string pathKey, string fileKey)
        {
            string path = item[pathKey]?.ToString();
            string file = item[fileKey]?.ToString();
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(file)) return null;
            return $"https://cdn.cgv.co.kr/{path}/{file}";
        }

        public Dictionary<string, string> GetCategories()
        {
            return new Dictionary<string, string>
            {
                { "SPECIAL", "01" },
                { "영화", "03" },
                { "극장", "04" },
                { "멤버십/CLUB", "07" },
            };
        }

        public Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId) => Task.FromResult(new List<CinemaStockItem>());
        public string GetStockStatusText(int stockCount) => $"{stockCount}개";
    }
}