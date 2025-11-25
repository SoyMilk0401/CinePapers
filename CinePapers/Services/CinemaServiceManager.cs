using System.Collections.Generic;
using CinePapers.Models.Common;
using CinePapers.Models.Mega;

namespace CinePapers.Services
{
    public static class CinemaServiceManager
    {
        public static List<ICinemaService> GetAvailableServices()
        {
            return new List<ICinemaService>
            {
                new LotteCinemaService(),
                new MegaCinemaService()
            };
        }
    }
}