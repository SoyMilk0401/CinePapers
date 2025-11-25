using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CinePapers.Models.Common;

namespace CinePapers
{
    public class LotteCinemaService : ICinemaService
    {
        private readonly HttpClient _client;
        private const string RequestUrl = "https://www.lottecinema.co.kr/LCWS/Event/EventData.aspx";

        public string CinemaName => "롯데시네마";

        public LotteCinemaService()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Referer", "https://www.lottecinema.co.kr/NLCHS/Event/DetailList");
        }

        public Dictionary<string, string> GetCategories()
        {
            return new Dictionary<string, string>
            {
                { "HOT", "10" },
                { "영화", "20" },
                { "시사회/무대인사", "40" },
                { "제휴할인", "50" },
                { "우리동네영화관", "30" }
            };
        }

        private async Task<T> SendRequestAsync<T>(object paramData)
        {
            string jsonParam = JsonSerializer.Serialize(paramData);
            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(new StringContent(jsonParam), "paramList");
                try
                {
                    var response = await _client.PostAsync(RequestUrl, formData);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<T>(responseBody, options);
                }
                catch { return default(T); }
            }
        }

        // 이벤트 리스트 조회
        public async Task<List<CinemaEventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = "")
        {
            var paramData = new
            {
                MethodName = "GetEventLists",
                channelType = "HO",
                osType = "W",
                osVersion = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
                EventClassificationCode = categoryCode,
                SearchText = searchText,
                CinemaID = "",
                PageNo = pageNo,
                PageSize = 20,
                MemberNo = "0"
            };

            var response = await SendRequestAsync<LotteEventListResponse>(paramData);

            if (response?.Items == null) return new List<CinemaEventItem>();

            return response.Items.Select(item => new CinemaEventItem
            {
                EventId = item.EventID,
                Title = item.EventName,
                ImageUrl = item.ImageUrl,
                DatePeriod = $"{item.ProgressStartDate} ~ {item.ProgressEndDate}"
            }).ToList();
        }

        // 이벤트 디테일 페이지 조회
        public async Task<CinemaEventDetail> GetEventDetailAsync(string eventId)
        {
            var paramData = new
            {
                MethodName = "GetInfomationDeliveryEventDetail",
                channelType = "HO",
                osType = "W",
                osVersion = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
                EventID = eventId
            };

            var response = await SendRequestAsync<LotteDetailResponse>(paramData);
            var detail = response?.InfomationDeliveryEventDetail?.FirstOrDefault();

            if (detail == null) return null;

            var commonDetail = new CinemaEventDetail
            {
                Title = detail.EventName,
                DatePeriod = $"{detail.ProgressStartDate} ~ {detail.ProgressEndDate}",
                OriginalEventId = detail.EventID,
                HasStockCheck = false
            };

            if (!string.IsNullOrEmpty(detail.ImgUrl))
                commonDetail.ImageUrls.Add(detail.ImgUrl);

            if (detail.GoodsGiftItems != null && detail.GoodsGiftItems.Count > 0)
            {
                commonDetail.OriginalGiftId = detail.GoodsGiftItems[0].FrGiftID;
                commonDetail.HasStockCheck = true;
            }

            return commonDetail;
        }

        // 이벤트 경품 수량 조회
        public async Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId)
        {
            var paramData = new
            {
                MethodName = "GetCinemaGoods",
                channelType = "HO",
                osType = "W",
                osVersion = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
                EventID = eventId,
                GiftID = giftId
            };

            var response = await SendRequestAsync<LotteGiftStockResponse>(paramData);

            if (response?.CinemaDivisionGoods == null) return new List<CinemaStockItem>();

            return response.CinemaDivisionGoods.Select(g => new CinemaStockItem
            {
                Region = g.DetailDivisionNameKR,
                CinemaName = g.CinemaNameKR,
                StockCount = g.Cnt,
                SortOrder = g.SortSequence
            }).ToList();
        }
    }
}