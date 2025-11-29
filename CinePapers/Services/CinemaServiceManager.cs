using CinePapers.Models.CGV_WebView;
using CinePapers.Models.Common;
using CinePapers.Models.Lottee;
using CinePapers.Models.Mega;
using System.Collections.Generic;

namespace CinePapers.Services
{
    public static class CinemaServiceManager
    {
        public static List<ICinemaService> GetAvailableServices()
        {
            return new List<ICinemaService>
            {
                new CgvWebViewService(),
                new LotteCinemaService(),
                new MegaCinemaService()
            };
        }
    }
}