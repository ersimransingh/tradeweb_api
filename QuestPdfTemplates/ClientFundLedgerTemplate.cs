using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Data;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace TradeWeb.API.QuestPdfTemplates
{
    public class ClientFundLedgerTemplate : IDocument
    {

        private readonly DataRow _masterRow;
        private readonly DataSet _detailTable;
        private readonly JsonElement _root;


        public ClientFundLedgerTemplate(DataRow masterRow, DataSet detailTable, string strJson)
        {
            _masterRow = masterRow;
            _detailTable = detailTable;
            _root = JsonDocument.Parse(strJson).RootElement;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings()
        {
            return DocumentSettings.Default;
        }


        public void Compose(IDocumentContainer container)
        {
            var docCfg = J(_root, "document");
            var ghCfg = J(_root, "globalHeader");
            var gfCfg = J(_root, "globalFooter");
            var beforUpdateSections = J(_root, "sections");
            var tableSec = J(_root, "tableSections");
            var tableCols = J(_root, "tableColumns");

            var sections = UpdateSection(beforUpdateSections, _masterRow);

            //List<ReportItem> globalHeaders = _root.GetProperty("globalHeader").Deserialize<List<ReportItem>>();

            List<ReportItem> gHeaders = JsonSerializer.Deserialize<List<ReportItem>>(
                                _root.GetProperty("globalHeader").GetRawText()
                            );
            List<ReportItem> gFooters = JsonSerializer.Deserialize<List<ReportItem>>(
                                _root.GetProperty("globalFooter").GetRawText()
                            );
            List<ReportItem> gContents = JsonSerializer.Deserialize<List<ReportItem>>(
                              sections.GetRawText()
                          );


            //var sections = J(J(_root, "sections"), "content");

            container.Page(page =>
            {
                // ── Page size ──────────────────────────────────────────────────
                switch (S(docCfg, "PageSize", "A4").ToUpperInvariant())
                {
                    case "A4": page.Size(PageSizes.A4); break;
                    case "A4-LANDSCAPE": page.Size(PageSizes.A4.Landscape()); break;
                    case "A3": page.Size(PageSizes.A3); break;
                    case "A3-LANDSCAPE": page.Size(PageSizes.A3.Landscape()); break;
                    case "LETTER": page.Size(PageSizes.Letter); break;
                    case "LEGAL": page.Size(PageSizes.Legal); break;
                    case "CUSTOM":
                        var cp = J(docCfg, "customPageSize");
                        var cpU = S(cp, "unit", "pt") == "mm" ? Unit.Millimetre : Unit.Point;
                        page.Size((float)N(cp, "width", 595), (float)N(cp, "height", 842), cpU);
                        break;
                    default: page.Size(PageSizes.A4); break;
                }
                // ── Page Orientation  ──────────────────────────────────────────────────
                switch (S(docCfg, "Orientation", "PORTRAIT").ToUpperInvariant())
                {
                    case "LANDSCAPE": page.Size(PageSizes.A4.Landscape()); break;
                    case "PORTRAIT": page.Size(PageSizes.A4.Portrait()); break;
                    default: page.Size(PageSizes.A4); break;
                }
                // ── Margins ────────────────────────────────────────────────────
                page.MarginTop((float)N(docCfg, "MarginTop", 20));
                page.MarginBottom((float)N(docCfg, "MarginBottom", 20));
                page.MarginLeft((float)N(docCfg, "MarginLeft", 20));
                page.MarginRight((float)N(docCfg, "MarginRight", 20));

                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x
                    .FontFamily(S(docCfg, "defaultFont", "Helvetica"))
                    .FontSize((float)N(docCfg, "defaultFontSize", 8)));



                try
                {
                    RenderGridHeader(page.Header().ShowOnce(), gHeaders);
                    RenderGrid(page.Content(), gContents);
                    RenderGrid(page.Footer(), gFooters);
                }
                catch (Exception ex)
                {

                }

                //page.Header().Element(c =>
                //{
                //    RenderReportHeader(c, Arr(ghCfg, "content"));
                //});

                //page.Footer().Element(c =>
                //{
                //    RenderReportFooter(c, Arr(gfCfg, "content"));
                //});

                //page.Content().Element(c =>
                //{
                //    RenderReportSections(c, sections);
                //});

            });

        }

        private void RenderGridHeader(IContainer container, List<ReportItem> items)
        {
            var groups = items.GroupBy(x => x.Position);
            container.Row(row =>
            {
                foreach (var group in groups)
                {
                    row.RelativeItem(group.First().Width)
                       .Column(col =>
                       {
                           int currentPosition = 0;
                           foreach (var item in group.OrderBy(x => x.RowNo))
                           {
                               // Empty space before item
                               if (item.Position > currentPosition)
                               {
                                   row.RelativeItem(item.Position - currentPosition);
                               }
                               // Content
                               if (item.Label?.ToLower() == "image")
                               {
                                   col.Item().MaxHeight(100).MaxWidth(130).Image(item.DataField).FitArea();
                               }
                               else
                               {
                                   col.Item().Element(c => RenderItem(c, item));
                               }

                               currentPosition = item.Position + item.Width;
                           }
                           // Remaining space
                           if (currentPosition <= 12)
                           {
                               row.RelativeItem(12 - currentPosition + 1);
                           }
                       });
                }
            });
        }

        //private void RenderGrid(IContainer container, List<ReportItem> items)
        //{
        //    container.Column(column =>
        //    {
        //        foreach (var row in items.GroupBy(x => x.RowNo))
        //        {
        //            column.Item().Row(r =>
        //            {
        //                int currentPosition = 1;

        //                foreach (var item in row.OrderBy(x => x.Position))
        //                {
        //                    // Empty space before item
        //                    if (item.Position > currentPosition)
        //                    {
        //                        r.RelativeItem(item.Position - currentPosition);
        //                    }
        //                    if (item.Label?.ToLower() == "image")
        //                    {
        //                        r.RelativeItem().Image(item.DataField).FitArea();
        //                    }
        //                    // Content
        //                    else if (item.Label?.ToLower() == "table")
        //                    {
        //                        foreach (DataTable tableData in _detailTable.Tables)
        //                        {
        //                            var tableColumns = JsonSerializer.Deserialize<List<TableColumnConfig>>(
        //                                                    _root.GetProperty("tableColumns").GetRawText());
        //                            if (tableData?.Rows.Count > 0)
        //                            {
        //                                column.Item().Element(x => RenderDataTableGrid(x, tableData));
        //                                // r.RelativeItem().Element(x => CreateTransactionTable(x, tableData, tableColumns));
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        r.RelativeItem(item.Width)
        //                        .Element(c => RenderItem(c, item));
        //                    }

        //                    currentPosition = item.Position + item.Width;
        //                }
        //                // Remaining space
        //                if (currentPosition <= 12)
        //                {
        //                    r.RelativeItem(12 - currentPosition);
        //                }
        //            });
        //        }
        //    });
        //}


        private void RenderGrid(IContainer container, List<ReportItem> items)
        {
            container.Column(column =>
            {
                foreach (var row in items.GroupBy(x => x.RowNo))
                {
                    var tableItem = row.FirstOrDefault(x =>
                        x.Label?.Equals("table", StringComparison.OrdinalIgnoreCase) == true);

                    if (tableItem != null)
                    {
                        // Render all items before table
                        var itemsBeforeTable = row
                            .Where(x => x.Position < tableItem.Position)
                            .OrderBy(x => x.Position)
                            .ToList();

                        if (itemsBeforeTable.Any())
                        {
                            column.Item().Row(r =>
                            {
                                foreach (var item in itemsBeforeTable)
                                {
                                    r.RelativeItem(item.Width).Border(1)
                                     .Element(c => RenderItem(c, item));
                                }
                            });
                        }

                        // Render table at exact row position
                        //var tableColumnsDict = JsonSerializer.Deserialize<Dictionary<string, List<TableColumnConfig>>>(
                        //        _root.GetProperty("tableColumns").GetRawText());

                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        var tableColumnsDict =
                            JsonSerializer.Deserialize<Dictionary<string, TableDefinition>>(
                                _root.GetProperty("tableColumns").GetRawText(),
                                options);

                        for (int i = 0; i < _detailTable.Tables.Count; i++)
                        {
                            var tableData = _detailTable.Tables[i];

                            if (tableData.Rows.Count == 0)
                                continue;

                            string tableKey = $"table{i + 1}";

                            var tableConfig = tableColumnsDict[tableKey];

                            //if (!tableColumnsDict.TryGetValue(tableKey, out var tableColumns))
                            //    continue;

                            column.Item()
                                .Element(x => CreateTransactionTable(
                                    x,
                                    tableData,
                                    tableConfig));

                            column.Item().Height(10);
                        }

                        continue;
                    }

                    // Normal row rendering

                    column.Item().Row(r =>
                    {
                        int currentPosition = 0;

                        foreach (var item in row.OrderBy(x => x.Position))
                        {
                            // Empty space before item
                            //if (item.Position > currentPosition)
                            //{
                            //    r.RelativeItem(item.Position - currentPosition);
                            //}
                            if (item.Label?.ToLower() == "image")
                            {
                                r.RelativeItem().Image(item.DataField).FitArea();
                            }
                            // Spacer
                            else if (item.Label?.ToLower() == "spacer")
                            {
                                r.RelativeItem(100).Height(item.Width);
                            }
                            // Content
                            else
                            {
                                r.RelativeItem(item.Width).Border(0)
                                .Element(c => RenderItem(c, item));
                            }

                            currentPosition = currentPosition + item.Width;
                        }
                        // Remaining space
                        if (currentPosition < 100)
                        {
                            r.RelativeItem(100 - currentPosition);
                        }
                    });
                }
            });
        }


        private void RenderDataTableGrid(IContainer container, DataTable dt)
        {
            container.Column(column =>
            {
                if (dt.Rows.Count > 0)
                {
                    var tableColumns1 =
                        JsonSerializer.Deserialize<List<TableColumnConfig>>(
                            _root.GetProperty("tableColumns").GetRawText());

                    //column.Item()
                    //    .Element(x => CreateTransactionTable(
                    //        x,
                    //        dt,
                    //        tableColumns1));
                }

            });
        }


        private void RenderItem(IContainer container, ReportItem item)
        {
            switch (item.Label?.ToLower())
            {
                //case "image":
                //    container.Image(item.Value);
                //    break;
                case "text":

                    var txt = container.Padding((float)2.2).Text(item.DataField);
                    if (item.IsBold)
                        txt.Bold();
                    if (item.FontSize > 0)
                        txt.FontSize(item.FontSize);

                    switch (item.Align?.ToUpper())
                    {
                        case "C":
                            txt.AlignCenter();
                            break;

                        case "R":
                            txt.AlignRight();
                            break;

                        default:
                            txt.AlignLeft();
                            break;
                    }
                    break;

                //case "spacer":
                //    container.Height(10);
                //    break;
            }
        }
        private int GetRowNumber(ReportItem item)
        {
            return item.Position == 1 ? item.Position : 1;
        }

        private void CreateTransactionTable(
    IContainer container,
    DataTable dtlTable,
   TableDefinition tableConfig)
        {
            var columnsConfig = tableConfig.Columns
                           .OrderBy(x => x.DisplayOrder)
                           .ToList();

            container.Table(table =>
            {
                int totalWidth = columnsConfig.Sum(x => x.Width);
                // Column Widths
                table.ColumnsDefinition(columns =>
                {
                    foreach (var col in columnsConfig)
                    {
                        columns.RelativeColumn(col.Width);
                    }
                });

                // Multi-row header
                table.Header(header =>
                {
                    foreach (var row in tableConfig.HeaderRows)
                    {
                        foreach (var headerCell in row)
                        {
                            var cell = header.Cell();

                            if (headerCell.colspan > 1)
                                cell = cell.ColumnSpan((uint)headerCell.colspan);

                            cell.Border(1)
                                .Background(Colors.Grey.Lighten2)
                                .Padding(3)
                                .AlignCenter()
                                .Text(headerCell.label)
                                .Bold();
                        }
                    }
                });


                // Data Rows
                foreach (DataRow row in dtlTable.Rows)
                {
                    foreach (var col in columnsConfig)
                    {
                        string value = "";

                        if (dtlTable.Columns.Contains(col.DataField))
                            value = row[col.DataField]?.ToString() ?? "";

                        var cell = table.Cell()
                            .Border(1)
                            .Padding(3);

                        switch (col.Alignment?.ToUpper())
                        {
                            case "R":
                                cell.AlignRight().Text(value);
                                break;

                            case "C":
                                cell.AlignCenter().Text(value);
                                break;

                            default:
                                cell.AlignLeft().Text(value);
                                break;
                        }
                    }
                }
            });

        }

        //public void Compose(IDocumentContainer container)
        //{
        //    container.Page(page =>
        //    {
        //        page.Margin(20);

        //        page.Header()
        //            .Text("Dynamic Report")
        //            .FontSize(18)
        //            .Bold();

        //        page.Content()
        //            .Column(col =>
        //            {
        //                col.Spacing(10);

        //                BuildMasterSection(col);

        //                col.Item().PaddingVertical(10);

        //                BuildDetailTable(col);
        //            });

        //        page.Footer()
        //            .AlignCenter()
        //            .Text(x =>
        //            {
        //                x.Span("Page ");
        //                x.CurrentPageNumber();
        //            });
        //    });
        //}

        //public void Compose(IDocumentContainer container)
        //{
        //    container.Page(page =>
        //    {
        //        page.Size(PageSizes.A4.Landscape());
        //        page.Margin(20);
        //        page.MarginBottom(40);
        //        page.DefaultTextStyle(x => x
        //          .FontFamily("Helvetica")
        //          .FontSize(8));
        //        // ================= HEADER =================
        //        page.Header()
        //        .ShowOnce()
        //        .Column(column =>
        //        {
        //            column.Spacing(3);

        //            // Logo and Company name + address
        //            column.Item().Row(row =>
        //            {
        //                if (!string.IsNullOrWhiteSpace(headerImagePath))
        //                {
        //                    try
        //                    {
        //                        if (File.Exists(headerImagePath))
        //                        {
        //                            var imageBytes = File.ReadAllBytes(headerImagePath);
        //                            //row.ConstantItem(200).Height(90).Image(imageBytes).FitArea();
        //                            row.AutoItem().Element(container =>
        //                            {
        //                                container
        //                                    .MaxWidth(490)
        //                                    .MaxHeight(105)
        //                                    .Image(imageBytes);
        //                            });
        //                        }
        //                    }
        //                    catch { /* ignore image errors */ }
        //                }
        //                else
        //                {
        //                    if (!string.IsNullOrWhiteSpace(logoPath))
        //                    {
        //                        try
        //                        {
        //                            if (File.Exists(logoPath))
        //                            {
        //                                var imageBytes = File.ReadAllBytes(logoPath);
        //                                row.ConstantItem(80).Height(50).Image(imageBytes).FitArea();
        //                            }
        //                        }
        //                        catch { /* ignore image errors */ }
        //                    }
        //                    // CENTER: Company Details
        //                    row.RelativeItem().Column(col =>
        //                    {
        //                        col.Item().AlignCenter().Text(_compDT.Rows[0]["CompanyName"].ToString()).SemiBold().FontSize(13);

        //                        col.Item().AlignCenter().Text(_compDT.Rows[0]["Address1"].ToString()).FontSize(10);
        //                        col.Item().AlignCenter().Text(_compDT.Rows[0]["Address2"].ToString()).FontSize(10);
        //                        col.Item().AlignCenter().Text(_compDT.Rows[0]["Address3"].ToString()).FontSize(10);
        //                        col.Item().AlignCenter().Text("SEBI REGN.NO.- " + _compDT.Rows[0]["SEBIRefno"].ToString()).FontSize(10);
        //                        col.Item().AlignCenter().Text("CIN No. : " + _compDT.Rows[0]["CINNo"].ToString()).FontSize(10);
        //                        col.Item().AlignCenter().Text(_compDT.Rows[0]["ReportName"].ToString()).FontSize(10);
        //                    });
        //                    // RIGHT EMPTY SPACE (for balance)
        //                    row.ConstantItem(80);
        //                }
        //            });
        //        });

        //        // ================= MAIN/DYNAMIC CONTENT =================
        //        page.Content().Column(col =>
        //        {
        //            // ---------------- Header ----------------
        //            col.Item().Row(row =>
        //            {
        //                row.RelativeItem(3).Column(c =>
        //                {
        //                    string nameWithCode = (_masterRow["ClientName"].ToString() + " [" + _masterRow["ClientCode"].ToString() + "]");
        //                    c.Item().Text("Account: " + (_masterRow.Table.Columns.Contains("ClientName") ? nameWithCode ?? "" : "")).Bold();

        //                    c.Item().Text("" + (_masterRow.Table.Columns.Contains("ClientAddress1") ? _masterRow["ClientAddress1"] ?? "" : ""));
        //                    c.Item().Text("" + (_masterRow.Table.Columns.Contains("ClientAddress2") ? _masterRow["ClientAddress2"] ?? "" : ""));
        //                    c.Item().Text("" + (_masterRow.Table.Columns.Contains("ClientAddress3") ? _masterRow["ClientAddress3"] ?? "" : ""));
        //                    c.Item().Text("" + (_masterRow.Table.Columns.Contains("ClientCity") ? _masterRow["ClientCity"] ?? "" : ""));
        //                    c.Item().Text("" + (_masterRow.Table.Columns.Contains("ClientAddPIN") ? _masterRow["ClientAddPIN"] ?? "" : ""));
        //                    c.Item().Text("" + (_masterRow.Table.Columns.Contains("ClientState") ? _masterRow["ClientState"] ?? "" : ""));
        //                    c.Item().Text($"Mobile :" + (_masterRow.Table.Columns.Contains("ClientTel") ? _masterRow["ClientTel"] ?? "" : ""));
        //                    c.Item().Text($"Email  :" + (_masterRow.Table.Columns.Contains("ClientEmailId") ? _masterRow["ClientEmailId"] ?? "" : ""));
        //                    c.Item().Text($"PAN    :" + (_masterRow.Table.Columns.Contains("PANNo") ? _masterRow["PANNo"] ?? "" : ""));
        //                });

        //            });
        //            col.Item().PaddingTop(7);

        //            col.Item().Row(r =>
        //            {
        //                r.RelativeItem(20).AlignCenter().AlignMiddle()
        //                                        .Text(_masterRow["Grid1"].ToString());
        //            });

        //            col.Item().PaddingTop(7);

        //            // ---------------- Company Details ----------------
        //            col.Item().Column(col =>
        //                        {
        //                            col.Item().Text(_compDT.Rows[0]["CompanyName"].ToString());
        //                            col.Item().Text(_compDT.Rows[0]["Address1"].ToString());
        //                            col.Item().Text(_compDT.Rows[0]["Address2"].ToString());
        //                            col.Item().Text(_compDT.Rows[0]["Address3"].ToString());
        //                            col.Item().Text("SEBI Regin No. :- " + _compDT.Rows[0]["SEBIRefno"].ToString());
        //                            col.Item().Text("Tel No.:- " + _compDT.Rows[0]["CompTeleNo"].ToString() + " Fax No.:- " + _compDT.Rows[0]["CompFaxNo"].ToString());
        //                            col.Item().Text("Email ID for Investor Complaint : " + _compDT.Rows[0]["ComplianceEmail"].ToString());
        //                            col.Item().Text("Website : " + _compDT.Rows[0]["CompWebSite"].ToString());
        //                            col.Item().Text("CIN No.: " + _compDT.Rows[0]["CINNo"].ToString());
        //                            col.Item().Text("Compliance Officeer:- " + _compDT.Rows[0]["ComplianceOfficer"].ToString());
        //                            col.Item().Text("Email: " + _compDT.Rows[0]["ComplianceEmail"].ToString());
        //                        });

        //            col.Item().PaddingVertical(5);

        //            // ---------------- Main Table ----------------
        //            foreach (DataTable tableData in _detailTable.Tables)
        //            {
        //                if (tableData?.Rows.Count > 0)
        //                {
        //                    col.Item().Element(x => CreateTransactionTable(x, tableData));
        //                }
        //                col.Item().PaddingVertical(10);
        //            }
        //            // ---------------- Summary Table ----------------
        //            //if (Summary != null && Summary.Rows.Count > 0)
        //            //    col.Item().Element(x => CreateSummaryTable(x, Summary));

        //            col.Item().PaddingTop(5);

        //            // ---------------- Footer Notes ----------------
        //            col.Item().Text(_compDT.Rows[0]["FooterText"].ToString());
        //        });

        //        // ================= FOOTER =================
        //        page.Footer().Column(column =>
        //        {
        //            // Horizontal line above footer content
        //            column.Item()
        //                .LineHorizontal(1)
        //                .LineColor(Colors.Black);

        //            column.Item().PaddingTop(5);

        //            column.Item().Row(row =>
        //            {
        //                row.RelativeItem().AlignLeft()
        //                .Text($"Print Date : {DateTime.Now:dd/MM/yyyy}");

        //                row.RelativeItem().AlignRight()
        //                    .Text(text =>
        //                    {
        //                        text.Span(_compDT.Rows[0]["FooterModuleName"].ToString() + " [Page: ");
        //                        text.CurrentPageNumber();
        //                        text.Span("]");
        //                    });
        //            });
        //        });


        //        //page.Footer().Row(row =>
        //        //{
        //        //    row.RelativeItem().AlignLeft()
        //        //        .Text($"Print Date : {DateTime.Now:dd/MM/yyyy}");

        //        //    row.RelativeItem().AlignRight()
        //        //        .Text(text =>
        //        //        {
        //        //            text.Span(_compDT.Rows[0]["FooterModuleName"].ToString() + " [Page: ");
        //        //            text.CurrentPageNumber();
        //        //            text.Span("]");
        //        //        });
        //        //});

        //    });
        //}

        //private void CreateTransactionTable(IContainer container, DataTable dtlTable)
        //{
        //    container.Table(t =>
        //    {
        //        t.ColumnsDefinition(columns =>
        //        {
        //            foreach (DataColumn column in dtlTable.Columns)
        //            {
        //                columns.RelativeColumn();
        //            }
        //        });
        //        // Header
        //        t.Header(header =>
        //        {
        //            foreach (DataColumn column in dtlTable.Columns)
        //            {
        //                header.Cell()
        //                    .Border(1)
        //                    .Background(Colors.Grey.Lighten2)
        //                    .Padding(5)
        //                    .Text(column.ColumnName)
        //                    .Bold();
        //            }
        //        });
        //        // Rows
        //        foreach (DataRow row in dtlTable.Rows)
        //        {
        //            foreach (DataColumn column in dtlTable.Columns)
        //            {
        //                t.Cell()
        //                    .Border(1)
        //                    .Padding(5)
        //                    .Text(row[column]?.ToString() ?? "");
        //            }
        //        }
        //    });
        //}



        private static IContainer ContainerAlign(IContainer c, string align) => align switch
        {
            "center" => c.AlignCenter(),
            "right" => c.AlignRight(),
            _ => c.AlignLeft()
        };

        private static JsonElement J(JsonElement e, string k)
        {
            if (e.ValueKind == JsonValueKind.Undefined || e.ValueKind == JsonValueKind.Null)
            {
                return default;
            }
            return e.TryGetProperty(k, out var v) ? v : default;
        }
        private static string S(JsonElement e, string k, string d = "") => J(e, k) is { ValueKind: JsonValueKind.String } v ? v.GetString() ?? d : d;
        private static double N(JsonElement e, string k, double d = 0) => J(e, k) is { ValueKind: JsonValueKind.Number } v ? v.GetDouble() : d;
        private static bool B(JsonElement e, string k, bool d = false) => J(e, k).ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => d };
        private static JsonElement Arr(JsonElement e, string k) => J(e, k) is { ValueKind: JsonValueKind.Array } v ? v : default;


        private static JsonElement UpdateSection(JsonElement section, DataRow row1)
        {
            var sectionArray = JArray.Parse(section.GetRawText());

            foreach (JObject item in sectionArray)
            {
                string colName = item["DataField"]?.ToString();

                if (row1.Table.Columns.Contains(colName))
                    item["DataField"] = Convert.ToString(row1[colName]);
            }

            // Convert JArray back to JsonElement
            string json = sectionArray.ToString(Newtonsoft.Json.Formatting.None);

            JsonElement updatedSections = JsonDocument.Parse(json).RootElement.Clone();

            return updatedSections;
        }

    }

    public class ReportItem
    {
        public int Position { get; set; }   // 1-12
        public int RowNo { get; set; }
        public int Width { get; set; }      // 1-12
        public string Label { get; set; }
        public string DataField { get; set; }
        public string Align { get; set; }
        public int FontSize { get; set; }
        public bool IsBold { get; set; }
    }

    public class TableColumnConfig
    {
        public string ColumnName { get; set; }
        public string DataField { get; set; }
        public int Width { get; set; }
        public string Alignment { get; set; }
        public int DisplayOrder { get; set; }
    }


    public class HeaderCell
    {
        public string label { get; set; }
        public int colspan { get; set; } = 1;
        public int rowspan { get; set; } = 1;
    }

    public class TableDefinition
    {
        public List<List<HeaderCell>> HeaderRows { get; set; }
        public List<TableColumnConfig> Columns { get; set; }
    }
}
