using System.Collections.Generic;
using CinePapers.Models.Common;
using CinePapers.Models.CGV_WebView;
using CinePapers.Models.Mega;

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