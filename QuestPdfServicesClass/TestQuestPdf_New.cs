using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Data;

namespace TradeWeb.API.QuestPdfServicesClass
{
    public class TestQuestPdf_New
    {

        public static void Generate(string jsonFilePath, string outputFilePath, string logoPath, DataSet dsDetail)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var json = File.ReadAllText(jsonFilePath);
            var root = JsonDocument.Parse(json).RootElement;
            new QuestPdfJsonDocNew(root, logoPath, dsDetail).GeneratePdf(outputFilePath);
            Console.WriteLine($"[JsonPdfGenerator] PDF saved → {outputFilePath}");
        }

        public static byte[] GenerateBytes(string jsonFilePath, string logoPath, DataSet ds)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var json = File.ReadAllText(jsonFilePath);
            var root = JsonDocument.Parse(json).RootElement;
            return new QuestPdfJsonDocNew(root, logoPath, ds).GeneratePdf();
        }
    }

    // ─── Document ────────────────────────────────────────────────────────────────

    sealed class QuestPdfJsonDocNew : IDocument
    {
        private struct TableColumnDefinition
        {
            public string Key;
            public JsonElement DataStyle;

            public TableColumnDefinition(string key, JsonElement dataStyle)
            {
                Key = key;
                DataStyle = dataStyle;
            }
        }

        private struct ResolvedCell
        {
            public string Value;
            public JsonElement Style;
            public int Colspan;
            public int Rowspan;

            public ResolvedCell(string value, JsonElement style, int colspan = 1, int rowspan = 1)
            {
                Value = value;
                Style = style;
                Colspan = colspan;
                Rowspan = rowspan;
            }
        }

        private readonly JsonElement _root;

        private readonly string _logoImgPath;
        private readonly DataSet _dsDetails;
        public QuestPdfJsonDocNew(JsonElement root, string imageLogo, DataSet dsDtl)
        {
            _root = root;
            _logoImgPath = imageLogo;
            _dsDetails = dsDtl;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            var docCfg = J(_root, "document");
            var ghCfg = J(_root, "globalHeader");
            var gfCfg = J(_root, "globalFooter");
            var sections = J(_root, "sections");

            //List<ReportItem> globalHeaders = _root.GetProperty("globalHeader").Deserialize<List<ReportItem>>();

            List<ReportItem> gHeaders = JsonSerializer.Deserialize<List<ReportItem>>(
                                _root.GetProperty("globalHeader").GetRawText()
                            );
            List<ReportItem> gFooters = JsonSerializer.Deserialize<List<ReportItem>>(
                                _root.GetProperty("globalFooter").GetRawText()
                            );
            List<ReportItem> gContents = JsonSerializer.Deserialize<List<ReportItem>>(
                              _root.GetProperty("sections").GetRawText()
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
                switch (S(docCfg, "Orientation", "A4").ToUpperInvariant())
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



                // ── Global header /

                //page.Header().Element(c => RenderList(c, Arr(ghCfg, "content")));

                // ── Global footer / bottom page numbers ───────────────────────

                //page.Footer().Element(c => RenderList(c, Arr(gfCfg, "content")));

                // ── Main content ───────────────────────────────────────────────
                //page.Content().Column(col =>
                //{
                //    col.Spacing(0);
                //    if (sections.ValueKind != JsonValueKind.Undefined)
                //    {
                //        foreach (var sec in sections.EnumerateArray())
                //        {
                //            if (S(sec, "type", "") == "page-break")
                //                col.Item().PageBreak();
                //            else
                //                col.Item().Element(c => Render(c, sec));
                //        }
                //    }
                //});

                RenderGridHeader(page.Header(), gHeaders);
                RenderGrid(page.Content(), gContents);
                RenderGrid(page.Footer(), gFooters);


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
                           int currentPosition = 1;
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
                                   col.Item().Image(item.DataField).FitArea();
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


        private void RenderGrid(IContainer container, List<ReportItem> items)
        {
            container.Column(column =>
            {
                //var rows = items
                //    .OrderBy(x => x.Position)
                //    .GroupBy(x => GetRowNumber(x));

                foreach (var row in items.GroupBy(x => x.RowNo))
                {
                    column.Item().Row(r =>
                    {
                        int currentPosition = 1;

                        foreach (var item in row.OrderBy(x => x.Position))
                        {
                            // Empty space before item
                            if (item.Position > currentPosition)
                            {
                                r.RelativeItem(item.Position - currentPosition);
                            }
                            // Content
                            r.RelativeItem(item.Width)
                                .Element(c => RenderItem(c, item));

                            currentPosition = item.Position + item.Width;
                        }
                        // Remaining space
                        if (currentPosition <= 12)
                        {
                            r.RelativeItem(12 - currentPosition + 1);
                        }
                    });
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

                    var txt = container.Text(item.DataField);
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

                case "spacer":
                    container.Height(10);
                    break;
            }
        }
        private int GetRowNumber(ReportItem item)
        {
            return item.Position == 1 ? item.Position : 1;
        }



        private void RenderHeaderText(
    IContainer container,
    JsonElement item)
        {
            string text =
                S(item, "value",
                S(item, "HeaderDataField", ""));

            float fontSize =
                (float)N(item, "FontSize", 8);

            bool bold =
                B(item, "IsBold", false);

            string align =
                S(item, "Align", "L");

            ContainerAlign(container,
                align == "C"
                    ? "center"
                    : align == "R"
                        ? "right"
                        : "left")
                .Text(t =>
                {
                    t.DefaultTextStyle(x =>
                    {
                        x = x.FontSize(fontSize);

                        if (bold)
                            x = x.Bold();

                        return x;
                    });

                    t.Span(text);
                });
        }
        private void RenderHeaderImage(
    IContainer container,
    JsonElement item)
        {
            string file =
                S(item, "HeaderDataField", "");

            if (!File.Exists(file))
                return;

            container.Width(80)
                     .Height(50)
                     .Image(File.ReadAllBytes(file))
                     .FitArea();
        }

        private void RenderSectionText(
    IContainer container,
    JsonElement section)
        {
            string text = S(section, "Text", "");
            int fontSize = 10; // N(section, "FontSize", 10);
            bool bold = B(section, "Bold", false);
            string align = S(section, "Align", "Left");

            container.PaddingVertical(2).Text(t =>
            {
                var span = t.Span(text).FontSize(fontSize);

                if (bold)
                    span.Bold();

                switch (align.ToLower())
                {
                    case "center":
                        t.AlignCenter();
                        break;

                    case "right":
                        t.AlignRight();
                        break;

                    default:
                        t.AlignLeft();
                        break;
                }
            });
        }

        private void RenderLedgerTable(
    IContainer container,
    DataTable dt)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    foreach (DataColumn column in dt.Columns)
                        cols.RelativeColumn();
                });

                table.Header(header =>
                {
                    foreach (DataColumn column in dt.Columns)
                    {
                        header.Cell()
                              .Border(1)
                              .Padding(5)
                              .Text(column.ColumnName)
                              .Bold();
                    }
                });

                foreach (DataRow row in dt.Rows)
                {
                    foreach (DataColumn column in dt.Columns)
                    {
                        table.Cell()
                             .Border(1)
                             .Padding(5)
                             .Text(row[column]?.ToString() ?? "");
                    }
                }
            });
        }

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


        public DocumentSettings GetSettings()
        {
            return DocumentSettings.Default;
        }

    }


    public class ReportItem
    {
        public int Position { get; set; }   // 1-12
        public int RowNo { get; set; }   // 1-12
        public int Width { get; set; }      // 1-12
        public string Label { get; set; }
        public string DataField { get; set; }
        public string Align { get; set; }
        public int FontSize { get; set; }
        public bool IsBold { get; set; }
    }


}
