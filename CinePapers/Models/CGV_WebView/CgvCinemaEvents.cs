using System.Collections.Generic;

namespace CinePapers.Models.CGV_WebView
{
    public class CgvEventListResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public CgvData Data { get; set; }
    }
    public class CgvData
    {
        public int StartRow { get; set; }
        public int ListCount { get; set; }
        public int TotalCount { get; set; }
        public List<CgvEventItem> List { get; set; }
    }
    public class CgvEventItem
    {
        public string EvntNo { get; set; }        // 이벤트 ID
        public string EvntNm { get; set; }        // 이벤트명
        public string EvntStartDt { get; set; }   // 시작일
        public string EvntEndDt { get; set; }     // 종료일
        public string MduBanrPhyscFilePathnm { get; set; }
        public string MduBanrPhyscFnm { get; set; }
        public string ImageUrl
        {
            get
            {
                if (string.IsNullOrEmpty(MduBanrPhyscFilePathnm) || string.IsNullOrEmpty(MduBanrPhyscFnm))
                    return null;
                return $"https://cdn.cgv.co.kr/{MduBanrPhyscFilePathnm}/{MduBanrPhyscFnm}";
            }
        }
    }

    public class CgvEventDetailResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public CgvEventDetailItem Data { get; set; }
    }
    public class CgvEventDetailItem
    {
        public string EvntNo { get; set; }
        public string EvntNm { get; set; }
        public string EvntStartDt { get; set; }
        public string EvntEndDt { get; set; }
        public string EvntImfilePhyscFilePathnm { get; set; }
        public string EvntImfilePhyscFnm { get; set; }
        public string EvntHtmlCont { get; set; } // HTML 내용
        public string DetailImageUrl
        {
            get
            {
                if (string.IsNullOrEmpty(EvntImfilePhyscFilePathnm) || string.IsNullOrEmpty(EvntImfilePhyscFnm))
                    return null;
                return $"https://cdn.cgv.co.kr/{EvntImfilePhyscFilePathnm}/{EvntImfilePhyscFnm}";
            }
        }
    }

    public class CgvSearchResponse
    {
        public int StatusCode { get; set; }
        public CgvSearchData Data { get; set; }
    }
    public class CgvSearchData
    {
        public CgvSearchEvntInfo EvntInfo { get; set; }
    }
    public class CgvSearchEvntInfo
    {
        public List<CgvSearchEventItem> EvntLst { get; set; }
    }
    public class CgvSearchEventItem
    {
        public string EvntNo { get; set; }
        public string EvntNm { get; set; }
        public string EvntStartDt { get; set; }
        public string EvntEndDt { get; set; }
        public string MduBanrPhyscFilePathnm { get; set; }
        public string MduBanrPhyscFnm { get; set; }
        public string ImageUrl
        {
            get
            {
                if (string.IsNullOrEmpty(MduBanrPhyscFilePathnm) || string.IsNullOrEmpty(MduBanrPhyscFnm))
                    return null;
                return $"https://cdn.cgv.co.kr/{MduBanrPhyscFilePathnm}/{MduBanrPhyscFnm}";
            }
        }
    }

    public class CgvGiftListResponse
    {
        public int StatusCode { get; set; }
        public CgvGiftListData Data { get; set; }
    }
    public class CgvGiftListData
    {
        public List<CgvGiftItem> List { get; set; }
    }
    public class CgvGiftItem
    {
        public string SaprmEvntNo { get; set; }      // 경품 이벤트 ID (중요)
        public string SaprmEvntNm { get; set; }      // 경품명
        public string EvntOnlnExpoNm { get; set; }
        public string SaprmEvntImageUrl { get; set; } // 썸네일 URL
        public string EvntStartYmd { get; set; }
        public string EvntEndYmd { get; set; }
    }

    public class CgvGiftDetailResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public List<CgvGiftStockData> Data { get; set; }
    }
    public class CgvGiftStockData
    {
        public string SiteNm { get; set; }       // 극장명
        public string RegnGrpNm { get; set; }    // 지역명
        public int RlInvntQty { get; set; }      // 실재고 수량
        public int SortOseq { get; set; }        // 정렬 순서
        public string FcfsPayYn { get; set; }    // 선착순 여부
    }

    public class CgvGiftProductResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public List<CgvGiftProductItem> Data { get; set; }
    }

    public class CgvGiftProductItem
    {
        public string CoCd { get; set; }
        public string SpmtlNo { get; set; }        // [중요] 경품 상세 번호 (예: 2025112407070404)
        public string SpmtlProdNm { get; set; }
        public string OnlnExpoNm { get; set; }     // 경품명 (예: [국보] 메인 포스터)
        public string SpmtlDsc { get; set; }
    }
}