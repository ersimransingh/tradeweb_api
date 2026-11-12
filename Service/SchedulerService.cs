using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TradeWeb.API.Repository;

namespace TradeWeb.API.Service
{
    public class SchedulerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private DateTime? _lastExecutionTime;
        private readonly IServiceScopeFactory _scopeFactory;
        private bool _isRunning = false;

        public SchedulerService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessSchedule();
                }
                catch (Exception ex)
                {
                    // Log Error
                    try
                    {
                        InsertLog(
                            0,
                            "SchedulerService",
                            "",
                            "",
                            "",
                            ex.ToString(),
                            "SA");
                    }
                    catch
                    {
                        // Don't let logging errors break scheduler
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessSchedule()
        {
            string parameterValue = await GetParameterValue();

            if (string.IsNullOrWhiteSpace(parameterValue))
                return;

            string[] arr = parameterValue.Split('~');

            if (arr == null || arr.Length != 4)
                return;

            TimeSpan startTime = TimeSpan.Parse(arr[0]);
            TimeSpan endTime = TimeSpan.Parse(arr[1]);
            int interval = Convert.ToInt32(arr[2]);
            string spName = arr[3];

            DateTime now = DateTime.Now;

            DateTime todayStart = now.Date.Add(startTime);
            DateTime todayEnd = now.Date.Add(endTime);

            if (now < todayStart || now > todayEnd)
                return;

            bool execute = false;

            if (_lastExecutionTime == null)
            {
                execute = true;
            }
            else
            {
                DateTime nextRun =
                    _lastExecutionTime.Value.AddMinutes(interval);

                if (now >= nextRun)
                    execute = true;
            }

            //if (execute)
            //{
            //    _lastExecutionTime = DateTime.Now;
            //    await ExecuteStoredProcedure(spName);
            //}

            if (execute && !_isRunning)
            {
                try
                {
                    _isRunning = true;

                    await ExecuteStoredProcedure(spName);
                    _lastExecutionTime = DateTime.Now;
                }
                finally
                {
                    _isRunning = false;
                }
            }

            // Reset for next day
            if (_lastExecutionTime != null &&
                _lastExecutionTime.Value.Date < DateTime.Today)
            {
                _lastExecutionTime = null;
            }
        }

        private async Task<string> GetParameterValue()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var utility =
                    scope.ServiceProvider.GetRequiredService<UtilityCommon>();
                string connStr = utility.GetConnectionStr();

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    SqlCommand cmd = new SqlCommand(
                        @"SELECT sp_sysvalue
                  FROM WebParameter
                  WHERE sp_parmcd='SchedulerConfig'",
                        conn);

                    object result = await cmd.ExecuteScalarAsync();

                    return result?.ToString();
                }
            }
        }

        private async Task ExecuteStoredProcedure(string procedureName)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var utility =
                    scope.ServiceProvider.GetRequiredService<UtilityCommon>();
                string connStr = utility.GetConnectionStr();

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    SqlCommand cmd =
                        new SqlCommand(procedureName, conn);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public void InsertLog(long dblParentSrNo, string strReqSrc, string strReqID, string strRequest, string strResponse, string strException, string strUpdateBy)
        {
            string strsql = "";

            using (var scope = _scopeFactory.CreateScope())
            {
                var utility =
                    scope.ServiceProvider.GetRequiredService<UtilityCommon>();
                string connStr = utility.GetConnectionStr();

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    strsql = @"IF OBJECT_ID('dbo.tbl_GenericAPIDebugLog', 'U') IS NULL
                           BEGIN
                               CREATE TABLE tbl_GenericAPIDebugLog(
                               [SerialNo] [int] IDENTITY(1,1) NOT NULL,
                               [ParentSerialNo] [int] NOT NULL,
                               [RequestSource] [varchar](50) NOT NULL,
                               [RequestUniqueID] [varchar](100) NOT NULL,
                               [RequestString] [varchar](max) NOT NULL,
                               [ResponseString] [varchar](max) NOT NULL,
                               [ExceptionMessage] [varchar](max) NOT NULL,
                               [UpdateBy] [varchar](50) NOT NULL,
                               [UpdateTimeStamp] [datetime] NOT NULL,
                               CONSTRAINT [PK_tbl_GenericAPIDebugLog] PRIMARY KEY NONCLUSTERED 
                               (SerialNo)
                               ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
                           END";

                    using (SqlCommand cmd = new SqlCommand(strsql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    strsql = "insert into tbl_GenericAPIDebugLog(ParentSerialNo, RequestSource, RequestUniqueID, RequestString, ResponseString, ExceptionMessage, UpdateBy, UpdateTimeStamp) values(@ParentSerialNo , @RequestSource, @RequestUniqueID, @RequestString, '', @ExceptionMessage, '', GETDATE())";

                    using (SqlCommand command = new SqlCommand(strsql, conn))
                    {
                        command.Parameters.AddWithValue("@ParentSerialNo", dblParentSrNo);
                        command.Parameters.AddWithValue("@RequestSource", strReqSrc);
                        command.Parameters.AddWithValue("@RequestUniqueID", strReqID);
                        command.Parameters.AddWithValue("@RequestString", strRequest);
                        command.Parameters.AddWithValue("@ResponseString", strResponse);
                        command.Parameters.AddWithValue("@ExceptionMessage", strException);
                        command.Parameters.AddWithValue("@UpdateBy", strUpdateBy);

                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
