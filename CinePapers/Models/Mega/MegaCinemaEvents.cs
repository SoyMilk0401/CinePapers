using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinePapers.Models.Mega
{
    // 영화 이벤트 목록 페이지
    public class MegaEventRequest
    {
        public string currentPage { get; set; } = "1";
        public string recordCountPerPage { get; set; } = "10";
        public string eventStatCd { get; set; } = "ONG"; // 진행중(ONG)
        public string eventTitle { get; set; } = "";
        public string eventDivCd { get; set; } // 카테고리 코드 (CED01: 영화, CED03: 메가Pick 등)
        public string eventTyCd { get; set; } = "";
        public string orderReqCd { get; set; } = "ONGlist";
    }
    public class MegaEventItem
    {
        public string EventNo { get; set; }       // 이벤트 번호 (data-no="19256")
        public string EventTitle { get; set; }    // 제목 (p.tit)
        public string ImageUrl { get; set; }      // 이미지 URL (img src)
        public string DatePeriod { get; set; }    // 기간 (p.date) -> "2025.11.29 ~ 2025.11.30"
        public string DetailUrl
        {
            get { return $"https://www.megabox.co.kr/event/detail?eventNo={EventNo}"; }
        }
    }

    // 특정 영화 디테일 페이지
    public class MegaEventDetail
    {
        public string EventNo { get; set; }
        public string Title { get; set; }       // 제목 (h2.tit)
        public string DatePeriod { get; set; }  // 기간 (p.event-detail-date > em)
        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}
