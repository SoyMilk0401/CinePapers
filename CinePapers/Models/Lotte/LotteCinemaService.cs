using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CinePapers
{
    public class LotteCinemaService
    {
        private readonly HttpClient _client;
        private const string RequestUrl = "https://www.lottecinema.co.kr/LCWS/Event/EventData.aspx";

        public LotteCinemaService()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Referer", "https://www.lottecinema.co.kr/NLCHS/Event/DetailList");
        }


        // 이벤트 목록 조회
        public async Task<List<EventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = "")
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
                PageSize = "20",
                MemberNo = ""
            };

            var response = await SendRequestAsync<LotteEventListResponse>(paramData);
            return response?.Items ?? new List<EventItem>();
        }

        // 이벤트 상세 정보 조회
        public async Task<EventDetailItem> GetEventDetailAsync(string eventId)
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
            return response?.InfomationDeliveryEventDetail?.FirstOrDefault();
        }

        // 경품 수량(재고) 조회
        public async Task<List<CinemaGoodsItem>> GetGiftStockAsync(string eventId, string giftId)
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
            return response?.CinemaDivisionGoods ?? new List<CinemaGoodsItem>();
        }

        // 통합 요청 메서드
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
                    
                    return JsonSerializer.Deserialize<T>(responseBody);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error requesting {paramData}: {ex.Message}");
                    return default(T);
                }
            }
        }
    }
}