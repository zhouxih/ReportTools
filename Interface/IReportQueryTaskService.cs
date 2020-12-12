using ReportTools.DTO;
using ReportTools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTools.Interface
{
    public interface IReportQueryTaskService
    {
        IList<SearchPlanInfoDTO> GetAllSearchPlanInfos(OpenAPI openAPI);

        void ExcuteReportQueryTask(RequestDTO _reportQueryTask);
    }
}
