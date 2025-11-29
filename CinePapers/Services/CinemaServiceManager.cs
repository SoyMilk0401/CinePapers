using CinePapers.Models.CGV_WebView;
using CinePapers.Models.Common;
using CinePapers.Models.Lottee;
using CinePapers.Models.Mega;
using System.Collections.Generic;

namespace CinePapers.Services
{
    public static class CinemaServiceManager
    {
        private static List<ICinemaService> _services;

        public static List<ICinemaService> GetAvailableServices()
        {
            if (_services == null)
            {
                _services = new List<ICinemaService>
            {
                new CgvWebViewService(),
                new LotteCinemaService(),
                new MegaCinemaService()
            };
            }
            return _services;
        }
    }
}