using System.Collections.Generic;

namespace CinePapers.Models.CGV
{
    // 영화 이벤트 목록 페이지
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
        public string EvntStartDt { get; set; }   // 시작일 (2025-11-26 00:00:00)
        public string EvntEndDt { get; set; }     // 종료일
        public string EvntCtgryLclsCd { get; set; } // 카테고리 코드
        public string MduBanrPhyscFilePathnm { get; set; } // 경로
        public string MduBanrPhyscFnm { get; set; }        // 작은 배너 파일명
        public string LagBanrPhyscFilePathnm { get; set; }
        public string LagBanrPhyscFnm { get; set; } // 큰 배너 파일명
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

    // 특정 영화 디테일 페이지
    public class CgvEventDetailResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public CgvEventDetailItem Data { get; set; }
    }
    public class CgvEventDetailItem
    {
        public string CoCd { get; set; }
        public string EvntNo { get; set; }        // 이벤트 번호
        public string EvntNm { get; set; }        // 이벤트 이름
        public string EvntStartDt { get; set; }   // 시작일
        public string EvntEndDt { get; set; }     // 종료일
        public string EvntImfilePhyscFilePathnm { get; set; }
        public string EvntImfilePhyscFnm { get; set; }        // 상세 이미지 파일명

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

    // 검색 페이지
    public class CgvSearchResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public CgvSearchData Data { get; set; }
    }
    public class CgvSearchData
    {
        public int TotalCnt { get; set; }
        public CgvSearchEvntInfo EvntInfo { get; set; }
    }
    public class CgvSearchEvntInfo
    {
        public int TotalCnt { get; set; }
        public List<CgvSearchEventItem> EvntLst { get; set; }
    }
    public class CgvSearchEventItem
    {
        public string CoCd { get; set; }
        public string EvntNo { get; set; }        // 이벤트 번호
        public string EvntNm { get; set; }        // 이벤트 이름
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

    // 경품 현황 리스트
    public class CgvGiftListResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public CgvGiftData Data { get; set; }
    }
    public class CgvGiftData
    {
        public int StartRow { get; set; }
        public int ListCount { get; set; }
        public int TotalCount { get; set; }
        public List<CgvGiftItem> List { get; set; }
    }
    public class CgvGiftItem
    {
        public string CoCd { get; set; }
        public string SaprmEvntNo { get; set; }      // 경품 이벤트 번호
        public string SaprmEvntNm { get; set; }      // 경품 이벤트 이름
        public string EvntOnlnExpoNm { get; set; }   // 온라인 노출명
        public string SaprmEvntImageUrl { get; set; } // 이미지 URL
        public string ExhsYn { get; set; }           // 소진 여부 (Y/N)
        public string EvntStartYmd { get; set; }     // 시작일 (YYYYMMDD)
        public string EvntEndYmd { get; set; }       // 종료일
    }

    // 경품 수량 조회
    public class CgvGiftDetailResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public List<CgvGiftDetailItem> Data { get; set; }
    }
    public class CgvGiftDetailItem
    {
        public string CoCd { get; set; }
        public string SpmtlNo { get; set; }      // 경품 상세 번호
        public string SiteNo { get; set; }       // 극장 코드
        public string SiteNm { get; set; }       // 극장명
        public string ExpoSiteNm { get; set; }   // 노출 극장명
        public string RegnGrpCd { get; set; }    // 지역 그룹 코드
        public string RegnGrpNm { get; set; }    // 지역 그룹명
        public int SortOseq { get; set; }        // 정렬 순서
        public string FcfsPayYn { get; set; }    // 선착순 지급 여부
        public int RlInvntQty { get; set; }      // 실재고 수량
        public int TotPayQty { get; set; }       // 전체 지급 수량
    }
}
