using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MimeKit;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TradeWeb.API.Models;
using TradeWeb.API.Repository;

namespace TradeWeb.API.Service
{
#nullable disable
    public record DigitalEmailJob(long JobId, string UserId);
    public record SendRunResult(bool Success, string Message, int Sent, int Failed);
    public class DigitalEmailService
    {
        private readonly UtilityCommon objUtility;

        public DigitalEmailService(UtilityCommon objUtility)
        {
            this.objUtility = objUtility;
        }

        // ---------- tunables ----------
        private const int BatchSize = 100;
        private const int PerMailDelayMs = 500;           // throttle per mail, per worker
        private const int ReconnectEvery = 80;            // reconnect SMTP after N mails, per worker
        private const int ReconnectDelayMs = 2000;
        private const int DbReadGapMs = 3000;             // gap between DB batch reads
        private const int FlushIntervalMs = 5000;         // status flush to DB
        private const int MaxWorkers = 4;                 // cap parallel SMTP connections per account
        private const int MaxConsecutiveInfraFailures = 10;
        private const string ReleaseFlag = "N";           // flag used to put a mail back to pending. VERIFY Stpr_BulkEmailStatusUpdate accepts it

        private static readonly string[] ThrottleKeywords = { "unusual", "rate limit", "too many", "throttl", "suspicious", "sending limit", "sending quota",
            "daily user sending", "try again later", "temporarily blocked" };

        public async Task<SendRunResult> RunSendAsync(long jobId, string userId)
        {
            long logSrNo = 0;
            string logRespMessage = "", logErrorMessage = "";
            var processWatch = Stopwatch.StartNew();
            string connectionString = objUtility.GetConnectionStr();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                logSrNo = objUtility.InsertLog(0, "TradeWeb", "",
                    "DigitalSendEmail Process Started. Start Time : " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                    "", "", "API", conn);
            }

            var counters = new Counters();
            using var hbStop = new CancellationTokenSource();
            Task heartbeat = HeartbeatLoopAsync(jobId, counters, hbStop.Token);   // NEW

            try
            {
                var stopReasons = new List<string>();
                var blockedParamCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int deferred = 0;

                DataTable dtDocType = objUtility.OpenDataTable(@"SELECT dd_DocumentType, COUNT(0) AS TotalCount, MIN(dd_Priority) AS MinPriority
                      FROM Digital_Emaildetails
                      WHERE dd_SendFlag = 'N' GROUP BY dd_DocumentType ORDER BY MinPriority");

                if (dtDocType.Rows.Count < 1)
                {
                    logRespMessage = "No records found";
                    return new SendRunResult(false, "No records found", 0, 0);       // CHANGED
                }

                // NEW: running + total, so the UI can show a progress bar
                int total = dtDocType.AsEnumerable().Sum(r => Convert.ToInt32(r["TotalCount"]));
                await ExecAsync("UPDATE tbl_DigitalEmailJob SET Status = 'R', Total = @t WHERE ExecutionID = @id",
                    new SqlParameter("@t", total), new SqlParameter("@id", jobId));

                bool isParallel = objUtility.GetWebParameter("ParallelEmail").Trim() == "Y";
                int workerCount = isParallel ? Math.Min(MaxWorkers, Math.Max(2, Environment.ProcessorCount)) : 1;

                foreach (DataRow dr in dtDocType.Rows)
                {
                    string docType = dr["dd_DocumentType"].ToString().Trim();

                    SmtpSettings cfg = LoadSmtpSettings(connectionString, docType);
                    if (cfg == null)
                    {
                        int failedCount = await MarkFullGroupFailedNew(connectionString, docType, "SMTP details not found");
                        Interlocked.Add(ref counters.TotalErrors, failedCount);
                        continue;
                    }

                    // Same SMTP account already stopped by the provider in this run: leave these mails pending.
                    if (blockedParamCodes.Contains(cfg.ParamCode))
                    {
                        stopReasons.Add(docType + ": skipped, SMTP account '" + cfg.ParamCode + "' was stopped earlier in this run");
                        continue;
                    }

                    ReportingTemplate rt = LoadReportingTemplate(connectionString, docType);

                    // Connectivity + credentials check, and the [Started] report mail.
                    try
                    {
                        await PreflightAsync(cfg, rt);
                    }
                    catch (MailKit.Security.AuthenticationException)
                    {
                        int failedCount = await MarkFullGroupFailedNew(connectionString, docType, "Invalid SMTP username or password");
                        Interlocked.Add(ref counters.TotalErrors, failedCount);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        int failedCount = await MarkFullGroupFailedNew(connectionString, docType, "SMTP connection failed : " + ex.Message);
                        Interlocked.Add(ref counters.TotalErrors, failedCount);
                        continue;
                    }

                    int sent0 = Volatile.Read(ref counters.TotalProcessed);
                    int failed0 = Volatile.Read(ref counters.TotalErrors);

                    var abort = new AbortSignal(MaxConsecutiveInfraFailures);
                    try
                    {
                        await RunPipelineAsync(connectionString, docType, cfg, userId, workerCount, counters, abort);
                    }
                    finally
                    {
                        int sent = Volatile.Read(ref counters.TotalProcessed) - sent0;
                        int failed = Volatile.Read(ref counters.TotalErrors) - failed0;

                        string suffix = abort.IsSet ? " [Ended - stopped early: " + Short(abort.Reason) + "]" : " [Ended]";
                        await SendEndReportSafeAsync(cfg, rt, suffix, sent, failed,
                                 abort.IsSet ? abort.Released : 0,
                                 abort.IsSet ? abort.Reason : "");
                    }

                    if (abort.IsSet)
                    {
                        blockedParamCodes.Add(cfg.ParamCode);
                        stopReasons.Add(docType + ": " + abort.Reason);
                        deferred += abort.Released;
                    }
                }

                processWatch.Stop();
                bool stopped = stopReasons.Count > 0;
                string message = stopped
                    ? "Email sending stopped early. " + string.Join(" | ", stopReasons) + ". " + deferred + " email(s) left pending for retry."
                    : "Email has been sent successfully";

                logRespMessage = "DigitalSendEmail Process completed. End Time : " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
                                 + " Time taken : " + processWatch.Elapsed + (stopped ? " | " + message : "");

                return new SendRunResult(!stopped, message, counters.TotalProcessed, counters.TotalErrors);   // CHANGED
            }
            catch (Exception ex)
            {
                processWatch.Stop();
                logErrorMessage = "Error in DigitalSendEmail process. Error : " + ex.Message + ". End Time : "
                                  + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " Time taken " + processWatch.Elapsed;
                return new SendRunResult(false, "Error: " + ex.Message, counters.TotalProcessed, counters.TotalErrors);   // CHANGED
            }
            finally
            {
                hbStop.Cancel();                    // NEW
                await heartbeat;                    // NEW
                using var conn = new SqlConnection(connectionString);
                conn.Open();
                objUtility.UpdateLog(logSrNo, "", logRespMessage, logErrorMessage, conn);
            }
        }

        // =====================================================================================
        // Pipeline: DB producer -> bounded channel -> N SMTP workers -> periodic status flush.
        // Sequential mode is simply workerCount = 1.
        // =====================================================================================
        private async Task RunPipelineAsync(string connectionString, string docType, SmtpSettings cfg, string userId,
                                    int workerCount, Counters counters, AbortSignal abort)
        {
            var results = new StatusFlusher(async batch =>
            {
                // S / F go through the existing stored procedure (unchanged).
                // Released mails (back to pending) use a direct update so they don't depend on what the proc accepts.
                var release = batch.Where(x => x.Flag == ReleaseFlag).ToList();
                var normal = batch.Where(x => x.Flag != ReleaseFlag).ToList();

                if (normal.Count > 0) await BulkEmailStatusUpdateAsync(connectionString, userId, normal);
                if (release.Count > 0) await ReleaseEmailsAsync(connectionString, release);
            });

            // Bounded: producer cannot lock more than ~BatchSize rows ahead of the workers.
            var channel = Channel.CreateBounded<EmailModel>(new BoundedChannelOptions(BatchSize)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });

            using (var flushStop = new CancellationTokenSource())
            {
                Task flusher = FlushLoopAsync(results, flushStop.Token);

                Task producer = Task.Run(async () =>
                {
                    try
                    {
                        while (!abort.IsSet)
                        {
                            var batch = await GetEmailBatchAsync(connectionString, docType, BatchSize);
                            if (batch.Count == 0) break;

                            // Always write the whole fetched batch: those rows are already locked in the DB.
                            // If the signal trips meanwhile, workers hand them back as pending.
                            foreach (var mail in batch)
                                await channel.Writer.WriteAsync(mail);

                            await Task.Delay(DbReadGapMs);
                        }
                    }
                    finally
                    {
                        channel.Writer.Complete();
                    }
                });

                var tasks = new List<Task> { producer };
                for (int i = 0; i < workerCount; i++)
                    tasks.Add(Task.Run(() => WorkerAsync(channel.Reader, connectionString, cfg, results, counters, abort)));

                Exception pipelineError = null;
                try { await Task.WhenAll(tasks); }
                catch (Exception ex) { pipelineError = ex; }

                flushStop.Cancel();
                await flusher;
                await results.FlushAsync();   // final flush

                if (pipelineError != null)
                    ExceptionDispatchInfo.Capture(pipelineError).Throw();
            }
        }

        private async Task WorkerAsync(ChannelReader<EmailModel> reader, string connectionString, SmtpSettings cfg,
                                       StatusFlusher results, Counters counters, AbortSignal abort)
        {
            using (var smtp = NewClient(cfg))
            {
                int sentOnConnection = 0;
                bool everConnected = false;

                try
                {
                    await foreach (var email in reader.ReadAllAsync())
                    {
                        // Provider said stop: give every remaining mail back as pending, send nothing.
                        if (abort.IsSet)
                        {
                            results.Add(email.Id, ReleaseFlag, Short(abort.Reason));
                            abort.NoteReleased();
                            continue;
                        }

                        // Build message (address parsing + PDF protection). Failures here are per-mail.
                        MimeMessage message;
                        try
                        {
                            message = await BuildMessageAsync(connectionString, cfg, email);
                        }
                        catch (Exception ex)
                        {
                            FailMail(results, counters, email, ex);
                            if (Classify(ex) != SendErrorKind.BadRecipient)
                                abort.NoteInfraFailure(ex.Message);
                            continue;
                        }

                        bool done = false;
                        for (int attempt = 0; attempt < 2 && !done; attempt++)
                        {
                            try
                            {
                                if (!smtp.IsConnected || !smtp.IsAuthenticated || sentOnConnection >= ReconnectEvery)
                                {
                                    if (smtp.IsConnected)
                                        await smtp.DisconnectAsync(true);
                                    if (everConnected)
                                        await Task.Delay(ReconnectDelayMs);

                                    await ConnectAsync(smtp, cfg);
                                    everConnected = true;
                                    sentOnConnection = 0;
                                }

                                await smtp.SendAsync(message);

                                sentOnConnection++;
                                results.Add(email.Id, "S", "");
                                Interlocked.Increment(ref counters.TotalProcessed);
                                abort.NoteSuccess();
                                done = true;
                            }
                            catch (Exception ex)
                            {
                                switch (Classify(ex))
                                {
                                    case SendErrorKind.Stop:
                                        // Throttling / auth problem: stop everything, keep this mail pending.
                                        abort.Trip("SMTP provider stopped sending: " + ex.Message);
                                        results.Add(email.Id, ReleaseFlag, Short(abort.Reason));
                                        abort.NoteReleased();
                                        done = true;
                                        break;

                                    case SendErrorKind.Connection when attempt == 0:
                                        // Drop the connection; the next attempt reconnects and retries once.
                                        await SafeDisconnectAsync(smtp);
                                        break;

                                    case SendErrorKind.BadRecipient:
                                        FailMail(results, counters, email, ex);
                                        done = true;
                                        break;

                                    default:
                                        FailMail(results, counters, email, ex);
                                        abort.NoteInfraFailure(ex.Message);
                                        done = true;
                                        break;
                                }
                            }
                        }

                        if (!abort.IsSet)
                            await Task.Delay(PerMailDelayMs);
                    }
                }
                finally
                {
                    await SafeDisconnectAsync(smtp);
                }
            }
        }

        private static void FailMail(StatusFlusher results, Counters counters, EmailModel email, Exception ex)
        {
            results.Add(email.Id, "F", Short(ex.Message));
            Interlocked.Increment(ref counters.TotalErrors);
        }

        // ---------- status flushing ----------
        private static async Task FlushLoopAsync(StatusFlusher results, CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(FlushIntervalMs));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try { await results.FlushAsync(); }
                    catch { /* rows were re-queued; next tick (or the final flush) retries */ }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task<List<EmailModel>> GetEmailBatchAsync(string conStr, string docType, int batchSize)
        {
            var list = new List<EmailModel>();

            using var con = new SqlConnection(conStr);
            await con.OpenAsync();

            string sql = @"
DECLARE @SQL NVARCHAR(MAX) = N'
;WITH cte AS
(
    SELECT TOP (@BatchSize) *
    FROM Digital_Emaildetails WITH (ROWLOCK, READPAST)
    WHERE dd_SendFlag = ''N''
      AND dd_DocumentType = @ParamCode
    ORDER BY dd_Priority ASC, dd_srno ASC
)
UPDATE cte
SET dd_SendFlag = ''L'',
    dd_RetryCount = ISNULL(dd_RetryCount, 0) + 1,
    dd_LastRetryDate = GETDATE()
OUTPUT inserted.dd_srno,
       inserted.dd_toEmailid,
       inserted.dd_Subject,
       inserted.dd_bodyText,
       inserted.dd_Document,
       inserted.dd_FileName,
       inserted.dd_Encpassword,
       inserted.dd_FinalDocument,
       inserted.dd_ccEmailid';

-- BCC column is optional: use it if this database has it, otherwise return NULL
IF COL_LENGTH('Digital_Emaildetails', 'dd_BCCEmailID') IS NOT NULL
    SET @SQL += N', inserted.dd_BCCEmailID';
ELSE
    SET @SQL += N', CAST(NULL AS VARCHAR(MAX)) AS dd_BCCEmailID';

EXEC sp_executesql
    @SQL,
    N'@BatchSize INT, @ParamCode VARCHAR(50)',
    @BatchSize = @BatchSize,
    @ParamCode = @ParamCode;
";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@BatchSize", batchSize);
            cmd.Parameters.AddWithValue("@ParamCode", docType);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new EmailModel
                {
                    Id = reader["dd_srno"].ToString(),
                    ToEmail = reader["dd_toEmailid"].ToString(),
                    Subject = reader["dd_Subject"].ToString(),
                    Body = reader["dd_bodyText"].ToString(),
                    Document = reader["dd_Document"] as byte[],
                    FileName = reader["dd_FileName"].ToString(),
                    FilePassword = reader["dd_Encpassword"].ToString(),
                    FinalDocument = reader["dd_FinalDocument"] as byte[],
                    CCEmail = reader["dd_ccEmailid"].ToString(),
                    BCCEmail = reader["dd_BCCEmailID"].ToString()   // DBNull.ToString() is ""
                });
            }

            return list;
        }

        public async Task<int> MarkFullGroupFailedNew(string connectionString, string documentType, string reason)
        {
            using var con = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(@"
        UPDATE Digital_Emaildetails
        SET dd_SendFlag = 'F',
            dd_RetryCount = ISNULL(dd_RetryCount,0) + 1,
            dd_LastRetryDate = GETDATE(),
            dd_BounceReason = @reason
        WHERE dd_DocumentType = @documentType
        AND dd_SendFlag = 'N'", con); //dd_SendFlag IN ('P','F')

            cmd.Parameters.AddWithValue("@reason", reason);
            cmd.Parameters.AddWithValue("@documentType", documentType);

            await con.OpenAsync();
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows;
        }

        public async Task BulkEmailStatusUpdateAsync(string connectionString, string userId, IEnumerable<(string Id, string Flag, string Reason)> results)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("SendFlag", typeof(string));
            table.Columns.Add("BounceReason", typeof(string));

            foreach (var r in results)
                table.Rows.Add(r.Id, r.Flag, r.Reason);

            using var con = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.Stpr_BulkEmailStatusUpdate", con);
            cmd.CommandType = CommandType.StoredProcedure;

            var tvp = cmd.Parameters.AddWithValue("@EmailStatus", table);
            tvp.SqlDbType = SqlDbType.Structured;
            tvp.TypeName = "dbo.EmailStatusType";
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task SaveFinalDocumentAsync(string conStr, string id, byte[] data)
        {
            using var con = new SqlConnection(conStr);
            await con.OpenAsync();

            using var cmd = new SqlCommand(
                "UPDATE Digital_Emaildetails SET dd_FinalDocument = @Doc WHERE dd_srno = @Id", con);
            cmd.Parameters.Add("@Doc", SqlDbType.VarBinary, -1).Value = data;
            cmd.Parameters.AddWithValue("@Id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        // Puts locked rows (L) back to pending (N) and undoes the retry increment done by GetEmailBatchAsync,
        // so a provider-throttle stop does not eat into dd_RetryCount.
        private async Task ReleaseEmailsAsync(string connectionString, List<(string Id, string Flag, string Reason)> items)
        {
            using var con = new SqlConnection(connectionString);
            await con.OpenAsync();

            foreach (var group in items.GroupBy(i => i.Reason ?? ""))
            {
                var ids = group.Select(i => i.Id).ToList();

                for (int offset = 0; offset < ids.Count; offset += 500)
                {
                    var chunk = ids.Skip(offset).Take(500).ToList();

                    using var cmd = new SqlCommand();
                    cmd.Connection = con;

                    var names = new List<string>();
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        names.Add("@id" + i);
                        cmd.Parameters.AddWithValue("@id" + i, int.Parse(chunk[i]));   // dd_srno is int
                    }
                    cmd.Parameters.AddWithValue("@reason", group.Key);

                    cmd.CommandText =
                        @"UPDATE Digital_Emaildetails
                     SET dd_SendFlag = 'N',
                         dd_RetryCount = CASE WHEN ISNULL(dd_RetryCount, 0) > 0 THEN dd_RetryCount - 1 ELSE 0 END,
                         dd_BounceReason = @reason
                   WHERE dd_SendFlag = 'L'
                     AND dd_srno IN (" + string.Join(",", names) + ")";

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // ---------- DB helpers (parameterized) ----------
        private static DataTable Query(string connectionString, string sql, params SqlParameter[] ps)
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            using (var da = new SqlDataAdapter(cmd))
            {
                if (ps != null && ps.Length > 0) cmd.Parameters.AddRange(ps);
                da.Fill(dt);
            }
            return dt;
        }

        private static SmtpSettings LoadSmtpSettings(string connectionString, string docType)
        {
            DataTable dt = Query(connectionString,
                @"SELECT SMTPServer AS Host, SMTPPort AS Port, SMTPMailId AS Username, SMTPPassword AS [Password],
                 SMTPSSLFlag AS Tls, SMTPAccountName AS SenderName, ParamCode
          FROM tbl_EMailParameters
          WHERE LTRIM(RTRIM(ParamCode)) = (SELECT TOP 1 LTRIM(RTRIM(dd_EmailParamCode))
                                           FROM Digital_Emaildetails WHERE dd_DocumentType = @dt)",
                new SqlParameter("@dt", docType));

            if (dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            return new SmtpSettings
            {
                Host = r["Host"].ToString().Trim(),
                Port = Convert.ToInt32(r["Port"]),
                Username = r["Username"].ToString().Trim(),
                Password = r["Password"].ToString(),
                SenderName = r["SenderName"].ToString().Trim(),
                ParamCode = r["ParamCode"].ToString().Trim(),
                BypassCertValidation = r["Tls"].ToString().Trim() == "N"
            };
        }

        // Same PDF password logic as the old sequential branch, now used by every worker.
        private async Task<MimeMessage> BuildMessageAsync(string connectionString, SmtpSettings cfg, EmailModel email)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(cfg.SenderName, cfg.Username));
            message.To.Add(MailboxAddress.Parse(email.ToEmail));
            AddAddresses(message.Cc, email.CCEmail);
            AddAddresses(message.Bcc, email.BCCEmail);
            message.Subject = email.Subject;

            var builder = new BodyBuilder { HtmlBody = email.Body };
            if (email.Document != null)
            {
                byte[] attachment = await GetAttachmentAsync(connectionString, email);
                builder.Attachments.Add(email.FileName, attachment);
            }

            message.Body = builder.ToMessageBody();
            return message;
        }

        private async Task<byte[]> GetAttachmentAsync(string connectionString, EmailModel email)
        {
            bool needsPassword = email.Document != null
                && !string.IsNullOrWhiteSpace(email.FilePassword)
                && email.FileName != null
                && email.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            if (!needsPassword)
                return email.Document;

            if (email.FinalDocument != null && email.FinalDocument.Length > 0)
                return email.FinalDocument;                       // already protected (retry), nothing new to save

            string pwd = email.FilePassword.Trim();               // decrypt here if stored encrypted
            byte[] protectedPdf = objUtility.Protect(email.Document, pwd, pwd);
            await SaveFinalDocumentAsync(connectionString, email.Id, protectedPdf);
            email.FinalDocument = protectedPdf;
            return protectedPdf;
        }

        private static MimeMessage BuildReportMessage(SmtpSettings cfg, ReportingTemplate rt, string suffix, int? sent = null, int failed = 0, int pending = 0, string stopReason = "")
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(cfg.SenderName, cfg.Username));
            message.To.Add(MailboxAddress.Parse(rt.Email));
            message.Subject = rt.Subject + suffix;

            string html = rt.Body + suffix;
            if (sent.HasValue)
            {
                html += "<br/><br/><table border='1' cellpadding='4' style='border-collapse:collapse'>"
                      + "<tr><td>Sent successfully</td><td align='right'>" + sent.Value + "</td></tr>"
                      + "<tr><td>Failed</td><td align='right'>" + failed + "</td></tr>"
                      + (pending > 0 ? "<tr><td>Left pending for retry</td><td align='right'>" + pending + "</td></tr>" : "")
                      + "<tr><td>Total processed</td><td align='right'>" + (sent.Value + failed) + "</td></tr>"
                      + "</table>";
                if (!string.IsNullOrEmpty(stopReason))
                    html += "<br/>Stopped early: " + System.Net.WebUtility.HtmlEncode(stopReason);
            }
            message.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();
            return message;
        }

        // ---------- SMTP helpers ----------
        private static SmtpClient NewClient(SmtpSettings cfg)
        {
            var smtp = new SmtpClient();
            if (cfg.BypassCertValidation)
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;   // only when SMTPSSLFlag = 'N'
            return smtp;
        }

        private static async Task ConnectAsync(SmtpClient smtp, SmtpSettings cfg)
        {
            var options = cfg.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await smtp.ConnectAsync(cfg.Host, cfg.Port, options);
            await smtp.AuthenticateAsync(cfg.Username, cfg.Password);
        }

        private static async Task SafeDisconnectAsync(SmtpClient smtp)
        {
            try { if (smtp.IsConnected) await smtp.DisconnectAsync(true); } catch { }
        }

        private static async Task PreflightAsync(SmtpSettings cfg, ReportingTemplate rt)
        {
            using (var smtp = NewClient(cfg))
            {
                await ConnectAsync(smtp, cfg);
                if (rt.HasRecipient)
                    await smtp.SendAsync(BuildReportMessage(cfg, rt, " [Started]"));
                await smtp.DisconnectAsync(true);
            }
        }

        private static async Task SendEndReportSafeAsync(SmtpSettings cfg, ReportingTemplate rt, string suffix, int sent, int failed, int pending, string stopReason)
        {
            if (!rt.HasRecipient) return;
            try
            {
                using (var smtp = NewClient(cfg))
                {
                    await ConnectAsync(smtp, cfg);
                    await smtp.SendAsync(BuildReportMessage(cfg, rt, suffix, sent, failed, pending, stopReason));
                    await smtp.DisconnectAsync(true);
                }
            }
            catch
            {
                // Never crash the main flow because the END mail failed.
            }
        }

        // ---------- error classification ----------
        private enum SendErrorKind { BadRecipient, Stop, Connection, Other }

        [ApiExplorerSettings(IgnoreApi = true)]
        private static SendErrorKind Classify(Exception ex)
        {
            if (ex is MailKit.Security.AuthenticationException)
                return SendErrorKind.Stop;

            string msg = (ex.Message ?? "").ToLowerInvariant();

            var sce = ex as SmtpCommandException;
            if (sce != null)
            {
                if (ThrottleKeywords.Any(k => msg.Contains(k)))
                    return SendErrorKind.Stop;

                if (sce.ErrorCode == SmtpErrorCode.RecipientNotAccepted)
                    return SendErrorKind.BadRecipient;

                switch (sce.StatusCode)
                {
                    case SmtpStatusCode.ServiceNotAvailable:                // 421
                    case SmtpStatusCode.MailboxBusy:                        // 450
                    case SmtpStatusCode.ErrorInProcessing:                  // 451 (was LocalErrorInProcessing, which doesn't exist)
                    case SmtpStatusCode.InsufficientStorage:                // 452
                    case SmtpStatusCode.TemporaryAuthenticationFailure:     // 454
                        return SendErrorKind.Stop;
                }
                return SendErrorKind.Other;
            }

            if (ex is SmtpProtocolException && ThrottleKeywords.Any(k => msg.Contains(k)))
                return SendErrorKind.Stop;

            if (ex is FormatException)                            // MimeKit ParseException derives from this
                return SendErrorKind.BadRecipient;

            if (ex is SmtpProtocolException || ex is IOException || ex is SocketException
                || ex is ServiceNotConnectedException || ex is ServiceNotAuthenticatedException)
                return SendErrorKind.Connection;

            return SendErrorKind.Other;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private static string Short(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= 400 ? s : s.Substring(0, 400);   // adjust to your reason column length
        }

        // ---------- small nested types ----------
        [ApiExplorerSettings(IgnoreApi = true)]
        private sealed class SmtpSettings
        {
            public string Host;
            public int Port;
            public string Username;
            public string Password;
            public string SenderName;
            public string ParamCode;
            public bool BypassCertValidation;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private sealed class ReportingTemplate
        {
            public string Email;
            public string Subject;
            public string Body;
            public bool HasRecipient { get { return !string.IsNullOrEmpty(Email); } }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private sealed class StatusFlusher
        {
            private readonly ConcurrentQueue<(string Id, string Flag, string Reason)> _queue = new();
            private readonly Func<List<(string Id, string Flag, string Reason)>, Task> _sink;

            public StatusFlusher(Func<List<(string Id, string Flag, string Reason)>, Task> sink) { _sink = sink; }

            public void Add(string id, string flag, string reason) { _queue.Enqueue((id, flag, reason)); }

            public async Task FlushAsync()
            {
                var batch = new List<(string Id, string Flag, string Reason)>();
                while (_queue.TryDequeue(out var item)) batch.Add(item);
                if (batch.Count == 0) return;

                try { await _sink(batch); }
                catch
                {
                    foreach (var item in batch) _queue.Enqueue(item);   // do not lose results
                    throw;
                }
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private sealed class AbortSignal
        {
            private readonly int _maxConsecutive;
            private readonly object _gate = new object();
            private volatile bool _isSet;
            private string _reason = "";
            private int _consecutive;
            private int _released;

            public AbortSignal(int maxConsecutive) { _maxConsecutive = maxConsecutive; }

            public bool IsSet { get { return _isSet; } }
            public string Reason { get { lock (_gate) { return _reason; } } }
            public int Released { get { return Volatile.Read(ref _released); } }

            public void Trip(string reason)
            {
                lock (_gate)
                {
                    if (_isSet) return;
                    _reason = reason;
                    _isSet = true;
                }
            }

            public void NoteReleased() { Interlocked.Increment(ref _released); }
            public void NoteSuccess() { Interlocked.Exchange(ref _consecutive, 0); }

            public void NoteInfraFailure(string reason)
            {
                if (Interlocked.Increment(ref _consecutive) >= _maxConsecutive)
                    Trip("Too many consecutive send failures. Last error: " + reason);
            }
        }

        // crash recovery: a job with no heartbeat for 10 min is considered dead
//        public async Task ReleaseStaleAsync(int minutes = 10)
//        {
//            using var conn = new SqlConnection(objUtility.GetConnectionStr());
//            await conn.OpenAsync();
//            using var cmd = new SqlCommand(
//                @"UPDATE DigitalEmailJob
//SET IsActive = 0, Status = 'F', FinishedAt = GETDATE(), Message = 'Stale: no heartbeat'
//WHERE IsActive = 1 AND HeartbeatAt < DATEADD(MINUTE, -@m, GETDATE());

//IF NOT EXISTS (SELECT 1 FROM DigitalEmailJob WHERE IsActive = 1)
//    UPDATE Digital_Emaildetails SET dd_SendFlag = 'N'
//    WHERE dd_SendFlag = 'L' AND dd_LastRetryDate < DATEADD(MINUTE, -@m, GETDATE());", conn);
//            cmd.Parameters.AddWithValue("@m", minutes);
//            await cmd.ExecuteNonQueryAsync();
//        }

        // ---------- job helpers ----------
        private async Task ExecAsync(string sql, params SqlParameter[] ps)
        {
            using var con = new SqlConnection(objUtility.GetConnectionStr());
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddRange(ps);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<long?> TryStartJobAsync(string user)
        {
            try
            {
                using var con = new SqlConnection(objUtility.GetConnectionStr());
                using var cmd = new SqlCommand(
                    @"INSERT INTO tbl_DigitalEmailJob (StartedBy, Status) VALUES (@u, 'Q');
                  SELECT CAST(SCOPE_IDENTITY() AS BIGINT);", con);
                cmd.Parameters.AddWithValue("@u", user);
                await con.OpenAsync();
                return (long)await cmd.ExecuteScalarAsync();
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                return null;   // unique index: another job is active
            }
        }

        public Task ReleaseStaleAsync(int minutes = 5) => ExecAsync("UPDATE tbl_DigitalEmailJob\r\nSET IsActive = 0, Status = 'F', FinishedAt = GETDATE(), Message = 'Stale: no heartbeat'\r\nWHERE IsActive = 1 AND HeartbeatAt < DATEADD(MINUTE, -@m, GETDATE());\r\n\r\nIF NOT EXISTS (SELECT 1 FROM tbl_DigitalEmailJob WHERE IsActive = 1)\r\n    UPDATE Digital_Emaildetails SET dd_SendFlag = 'N'\r\n    WHERE dd_SendFlag = 'L' AND dd_LastRetryDate < DATEADD(MINUTE, -@m, GETDATE());",
        new SqlParameter("@m", minutes));

        public Task FinishJobAsync(long jobId, SendRunResult r) => ExecAsync(
        @"UPDATE tbl_DigitalEmailJob
          SET IsActive = 0, Status = @st, FinishedAt = GETDATE(), Sent = @s, Failed = @f, Message = @m
          WHERE ExecutionID = @id",
        new SqlParameter("@st", r.Success ? "C" : "F"),
        new SqlParameter("@s", r.Sent), new SqlParameter("@f", r.Failed),
        new SqlParameter("@m", Short(r.Message)), new SqlParameter("@id", jobId));

        public async Task<bool> HasPendingAsync() =>
            Query(objUtility.GetConnectionStr(),
                  "SELECT TOP 1 1 FROM Digital_Emaildetails WHERE dd_SendFlag = 'N'").Rows.Count > 0;

        public Task<DataTable> GetCurrentJobAsync() => Task.FromResult(Query(objUtility.GetConnectionStr(),
            @"SELECT TOP 1 ExecutionID, StartedBy, Status, IsActive, StartedAt, HeartbeatAt, FinishedAt,
                 Total, Sent, Failed, Message
          FROM tbl_DigitalEmailJob ORDER BY IsActive DESC, ExecutionID DESC"));   // active job first, else latest

        private async Task HeartbeatLoopAsync(long jobId, Counters c, CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        await ExecAsync(
                            "UPDATE tbl_DigitalEmailJob SET HeartbeatAt = GETDATE(), Sent = @s, Failed = @f WHERE ExecutionID = @id",
                            new SqlParameter("@s", Volatile.Read(ref c.TotalProcessed)),
                            new SqlParameter("@f", Volatile.Read(ref c.TotalErrors)),
                            new SqlParameter("@id", jobId));
                    }
                    catch { /* next tick retries */ }
                }
            }
            catch (OperationCanceledException) { }
        }

        private static ReportingTemplate LoadReportingTemplate(string connectionString, string docType)
        {
            DataTable dt = Query(connectionString,
                "SELECT ReportingEmailID, ReportingEmailSubject, ReportingEmailBody FROM tbl_EmailTemplate WHERE RefName = @dt",
                new SqlParameter("@dt", docType));

            var rt = new ReportingTemplate { Email = "", Subject = "", Body = "" };
            if (dt.Rows.Count > 0)
            {
                rt.Email = dt.Rows[0]["ReportingEmailID"].ToString().Trim();
                rt.Subject = dt.Rows[0]["ReportingEmailSubject"].ToString().Trim();
                rt.Body = dt.Rows[0]["ReportingEmailBody"].ToString().Trim();
            }
            return rt;
        }

        private static void AddAddresses(InternetAddressList list, string addresses)
        {
            if (string.IsNullOrWhiteSpace(addresses)) return;

            foreach (var address in addresses.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string a = address.Trim();
                if (a.Length > 0)
                    list.Add(MailboxAddress.Parse(a));
            }
        }
    }
#nullable restore
}
