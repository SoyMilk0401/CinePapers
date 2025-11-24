using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinePapers.Models.Common
{
    // 공통 이벤트 데이터
    public class CinemaEventItem
    {
        public string EventId { get; set; }       // 이벤트 ID
        public string Title { get; set; }         // 제목
        public string ImageUrl { get; set; }      // 썸네일 이미지 URL
        public string DatePeriod { get; set; }    // 기간
        public string DetailUrl { get; set; }     // (선택) 웹페이지 링크
    }

    // 상세 페이지 공통 데이터
    public class CinemaEventDetail
    {
        public string Title { get; set; }
        public string DatePeriod { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();

        // 부가 기능 (재고 조회 등)을 위한 원본 데이터 ID들
        public string OriginalEventId { get; set; }
        public string OriginalGiftId { get; set; }
        public bool HasStockCheck { get; set; } = false;
    }

    // 재고 현황 공통 데이터
    public class CinemaStockItem
    {
        public string Region { get; set; }      // 지역 (서울, 경기...)
        public string CinemaName { get; set; }  // 극장명
        public int StockCount { get; set; }     // 재고 수량
        public int SortOrder { get; set; }      // 정렬 순서
    }

    public interface ICinemaService
    {
        string CinemaName { get; } // 영화관 이름 (Lotte, CGV, Mega)
        Dictionary<string, string> GetCategories(); // 카데고리 목록
        Task<List<CinemaEventItem>> GetEventsListAsync(string categoryCode, int pageNo, string searchText = ""); // 이벤트 목록 조회
        Task<CinemaEventDetail> GetEventDetailAsync(string eventId); // 이벤트 디테일 페이지 조회
        Task<List<CinemaStockItem>> GetGiftStockAsync(string eventId, string giftId); // 이벤트 경품 수량 조회
    }
}
