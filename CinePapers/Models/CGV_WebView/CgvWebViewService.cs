using CinePapers.Models.Common;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                var options = new CoreWebView2EnvironmentOptions("--disable-web-security --disable-features=IsolateOrigins,site-per-process");
                var env = await CoreWebView2Environment.CreateAsync(null, null, options);
                await _webView.EnsureCoreWebView2Async(env);

                _webView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";
                _webView.NavigationCompleted += WebView_NavigationCompleted;
                _webView.WebMessageReceived += WebView_WebMessageReceived;

                _webView.CoreWebView2.Navigate("https://cgv.co.kr");
                _isInitialized = true;
            }
            catch (Exception ex) { Debug.WriteLine($"[CGV Init Error] {ex.Message}"); }
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
                if (json.Contains("\"type\":\"log\"") || json.Contains("\"type\":\"error\"") || json.Contains("\"type\": \"log\"")) return;

                if (_responseTcs != null && !_responseTcs.Task.IsCompleted)
                    _responseTcs.SetResult(json);
            }
            catch (Exception ex)
            {
                if (_responseTcs != null && !_responseTcs.Task.IsCompleted) _responseTcs.SetException(ex);
            }
        }

        private async Task<bool> WaitForHookingAsync()
        {
            int retry = 0;
            while ((!_isInitialized || !_isHooked) && retry < 40)
            {
                await Task.Delay(500);
                retry++;
            }
            return _isHooked;
        }

        public Dictionary<string, string> GetCategories()
        {
            return new Dictionary<string, string>
            {
                { "SPECIAL", "01" },
                { "영화", "03" },
                { "극장", "04" },
                { "멤버십/CLUB", "07" },
                { "경품현황", "GIFT" }
            };
        }

        public async Task<List<CinemaEventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = "")
        {
            if (!await WaitForHookingAsync()) return new List<CinemaEventItem>();

            if (categoryCode == "GIFT") return await FetchCgvGiftList(pageNo);

            if (!string.IsNullOrEmpty(searchText))
            {
                if (pageNo > 1) return new List<CinemaEventItem>();
                return await FetchCgvSearchList(searchText);
            }

            return await FetchCgvEventList(categoryCode, pageNo);
        }

        private async Task<List<CinemaEventItem>> FetchCgvGiftList(int pageNo)
        {
            int startRow = (pageNo - 1) * 10;
            _responseTcs = new TaskCompletionSource<string>();

            string script = $@"
                (async function() {{
                    try {{
                        if (!window.cgvParamBuilder || !window.cgvFetcher) return;
                        
                        var params = {{ 'coCd': 'A420', 'startRow': '{startRow}', 'listCount': '10' }};
                        var signedQuery = window.cgvParamBuilder.n(params);
                        var url = 'https://event.cgv.co.kr/evt/saprm/saprm/searchSaprmEvtListForPage' + signedQuery;
                        
                        var response = await window.cgvFetcher.Z(url);
                        window.chrome.webview.postMessage(JSON.stringify(await response.json()));
                    }} catch (e) {{ }}
                }})();
            ";

            await _webView.ExecuteScriptAsync(script);
            var jsonResult = await Task.WhenAny(_responseTcs.Task, Task.Delay(5000)) == _responseTcs.Task ? await _responseTcs.Task : null;
            if (string.IsNullOrEmpty(jsonResult)) return new List<CinemaEventItem>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var response = JsonSerializer.Deserialize<CgvGiftListResponse>(jsonResult, options);
                if (response?.Data?.List == null) return new List<CinemaEventItem>();

                return response.Data.List.Select(item => new CinemaEventItem
                {
                    EventId = "GIFT_" + item.SaprmEvntNo,
                    Title = item.SaprmEvntNm,
                    ImageUrl = item.SaprmEvntImageUrl,
                    DatePeriod = $"{item.EvntStartYmd} ~ {item.EvntEndYmd}"
                }).ToList();
            }
            catch { return new List<CinemaEventItem>(); }
        }

        private async Task<List<CinemaEventItem>> FetchCgvEventList(string categoryCode, int pageNo)
        {
            int startRow = (pageNo - 1) * 10;
            _responseTcs = new TaskCompletionSource<string>();

            string script = $@"
                (async function() {{
                    try {{
                        if (!window.cgvParamBuilder || !window.cgvFetcher) return;
                        var params = {{ 'coCd': 'A420', 'evntCtgryLclsCd': '{categoryCode}', 'sscnsChoiYn': 'N', 'expnYn': 'N', 'expoChnlCd': '01', 'startRow': '{startRow}', 'listCount': '10' }};
                        var signedQuery = window.cgvParamBuilder.n(params);
                        var url = 'https://event.cgv.co.kr/evt/evt/evt/searchEvtListForPage' + signedQuery;
                        var response = await window.cgvFetcher.Z(url);
                        window.chrome.webview.postMessage(JSON.stringify(await response.json()));
                    }} catch (e) {{ }}
                }})();
            ";
            await _webView.ExecuteScriptAsync(script);
            var jsonResult = await Task.WhenAny(_responseTcs.Task, Task.Delay(5000)) == _responseTcs.Task ? await _responseTcs.Task : null;
            if (string.IsNullOrEmpty(jsonResult)) return new List<CinemaEventItem>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var response = JsonSerializer.Deserialize<CgvEventListResponse>(jsonResult, options);
                return response?.Data?.List?.Select(item => new CinemaEventItem { EventId = item.EvntNo, Title = item.EvntNm, ImageUrl = item.ImageUrl, DatePeriod = $"{item.EvntStartDt} ~ {item.EvntEndDt}" }).ToList() ?? new List<CinemaEventItem>();
            }
            catch { return new List<CinemaEventItem>(); }
        }

        private async Task<List<CinemaEventItem>> FetchCgvSearchList(string searchText)
        {
            _responseTcs = new TaskCompletionSource<string>();
            string script = $@"
                (async function() {{
                    try {{
                        if (!window.cgvParamBuilder || !window.cgvFetcher) return;
                        var params = {{ 'coCd': 'A420', 'swrd': '{searchText}', 'lmtSrchYn': 'Y' }};
                        var signedQuery = window.cgvParamBuilder.n(params);
                        var url = 'https://api.cgv.co.kr/tme/more/itgrSrch/searchItgrSrchAll' + signedQuery;
                        var response = await window.cgvFetcher.Z(url);
                        window.chrome.webview.postMessage(JSON.stringify(await response.json()));
                    }} catch (e) {{ }}
                }})();
            ";
            await _webView.ExecuteScriptAsync(script);
            var jsonResult = await Task.WhenAny(_responseTcs.Task, Task.Delay(5000)) == _responseTcs.Task ? await _responseTcs.Task : null;
            if (string.IsNullOrEmpty(jsonResult)) return new List<CinemaEventItem>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var response = JsonSerializer.Deserialize<CgvSearchResponse>(jsonResult, options);
                return response?.Data?.EvntInfo?.EvntLst?.Select(item => new CinemaEventItem { EventId = item.EvntNo, Title = item.EvntNm, ImageUrl = item.ImageUrl, DatePeriod = $"{item.EvntStartDt} ~ {item.EvntEndDt}" }).ToList() ?? new List<CinemaEventItem>();
            }
            catch { return new List<CinemaEventItem>(); }
        }

        public async Task<CinemaEventDetail> GetEventDetailAsync(string eventId)
        {
            if (!await WaitForHookingAsync()) return null;

            if (eventId.StartsWith("GIFT_"))
            {
                string saprmEvntNo = eventId.Replace("GIFT_", "");
                return new CinemaEventDetail
                {
                    OriginalEventId = saprmEvntNo,
                    Title = "경품 현황 조회",
                    DatePeriod = "재고 확인을 눌러주세요",
                    HasStockCheck = true,
                    OriginalGiftId = saprmEvntNo
                };
            }
            return await FetchCgvEventDetail(eventId);
        }

        private async Task<CinemaEventDetail> FetchCgvEventDetail(string eventId)
        {
            _responseTcs = new TaskCompletionSource<string>();
            string script = $@"
                (async function() {{
                    try {{
                        if (!window.cgvParamBuilder || !window.cgvFetcher) return;
                        var params = {{ 'coCd': 'A420', 'evntNo': '{eventId}', 'expoChnlCd': '01', 'previewYn': 'N', 'expnYn': 'N' }};
                        var signedQuery = window.cgvParamBuilder.n(params);
                        var url = 'https://event.cgv.co.kr/evt/evt/evtDtl/searchEvtDtl' + signedQuery;
                        var response = await window.cgvFetcher.Z(url);
                        window.chrome.webview.postMessage(JSON.stringify(await response.json()));
                    }} catch (e) {{ }}
                }})();
            ";
            await _webView.ExecuteScriptAsync(script);
            var jsonResult = await Task.WhenAny(_responseTcs.Task, Task.Delay(5000)) == _responseTcs.Task ? await _responseTcs.Task : null;
            if (string.IsNullOrEmpty(jsonResult)) return null;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var response = JsonSerializer.Deserialize<CgvEventDetailResponse>(jsonResult, options);
                if (response?.Data == null) return null;
                var data = response.Data;
                var detail = new CinemaEventDetail { OriginalEventId = data.EvntNo, Title = data.EvntNm, DatePeriod = $"{data.EvntStartDt} ~ {data.EvntEndDt}" };
                if (!string.IsNullOrEmpty(data.EvntHtmlCont))
                {
                    string html = data.EvntHtmlCont;
                    if (!html.Trim().StartsWith("<")) try { html = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(html)); } catch { }
                    detail.ImageUrls.AddRange(ExtractImagesFromHtml(html));
                }
                if (detail.ImageUrls.Count == 0 && !string.IsNullOrEmpty(data.DetailImageUrl)) detail.ImageUrls.Add(data.DetailImageUrl);
                return detail;
            }
            catch { return null; }
        }

        public async Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId)
        {
            string saprmEvntNo = giftId;

            var productList = await FetchCgvGiftProductList(saprmEvntNo);

            if (productList == null || productList.Count == 0)
            {
                Debug.WriteLine($"[CGV Stock] 품목 조회 실패: {saprmEvntNo}");
                return new List<CinemaStockItem>();
            }

            string spmtlNo = productList[0].SpmtlNo;
            Debug.WriteLine($"[CGV Stock] ID 변환: {saprmEvntNo} -> {spmtlNo} ({productList[0].OnlnExpoNm})");

            return await FetchCgvGiftStock(saprmEvntNo, spmtlNo);
        }

        private async Task<List<CgvGiftProductItem>> FetchCgvGiftProductList(string saprmEvntNo)
        {
            _responseTcs = new TaskCompletionSource<string>();

            string script = $@"
                (async function() {{
                    try {{
                        if (!window.cgvParamBuilder || !window.cgvFetcher) return;

                        var params = {{
                            'coCd': 'A420',
                            'saprmEvntNo': '{saprmEvntNo}'
                        }};

                        var signedQuery = window.cgvParamBuilder.n(params);
                        var url = 'https://event.cgv.co.kr/evt/saprm/saprm/searchSaprmEvtProdList' + signedQuery;
                        
                        var response = await window.cgvFetcher.Z(url);
                        var jsonBody = await response.json();
                        window.chrome.webview.postMessage(JSON.stringify(jsonBody));

                    }} catch (e) {{ }}
                }})();
            ";

            await _webView.ExecuteScriptAsync(script);
            var jsonResult = await Task.WhenAny(_responseTcs.Task, Task.Delay(5000)) == _responseTcs.Task ? await _responseTcs.Task : null;

            if (string.IsNullOrEmpty(jsonResult)) return new List<CgvGiftProductItem>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var response = JsonSerializer.Deserialize<CgvGiftProductResponse>(jsonResult, options);
                return response?.Data ?? new List<CgvGiftProductItem>();
            }
            catch { return new List<CgvGiftProductItem>(); }
        }

        private async Task<List<CinemaStockItem>> FetchCgvGiftStock(string saprmEvntNo, string spmtlNo)
        {
            _responseTcs = new TaskCompletionSource<string>();
            string script = $@"
                (async function() {{
                    try {{
                        if (!window.cgvParamBuilder || !window.cgvFetcher) return;
                        var params = {{ 'coCd': 'A420', 'saprmEvntNo': '{saprmEvntNo}', 'spmtlNo': '{spmtlNo}' }};
                        var signedQuery = window.cgvParamBuilder.n(params);
                        var url = 'https://event.cgv.co.kr/evt/saprm/saprm/searchSaprmEvtTgtsiteList' + signedQuery;
                        var response = await window.cgvFetcher.Z(url);
                        window.chrome.webview.postMessage(JSON.stringify(await response.json()));
                    }} catch (e) {{ }}
                }})();
            ";

            await _webView.ExecuteScriptAsync(script);
            var jsonResult = await Task.WhenAny(_responseTcs.Task, Task.Delay(5000)) == _responseTcs.Task ? await _responseTcs.Task : null;
            if (string.IsNullOrEmpty(jsonResult)) return new List<CinemaStockItem>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var response = JsonSerializer.Deserialize<CgvGiftDetailResponse>(jsonResult, options);

                return response?.Data?.Select(item => new CinemaStockItem
                {
                    Region = item.RegnGrpNm,
                    CinemaName = item.SiteNm,
                    StockCount = item.RlInvntQty,
                    SortOrder = item.SortOseq
                }).ToList() ?? new List<CinemaStockItem>();
            }
            catch { return new List<CinemaStockItem>(); }
        }

        private List<string> ExtractImagesFromHtml(string html)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(html)) return list;
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(@"<img\s+[^>]*\bsrc\s*=\s*[""'](?<url>[^""']+)[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (System.Text.RegularExpressions.Match match in regex.Matches(html))
                {
                    string src = match.Groups["url"].Value;
                    if (!string.IsNullOrEmpty(src))
                    {
                        if (src.StartsWith("/")) src = "https://www.cgv.co.kr" + src;
                        if (!list.Contains(src)) list.Add(src);
                    }
                }
            }
            catch { }
            return list;
        }

        public string GetStockStatusText(int stockCount) => $"{stockCount}개";
    }
}