using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTools.DTO
{
    public class RunningStatusDTO
    {
        public int TotalCount { get; set; }
        public List<SearchPlanInfoDTO> Plans { get; set; } = new List<SearchPlanInfoDTO>();
        public int UnExecuteCount { get; set; }
    }
}
