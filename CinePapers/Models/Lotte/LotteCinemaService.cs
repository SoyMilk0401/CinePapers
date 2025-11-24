using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CinePapers.Models.Common; // 공통 모델 사용

namespace CinePapers
{
    public class LotteCinemaService : ICinemaService // 인터페이스 구현
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

        // 1. 목록 조회 (반환 타입이 List<CinemaEventItem>으로 변경됨)
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
                PageSize = 10,
                MemberNo = "0"
            };

            // 내부 전용 모델(LotteEventListResponse)로 받음
            var response = await SendRequestAsync<LotteEventListResponse>(paramData);

            if (response?.Items == null) return new List<CinemaEventItem>();

            // [핵심] 공통 모델(CinemaEventItem)로 변환
            return response.Items.Select(item => new CinemaEventItem
            {
                EventId = item.EventID,
                Title = item.EventName,
                ImageUrl = item.ImageUrl,
                DatePeriod = $"{item.ProgressStartDate} ~ {item.ProgressEndDate}"
            }).ToList();
        }

        // 2. 상세 조회
        public async Task<CinemaEventDetail> GetEventDetailAsync(string eventId)
        {
            var paramData = new
            {
                MethodName = "GetInfomationDeliveryEventDetail",
                channelType = "HO",
                osType = "W",
                osVersion = "Mozilla/5.0",
                EventID = eventId
            };

            var response = await SendRequestAsync<LotteDetailResponse>(paramData);
            var detail = response?.InfomationDeliveryEventDetail?.FirstOrDefault();

            if (detail == null) return null;

            // 공통 상세 모델로 변환
            var commonDetail = new CinemaEventDetail
            {
                Title = detail.EventName,
                DatePeriod = $"{detail.ProgressStartDate} ~ {detail.ProgressEndDate}",
                OriginalEventId = detail.EventID,
                HasStockCheck = false
            };

            if (!string.IsNullOrEmpty(detail.ImgUrl))
                commonDetail.ImageUrls.Add(detail.ImgUrl);

            // 경품 ID가 있으면 재고 조회 가능하도록 설정
            if (detail.GoodsGiftItems != null && detail.GoodsGiftItems.Count > 0)
            {
                commonDetail.OriginalGiftId = detail.GoodsGiftItems[0].FrGiftID;
                commonDetail.HasStockCheck = true;
            }

            return commonDetail;
        }

        // 3. 재고 조회
        public async Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId)
        {
            var paramData = new
            {
                MethodName = "GetCinemaGoods",
                channelType = "HO",
                osType = "W",
                osVersion = "Mozilla/5.0",
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

        private async Task<T> SendRequestAsync<T>(object paramData)
        {
            // (기존과 동일한 통신 로직)
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
    }
}