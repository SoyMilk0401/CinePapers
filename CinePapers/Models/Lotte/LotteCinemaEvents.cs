using System.Collections.Generic;

namespace CinePapers
{
    // 영화 이벤트 목록 페이지
    public class LotteEventListResponse
    {
        public List<EventItem> Items { get; set; }
        public int TotalCount { get; set; }          // 전체 아이템 개수 (예: 34)
        public string IsOK { get; set; }             // 성공 여부 ("true")
        public string ResultMessage { get; set; }    // 메시지 ("SUCCESS")
        public string ResultCode { get; set; }       // 결과 코드 (null)
        public string EventResultYn { get; set; }    // 이벤트 결과 여부 (null)
    }
    public class EventItem
    {
        public string EventID { get; set; }                 // 이벤트 ID (예: 201010016925750)
        public string EventName { get; set; }               // 이벤트명 (<주토피아2>1주차...)
        public string EventClassificationCode { get; set; } // 분류 코드 ("20")
        public string EventTypeCode { get; set; }           // 타입 코드 ("101")
        public string EventTypeName { get; set; }           // 타입명 (정보전달형(공지))
        public string ProgressStartDate { get; set; }       // 시작일 (2025.11.26 - 점 구분)
        public string ProgressEndDate { get; set; }         // 종료일 (2025.12.02)
        public int RemainsDayCount { get; set; }            // 남은 일수 (10)
        public string ImageUrl { get; set; }                // 썸네일 이미지 URL
        public string ImageAlt { get; set; }                // 이미지 대체 텍스트 (줄바꿈 포함 상세)
        public int ImageDivisionCode { get; set; }          // 이미지 구분 (20)
        public string CinemaID { get; set; }                // 영화관 ID (빈값 가능)
        public string CinemaName { get; set; }              // 영화관명
        public string CinemaAreaCode { get; set; }          // 지역 코드
        public string CinemaAreaName { get; set; }          // 지역명
        public int DevTemplateYN { get; set; }              // 개발 템플릿 여부 (0)
        public int CloseNearYN { get; set; }                // 마감 임박 여부 (0)
        public int EventWinnerYN { get; set; }              // 당첨자 발표 여부 (0)
        public int EventSeq { get; set; }                   // 정렬 순서 (34)
        public string EventCntnt { get; set; }              // 이벤트 내용 (HTML 등)
        public string EventNtc { get; set; }                // 유의사항 (<P><FONT... HTML 포함 가능)
    }

    // 특정 영화 디테일 페이지
    public class LotteDetailResponse
    {
        public List<EventDetailItem> InfomationDeliveryEventDetail { get; set; }
        public string IsOK { get; set; }             // 성공 여부 ("true")
        public string ResultMessage { get; set; }    // 메시지 ("SUCCESS")
        public string ResultCode { get; set; }       // 결과 코드 (null)
        public string EventResultYn { get; set; }    // 결과 여부 (null)
    }
    public class EventDetailItem
    {
        public string EventID { get; set; }                         // 이벤트 ID
        public string EventName { get; set; }                       // 이벤트명
        public string EventClassificationCode { get; set; }         // 분류 코드 ("20")
        public string ProgressStartDate { get; set; }               // 시작일 (2025-11-26 - 하이픈 구분)
        public string ProgressEndDate { get; set; }                 // 종료일 (2025-12-02)
        public string WinnerAnnouncmentDate { get; set; }           // 당첨자 발표일 (2025-12-03)
        public string ImgUrl { get; set; }                          // 상세 이미지 URL
        public string ImgAlt { get; set; }                          // 상세 텍스트
        public string ListImgUrl { get; set; }                      // 목록용 썸네일 URL
        public string ListImgAlt { get; set; }                      // 목록용 텍스트
        public int ImageDivisionCode { get; set; }                  // 이미지 구분 (20)
        public object ImageGameTypeDivisionCode { get; set; }       // 게임 타입 구분 (null)
        public string EventContents { get; set; }                   // 추가 컨텐츠 (빈값)
        public string EventNotice { get; set; }                     // 공지 사항
        public object WinnerNotice { get; set; }                    // 당첨자 공지 (null)
        public string CinemaID { get; set; }                        // 영화관 ID ("0")
        public string CinemaName { get; set; }                      // 영화관명
        public int EventProgressDivisionCode { get; set; }          // 진행 상태 코드 (10)
        public string EventMovieURL { get; set; }                   // 관련 영화 URL
        public string EventMovieImageURL { get; set; }              // 관련 영화 이미지
        public string EventMovieImageAlt { get; set; }              // 관련 영화 텍스트
        public List<GoodsGiftItem> GoodsGiftItems { get; set; }     // 경품 목록 리스트
        public List<object> ButtonSetting { get; set; }             // 버튼 설정 (빈 배열 [])
        public List<object> JoinStatus { get; set; }                // 참여 상태 (빈 배열 [])
        public string GoodsShowYN { get; set; }                     // 굿즈 노출 여부 ("0")
        public int InformationOfferingAgreementYN { get; set; }     // 정보 제공 동의 여부 (0)
        public string InformationOfferingAgreementContents { get; set; } // 동의 내용
    }
    public class GoodsGiftItem
    {
        public string EventID { get; set; }    // 이벤트 ID
        public string FrGiftID { get; set; }   // 경품 ID (13422)
        public string FrGiftNm { get; set; }   // 경품명 (<주토피아2>1주차...)
    }

    // 경품 수량 조회 페이지
    public class LotteGiftStockResponse
    {
        public List<CinemaDivisionItem> CinemaDivisions { get; set; }     // 지역 구분 (서울, 경기/인천 등)
        public List<CinemaGoodsItem> CinemaDivisionGoods { get; set; }    // 극장별 수량 정보 (핵심 데이터)
        public string IsOK { get; set; }             // "true"
        public string ResultMessage { get; set; }    // "SUCCESS"
        public string ResultCode { get; set; }       // null
        public string EventResultYn { get; set; }    // null
    }
    public class CinemaDivisionItem
    {
        public int DivisionCode { get; set; }          // 구분 코드 (예: 1)
        public string DetailDivisionCode { get; set; } // 상세 구분 코드 (예: "0001")
        public string GroupNameKR { get; set; }        // 지역명 한글 (예: "서울")
        public string GroupNameUS { get; set; }        // 지역명 영문 (예: "Seoul")
        public int SortSequence { get; set; }          // 정렬 순서
        public int CinemaCount { get; set; }           // 해당 지역 극장 수
    }
    public class CinemaGoodsItem
    {
        public int DivisionCode { get; set; }          // 구분 코드
        public string DetailDivisionCode { get; set; } // 상세 구분 코드 ("0001" -> 서울)
        public string CinemaID { get; set; }           // 영화관 ID (예: "1013")
        public string CinemaNameKR { get; set; }       // 영화관명 (예: "가산디지털")
        public string CinemaNameUS { get; set; }       // 영화관명 영문
        public int SortSequence { get; set; }          // 정렬 순서
        public int Cnt { get; set; }                   // [핵심] 현재 남은 수량 (재고)
        public string DetailDivisionNameKR { get; set; } // 지역명 한글 ("서울")
        public string DetailDivisionNameUS { get; set; } // 지역명 영문 ("Seoul")
    }
}