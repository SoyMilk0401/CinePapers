using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using CinePapers.Models.Common;

namespace CinePapers.Models.CGV
{
    public class CgvCinemaService : ICinemaService
    {
        private readonly HttpClient _client;
        public string CinemaName => "CGV";

        public CgvCinemaService()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Referer", "https://www.cgv.co.kr/");
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

        public async Task<List<CinemaEventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = "")
        {
            string url;

            if (!string.IsNullOrEmpty(searchText))
            {
                url = $"https://api.cgv.co.kr/tme/more/itgrSrch/searchItgrSrchAll?coCd=A420&swrd={Uri.EscapeDataString(searchText)}&lmtSrchYn=Y";
                var response = await GetJsonAsync<CgvSearchResponse>(url);

                if (response?.Data?.EvntInfo?.EvntLst == null) return new List<CinemaEventItem>();

                return response.Data.EvntInfo.EvntLst.Select(e => new CinemaEventItem
                {
                    EventId = e.EvntNo,
                    Title = e.EvntNm,
                    ImageUrl = e.ImageUrl,
                    DatePeriod = $"{e.EvntStartDt} ~ {e.EvntEndDt}"
                }).ToList();
            }
            else
            {
                // CGV는 startRow가 0, 10, 20... (페이지 번호 아님)
                int startRow = (pageNo - 1) * 10;
                url = $"https://event.cgv.co.kr/evt/evt/evt/searchEvtListForPage?coCd=A420&evntCtgryLclsCd={categoryCode}&startRow={startRow}&listCount=10&sscnsChoiYn=N&expnYn=N&expoChnlCd=01";

                var response = await GetJsonAsync<CgvEventListResponse>(url);

                if (response?.Data?.List == null) return new List<CinemaEventItem>();

                // 목록 결과 -> 공통 모델 변환
                return response.Data.List.Select(e => new CinemaEventItem
                {
                    EventId = e.EvntNo,
                    Title = e.EvntNm,
                    ImageUrl = e.ImageUrl,
                    DatePeriod = $"{e.EvntStartDt} ~ {e.EvntEndDt}"
                }).ToList();
            }
        }

        public async Task<CinemaEventDetail> GetEventDetailAsync(string eventId)
        {
            string url = $"https://event.cgv.co.kr/evt/evt/evtDtl/searchEvtDtl?coCd=A420&evntNo={eventId}&expoChnlCd=01";
            var response = await GetJsonAsync<CgvEventDetailResponse>(url);
            var data = response?.Data;

            if (data == null) return null;

            var detail = new CinemaEventDetail
            {
                Title = data.EvntNm,
                DatePeriod = $"{data.EvntStartDt} ~ {data.EvntEndDt}",
                OriginalEventId = data.EvntNo
                // CGV는 현재 재고 조회 기능 구현 안 함 (필요시 추가 분석)
            };

            if (!string.IsNullOrEmpty(data.DetailImageUrl))
                detail.ImageUrls.Add(data.DetailImageUrl);

            return detail;
        }

        // CGV는 재고 조회 구조가 달라 추후 구현하거나 빈 리스트 반환
        public Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId)
        {
            return Task.FromResult(new List<CinemaStockItem>());
        }

        private async Task<T> GetJsonAsync<T>(string url)
        {
            try
            {
                var json = await _client.GetStringAsync(url);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch { return default(T); }
        }
    }
}