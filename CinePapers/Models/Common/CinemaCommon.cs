using System.Collections.Generic;
using System.Threading.Tasks;

namespace CinePapers.Models.Common
{
    // 이벤트 목록의 이벤트
    public class CinemaEventItem
    {
        public string EventId { get; set; }       // 이벤트 ID
        public string Title { get; set; }         // 제목
        public string ImageUrl { get; set; }      // 썸네일 이미지 URL
        public string DatePeriod { get; set; }    // 기간
        public string DetailUrl { get; set; }     // 웹페이지 링크
    }

    // 상세 이벤트 페이지
    public class CinemaEventDetail
    {
        public string Title { get; set; }
        public string DatePeriod { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public string OriginalEventId { get; set; }
        public string OriginalGiftId { get; set; }
        public bool HasStockCheck { get; set; } = false;
    }

    // 경품 수량
    public class CinemaStockItem
    {
        public string Region { get; set; }      // 지역
        public string CinemaName { get; set; }  // 극장명
        public int StockCount { get; set; }     // 재고 수량
        public int SortOrder { get; set; }      // 정렬 순서
    }

    public interface ICinemaService
    {
        string CinemaName { get; } // CGV, 롯데시네마, 메가박스
        Dictionary<string, string> GetCategories(); // 카데고리 목록
        Task<List<CinemaEventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = ""); // 이벤트 목록 조회
        Task<CinemaEventDetail> GetEventDetailAsync(string eventId); // 이벤트 디테일 페이지 조회
        Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId); // 이벤트 경품 수량 조회
        string GetStockStatusText(int stockCount); // 이벤트 경품 수량 표기 방식
    }
}
