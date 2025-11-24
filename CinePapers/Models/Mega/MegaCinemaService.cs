using CinePapers.Models.Common;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CinePapers.Models.Mega
{
    public class MegaCinemaService : ICinemaService
    {
        private readonly HttpClient _client;
        private const string ListUrl = "https://www.megabox.co.kr/on/oh/ohe/Event/eventMngDiv.do";

        public string CinemaName => "메가박스";

        public MegaCinemaService()
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
                { "시사회/무대인사", "CED02" },
                { "제휴/할인", "CED05" }
            };
        }

        public async Task<List<CinemaEventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = "")
        {
            // 메가박스 요청 파라미터
            var requestData = new
            {
                currentPage = pageNo.ToString(),
                eventDivCd = categoryCode,
                eventStatCd = "ONG",
                recordCountPerPage = "10",
                eventTitle = searchText // 검색어
            };

            string jsonContent = JsonSerializer.Serialize(requestData);
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

                        // HTML 속성에서 ID 추출
                        item.EventId = node.GetAttributeValue("data-no", "");

                        // 제목 추출
                        var titNode = node.SelectSingleNode(".//p[@class='tit']");
                        if (titNode != null) item.Title = titNode.InnerText.Trim();

                        // 이미지 추출
                        var imgNode = node.SelectSingleNode(".//p[@class='img']//img");
                        if (imgNode != null) item.ImageUrl = imgNode.GetAttributeValue("src", "");

                        // 기간 추출
                        var dateNode = node.SelectSingleNode(".//p[@class='date']");
                        if (dateNode != null) item.DatePeriod = dateNode.InnerText.Trim();

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
                if (titleNode != null) detail.Title = titleNode.InnerText.Trim();

                var dateNode = doc.DocumentNode.SelectSingleNode("//p[@class='event-detail-date']/em");
                if (dateNode != null) detail.DatePeriod = dateNode.InnerText.Trim();

                // 이미지 추출 (메가박스는 이미지가 여러 장일 수 있음)
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
                return detail;
            }
            catch { return null; }
        }

        public Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId)
        {
            return Task.FromResult(new List<CinemaStockItem>());
        }
    }
}
