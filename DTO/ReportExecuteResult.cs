using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTools.DTO
{
    public class ReportExecuteResult
    {
        public string ReportName { get; set; }
        public string ReportTitle { get; set; }
        public string PlanName { get; set; }
        public Status Status { get; set; }
        public double TotalConsumingTime { get; set; }
        public double AVGConsumingTime { get; set; }
        public int DataRowCount { get; set; }
        public string ErrorMessage { get; set; }
        public int LoopTimes { get; set; }
    }
}
