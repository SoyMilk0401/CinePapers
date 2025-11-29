using CinePapers.Models.Common;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CinePapers.Models.Mega
{
    public class MegaCinemaService : ICinemaService
    {
        private static readonly HttpClient _client;
        private const string ListUrl = "https://www.megabox.co.kr/on/oh/ohe/Event/eventMngDiv.do";
        private const string StockUrl = "https://www.megabox.co.kr/on/oh/ohe/Event/selectGoodsStockPrco.do";

        public string CinemaName => "메가박스";

        static MegaCinemaService()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Referer", "https://www.megabox.co.kr/event");
            _client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        public Dictionary<string, string> GetCategories()
        {
            return new Dictionary<string, string>
            {
                { "메가Pick", "CED03" },
                { "영화", "CED01" },
                { "극장", "CED04" },
                { "시사회/무대인사", "CED02" },
                { "제휴/할인", "CED05" }
            };
        }

        // 이벤트 리스트 조회
        public async Task<List<CinemaEventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = "")
        {
            var requestData = new
            {
                currentPage = pageNo.ToString(),
                eventDivCd = categoryCode,
                eventStatCd = "ONG",
                recordCountPerPage = "12",
                eventTitle = searchText
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _client.PostAsync(ListUrl, content);
                string html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var nodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'eventBtn')]");
                var list = new List<CinemaEventItem>();

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var item = new CinemaEventItem();
                        item.EventId = node.GetAttributeValue("data-no", "");

                        var titNode = node.SelectSingleNode(".//p[@class='tit']");
                        if (titNode != null) item.Title = HtmlEntity.DeEntitize(titNode.InnerText.Trim());

                        var imgNode = node.SelectSingleNode(".//p[@class='img']//img");
                        if (imgNode != null) item.ImageUrl = imgNode.GetAttributeValue("src", "");

                        var dateNode = node.SelectSingleNode(".//p[@class='date']");
                        if (dateNode != null) item.DatePeriod = HtmlEntity.DeEntitize(dateNode.InnerText.Trim());

                        list.Add(item);
                    }
                }
                return list;
            }
            catch
            {
                return new List<CinemaEventItem>();
            }
        }

        // 이벤트 디테일 페이지 조회
        public async Task<CinemaEventDetail> GetEventDetailAsync(string eventId)
        {
            string url = $"https://www.megabox.co.kr/event/detail?eventNo={eventId}";
            try
            {
                var html = await _client.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var detail = new CinemaEventDetail { OriginalEventId = eventId };

                var titleNode = doc.DocumentNode.SelectSingleNode("//div[@class='event-detail']/h2[@class='tit']");
                if (titleNode != null) detail.Title = HtmlEntity.DeEntitize(titleNode.InnerText.Trim());

                var dateNode = doc.DocumentNode.SelectSingleNode("//p[@class='event-detail-date']/em");
                if (dateNode != null) detail.DatePeriod = HtmlEntity.DeEntitize(dateNode.InnerText.Trim());

                var imgNodes = doc.DocumentNode.SelectNodes("//div[@class='event-html']//img");
                if (imgNodes != null)
                {
                    foreach (var img in imgNodes)
                    {
                        string src = img.GetAttributeValue("src", "");
                        if (!string.IsNullOrEmpty(src))
                        {
                            src = src.Replace("\\", "/");
                            if (!src.StartsWith("http")) src = "https://img.megabox.co.kr" + src;
                            detail.ImageUrls.Add(src);
                        }
                    }
                }

                var stockBtn = doc.DocumentNode.SelectSingleNode("//button[@id='btnSelectGoodsStock']");
                if (stockBtn != null)
                {
                    string goodsNo = stockBtn.GetAttributeValue("data-pn", "");
                    if (!string.IsNullOrEmpty(goodsNo))
                    {
                        detail.OriginalGiftId = goodsNo;
                        detail.HasStockCheck = true;
                    }
                }

                return detail;
            }
            catch { return null; }
        }

        // 이벤트 경품 수량 조회
        public async Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId)
        {
            var parameters = new Dictionary<string, string>
            {
                { "eventNo", eventId },
                { "goodsNo", giftId }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, StockUrl)
            {
                Content = new FormUrlEncodedContent(parameters)
            };
            request.Headers.Referrer = new Uri($"https://www.megabox.co.kr/event/detail?eventNo={eventId}");

            try
            {
                var response = await _client.SendAsync(request);
                string html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var stockList = new List<CinemaStockItem>();

                var areaNodes = doc.DocumentNode.SelectNodes("//li[contains(@class, 'area-cont')]");
                if (areaNodes == null) return stockList;

                int sortOrder = 0;
                foreach (var areaNode in areaNodes)
                {
                    string region = "기타";
                    var btnNode = areaNode.SelectSingleNode(".//button[contains(@class, 'btn')]");
                    if (btnNode != null)
                    {
                        string text = HtmlEntity.DeEntitize(btnNode.InnerText.Trim());
                        int idx = text.IndexOf('(');
                        region = idx > 0 ? text.Substring(0, idx).Trim() : text;
                    }

                    var cinemaNodes = areaNode.SelectNodes(".//li[contains(@class, 'brch')]");
                    if (cinemaNodes != null)
                    {
                        foreach (var cinemaNode in cinemaNodes)
                        {
                            var item = new CinemaStockItem
                            {
                                Region = region,
                                SortOrder = ++sortOrder
                            };

                            var linkNode = cinemaNode.SelectSingleNode(".//a");
                            if (linkNode != null) item.CinemaName = HtmlEntity.DeEntitize(linkNode.InnerText.Trim());

                            var spanNode = cinemaNode.SelectSingleNode(".//span");
                            if (spanNode != null)
                            {
                                string status = HtmlEntity.DeEntitize(spanNode.InnerText.Trim());

                                if (status.Contains("소진"))
                                {
                                    item.StockCount = 0;
                                }
                                else if (status.Contains("소량"))
                                {
                                    item.StockCount = 1;
                                }
                                else if (status.Contains("보유"))
                                {
                                    item.StockCount = 2;
                                }
                                else
                                {
                                    var match = Regex.Match(status, @"\d+");
                                    if (match.Success && int.TryParse(match.Value, out int count))
                                    {
                                        item.StockCount = count;
                                    }
                                    else
                                    {
                                        item.StockCount = 0;
                                    }
                                }
                            }
                            else
                            {
                                item.StockCount = 0;
                            }

                            stockList.Add(item);
                        }
                    }
                }
                return stockList;
            }
            catch
            {
                return new List<CinemaStockItem>();
            }
        }

        // 이벤트 경품 수량 표기 방식 정의
        public string GetStockStatusText(int stockCount)
        {
            switch (stockCount)
            {
                case 1: return "소량 보유";
                case 2: return "보유";
                default: return "소진";
            }
        }
    }
}