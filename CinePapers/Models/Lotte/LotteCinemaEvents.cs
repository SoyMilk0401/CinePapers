using System.Collections.Generic;

namespace CinePapers.Models.Lotte
{
    // 영화 이벤트 목록 응답
    public class LotteEventListResponse
    {
        public List<EventItem> Items { get; set; }
        public int TotalCount { get; set; }
        public string IsOK { get; set; }
        public string ResultMessage { get; set; }
        public string ResultCode { get; set; }
        public string EventResultYn { get; set; }
    }
    public class EventItem
    {
        public string EventID { get; set; }                 // 이벤트 ID
        public string EventName { get; set; }               // 이벤트명
        public string EventClassificationCode { get; set; } // 분류 코드
        public string EventTypeCode { get; set; }           // 타입 코드
        public string EventTypeName { get; set; }           // 타입명
        public string ProgressStartDate { get; set; }       // 시작일
        public string ProgressEndDate { get; set; }         // 종료일
        public int RemainsDayCount { get; set; }            // 남은 일수
        public string ImageUrl { get; set; }                // 썸네일 이미지 URL
        public string ImageAlt { get; set; }                // 이미지 대체 텍스트
        public int ImageDivisionCode { get; set; }          // 이미지 구분
        public string CinemaID { get; set; }                // 영화관 ID
        public string CinemaName { get; set; }              // 영화관명
        public string CinemaAreaCode { get; set; }          // 지역 코드
        public string CinemaAreaName { get; set; }          // 지역명
        public int DevTemplateYN { get; set; }              // 개발 템플릿 여부
        public int CloseNearYN { get; set; }                // 마감 임박 여부
        public int EventWinnerYN { get; set; }              // 당첨자 발표 여부
        public int EventSeq { get; set; }                   // 정렬 순서
        public string EventCntnt { get; set; }              // 이벤트 내용
        public string EventNtc { get; set; }                // 유의사항
    }

    // 특정 영화 디테일 응답
    public class LotteDetailResponse
    {
        public List<EventDetailItem> InfomationDeliveryEventDetail { get; set; }
        public string IsOK { get; set; }
        public string ResultMessage { get; set; }
        public string ResultCode { get; set; }
        public string EventResultYn { get; set; }
    }
    public class EventDetailItem
    {
        public string EventID { get; set; }                         // 이벤트 ID
        public string EventName { get; set; }                       // 이벤트명
        public string EventClassificationCode { get; set; }         // 분류 코드
        public string ProgressStartDate { get; set; }               // 시작일
        public string ProgressEndDate { get; set; }                 // 종료일
        public string WinnerAnnouncmentDate { get; set; }           // 당첨자 발표일
        public string ImgUrl { get; set; }                          // 상세 이미지 URL
        public string ImgAlt { get; set; }                          // 상세 텍스트
        public string ListImgUrl { get; set; }                      // 목록용 썸네일 URL
        public string ListImgAlt { get; set; }                      // 목록용 텍스트
        public int ImageDivisionCode { get; set; }                  // 이미지 구분
        public object ImageGameTypeDivisionCode { get; set; }       // 게임 타입 구분
        public string EventContents { get; set; }                   // 추가 컨텐츠
        public string EventNotice { get; set; }                     // 공지 사항
        public object WinnerNotice { get; set; }                    // 당첨자 공지
        public string CinemaID { get; set; }                        // 영화관 ID
        public string CinemaName { get; set; }                      // 영화관명
        public int EventProgressDivisionCode { get; set; }          // 진행 상태 코드
        public string EventMovieURL { get; set; }                   // 관련 영화 URL
        public string EventMovieImageURL { get; set; }              // 관련 영화 이미지
        public string EventMovieImageAlt { get; set; }              // 관련 영화 텍스트
        public List<GoodsGiftItem> GoodsGiftItems { get; set; }     // 경품 목록 리스트
        public List<object> ButtonSetting { get; set; }             // 버튼 설정
        public List<object> JoinStatus { get; set; }                // 참여 상태
        public string GoodsShowYN { get; set; }                     // 굿즈 노출 여부
        public int InformationOfferingAgreementYN { get; set; }     // 정보 제공 동의 여부
        public string InformationOfferingAgreementContents { get; set; } // 동의 내용
    }
    public class GoodsGiftItem
    {
        public string EventID { get; set; }    // 이벤트 ID
        public string FrGiftID { get; set; }   // 경품 ID
        public string FrGiftNm { get; set; }   // 경품명
    }

    // 경품 수량 조회 응답
    public class LotteGiftStockResponse
    {
        public List<CinemaDivisionItem> CinemaDivisions { get; set; }
        public List<CinemaGoodsItem> CinemaDivisionGoods { get; set; }    // 극장별 수량 정보
        public string IsOK { get; set; }
        public string ResultMessage { get; set; }
        public string ResultCode { get; set; }
        public string EventResultYn { get; set; }
    }
    public class CinemaDivisionItem
    {
        public int DivisionCode { get; set; }          // 구분 코드
        public string DetailDivisionCode { get; set; } // 상세 구분 코드
        public string GroupNameKR { get; set; }        // 지역명 한글
        public string GroupNameUS { get; set; }        // 지역명 영문
        public int SortSequence { get; set; }          // 정렬 순서
        public int CinemaCount { get; set; }           // 해당 지역 극장 수
    }
    public class CinemaGoodsItem
    {
        public int DivisionCode { get; set; }          // 구분 코드
        public string DetailDivisionCode { get; set; } // 상세 구분 코드
        public string CinemaID { get; set; }           // 영화관 ID
        public string CinemaNameKR { get; set; }       // 영화관명
        public string CinemaNameUS { get; set; }       // 영화관명 영문
        public int SortSequence { get; set; }          // 정렬 순서
        public int Cnt { get; set; }                   // 현재 남은 수량
        public string DetailDivisionNameKR { get; set; } // 지역명 한글
        public string DetailDivisionNameUS { get; set; } // 지역명 영문
    }
}