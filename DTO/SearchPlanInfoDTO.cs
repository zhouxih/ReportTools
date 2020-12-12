using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTools.DTO
{
    public class SearchPlanInfoDTO
    {
        public int ID { get; set; }
        public string SearchName { get; set; }
        public string PlanName { get; set; }
        public int SearchStyle { get; set; }
        public bool IsDefault { get; set; }
        public bool IsSystem { get; set; }
        public bool IsPublicPlan { get; set; }
        public bool IsDisplay { get; set; }
        public string ErrorMessage { get; set; }
        //耗时
        public double ConsumingTimes { get; set; }
        public string Name { get; set; }
        public Status Status { get; set; } = Status.UnExectued;
        public string Result { get; set; }
        public int RowsCount { get; set; }

        public int FatherID { get; set; }

        public bool IsFather{ get; set; }

        public string TaskSessionID { get; set; }
    }

    public enum Status
    {
        Succeed,
        Fail,
        UnExectued,
        OnExectued
    }
}
