using ReportTools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTools.DTO
{
    public class RequestDTO
    {
        public string ID { get; set; }
        public string ServerURL { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string AccountNum { get; set; }
        public string LoopTimes { get; set; }
        public string FileAddress { get; set; }
        public Status Status { get; set; } = Status.UnExectued;
        public List<SearchPlanInfoDTO> Plans { get; set; } = new List<SearchPlanInfoDTO>();

        public RunningStatusDTO RunningStatus { get; set; } = new RunningStatusDTO();

        //获取当前执行这个任务的OpenApi
        public OpenAPI OpenApi { get; set; }

        public string StartTime { get; set; }
        public double TotalRunTime { get; set; }

        public bool IsSystem { get; set; }

        public int Order { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsGetAllData { get; set; }

        public int PageSize { get; set; } = 100;

    }
}
