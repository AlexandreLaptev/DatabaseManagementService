using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace DatabaseManagement.Controllers
{
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IBackgroundJobClient backgroundJobs, ILogger<HomeController> logger)
        {
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        [HttpPost("UpdateDatabase")]
        public string UpdateDatabase()
        {
            string message = null;
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var queues = monitoringApi.Queues();

            if (!queues.Any())
            {
                _backgroundJobs.Enqueue<DatabaseUpgrater>(method => method.PerformAsync());
                message = "Request to update database has been submitted.";
            }
            else
            {
                message = "Request to update database has been denied because another process is running.";
            }

            _logger.LogInformation(message);
            return message;
        }
    }
}