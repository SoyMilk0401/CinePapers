using System.Collections.Generic;

namespace CinePapers.Models.Mega
{
    // 영화 이벤트 목록 응답
    public class MegaEventRequest
    {
        public string currentPage { get; set; } = "1";
        public string recordCountPerPage { get; set; } = "10";
        public string eventStatCd { get; set; } = "ONG";
        public string eventTitle { get; set; } = "";
        public string eventDivCd { get; set; }
        public string eventTyCd { get; set; } = "";
        public string orderReqCd { get; set; } = "ONGlist";
    }
    public class MegaEventItem
    {
        public string EventNo { get; set; }
        public string EventTitle { get; set; }
        public string ImageUrl { get; set; }
        public string DatePeriod { get; set; }
        public string DetailUrl
        {
            get { return $"https://www.megabox.co.kr/event/detail?eventNo={EventNo}"; }
        }
    }

    // 특정 영화 디테일 응답
    public class MegaEventDetail
    {
        public string EventNo { get; set; }
        public string Title { get; set; }
        public string DatePeriod { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public string GoodsNo { get; set; }
        public bool HasStockCheck { get; set; }
    }
    public class MegaStockResponse
    {
        public string Msg { get; set; }
        public List<MegaStockItem> StockList { get; set; }
    }
    public class MegaStockItem
    {
        public string BrchNm { get; set; }
        public string RemainQty { get; set; }
        public string TotalQty { get; set; }
    }
}