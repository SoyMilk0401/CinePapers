using CinePapers.Models.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CinePapers.ViewModels
{
    // 이벤트 정보 조회 로직
    public class MainViewModel
    {
        private int _currentPage = 1;
        private bool _isEnded = false;
        public bool IsLoading { get; private set; } = false;

        public ICinemaService CurrentService { get; set; }

        public async Task<List<CinemaEventItem>> LoadEventsAsync(string categoryCode, string keyword, bool isReload)
        {
            if (IsLoading || CurrentService == null) return null;
            if (isReload)
            {
                _currentPage = 1;
                _isEnded = false;
            }
            if (_isEnded) return null;

            IsLoading = true;
            try
            {
                var events = await CurrentService.GetEventsListAsync(categoryCode, _currentPage, keyword);

                if (events == null || events.Count == 0)
                {
                    _isEnded = true;
                    return new List<CinemaEventItem>();
                }

                _currentPage++;
                return events;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}