using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TradeWeb.API.QuestPdfTemplates;
using TradeWeb.API.Repository;
namespace TradeWeb.API.QuestPdfServicesClass
{

    public static class ImageCache
    {
        private static readonly ConcurrentDictionary<string, byte[]> _cache
            = new ConcurrentDictionary<string, byte[]>();

        public static byte[] Get(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return _cache.GetOrAdd(path, p =>
            {
                if (!File.Exists(p))
                    return null;

                return File.ReadAllBytes(p);
            });
        }
    }
    public static class LayoutCache
    {
        private static readonly ConcurrentDictionary<int, LayoutDefinition>
            Cache = new ConcurrentDictionary<int, LayoutDefinition>();

        public static LayoutDefinition Get(string json)
        {
            int hash = json.GetHashCode();

            return Cache.GetOrAdd(hash, _ =>
            {
                return LayoutDefinition.Parse(json);
            });
        }
    }
    public class LayoutDefinition
    {
        public JObject Root { get; set; }

        public JObject Document { get; set; }

        public List<ReportItem> Headers { get; set; }

        public List<ReportItem> Footers { get; set; }

        public Dictionary<string, TableDefinition> Tables { get; set; }

        public List<TableSection> TableSections { get; set; }

        public static LayoutDefinition Parse(string json)
        {
            JObject root = JObject.Parse(json);

            return new LayoutDefinition
            {
                Root = root,

                Document = (JObject)root["document"],

                Headers =
                    root["companyheader"]
                    .ToObject<List<ReportItem>>(),

                Footers =
                    root["globalfooter"]
                    .ToObject<List<ReportItem>>(),

                Tables =
                    root["tablecolumns"]
                    .ToObject<Dictionary<string, TableDefinition>>(),

                TableSections =
                    root["tablesection"]
                    .ToObject<List<TableSection>>()
            };
        }
    }

    public static class ColumnIndexCache
    {
        private static readonly ConcurrentDictionary<DataTable,
            Dictionary<string, int>> Cache = new ConcurrentDictionary<DataTable, Dictionary<string, int>>();

        public static Dictionary<string, int> Get(DataTable table)
        {
            return Cache.GetOrAdd(table, t =>
            {
                var dict =
                    new Dictionary<string, int>(
                        StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < t.Columns.Count; i++)
                    dict[t.Columns[i].ColumnName] = i;

                return dict;
            });
        }
    }

    public class QuestPdfCommonService
    {

        private readonly IConfiguration _config;
        private readonly UtilityCommon _objUtility;

        public DataSet TransactionData { get; set; }
        public QuestPdfCommonService(IConfiguration config, UtilityCommon objUtility)
        {
            _config = config;
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.EnableDebugging = true;
            this._objUtility = objUtility;
        }


        /// <summary>
        /// Processes a pre-filled DataTable instead of querying SQL.
        /// </summary>

        public async Task<DataSet> QuestPDFProcessAsync(DataTable dtCompEmail, string strJson, string userId, string companyLogoPath, string headerImagePath)
        {
            var sw = Stopwatch.StartNew();
            var counters = new CommonCounters();
            try
            {
                // Parse only once (or get from cache)
                LayoutDefinition layout = LayoutCache.Get(strJson);

                // Strongly typed objects
                JObject document = layout.Document;

                List<ReportItem> headerItems = layout.Headers;

                List<ReportItem> footerItems = layout.Footers;

                List<TableSection> tableSections = layout.TableSections;

                TableDefinition table1 = layout.Tables["table1"];

                List<TableColumnConfig> columns = table1.Columns;

                bool showCompanyLogo =
    bool.TryParse(GetValueIgnoreCase(layout.Document, "isshowcompanylogo"), out var logo)
        && logo;

                bool showHeaderImage =
                    bool.TryParse(GetValueIgnoreCase(layout.Document, "isShowheaderimage"), out var header)
                        && header;

                byte[] companyLogo = null;
                byte[] headerImage = null;

                if (showCompanyLogo && !string.IsNullOrWhiteSpace(companyLogoPath))
                {
                    companyLogo = ImageCache.Get(companyLogoPath);
                }

                if (showHeaderImage && !string.IsNullOrWhiteSpace(headerImagePath))
                {
                    headerImage = ImageCache.Get(headerImagePath);
                }


                var rowWiseWidths = layout.Tables["table1"]
                                            .Columns
                                            .GroupBy(c => c.rowno)
                                            .Select(g => new
                                            {
                                                RowNo = g.Key,
                                                TotalWidth = g.Sum(x => x.width)
                                            })
                                            .ToList();

                int maxWidth = rowWiseWidths.Max(x => x.TotalWidth);


                DataRow row1 = dtCompEmail.Rows[0];

                foreach (var item in layout.Headers)
                {
                    if (dtCompEmail.Columns.Contains(item.datafield))
                    {
                        item.datafield = Convert.ToString(row1[item.datafield]);
                    }
                }
                foreach (var item in layout.Footers)
                {
                    if (dtCompEmail.Columns.Contains(item.datafield))
                    {
                        item.datafield = Convert.ToString(row1[item.datafield]);
                    }
                }

                var emailInfo = new EmailInfo
                {
                    Subject = row1["EmailSubject"].ToString(),
                    Body = row1["BodyText"].ToString(),
                    FileName = row1["EmailFileName"].ToString(),
                    EmailParamCode = row1["EmailParamCode"].ToString(),
                    DocumentType = row1["DocumentType"].ToString(),
                    CompanyCode = row1["CompanyCode"].ToString()
                };
                //// For Master table and Transaction SP 
                string masterTableName = "", updateQuery = "";

                var transactionTables = new List<TableSection>();
                var tableSectionsVal = layout.TableSections;
                foreach (var table in tableSections)
                {
                    if (table.TableName.Equals("mastertable", StringComparison.OrdinalIgnoreCase))
                    {
                        masterTableName = table.DataSource.ToString();
                        updateQuery = table.UpdateQuery.ToString();
                    }
                    else if (table.TableName.Equals("transactiontable", StringComparison.OrdinalIgnoreCase))
                    {
                        transactionTables.Add(table);
                    }
                }

                await using var sqlConn = new SqlConnection(BuildConnectionString());
                await sqlConn.OpenAsync();
                DataTable masterTable = await GetMasterDataAsync(sqlConn, masterTableName);
                if (masterTable == null || masterTable.Rows.Count == 0)
                {
                    return CreateResultTable("E", "Master Record Not Fount!");
                }

                bool hasReportDateColumn = masterTable.Columns.Contains("ReportDate");
                bool hasFilePwdColumn = masterTable.Columns.Contains("FilePassWord");
                bool hasClientCodeColumn = masterTable.Columns.Contains("ClientCode");

                //int batchSize = GetIntEnv("PdfBatchSize", 500);
                var numWorkers = GetIntEnv("NUM_WORKERS", Math.Min(Environment.ProcessorCount * 2, 16) > 0 ? Math.Min(Environment.ProcessorCount * 2, 16) : 8);

                var channel = Channel.CreateBounded<PdfJob>(new BoundedChannelOptions(numWorkers * 10)
                {
                    FullMode = BoundedChannelFullMode.Wait
                });

                var queueCount = 0;
                await using var gConn =
                           new SqlConnection(BuildConnectionString());
                await gConn.OpenAsync();

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
                                       job.MasterRow, transactionTables);

                                var pdfBytes = PdfWriterQuest.GeneratePdfBytes(
                                   job.MasterRow,
                                    transactionDS,
                                    layout, headerImage, companyLogo, maxWidth);

                                string reportDate = "";

                                /*if (hasReportDateColumn &&
                                    job.MasterRow["ReportDate"] != DBNull.Value)
                                {
                                    reportDate = job.MasterRow["ReportDate"].ToString() ?? "";
                                }*/

                                if (hasReportDateColumn)
                                {
                                    reportDate = GetString(job.MasterRow, hasReportDateColumn, "ReportDate");
                                }

                                /*string filePassword = "";

                                if (hasFilePwdColumn && job.MasterRow["FilePassWord"] != DBNull.Value && !string.IsNullOrWhiteSpace(job.MasterRow["FilePassWord"].ToString()))
                                {
                                    filePassword = job.MasterRow["FilePassWord"].ToString();
                                }*/

                                string filePassword = GetString(job.MasterRow, hasFilePwdColumn, "FilePassWord");

                                /*string fileName = emailInfo.FileName ?? string.Empty;
                                const string tag = "{ClientCode}";
                                if (fileName.Contains(tag, StringComparison.OrdinalIgnoreCase))
                                {
                                    string clientCode = GetString(job.MasterRow, hasClientCodeColumn, "ClientCode");
                                    if (clientCode.Length > 0)
                                        fileName = fileName.Replace(tag, SanitizeFileName(clientCode),
                                                                    StringComparison.OrdinalIgnoreCase);
                                }*/

                                string fileName = ResolveFileName(emailInfo.FileName, job.MasterRow, out var unresolved);

                                /*if (PlaceholderRegex.IsMatch(fileName))
                                {
                                    await EmailDetailsWriter.InsertPdfProcessLog(workerConn, job.ClientCode, "PDFGeneration", emailInfo.DocumentType, "", "", "FileName tag could not be resolved. FileName : " + fileName, userId);
                                    fileName = SanitizeFileName(emailInfo.DocumentType);
                                }*/

                                if (string.IsNullOrWhiteSpace(fileName) || unresolved.Count > 0)
                                {
                                    // fall back to the default document name
                                    fileName = SanitizeFileName(emailInfo.DocumentType);
                                    await EmailDetailsWriter.InsertPdfProcessLog(workerConn, job.ClientCode, "PDFGeneration", emailInfo.DocumentType, "", "",
                                        "FileName tag not resolved, default name used: " + string.Join(", ", unresolved), userId);
                                }

                                fileName = LimitFileName(fileName);

                                /*await EmailDetailsWriter.InsertAsync(
                                    workerConn,
                                    job,
                                    emailInfo,
                                    pdfBytes,
                                    userId, emailInfo.DocumentType, updateQuery, fileName, reportDate, filePassword);

                                Interlocked.Increment(ref counters.TotalProcessed);*/

                                bool ok = await EmailDetailsWriter.InsertAsync(
                                    workerConn, job, emailInfo, pdfBytes, userId, emailInfo.DocumentType, updateQuery, fileName, reportDate, filePassword);

                                if (ok) Interlocked.Increment(ref counters.TotalProcessed);
                                else Interlocked.Increment(ref counters.TotalErrors);
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref counters.TotalErrors);
                                await EmailDetailsWriter.InsertPdfProcessLog(workerConn, job.ClientCode, "PDFGeneration", emailInfo.DocumentType, "", "", ex.Message.ToString(), userId);
                            }
                        }
                    }));
                }

                foreach (DataRow masterRow in masterTable.Rows)
                {
                    var clientCode = masterRow["ClientCode"]?.ToString()?.Trim();

                    var job = new PdfJob
                    {
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

                string errorMsg = "";
                if (counters.TotalErrors > 0)
                    errorMsg = " Failed PDF Generation Count: " + counters.TotalErrors + " Please check log for the detail.";
                errorMsg = "Total PDF Generation Count: " + counters.TotalProcessed + errorMsg;

                await EmailDetailsWriter.InsertPdfProcessLog(gConn, "", "PDFGeneration", emailInfo.DocumentType, "Total Count", counters.TotalProcessed.ToString(), counters.TotalErrors.ToString(), userId);

                return CreateResultTable("S", errorMsg);
            }
            catch (Exception ex)
            {
                return CreateResultTable("E", "Error: " + ex.Message.ToString());
            }
            // return CreateResultTable("S", "Pdf Process Completed!");
        }

        public async Task<DataSet> QuestPDFForClientAsync(
    DataTable dtHeader, string strJson, string requestClientCode,
    string companyLogoPath, string headerImagePath)
        {
            try
            {
                if (!Regex.IsMatch(requestClientCode ?? "", @"^[A-Za-z0-9]{1,20}$"))
                    return CreateResultTable("E", "Invalid client code.");

                LayoutDefinition layout = LayoutCache.Get(strJson);

                bool showLogo = bool.TryParse(GetValueIgnoreCase(layout.Document, "isshowcompanylogo"), out var l) && l;
                bool showHdr = bool.TryParse(GetValueIgnoreCase(layout.Document, "isShowheaderimage"), out var h) && h;
                byte[] companyLogo = showLogo && !string.IsNullOrWhiteSpace(companyLogoPath) ? ImageCache.Get(companyLogoPath) : null;
                //byte[] headerImage = showHdr && !string.IsNullOrWhiteSpace(headerImagePath) ? ImageCache.Get(headerImagePath) : null;
                byte[] headerImage = null;

                if (showHdr)
                {
                    string docImgPath = GetValueIgnoreCase(layout.Document, "HeaderImagePath");

                    // 1) document-level override from PdfDocumentDefinition
                    if (!string.IsNullOrWhiteSpace(docImgPath))
                        headerImage = ImageCache.Get(docImgPath);

                    // 2) fall back to the company logo path
                    if (headerImage == null && !string.IsNullOrWhiteSpace(headerImagePath))
                        headerImage = ImageCache.Get(headerImagePath);
                }

                int maxWidth = layout.Tables["table1"].Columns
                    .GroupBy(c => c.rowno).Max(g => g.Sum(x => x.width));

                DataRow row1 = dtHeader.Rows[0];
                foreach (var item in layout.Headers.Concat(layout.Footers))
                    if (dtHeader.Columns.Contains(item.datafield))
                        item.datafield = Convert.ToString(row1[item.datafield]);

                string masterSql = "";
                var transactionTables = new List<TableSection>();
                foreach (var t in layout.TableSections)
                {
                    if (t.TableName.Equals("mastertable", StringComparison.OrdinalIgnoreCase))
                        masterSql = t.DataSource.ToString();
                    else if (t.TableName.Equals("transactiontable", StringComparison.OrdinalIgnoreCase))
                        transactionTables.Add(t);
                }

                if (!masterSql.Contains("##REQCLIENT##"))
                    return CreateResultTable("E", "Document is not configured for single-client generation.");
                masterSql = masterSql.Replace("##REQCLIENT##", requestClientCode);

                await using var conn = new SqlConnection(BuildConnectionString());
                await conn.OpenAsync();

                DataTable master = await GetMasterDataAsync(conn, masterSql);
                if (master == null || master.Rows.Count == 0)
                    return CreateResultTable("E", "No record found for this client.");

                var ds = CreateResultTable("S", "PDF generated.");
                var files = new DataTable("PdfFile");
                files.Columns.Add("FileName", typeof(string));
                files.Columns.Add("Base64PdfBytes", typeof(string));

                foreach (DataRow masterRow in master.Rows)
                {
                    var transactionDS = await GetClientTransactionDataAsync(conn, masterRow, transactionTables);
                    var bytes = PdfWriterQuest.GeneratePdfBytes(
                        masterRow, transactionDS, layout, headerImage, companyLogo, maxWidth);
                    files.Rows.Add($"{layout.Document["documentname"]}_{masterRow["ClientCode"]}.pdf", Convert.ToBase64String(bytes));
                }

                ds.Tables.Add(files);
                return ds;
            }
            catch (Exception ex)
            {
                return CreateResultTable("E", "Error: " + ex.Message);
            }
        }

        private async Task<DataTable> GetMasterDataAsync(SqlConnection conn, string tableName)
        {
            using var cmd = new SqlCommand(tableName, conn);
            cmd.CommandType = CommandType.Text;
            var ds = new DataSet();
            using var da = new SqlDataAdapter(cmd);
            await Task.Run(() => da.Fill(ds));
            return ds.Tables.Count > 0 ? ds.Tables[0] : null;
        }
        private async Task<DataSet> GetClientTransactionDataAsync(SqlConnection conn, DataRow masterRow, List<TableSection> transTables)
        {
            var ds = new DataSet();
            foreach (var table in transTables)
            {
                string query = table.DataSource;
                string mappingId = table.TableMappingId;
                bool showHeader = table.ShowHeader;

                query = EmailDetailsWriter.ReplacePlaceholders(table.DataSource, masterRow);

                using var cmd = new SqlCommand(query, conn);
                cmd.CommandType = CommandType.Text;

                using var reader = await cmd.ExecuteReaderAsync();
                DataTable dt = new DataTable();
                dt.Load(reader);

                dt.TableName = mappingId;
                ds.Tables.Add(dt);
            }
            return ds;
        }


        private static JsonElement J(JsonElement e, string k)
        {
            if (e.ValueKind == JsonValueKind.Undefined || e.ValueKind == JsonValueKind.Null)
            {
                return default;
            }
            return e.TryGetProperty(k, out var v) ? v : default;
        }


        public static JsonElement? GetPropertyIgnoreCase(JsonElement element, string propertyName)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return prop.Value;
            }

            return null;
        }

        private static string GetValueIgnoreCase(JObject obj, string propertyName)
        {
            return obj.Properties()
                      .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                      ?.Value?.ToString() ?? "";
        }

        private static JToken jIgnoreCase(JToken token, string propertyName)
        {
            if (token == null)
                return null;

            var obj = token as JObject;
            if (obj == null)
                return null;

            return obj.Properties()
                      .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                      ?.Value;
        }


        private string BuildConnectionString()
        {
            return _objUtility.GetConnectionStr();
        }

        private static DataSet CreateResultTable(string flag, string message)
        {
            DataSet dsRtn = new DataSet();
            var dt = new DataTable();
            dt.Columns.Add("Flag", typeof(string));
            dt.Columns.Add("Message", typeof(string));
            dt.Rows.Add(flag, message);
            dsRtn.Tables.Add(dt);
            dsRtn.Tables[0].TableName = "rs0";
            return dsRtn;
        }

        private int GetIntEnv(string name, int fallback)
        {
            var value = _config[name];
            return int.TryParse(value, out var parsed) ? parsed : fallback;
        }

        private static string GetString(DataRow row, bool hasColumn, string column)
        {
            if (!hasColumn || row[column] == DBNull.Value) return string.Empty;
            return row[column].ToString()?.Trim() ?? string.Empty;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        // {Name}, {Name:format}, {Name|fallback}, {Name:format|fallback}
        private static readonly Regex PlaceholderRegex =
            new(@"\{(\w+)(?::([^{}|]+))?(?:\|([^{}]*))?\}", RegexOptions.Compiled);

        // Allow-lists. Never add FilePassWord, PAN, mobile or email here.
        private static readonly HashSet<string> TextColumns =
            new(StringComparer.OrdinalIgnoreCase) { "ClientCode", "ClientName" };

        private static readonly HashSet<string> DateColumns =
            new(StringComparer.OrdinalIgnoreCase) { "ReportDate", "FromDate", "ToDate" };

        // Formats the dates may be stored in (yyyyMMdd is the main one)
        private static readonly string[] StoredDateFormats =
            { "yyyyMMdd", "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy" };

        #nullable enable
        private static bool TryGetDate(object? v, out DateTime d)
        {
            if (v is DateTime dt) { d = dt; return true; }
            return DateTime.TryParseExact(v?.ToString()?.Trim(), StoredDateFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
        }

        private static string? ResolveTag(string name, string? fmt, DataRow row)
        {
            if (!row.Table.Columns.Contains(name) || row[name] == DBNull.Value)
                return null;

            if (DateColumns.Contains(name))
                return TryGetDate(row[name], out var d)
                    ? d.ToString(fmt ?? "yyyyMMdd", CultureInfo.InvariantCulture)
                    : null;

            if (TextColumns.Contains(name))
            {
                string text = row[name].ToString()?.Trim() ?? "";
                return Regex.Replace(text, @"\s+", "_");   // collapses multiple spaces too
            }

            return null; // not allow-listed
        }

        private static string ResolveFileName(string? template, DataRow row, out List<string> unresolved)
        {
            var missing = new List<string>();
            unresolved = missing;

            if (string.IsNullOrWhiteSpace(template)) return string.Empty;

            string result = PlaceholderRegex.Replace(template, m =>
            {
                string name = m.Groups[1].Value;
                string? fmt = m.Groups[2].Success ? m.Groups[2].Value : null;
                string? fallback = m.Groups[3].Success ? m.Groups[3].Value : null;

                string? value;
                try { value = ResolveTag(name, fmt, row); }
                catch (FormatException) { value = null; }

                if (string.IsNullOrWhiteSpace(value)) value = fallback;

                if (value is null)
                {
                    missing.Add(m.Value);        // remember the original tag text
                    return m.Value;
                }
                return SanitizeFileName(value);
            });

            return SanitizeFileName(result);
        }

        private static string LimitFileName(string fileName, int maxLength = 100)
        {
            if (fileName.Length <= maxLength) return fileName;

            string ext = Path.GetExtension(fileName);          // ".pdf"
            string baseName = Path.GetFileNameWithoutExtension(fileName);

            int allowed = Math.Max(1, maxLength - ext.Length);
            if (baseName.Length > allowed)
                baseName = baseName[..allowed];

            return baseName.TrimEnd(' ', '.') + ext;
        }
        #nullable restore
    }

    public class EmailInfo
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public string FileName { get; set; }
        public string EmailParamCode { get; set; }
        public string DocumentType { get; set; }
        public string CompanyCode { get; set; }
    }

    public class PdfJob
    {
        public string ClientCode { get; set; }
        public string ToEmail { get; set; }

        public DataRow MasterRow { get; set; }
        public DataTable DetailTable { get; set; }
    }

    public sealed class CommonCounters
    {
        public int TotalProcessed;
        public int TotalErrors;
        public int TotalRowsReceived;
    }
    public static class PdfWriterQuest
    {
        public static byte[] GeneratePdfBytes(DataRow masterRow, DataSet detailDS, LayoutDefinition layout, byte[] headerImage, byte[] compLogo, int maxWidth)
        {
            var document = new QuestCommonTemplate(masterRow, detailDS, layout, headerImage, compLogo, maxWidth);
            using var ms = new MemoryStream();
            var sw = Stopwatch.StartNew();
            document.GeneratePdf(ms);

            sw.Restart();

            byte[] pdfBytes = ms.ToArray();
            return pdfBytes;
        }
    }

    static class EmailDetailsWriter
    {

        private static readonly Regex PlaceholderRegex =
    new Regex(@"\{([A-Za-z0-9_]+)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private const string InsertSql = @"
        INSERT INTO dbo.Digital_Emaildetails
        (
            dd_companycode, dd_clientcd, dd_dt, dd_DocumentType, dd_computername,
            dd_EmailParamCode, dd_toEmailid, dd_subject, dd_bodyText, dd_document, dd_filename, dd_CreatedBy, dd_SendFlag, dd_Encpassword
        )
        VALUES
        (
            @CompanyCode, @ClientCode, @Dt, @DocumentType, @ComputerName,
            @EmailParamCode, @ToEmailId, @Subject, @BodyText, @Document, @FileName, @CreatedBy, @SendFlag, @FilePassword
        );
    ";

        public static async Task<bool> InsertAsync(SqlConnection conn, PdfJob job, EmailInfo emailInfo, byte[] pdfBytes, string userId, string reportName, string updateQuery, string fileName, string reportDate = "", string filePassword = "")
        {
            const int maxAttempts = 4;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();
                try
                {
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = InsertSql;
                        cmd.Parameters.Add("@CompanyCode", SqlDbType.VarChar, 2).Value = emailInfo.CompanyCode ?? "";
                        cmd.Parameters.Add("@ClientCode", SqlDbType.VarChar, 10).Value = job.ClientCode ?? "";
                        cmd.Parameters.Add("@Dt", SqlDbType.VarChar, 10).Value = !string.IsNullOrWhiteSpace(reportDate) ? reportDate : DateTime.Now.ToString("yyyyMMdd"); //SqlDbType.VarChar, 10).Value = DateTime.Now.ToString("yyyyMMdd")
                        cmd.Parameters.Add("@DocumentType", SqlDbType.VarChar, 50).Value = emailInfo.DocumentType;
                        cmd.Parameters.Add("@ComputerName", SqlDbType.VarChar, 50).Value = Environment.MachineName;
                        cmd.Parameters.Add("@EmailParamCode", SqlDbType.VarChar, 50).Value = emailInfo.EmailParamCode;
                        cmd.Parameters.Add("@ToEmailId", SqlDbType.VarChar, 50).Value = job.ToEmail ?? "";
                        cmd.Parameters.Add("@Subject", SqlDbType.VarChar, 200).Value = emailInfo.Subject;
                        cmd.Parameters.Add("@BodyText", SqlDbType.VarChar, 2000).Value = emailInfo.Body;
                        cmd.Parameters.Add("@Document", SqlDbType.VarBinary, -1).Value = pdfBytes;
                        cmd.Parameters.Add("@FileName", SqlDbType.VarChar, 100).Value = fileName;
                        cmd.Parameters.Add("@CreatedBy", SqlDbType.VarChar, 10).Value = userId;
                        cmd.Parameters.Add("@SendFlag", SqlDbType.NVarChar, 2).Value = "X";
                        cmd.Parameters.Add("@FilePassword", SqlDbType.VarChar, 200).Value = filePassword;
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await using (var updateCmd = conn.CreateCommand())
                    {
                        updateCmd.Transaction = transaction;
                        updateCmd.CommandText = ReplacePlaceholders(updateQuery, job.MasterRow);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (SqlException ex) when (ex.Number == 1205 && attempt < maxAttempts)
                {
                    // deadlock victim: transaction is already rolled back, wait briefly and retry
                    try { await transaction.RollbackAsync(); } catch { }
                    await Task.Delay(Random.Shared.Next(50, 200) * attempt);
                }
                catch (Exception ex)
                {
                    try { await transaction.RollbackAsync(); } catch { }
                    await InsertPdfProcessLog(conn, job.ClientCode, "PDFGeneration", reportName, "", "", ex.Message, userId);
                    return false;
                }
            }
            return false;
        }


        public static string ReplacePlaceholders(string query, DataRow row)
        {
            var columns = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (DataColumn col in row.Table.Columns)
                columns[col.ColumnName] = row[col];

            return PlaceholderRegex.Replace(query, match =>
            {
                if (!columns.TryGetValue(match.Groups[1].Value, out var value))
                    return match.Value;

                return value == DBNull.Value || value == null
                    ? "NULL"
                    : $"'{value.ToString().Replace("'", "''")}'";
            });
        }

        public static async Task InsertPdfProcessLog(SqlConnection conn, string clientCode, string reqType,
    string reportName, string requestLog, string responseLog, string errorLog, string userId)
        {
            try
            {
                const string sql = @"INSERT INTO dbo.tbl_PdfProcessLog
            (ClientCode, RequestType, ReportName, RequestLog, ResponseLog, ErrorLog, UserCode)
            VALUES
            (@ClientCode, @RequestType, @ReportName, @RequestLog, @ResponseLog, @ErrorLog, @UserCode)";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;

                cmd.Parameters.Add("@ClientCode", SqlDbType.VarChar, 20).Value = Db(clientCode, 20);
                cmd.Parameters.Add("@RequestType", SqlDbType.VarChar, 100).Value = Db(reqType, 100);
                cmd.Parameters.Add("@ReportName", SqlDbType.VarChar, 100).Value = Db(reportName, 100);
                cmd.Parameters.Add("@RequestLog", SqlDbType.VarChar, -1).Value = Db(requestLog);   // match real column size
                cmd.Parameters.Add("@ResponseLog", SqlDbType.VarChar, -1).Value = Db(responseLog);  // match real column size
                cmd.Parameters.Add("@ErrorLog", SqlDbType.VarChar, -1).Value = Db(errorLog);     // match real column size
                cmd.Parameters.Add("@UserCode", SqlDbType.VarChar, 20).Value = Db(userId, 20);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Last resort: the log itself failed. Don't throw, but don't hide it either.
                Console.Error.WriteLine($"[PdfProcessLog] insert failed for client '{clientCode}': {ex.Message} | original: {errorLog}");
            }
        }

        private static object Db(string value, int maxLength = 0)
        {
            if (value == null) return DBNull.Value;
            if (maxLength > 0 && value.Length > maxLength) return value.Substring(0, maxLength);
            return value;
        }


    }



}

