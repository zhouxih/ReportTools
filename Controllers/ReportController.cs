using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReportTools.DTO;
using ReportTools.Interface;
using ReportTools.Model;
using ReportTools.Utility;
namespace ReportTools.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        //最多支持同时5个任务进行
        private const int _maxThread = 5;

        private readonly IReportQueryTaskService _service;
        private readonly string _url = "https://localhost:44351";
        private  static string _csvPath = string.Empty;
        private static string _filePath = string.Empty;
        private static List<RequestDTO> _reportQueryTask = new List<RequestDTO>();
        private static readonly object _lock = new object();

        public ReportController(IReportQueryTaskService service,IWebHostEnvironment webHostEnvironment)
        {
            _service = service;
            _csvPath = webHostEnvironment.WebRootPath;
            _filePath = AppContext.BaseDirectory;
        }

        /// <summary>
        /// 添加Task任务，并序列化到本地随时做执行处理
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        [HttpPost]
        public string Post([FromBody] dynamic d)
        {
            //设置任务信息
            RequestDTO request = JsonConvert.DeserializeObject<RequestDTO>(d.ToString());
            request.Status = Status.UnExectued;
            request.ID = DateTime.Now.ToString()+"TASK";
            request.ID = request.ID.Replace(' ','-');
            request.ID = request.ID.Replace('/', '-');
            request.ID = request.ID.Replace(':', '-');
            request.StartTime = DateTime.Now.ToString();
            if (_reportQueryTask.Count != 0) 
            {
                request.Order = _reportQueryTask.OrderBy(item => item.Order).LastOrDefault().Order + 1;
            }
            //将任务放入列表
            _reportQueryTask.Add(request);
            return "添加任务成功!等待执行所有报表查询...";
        }

        /// <summary>
        /// 获取指定任务的运行明细
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("RunningDetailStatus")]
        public List<SearchPlanInfoDTO> GetRunningDetailStatus(string id) 
        {
           RequestDTO request =  _reportQueryTask.Where(item => item.ID == id).FirstOrDefault();
           List<SearchPlanInfoDTO> plans = request.Plans;
           return plans;
        }
        /// <summary>
        /// 监控任务运行状态,开启任务执行。
        /// </summary>
        [HttpGet("RunningStatus")]
        public List<RequestDTO> GetRunningStatus() 
        {
            List<RequestDTO> response = new List<RequestDTO>();
            for (int i = 0; i < _reportQueryTask.Count; i++) 
            {
                RequestDTO temp = new RequestDTO();
                temp.ID = _reportQueryTask[i].ID;
                temp.AccountNum = _reportQueryTask[i].AccountNum;
                temp.ServerURL = _reportQueryTask[i].ServerURL;
                temp.UserName = _reportQueryTask[i].UserName;
                temp.Status = _reportQueryTask[i].Status;
                temp.LoopTimes = _reportQueryTask[i].LoopTimes;
                temp.StartTime = _reportQueryTask[i].StartTime;
                temp.FileAddress = _reportQueryTask[i].FileAddress;
                temp.ErrorMessage = _reportQueryTask[i].ErrorMessage;
                temp.RunningStatus = new RunningStatusDTO();
                temp.RunningStatus.UnExecuteCount = _reportQueryTask[i].Plans.Where(item => item.Status == Status.UnExectued).Count();
                temp.RunningStatus.TotalCount = _reportQueryTask[i].Plans.Count;
                response.Add(temp);
            }
            return response;

        }

       

        [HttpGet("AwakeTask")]
        public void AwakeTask() 
        {
            if (_reportQueryTask.Count <= 0) 
            {
                return;
            }
            lock (_lock) 
            {
                BeginExecute();
            }
        }
        /// <summary>
        /// 获取查询报表的json返回值
        /// </summary>
        /// <param name="taskid"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("ReportResult")]
        public string ReportResult(string taskid,int id) 
        {
            RequestDTO request = _reportQueryTask.Where(item => item.ID == taskid).FirstOrDefault();
            if (request == null) 
            {
                return string.Empty;
            }
            List<SearchPlanInfoDTO> plans = request.Plans;
            string result = plans.Where(item => item.ID == id).Select(item => item.Result).FirstOrDefault();
            return result;
        }
        /// <summary>
        /// 删除指定任务
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public bool Delete(string id) 
        {
            RequestDTO request = _reportQueryTask.Where(item => item.ID == id).FirstOrDefault();
            if (request != null)
            {
                _reportQueryTask.Remove(request);
                string filePath = Path.Combine(_filePath, "ResultFile", id);
                //删除本地文件夹中的数据
                if (Directory.Exists(filePath))
                {
                    FileHelper.DeleteDirectory(filePath);
                }
            }
            else 
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 下载指定任务详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private void GetCSVFile(string id) 
        {
            RequestDTO request = _reportQueryTask.Where(item => item.ID == id).FirstOrDefault();
            string basePath = _csvPath;
            if (!Directory.Exists(Path.Combine(_filePath, "ResultFile", id)))
            {
                Directory.CreateDirectory(Path.Combine(_filePath, "ResultFile", id));
            }
            string filePath = Path.Combine(_filePath, "ResultFile", id,"Sum.csv");
            CsvExportHelper.SaveCSV(request,filePath);
        }

        /// <summary>
        /// 执行Task任务
        /// </summary>
        private void BeginExecute() 
        {
           
            if (_reportQueryTask.Count == 0) 
            {
                return;
            }
            //重新排序任务列表
            _reportQueryTask = _reportQueryTask.OrderBy(item=>item.Order).ToList();
            //找到最大的序号
            int maxOrder = _reportQueryTask.Last().Order;

            //获取正在执行和未执行的任务
            List<RequestDTO> UnExecuteTask = _reportQueryTask.Where(item => item.Status == Status.UnExectued).OrderBy(item=>item.Order).ToList();
            List<RequestDTO> OnExecuteTask = _reportQueryTask.Where(item => item.Status == Status.OnExectued).OrderBy(item => item.Order).ToList();

            //判断是否有需要执行的任务
            if (UnExecuteTask.Count <=0)
            {
                return;
            }
            //支持的最大同时执行的线程数
            if (OnExecuteTask.Count >= _maxThread) 
            {
                return;
            }
            //查看是否有相同的任务
            foreach (var task in OnExecuteTask)
            {
                if (UnExecuteTask[0].UserName == task.UserName
                    && UnExecuteTask[0].AccountNum == task.AccountNum
                    && UnExecuteTask[0].ServerURL == task.ServerURL)
                {
                    UnExecuteTask[0].Order = maxOrder + 1;
                    return;
                }
            }
            //立马设置未正在进行时
            UnExecuteTask[0].Status = Status.OnExectued;
            ExeCuteQuery(UnExecuteTask[0]);
            

            //for (int i = 0; i < canExecuteNum; i++) 
            //{
            //    bool flag = true;
            //    //未执行的任务中是否有与正执行的相同任务
            //    foreach (var task in OnExecuteTask) 
            //    {
            //        if (UnExecuteTask[i].UserName == task.UserName 
            //            && UnExecuteTask[i].AccountNum == task.AccountNum
            //            && UnExecuteTask[i].ServerURL == task.ServerURL) 
            //        {
            //            UnExecuteTask[i].Order = maxOrder + 1;
            //            flag = false;
            //            break;
            //        }
            //    }
            //    if (flag) 
            //    {

            //        UnExecuteTask[i].Status = Status.OnExectued;
            //        //执行查询任务
            //        ExeCuteQuery(UnExecuteTask[i]);
            //        //再次填装OnExecute的列表
            //        OnExecuteTask = _reportQueryTask.Where(item => item.Status == Status.OnExectued).ToList();

            //    }
            //}
        }
        /// <summary>
        /// 将文件中的任务读取到内存中
        /// </summary>
        [HttpGet("GetHistoryData")]
        public void GetHistoryData() 
        {
            string path = Path.Combine(_filePath, "ResultFile");
            IEnumerable<string> files = Directory.EnumerateDirectories(path);
            foreach(var item in files) 
            {
                string result = Path.Combine(item, "ResultFile.json");
                if (System.IO.File.Exists(result)) 
                {
                    bool flag = true;
                    string task = System.IO.File.ReadAllText(result);
                    RequestDTO data = JsonConvert.DeserializeObject<RequestDTO>(task);
                    //判断当前_reportQueryTask是否已经存在了这个任务
                    foreach (var dto in _reportQueryTask) 
                    {
                        if (dto.ID == data.ID) 
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag) 
                    {
                        _reportQueryTask.Add(data);
                    }
                }
                
            }
        }
        /// <summary>
        /// 比较两条数据的差异
        /// </summary>
        /// <param name="lid"></param>
        /// <param name="rid"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        [HttpGet("ComparaData")]
        public List<string> ComparaData(string lid,string rid,string searchName,string planName,int pageNum) 
        {
            List<string> result = new List<string>();
            string directoryPath = Path.Combine(_filePath, "ResultFile");
            if (pageNum > 1)
            {
                planName += "-" + pageNum + "Page.json";
            }
            else 
            {
                planName +=".json";
            }
            string leftFile = Path.Combine(directoryPath, lid, searchName, planName);
            string rightFile = Path.Combine(directoryPath, rid, searchName, planName);
            if (!System.IO.File.Exists(leftFile) || !System.IO.File.Exists(rightFile)) 
            {
                return result;
            }
            string json1 = FileHelper.DeSerialize(leftFile).ToString();
            string json2 = FileHelper.DeSerialize(rightFile).ToString();
            json1 = json1.Replace(" ","");
            json2 = json2.Replace(" ", "");
            List<string> difference = JsonObjectCompare.GetJsonDifference(json1,json2);
            foreach (var item in difference) 
            {
                if (json1.Contains(item)) 
                {
                    json1 = json1.Replace(item, "<font color='red'>" + item + "</font>");
                }
            }
            json1 = "<font color='red'>一共有" + difference.Count + "处差异!超过3处则有异常！！！</font>" + json1;
            json2 = "<font color='red'>一共有" + difference.Count + "处差异!超过3处则有异常！！！</font>" + json2;
            result.Add(json1);
            result.Add(json2);
            return result;

        }

        /// <summary>
        /// 清除当前任务列表
        /// </summary>
        [HttpGet("CleanTask")]
        public void CleanTask() 
        {
            _reportQueryTask.Clear();
        }


        [HttpGet("ReportExecuteResult")]
        public List<ReportExecuteResult> GetAllReportExecuteResults(string ID)
        {
            List<ReportExecuteResult> result = new List<ReportExecuteResult>();
            string directoryPath = Path.Combine(_filePath, "ResultFile");
            string taskFilePath = Path.Combine(directoryPath, ID, "Sum.csv");

            List<SearchPlanInfoDTO> task = CsvExportHelper.GetSearchPlanInfos(taskFilePath);
            RequestDTO  dto = _reportQueryTask.Find(item => item.ID==ID);
            for (int i = 0; i < task.Count; i++)
            {
                result.Add(new ReportExecuteResult
                {
                    ReportName = task[i].SearchName,
                    ReportTitle = task[i].Name,
                    PlanName = task[i].PlanName,
                    Status = task[i].Status,
                    LoopTimes = int.Parse(dto.LoopTimes),
                    AVGConsumingTime = task[i].ConsumingTimes,
                    TotalConsumingTime = dto.TotalRunTime,
                    DataRowCount = task[i].RowsCount,
                    ErrorMessage = task[i].ErrorMessage
                }) ;
            }
            return result;

        }
        [HttpGet("ComparaTask")]
        public List<TimeCompareDTO> ComparaTask(string lid,string rid) 
        {
            List<TimeCompareDTO> result =new  List<TimeCompareDTO>();
            //将这两个任务的读取到内存
            string directoryPath = Path.Combine(_filePath, "ResultFile");
            string leftTaskFilePath = Path.Combine(directoryPath, lid,"Sum.csv");
            string rightTaskFilePath = Path.Combine(directoryPath, rid, "Sum.csv");

            List<SearchPlanInfoDTO> leftTask = CsvExportHelper.GetSearchPlanInfos(leftTaskFilePath);
            List<SearchPlanInfoDTO> rightTask = CsvExportHelper.GetSearchPlanInfos(rightTaskFilePath);
            int count = leftTask.Count > rightTask.Count ? rightTask.Count : leftTask.Count;
            for (int i = 0; i < count; i++) 
            {
                result.Add(new TimeCompareDTO
                {
                    SearchName = leftTask[i].SearchName,
                    LeftPlanName = leftTask[i].PlanName,
                    LeftReportTitle = leftTask[i].Name,
                    LeftConsumingTime = leftTask[i].ConsumingTimes,
                    LeftStatus = leftTask[i].Status,
                    RightPlanName = rightTask[i].PlanName,
                    RightReportTitle = rightTask[i].Name,
                    RightConsumingTime = rightTask[i].ConsumingTimes,
                    RightStatus = rightTask[i].Status,
                    DiffTime = Math.Round(((leftTask[i].ConsumingTimes - rightTask[i].ConsumingTimes) / leftTask[i].ConsumingTimes) * 100, 2)
                }) ;
            }
            return result;


        }
        /// <summary>
        /// 执行查询任务操作
        /// </summary>
        /// <param name="openAPI"></param>
        private void ExeCuteQuery(RequestDTO request) 
        {
            new Task(() =>
            {
                _service.ExcuteReportQueryTask(request);
                GetCSVFile(request.ID);
                //序列化到本地
                new Task(
                    () => { FileHelper.Serialize(request, Path.Combine(_filePath, "ResultFile")); }
                    ).Start();
            }).Start();
        }


    }
}
