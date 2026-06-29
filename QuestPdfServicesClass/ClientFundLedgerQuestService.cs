using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using TradeWeb.API.Repository;
using TradeWeb.API.QuestPdfTemplates;
using Newtonsoft.Json;
using System.Text.Json;

namespace TradeWeb.API.QuestPdfServicesClass
{
    public class ClientFundLedgerQuestService
    {

        private readonly IConfiguration _config;
        private readonly UtilityCommon _objUtility;
        public DataSet TransactionData { get; set; }
        public ClientFundLedgerQuestService(IConfiguration config, UtilityCommon objUtility)
        {
            _config = config;
            QuestPDF.Settings.License = LicenseType.Community;
            this._objUtility = objUtility;
        }


        /// <summary>
        /// Processes a pre-filled DataTable instead of querying SQL.
        /// </summary>
        //public async Task<DataSet> ClientFundLedgerProcessAsync(DataTable emailDT, DataTable compDT, DataSet dsCFL, string headerImagePath, string logoPath, string userId)
        //{
        //    if (dsCFL == null) throw new ArgumentNullException(nameof(dsCFL));

        //    DataTable masterTable = dsCFL.Tables[0];
        //    DataTable detailTable = dsCFL.Tables[1];

        //    DataSet newDS = new DataSet();
        //    newDS.Tables.Add(dsCFL.Tables[1].Copy());
        //    newDS.Tables.Add(dsCFL.Tables[2].Copy());
        //    newDS.AcceptChanges();

        //    var detailLookup =
        //        detailTable.AsEnumerable()
        //        .GroupBy(x => x["ClientCode"].ToString().Trim())
        //        .ToDictionary(
        //            g => g.Key,
        //            g => g.CopyToDataTable());

        //    var numWorkers = GetIntEnv("NUM_WORKERS", Environment.ProcessorCount > 0 ? Environment.ProcessorCount : 4);

        //    var channel = Channel.CreateBounded<PdfJob>(new BoundedChannelOptions(numWorkers * 10)
        //    {
        //        FullMode = BoundedChannelFullMode.Wait
        //    });

        //    var sw = Stopwatch.StartNew();
        //    var counters = new CFL_Counters();
        //    var queueCount = 0;
        //    await using var sqlConn = new SqlConnection(BuildConnectionString());
        //    await sqlConn.OpenAsync();

        //    var workers = new List<Task>();

        //    for (int i = 0; i < numWorkers; i++)
        //    {
        //        workers.Add(Task.Run(async () =>
        //        {
        //            await using var workerConn =
        //                new SqlConnection(BuildConnectionString());

        //            await workerConn.OpenAsync();

        //            await foreach (var job in channel.Reader.ReadAllAsync())
        //            {
        //                Interlocked.Decrement(ref queueCount);

        //                try
        //                {
        //                    var pdfBytes = PdfWriterQuest.GeneratePdfBytes(
        //                        compDT,
        //                        job.MasterRow,
        //                        newDS,
        //                        headerImagePath,
        //                        logoPath);

        //                    await EmailDetailsWriter.InsertAsync(
        //                        workerConn,
        //                        job,
        //                        emailDT.Rows[0],
        //                        pdfBytes,
        //                        userId);

        //                    Interlocked.Increment(ref counters.TotalProcessed);
        //                }
        //                catch (Exception ex)
        //                {
        //                    Interlocked.Increment(ref counters.TotalErrors);
        //                }
        //            }
        //        }));
        //    }

        //    foreach (DataRow masterRow in masterTable.Rows)
        //    {
        //        Interlocked.Increment(ref counters.TotalRowsReceived);

        //        string clientCode =
        //            masterRow["ClientCode"]?.ToString()?.Trim();

        //        string eml = masterRow["ClientEmailId"]?.ToString()?.Trim();

        //        if (!detailLookup.TryGetValue(clientCode, out DataTable details2))
        //        {
        //            Console.WriteLine($"No details found for {clientCode}");
        //        }

        //        detailLookup.TryGetValue(
        //            clientCode,
        //            out DataTable details);

        //        var job = new PdfJob
        //        {
        //            CompanyCode = "A",
        //            ClientCode = clientCode,
        //            ToEmail = masterRow["ClientEmailId"]?.ToString(),
        //            MasterRow = masterRow,
        //            DetailTable = details
        //        };
        //        Interlocked.Increment(ref queueCount);
        //        await channel.Writer.WriteAsync(job);
        //    }

        //    channel.Writer.Complete();
        //    await Task.WhenAll(workers);

        //    DataTable dt = new DataTable();
        //    dt.Columns.Add("MessageType", typeof(string));
        //    dt.Columns.Add("MessageText", typeof(string));
        //    dt.Rows.Add("SUCCESS", counters.TotalProcessed);
        //    dt.Rows.Add("ERROR", counters.TotalErrors);

        //    return CreateResultTable(counters);

        //}


        public async Task<DataSet> ClientFundLedgerProcessAsync(DataTable emailDT, DataTable compDT, string userId)
        {
            //if (dsCFL == null) throw new ArgumentNullException(nameof(dsCFL));


            string jsonFilePath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/ClientFundLedgerNew.json";
            //string jsonFilePath = "E:/My Work/16022026/TradeWebAPI/QuestPdfServicesClass/JsonFiles/CombineContractNote.json";
            var json = File.ReadAllText(jsonFilePath);
            var root = JsonDocument.Parse(json).RootElement;
            var layoutItems = JsonConvert.DeserializeObject<dynamic>(json);

            var scJsonSrl = JsonConvert.SerializeObject(layoutItems["sections"]);
            var sectionMainJsonObj = JsonConvert.DeserializeObject<object>(scJsonSrl);

            var headerJson = layoutItems["globalHeader"];
            var footerJson = layoutItems["globalFooter"];
            var sectionJson = layoutItems["sections"];
            var tableSecJson = layoutItems["tableSections"];

            DataRow row1 = compDT.Rows[0];

            foreach (var item in headerJson)
            {
                string colName = item["DataField"].ToString();
                if (compDT.Columns.Contains(colName))
                    item["DataField"] = Convert.ToString(row1[colName]);
            }
            foreach (var item in footerJson)
            {
                string colName = item["DataField"].ToString();
                if (compDT.Columns.Contains(colName))
                    item["DataField"] = Convert.ToString(row1[colName]);
            }
            //// For Master table and Transaction SP 
            string masterTableName = "", transactionSP = "";
            string dataSource = null;

            foreach (var section in tableSecJson)
            {
                if ((string)section["TableName"] == "MasterTable")
                {
                    masterTableName = (string)section["DataSource"];
                }
                else if ((string)section["TableName"] == "TransactionTable")
                {
                    transactionSP = (string)section["DataSource"];
                }
            }
            //var masterTable = ((JArray)layoutItems["tableSections"])
            //                        .FirstOrDefault(x => (string)x["TableName"] == "MasterTable");
            //var transactionTable = ((JArray)layoutItems["tableSections"])
            //                        .FirstOrDefault(x => (string)x["TableName"] == "TransactionTable");
            //string masterSource = (string)masterTable?["DataSource"];
            //string transactionSource = (string)transactionTable?["DataSource"];


            string finalStrJson = JsonConvert.SerializeObject(layoutItems);

            await using var sqlConn = new SqlConnection(BuildConnectionString());
            await sqlConn.OpenAsync();
            DataTable masterTable = await GetMasterDataAsync(sqlConn, masterTableName);
            if (masterTable == null) throw new ArgumentNullException(nameof(masterTable));

            //return new DataSet();

            var numWorkers = GetIntEnv("NUM_WORKERS", Environment.ProcessorCount > 0 ? Environment.ProcessorCount : 4);

            var channel = Channel.CreateBounded<PdfJob>(new BoundedChannelOptions(numWorkers * 10)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
            var sw = Stopwatch.StartNew();
            var counters = new CFL_Counters();
            var queueCount = 0;

            var workers = new List<Task>();
            for (int i = 0; i < numWorkers; i++)
            {
                workers.Add(Task.Run(async () =>
                {
                    await using var workerConn =
                        new SqlConnection(BuildConnectionString());

                    await workerConn.OpenAsync();

                    await foreach (var job in channel.Reader.ReadAllAsync())
                    {
                        Interlocked.Decrement(ref queueCount);

                        try
                        {
                            var transactionDS =
                               await GetClientTransactionDataAsync(
                                   workerConn,
                                   job.ClientCode, transactionSP);

                            var pdfBytes = PdfWriterQuest.GeneratePdfBytes(
                               job.MasterRow,
                                transactionDS,
                                finalStrJson);

                            await EmailDetailsWriter.InsertAsync(
                                workerConn,
                                job,
                                emailDT.Rows[0],
                                pdfBytes,
                                userId);

                            sectionJson = sectionMainJsonObj;
                            Interlocked.Increment(ref counters.TotalProcessed);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref counters.TotalErrors);
                        }
                    }
                }));
            }

            foreach (DataRow masterRow in masterTable.Rows)
            {
                var clientCode = masterRow["ClientCode"]?.ToString()?.Trim();

                var job = new PdfJob
                {
                    CompanyCode = "A",
                    ClientCode = clientCode,
                    ToEmail = masterRow["ClientEmailId"]?.ToString(),
                    MasterRow = masterRow
                };

                await channel.Writer.WriteAsync(job);

                Interlocked.Increment(ref queueCount);
                Interlocked.Increment(ref counters.TotalRowsReceived);
            }

            channel.Writer.Complete();
            await Task.WhenAll(workers);

            DataTable dt = new DataTable();
            dt.Columns.Add("MessageType", typeof(string));
            dt.Columns.Add("MessageText", typeof(string));
            dt.Rows.Add("SUCCESS", counters.TotalProcessed);
            dt.Rows.Add("ERROR", counters.TotalErrors);

            return CreateResultTable(counters);

        }


        private async Task<DataTable> GetMasterDataAsync(SqlConnection conn, string tableName)
        {
            using var cmd = new SqlCommand("Select Top 1 * FROM " + tableName + " Where IsProcess = 'N' ", conn);
            cmd.CommandType = CommandType.Text;
            var ds = new DataSet();
            using var da = new SqlDataAdapter(cmd);
            await Task.Run(() => da.Fill(ds));
            return ds.Tables.Count > 0 ? ds.Tables[0] : null;
        }
        private async Task<DataSet> GetClientTransactionDataAsync(SqlConnection conn, string clientCode, string spName)
        {
            using var cmd = new SqlCommand(spName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ClientCode", clientCode);
            var ds = new DataSet();
            using var da = new SqlDataAdapter(cmd);
            await Task.Run(() => da.Fill(ds));
            return ds;
        }

        private string BuildConnectionString()
        {
            return _objUtility.GetConnectionStr();
        }

        private static DataSet CreateResultTable(CFL_Counters counters)
        {
            DataSet dsRtn = new DataSet();
            var dt = new DataTable();
            dt.Columns.Add("MessageType", typeof(string));
            dt.Columns.Add("Count", typeof(int));

            dt.Rows.Add("SUCCESS", counters.TotalProcessed);
            dt.Rows.Add("ERROR", counters.TotalErrors);
            dsRtn.Tables.Add(dt);
            dsRtn.Tables[0].TableName = "rs0";
            return dsRtn;
        }

        private int GetIntEnv(string name, int fallback)
        {
            var value = _config[name];
            return int.TryParse(value, out var parsed) ? parsed : fallback;
        }

    }

    public class ReportDefinitionItems
    {
        public string Label { get; set; }
        public string DataField { get; set; }
        public int Position { get; set; }   // 1-12
        public int RowNo { get; set; }   // 1-12
        public int Width { get; set; }      // 1-12
        public string Align { get; set; }
        public int FontSize { get; set; }
        public bool IsBold { get; set; }
    }

    public class PdfJob
    {
        public string CompanyCode { get; set; }
        public string ClientCode { get; set; }
        public string ToEmail { get; set; }

        public DataRow MasterRow { get; set; }
        public DataTable DetailTable { get; set; }
    }

    public sealed class CFL_Counters
    {
        public int TotalProcessed;
        public int TotalErrors;
        public int TotalRowsReceived;
    }
    public static class PdfWriterQuest
    {
        public static byte[] GeneratePdfBytes(DataRow masterRow, DataSet detailDS, string strJson)
        {
            var document = new ClientFundLedgerTemplate(masterRow, detailDS, strJson);

            using var ms = new MemoryStream();

            document.GeneratePdf(ms);

            return ms.ToArray();
        }
    }

    static class EmailDetailsWriter
    {
        private const string InsertSql = @"
        INSERT INTO dbo.Digital_Emaildetails
        (
            dd_companycode, dd_clientcd, dd_dt, dd_DocumentType, dd_computername,
            dd_EmailParamCode, dd_toEmailid, dd_subject, dd_bodyText, dd_document, dd_filename, dd_CreatedBy
        )
        VALUES
        (
            @CompanyCode, @ClientCode, @Dt, @DocumentType, @ComputerName,
            @EmailParamCode, @ToEmailId, @Subject, @BodyText, @Document, @FileName, @CreatedBy
        );
    ";

        public static async Task InsertAsync(SqlConnection conn, PdfJob job, DataRow emailRow, byte[] pdfBytes, string userId)
        {
            //using var cmd = conn.CreateCommand();
            //cmd.CommandText = InsertSql;
            //cmd.Parameters.AddWithValue("@CompanyCode", job.CompanyCode ?? "");
            //cmd.Parameters.AddWithValue("@ClientCode", job.ClientCode ?? "");
            //cmd.Parameters.AddWithValue("@Dt", DateTime.Now.ToString("yyyyMMdd"));
            //cmd.Parameters.AddWithValue("@DocumentType", emailRow.Table.Columns.Contains("DocumentType") ? emailRow["DocumentType"] ?? "" : "");
            //cmd.Parameters.AddWithValue("@ComputerName", Environment.MachineName);
            //cmd.Parameters.AddWithValue("@EmailParamCode", emailRow.Table.Columns.Contains("EmailParamCode") ? emailRow["EmailParamCode"] ?? "" : "");
            //cmd.Parameters.AddWithValue("@ToEmailId", job.ToEmail ?? "");
            //cmd.Parameters.AddWithValue("@Subject", emailRow.Table.Columns.Contains("EmailSubject") ? emailRow["EmailSubject"] ?? "" : "");
            //cmd.Parameters.AddWithValue("@BodyText", emailRow.Table.Columns.Contains("EmailBodyText") ? emailRow["EmailBodyText"] ?? "" : "");
            //cmd.Parameters.Add("@Document", System.Data.SqlDbType.VarBinary, pdfBytes.Length).Value = pdfBytes;
            //cmd.Parameters.AddWithValue("@FileName", emailRow.Table.Columns.Contains("EmailFileName") ? emailRow["EmailFileName"] ?? "" : "");
            //cmd.Parameters.AddWithValue("@CreatedBy", userId);

            using var transaction = conn.BeginTransaction();

            try
            {
                // Insert
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = InsertSql;

                    cmd.Parameters.AddWithValue("@CompanyCode", job.CompanyCode ?? "");
                    cmd.Parameters.AddWithValue("@ClientCode", job.ClientCode ?? "");
                    cmd.Parameters.AddWithValue("@Dt", DateTime.Now.ToString("yyyyMMdd"));
                    cmd.Parameters.AddWithValue("@DocumentType", emailRow.Table.Columns.Contains("DocumentType") ? emailRow["DocumentType"] ?? "" : "");
                    cmd.Parameters.AddWithValue("@ComputerName", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@EmailParamCode", emailRow.Table.Columns.Contains("EmailParamCode") ? emailRow["EmailParamCode"] ?? "" : "");
                    cmd.Parameters.AddWithValue("@ToEmailId", job.ToEmail ?? "");
                    cmd.Parameters.AddWithValue("@Subject", emailRow.Table.Columns.Contains("EmailSubject") ? emailRow["EmailSubject"] ?? "" : "");
                    cmd.Parameters.AddWithValue("@BodyText", emailRow.Table.Columns.Contains("EmailBodyText") ? emailRow["EmailBodyText"] ?? "" : "");
                    cmd.Parameters.Add("@Document", SqlDbType.VarBinary, pdfBytes.Length).Value = pdfBytes;
                    cmd.Parameters.AddWithValue("@FileName", emailRow.Table.Columns.Contains("EmailFileName") ? emailRow["EmailFileName"] ?? "" : "");
                    cmd.Parameters.AddWithValue("@CreatedBy", userId);

                    await cmd.ExecuteNonQueryAsync();
                }

                // Update another table
                using (var updateCmd = conn.CreateCommand())
                {
                    updateCmd.Transaction = transaction;
                    updateCmd.CommandText = @"
                            UPDATE tbl_ProcessClientFundLedgerMaster
                            SET IsProcess = 'P' , PDFGenerated = 'Y'
                            WHERE ClientCode = @ClientCode  And IsProcess = 'N'";

                    updateCmd.Parameters.AddWithValue("@ClientCode", job.ClientCode);

                    await updateCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
            }
        }


    }


}
