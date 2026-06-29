using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TradeWeb.API.Repository
{
    public class QuestPdfServiceClass
    {
        private readonly IConfiguration _config;
        private readonly UtilityCommon _objUtility;
        public QuestPdfServiceClass(IConfiguration config, UtilityCommon objUtility)
        {
            _config = config;
            QuestPDF.Settings.License = LicenseType.Community;
            this._objUtility = objUtility;
        }


        /// <summary>
        /// Processes a pre-filled DataTable instead of querying SQL.
        /// </summary>
        public async Task<DataSet> ProcessAsync(DataTable table, string headerImagePath, string logoPath, string userId)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));

            var numWorkers = GetIntEnv("NUM_WORKERS", Environment.ProcessorCount > 0 ? Environment.ProcessorCount : 4);

            var sw = Stopwatch.StartNew();

            var channel = Channel.CreateBounded<LetterData>(new BoundedChannelOptions(numWorkers * 10)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            var counters = new Counters();
            var queueCount = 0;
            var workers = new List<Task>();
            //TemplateData template = null;

            for (int i = 0; i < numWorkers; i++)
            {
                var workerId = i;

                workers.Add(Task.Run(async () =>
                {
                    await using var workerConn = new SqlConnection(BuildConnectionString());
                    await workerConn.OpenAsync();

                    await foreach (var data in channel.Reader.ReadAllAsync())
                    {
                        Interlocked.Decrement(ref queueCount);

                        try
                        {
                            var pdfBytes = PdfWriterQuest.GeneratePdfBytes(data, headerImagePath, logoPath);
                            await EmailDetailsWriter.InsertAsync(workerConn, data, pdfBytes, userId);

                            Interlocked.Increment(ref counters.TotalProcessed);
                        }
                        catch (Exception e)
                        {
                            Interlocked.Increment(ref counters.TotalErrors);
                        }
                    }
                }));
            }
            // Enqueue data from DataTable
            //foreach (DataRow row in table.Rows)
            //{
            //    Interlocked.Increment(ref counters.TotalRowsReceived);
            //    var data = LetterData.FromReader(row);
            //    Interlocked.Increment(ref queueCount);
            //    await channel.Writer.WriteAsync(data);
            //}

            using (DataTableReader reader = table.CreateDataReader())
            {
                while (reader.Read())
                {
                    Interlocked.Increment(ref counters.TotalRowsReceived);

                    var data = LetterData.FromReader(reader);

                    Interlocked.Increment(ref queueCount);
                    await channel.Writer.WriteAsync(data);
                }
            }
            channel.Writer.Complete();
            await Task.WhenAll(workers);

            sw.Stop();

            DataTable dt = new DataTable();
            dt.Columns.Add("MessageType", typeof(string));
            dt.Columns.Add("MessageText", typeof(string));
            dt.Rows.Add("SUCCESS", counters.TotalProcessed);
            dt.Rows.Add("ERROR", counters.TotalErrors);

            return CreateResultTable(counters);

            //return new
            //{
            //counters.TotalProcessed,
            //counters.TotalErrors,
            //counters.TotalRowsReceived,
            //TotalTimeSeconds = sw.Elapsed.TotalSeconds
            //Rate = sw.Elapsed.TotalSeconds > 0
            //    ? counters.TotalProcessed / sw.Elapsed.TotalSeconds
            //    : 0
            // };
        }


        /// <summary>
        /// For Download sample pdf for Income and Dormant Alert.
        /// </summary>
        public async Task<DataSet> DownloadSamplePdfQuestForIncomeDormant(DataTable table, string headerImagePath, string logoPath)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            var counters = new Counters();

            using (DataTableReader reader = table.CreateDataReader())
            {
                while (reader.Read())
                {
                    var data = LetterData.FromReader(reader);
                    var pdfBytes = PdfWriterQuest.GeneratePdfBytes(data, headerImagePath, logoPath);

                    var pdfBase64 = Convert.ToBase64String(pdfBytes);
                    DataSet dsRtn = new DataSet();
                    var dt = new DataTable();
                    dt.Columns.Add("fileContents", typeof(string));
                    dt.Columns.Add("contentType", typeof(string));
                    dt.Columns.Add("fileDownloadName", typeof(string));
                    dt.Columns.Add("lastModified", typeof(string));
                    dt.Columns.Add("entityTag", typeof(string));
                    dt.Columns.Add("enableRangeProcessing", typeof(bool));

                    dt.Rows.Add(pdfBase64, "application/pdf", "samplePdf.pdf", null, null, false);
                    dsRtn.Tables.Add(dt);
                    dsRtn.Tables[0].TableName = "rs0";
                    return dsRtn;
                }
            }
            return CreateResultTable(counters);

        }


        private string BuildConnectionString()
        {
            return _objUtility.GetConnectionStr();
        }

        private static DataSet CreateResultTable(Counters counters)
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

        /// <summary>
        /// Converts a DataRow into a LetterData object.
        /// </summary>
        private LetterData LetterDataFromRow(DataRow row)
        {
            return new LetterData
            {
                ClientCode = row["ClientCode"]?.ToString()?.Trim() ?? "UNKNOWN",
                CompanyCode = row["CompanyCode"]?.ToString(),
                CompanyName = row["CompanyName"]?.ToString(),
                Address1 = row["Address1"]?.ToString(),
                Address2 = row["Address2"]?.ToString(),
                Address3 = row["Address3"]?.ToString(),
                CINNo = row["CINNo"]?.ToString(),
                Subject = row["Subject"]?.ToString(),
                Client = row["ClientName"]?.ToString(),
                ClientAddress1 = row["ClientAddress1"]?.ToString(),
                ClientAddress2 = row["ClientAddress2"]?.ToString(),
                ClientAddress3 = row["ClientAddress3"]?.ToString(),
                Clientpin = row["Clientpin"]?.ToString(),
                Telephone = row["Telephone"]?.ToString(),
                HeaderText = row["HeaderText"]?.ToString(),
                BodyText = row["BodyText"]?.ToString(),
                DocumentType = row["DocumentType"]?.ToString(),
                EmailParamCode = row["EmailParamCode"]?.ToString(),
                EmailSubject = row["Emailsubject"]?.ToString(),
                EmailBodyText = row["EmailBodyText"]?.ToString(),
                ToEmailId = row["ToEmailid"]?.ToString(),
                FileName = row["FileName"]?.ToString()
            };
        }
    }


    public sealed class Counters
    {
        public int TotalProcessed;
        public int TotalErrors;
        public int TotalRowsReceived;
    }
    public static class PdfWriterQuest
    {
        public static byte[] GeneratePdfBytes(LetterData data, string headerPath, string logoPth)
        {
            var doc = new LetterTemplate(data, headerPath, logoPth);

            using (var ms = new MemoryStream())
            {
                doc.GeneratePdf(ms);
                return ms.ToArray();
            }
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

        public static async Task InsertAsync(SqlConnection conn, LetterData data, byte[] pdfBytes, string userId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = InsertSql;
            cmd.Parameters.AddWithValue("@CompanyCode", (object?)data.CompanyCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ClientCode", (object?)data.ClientCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Dt", DateTime.Now.ToString("yyyyMMdd"));
            cmd.Parameters.AddWithValue("@DocumentType", (object?)data.DocumentType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ComputerName", Environment.MachineName);
            cmd.Parameters.AddWithValue("@EmailParamCode", (object?)data.EmailParamCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToEmailId", (object?)data.ToEmailId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Subject", (object?)data.EmailSubject ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BodyText", (object?)data.EmailBodyText ?? DBNull.Value);
            cmd.Parameters.Add("@Document", System.Data.SqlDbType.VarBinary, pdfBytes.Length).Value = pdfBytes;
            cmd.Parameters.AddWithValue("@FileName", (object?)data.FileName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", userId);
            await cmd.ExecuteNonQueryAsync();
        }
    }


    public sealed class LetterData
    {
        public string ClientCode { get; set; } = "UNKNOWN";
        public string CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string CINNo { get; set; }
        public string Subject { get; set; }
        public string Client { get; set; }
        public string ClientAddress1 { get; set; }
        public string ClientAddress2 { get; set; }
        public string ClientAddress3 { get; set; }
        public string Clientpin { get; set; }
        public string Telephone { get; set; }
        public string HeaderText { get; set; }
        public string BodyText { get; set; }
        public string FooterText { get; set; }
        public string DocumentType { get; set; }
        public string EmailParamCode { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBodyText { get; set; }
        public string ToEmailId { get; set; }
        public string FileName { get; set; }
        public string ReportName { get; set; }
        public string FooterModuleName { get; set; }

        public static LetterData FromReader(DataTableReader row)
        {
            return new LetterData
            {
                ClientCode = row["ClientCode"]?.ToString()?.Trim() ?? "UNKNOWN",
                CompanyCode = row["CompanyCode"]?.ToString(),
                CompanyName = row["CompanyName"]?.ToString(),
                Address1 = row["Address1"]?.ToString(),
                Address2 = row["Address2"]?.ToString(),
                Address3 = row["Address3"]?.ToString(),
                CINNo = row["CINNo"]?.ToString(),
                Subject = row["Subject"]?.ToString(),
                Client = row["ClientName"]?.ToString(),
                ClientAddress1 = row["ClientAddress1"]?.ToString(),
                ClientAddress2 = row["ClientAddress2"]?.ToString(),
                ClientAddress3 = row["ClientAddress3"]?.ToString(),
                Clientpin = row["Clientpin"]?.ToString(),
                Telephone = row["Telephone"]?.ToString(),
                HeaderText = row["HeaderText"]?.ToString(),
                BodyText = row["BodyText"]?.ToString(),
                FooterText = row["FooterText"]?.ToString(),
                DocumentType = row["DocumentType"]?.ToString(),
                EmailParamCode = row["EmailParamCode"]?.ToString(),
                EmailSubject = row["Emailsubject"]?.ToString(),
                EmailBodyText = row["EmailBodyText"]?.ToString(),
                ToEmailId = row["ToEmailid"]?.ToString(),
                FileName = row["FileName"]?.ToString(),
                ReportName = row["ReportName"]?.ToString(),
                FooterModuleName = row["FooterModuleName"]?.ToString()
            };
        }
    }

}