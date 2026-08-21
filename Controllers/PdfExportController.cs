using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using TradeWeb.API.Models;
using TradeWeb.API.QuestPdfTemplates;
using TradeWeb.API.Repository;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfExportController : ControllerBase
    {
        private readonly ConvertData returnJson = new ConvertData();
        private readonly ITradeWebRepository _tradeWebRepository;

        public PdfExportController(ITradeWebRepository tradeWebRepository)
        {
            _tradeWebRepository = tradeWebRepository;
        }

        // Company name/logo always come from the DB (SP_InitializeLogin's branding lookup)
        // rather than the caller, so the PDF header can't be spoofed by the request body.
        // Best-effort: a DB hiccup falls back to whatever the caller supplied (if anything)
        // instead of failing the whole PDF.
        private void ApplyCompanyBranding(Action<string, string> apply)
        {
            try
            {
                var result = _tradeWebRepository.GetCompanyBranding();
                if (result is DataTable dt && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    string companyName = dt.Columns.Contains("CompanyName") ? Convert.ToString(row["CompanyName"]) : "";
                    string companyLogo = dt.Columns.Contains("CompanyLogo") ? Convert.ToString(row["CompanyLogo"]) : "";
                    apply(companyName, companyLogo);
                }
            }
            catch
            {
            }
        }

        // Replays each tab's DataApi server-side when the caller didn't supply Rows:
        // same ActionName/Option -> stored procedure lookup (tbl_GenericAPIDefinition)
        // the main TradeWeb endpoint uses, so a layout-only payload still gets tab data.
        // Best-effort per tab: a failed tab renders "No data available" instead of
        // failing the whole PDF.
        private void PopulateTabRows(DashboardPdfRequest req)
        {
            foreach (var widget in req.Widgets ?? new List<DashboardWidget>())
            {
                if (!string.Equals(widget.Type, "tableWithGraph", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var tab in widget.GridTabs ?? new List<GridTab>())
                {
                    // Opt-in, same as DashboardPdfDocument's own tab filter: a tab that
                    // won't render in the PDF (Setting.ShowInPdf not explicitly true)
                    // shouldn't have its stored procedure called at all.
                    if (tab.Setting == null || tab.Setting.ShowInPdf != true)
                        continue;

                    if ((tab.Rows?.Count ?? 0) > 0)
                        continue;

                    var option = tab.DataApi?.JUi?.Option?.Trim();
                    if (string.IsNullOrEmpty(option))
                        continue;

                    try
                    {
                        string actionName = tab.DataApi.JUi.ActionName?.Trim();
                        if (string.IsNullOrEmpty(actionName))
                            actionName = "TradeWeb";

                        string userId = "";
                        if (tab.DataApi.JApi != null && tab.DataApi.JApi.TryGetValue("UserId", out var u))
                            userId = JsonValueToString(u);
                        if (!string.IsNullOrWhiteSpace(req.UserId))
                            userId = req.UserId.Trim();

                        // Same dsXml shape the page posts to api/Main/TradeWeb.
                        var sb = new StringBuilder();
                        sb.Append("<dsXml>");
                        sb.Append("<J_Ui>\"ActionName\":\"").Append(SecurityElement.Escape(actionName))
                            .Append("\", \"Option\":\"").Append(SecurityElement.Escape(option)).Append("\"</J_Ui>");
                        sb.Append("<Sql></Sql>");
                        AppendFilterXml(sb, "X_Filter", tab.DataApi.XFilter);
                        AppendFilterXml(sb, "X_Filter_Multiple", tab.DataApi.XFilterMultiple);
                        sb.Append("<J_Api>\"UserId\":\"").Append(SecurityElement.Escape(userId)).Append("\"</J_Api>");
                        sb.Append("</dsXml>");

                        var result = _tradeWebRepository.CommonAPIrepoCall(actionName, option, sb.ToString());
                        if (result?.Data is DataSet ds && ds.Tables.Count > 0)
                            tab.Rows = DataTableToRows(ds.Tables[0]);
                    }
                    catch
                    {
                        // leave tab.Rows empty -> the table renders "No data available"
                    }
                }
            }
        }

        private static void AppendFilterXml(StringBuilder sb, string elementName, Dictionary<string, JsonElement> filter)
        {
            sb.Append('<').Append(elementName).Append('>');
            if (filter != null)
            {
                foreach (var kv in filter)
                {
                    sb.Append('<').Append(kv.Key).Append('>')
                        .Append(SecurityElement.Escape(JsonValueToString(kv.Value)))
                        .Append("</").Append(kv.Key).Append('>');
                }
            }
            sb.Append("</").Append(elementName).Append('>');
        }

        private static string JsonValueToString(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
                return "";
            return value.ValueKind == JsonValueKind.String ? (value.GetString() ?? "") : value.ToString();
        }

        private static List<Dictionary<string, JsonElement>> DataTableToRows(DataTable dt)
        {
            // TradeWeb's generic API stored procedures (tbl_GenericAPIDefinition) return
            // their result set as a single unnamed column - ADO.NET names it "Column1" -
            // holding the whole row set as one JSON array (SQL Server FOR JSON). This is
            // the same shape MainController.TradeWeb() unwraps into "rs0" for the page, so
            // mirror that here instead of treating the blob as one literal-text cell.
            if (dt.Columns.Count > 0 && dt.Columns[0].ColumnName == "Column1" && dt.Rows.Count > 0)
            {
                var raw = dt.Rows[0]["Column1"]?.ToString();
                if (string.IsNullOrWhiteSpace(raw))
                    return new List<Dictionary<string, JsonElement>>();

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(raw)
                        ?? new List<Dictionary<string, JsonElement>>();
                }
                catch (System.Text.Json.JsonException)
                {
                    return new List<Dictionary<string, JsonElement>>();
                }
            }

            // Newtonsoft serializes a DataTable as a flat array of row objects
            // (dates as ISO strings, numerics as numbers, DBNull as null).
            var json = JsonConvert.SerializeObject(dt);
            return System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json)
                ?? new List<Dictionary<string, JsonElement>>();
        }

        [HttpPost("GenerateDashboardPdf", Name = "GenerateDashboardPdf")]
        public IActionResult GenerateDashboardPdf([FromBody] DashboardPdfRequest req)
        {
            try
            {
                if (req == null || req.Widgets == null || req.Widgets.Count == 0)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "At least one widget is required.", ""));
                }

                QuestPDF.Settings.License = LicenseType.Community;
                FontSetup.EnsureRegistered();

                ApplyCompanyBranding((name, logo) =>
                {
                    if (!string.IsNullOrWhiteSpace(name)) req.CompanyName = name;
                    if (!string.IsNullOrWhiteSpace(logo)) req.CompanyLogoBase64 = logo;
                });

                PopulateTabRows(req);

                var document = new DashboardPdfDocument(req);

                byte[] pdfBytes;
                using (var ms = new MemoryStream())
                {
                    document.GeneratePdf(ms);
                    pdfBytes = ms.ToArray();
                }

                string safeTitle = string.IsNullOrWhiteSpace(req.Title) ? "Dashboard" : req.Title;
                foreach (char c in Path.GetInvalidFileNameChars())
                    safeTitle = safeTitle.Replace(c, '_');

                string pdfName = $"{safeTitle}.pdf";

                if (string.Equals(req.Output, "file", StringComparison.OrdinalIgnoreCase))
                    return File(pdfBytes, "application/pdf", pdfName);

                var response = new PDFResponse
                {
                    PDFName = pdfName,
                    Base64PDF = Convert.ToBase64String(pdfBytes)
                };

                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", response, ""));
            }
            catch (Exception ex)
            {
                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "An error occurred during PDF generation. " + ex.Message, ""));
            }
        }
    }
}
