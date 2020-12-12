using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTools.DTO
{
    public class TimeCompareDTO
    {
        public string LeftPlanName { get; set; }
        public string LeftReportTitle { get; set; }
        public double LeftConsumingTime { get; set; }
        public Status LeftStatus { get; set; }
        public string RightPlanName { get; set; }
        public string RightReportTitle { get; set; }
        public double RightConsumingTime { get; set; }
        public Status RightStatus { get; set; }
        public string SearchName { get; set; }

        public double DiffTime { get; set; }
    }
}
