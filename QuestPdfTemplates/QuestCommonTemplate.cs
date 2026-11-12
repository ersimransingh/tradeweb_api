using Newtonsoft.Json.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TradeWeb.API.QuestPdfServicesClass;

namespace TradeWeb.API.QuestPdfTemplates
{
    public class QuestCommonTemplate : IDocument
    {

        private readonly DataRow _masterRow;
        private readonly DataSet _detailTable;
        private readonly byte[] _headerImgByte;
        private readonly byte[] _compLogoByte;
        public readonly int totalChars;
        LayoutDefinition _layout;

        public QuestCommonTemplate(DataRow masterRow, DataSet detailTable, LayoutDefinition layout, byte[] headerImage, byte[] compLogo, int maxWidth)
        {
            _masterRow = masterRow;
            _detailTable = detailTable;
            _headerImgByte = headerImage;
            _compLogoByte = compLogo;
            totalChars = maxWidth;
            _layout = layout;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings()
        {
            return DocumentSettings.Default;
        }

        private static string GetValueIgnoreCase(JObject obj, string propertyName)
        {
            return obj.Properties()
                      .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                      ?.Value?.ToString() ?? "";
        }
        private static bool ParseBool(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Trim();
            return value == "1"
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("y", StringComparison.OrdinalIgnoreCase);
        }
        public void Compose(IDocumentContainer container)
        {
            string masterTableMappingId = _layout.TableSections
                .Where(x => x.TableName.Equals("mastertable",
                    StringComparison.OrdinalIgnoreCase))
                .Select(x => x.TableMappingId)
                .FirstOrDefault();

            List<ReportItem> gHeaders = _layout.Headers;
            List<ReportItem> gFooters = _layout.Footers;

            var columnMap = _masterRow.Table.Columns
                            .Cast<DataColumn>()
                            .ToDictionary(
                                c => c.ColumnName,
                                c => c,
                                StringComparer.OrdinalIgnoreCase);

            List<ReportItem> gContents = _layout.Tables[masterTableMappingId]
                .Columns
                .Select(x => ToReportItem(x, _masterRow))
                .ToList();

            container.Page(page =>
            {
                string strPage = GetValueIgnoreCase(_layout.Document, "pagesize");
                string strOrient = GetValueIgnoreCase(_layout.Document, "orientation");
                string customSize = GetValueIgnoreCase(_layout.Document, "customsize");

                var pageSize = strPage.ToUpperInvariant();
                var orientation = strOrient.ToUpperInvariant();

                bool showHeaderEachPage = ParseBool(GetValueIgnoreCase(_layout.Document, "showheadereachpage"));

                double pgWidth;
                double pgHeight;

                switch (pageSize.ToUpperInvariant())
                {
                    case "A3":
                        pgWidth = 297;
                        pgHeight = 420;
                        break;

                    case "LETTER":
                        pgWidth = 215.9;
                        pgHeight = 279.4;
                        break;

                    case "LEGAL":
                        pgWidth = 215.9;
                        pgHeight = 355.6;
                        break;

                    case "CUSTOM":
                        string[] bothSize = customSize.Split(new[] { 'x', 'X' }, StringSplitOptions.RemoveEmptyEntries);
                        pgWidth = double.Parse(bothSize[0].Trim(), CultureInfo.InvariantCulture);
                        pgHeight = double.Parse(bothSize[1].Trim(), CultureInfo.InvariantCulture);
                        break;

                    case "A4":
                    default:
                        pgWidth = 210;
                        pgHeight = 297;
                        break;
                }

                bool landscape = strOrient.Equals(
                    "landscape",
                    StringComparison.OrdinalIgnoreCase);

                // For standard sizes, swap.
                // For custom, dimensions are already explicitly defined.
                if (landscape && !pageSize.Equals("CUSTOM", StringComparison.OrdinalIgnoreCase))
                {
                    (pgWidth, pgHeight) = (pgHeight, pgWidth);
                }

                page.Size(new PageSize(
                    (float)(pgWidth * 72.0 / 25.4),
                    (float)(pgHeight * 72.0 / 25.4)
                ));
                // ── Margins ────────────────────────────────────────────────────

                page.MarginTop((float)NFloat(_layout.Document, "margintop", 20));
                page.MarginBottom((float)NFloat(_layout.Document, "marginbottom", 20));
                page.MarginLeft((float)NFloat(_layout.Document, "marginleft", 20));
                page.MarginRight((float)NFloat(_layout.Document, "marginright", 20));

                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x
                    .FontFamily(SString(_layout.Document, "defaultfont", "Helvetica"))
                    .FontSize((float)NFloat(_layout.Document, "defaultfontsize", 8)));

                try
                {
                    var header = page.Header();
                    if (!showHeaderEachPage)
                        header = header.ShowOnce();

                    if (_headerImgByte != null) //// && _headerImgByte.Length > 0
                    {
                        header.Column(column =>
                        {
                            column.Spacing(3);
                            // Logo and Company name + address
                            column.Item().Row(row =>
                            {
                                try
                                {
                                    // var imageBytes = File.ReadAllBytes(_headerImgPath);
                                    row.RelativeItem()
                                   .Image(_headerImgByte)
                                   .FitArea();

                                }
                                catch { /* ignore image errors */ }

                            });
                        });
                    }
                    else
                    {
                        RenderGridHeader(header, gHeaders);
                    }

                    RenderGrid(page.Content(), gContents);
                    RenderGridFooter(page.Footer(), gFooters);
                }
                catch (Exception ex)
                {
                    throw;
                }
            });
        }

        private void RenderGridHeader(IContainer container, List<ReportItem> items)
        {
            var visibleItems = items
                .Where(x => x.isvaluevisible)
                .OrderBy(x => x.position)
                .ThenBy(x => x.rowno)
                .ToList();

            if (!visibleItems.Any())
                return;

            container.Row(row =>
            {
                // Logo - fixed width
                if (_compLogoByte != null)
                {
                    row.ConstantItem(130)
                        .AlignTop()
                        .AlignLeft()
                        .Image(_compLogoByte)
                        .FitArea();

                    // Company information - remaining width
                    row.RelativeItem()
                        .AlignCenter()
                        .Column(column =>
                        {
                            foreach (var item in visibleItems)
                            {
                                column.Item()
                                      .Element(c => RenderItem(c, item));
                            }
                        });
                }
                else
                {
                    row.RelativeItem()
                      .AlignCenter()
                      .Column(column =>
                      {
                          foreach (var item in visibleItems)
                          {
                              column.Item()
                                    .Element(c => RenderItem(c, item));
                          }
                      });
                }

            });
        }

        private void RenderGridFooter(IContainer container, List<ReportItem> items)
        {
            container.Row(row =>
            {
                foreach (var item in items.OrderBy(x => x.position))
                {
                    var cell = row.RelativeItem(item.width).Padding(3);

                    if (item.align?.Equals("R", StringComparison.OrdinalIgnoreCase) == true)
                        cell = cell.AlignRight();
                    else if (item.align?.Equals("C", StringComparison.OrdinalIgnoreCase) == true)
                        cell = cell.AlignCenter();
                    else
                        cell = cell.AlignLeft();

                    cell.Text(text =>
                    {
                        string value = "";

                        if (item.datafield?.Equals("printdate", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            value = item.label + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                        }
                        else if (item.datafield?.Equals("pagenumber", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            text.Span(item.label);

                            var pageSpan = text.CurrentPageNumber();

                            if (item.isbold)
                                pageSpan.Bold();

                            if (item.fontsize > 0)
                                pageSpan.FontSize(item.fontsize);

                            return;
                        }
                        else
                        {
                            value = item.label;
                        }

                        var span = text.Span(value);

                        if (item.isbold)
                            span.Bold();

                        if (item.fontsize > 0)
                            span.FontSize(item.fontsize);
                    });
                }
            });
        }

        //changes for text and column on same row
        private void RenderGrid(IContainer container, List<ReportItem> items)
        {
            var rows = items.GroupBy(x => x.rowno)
                            .OrderBy(g => g.Key)
                            .Select(g => g.ToList())
                            .ToList();

            container.Column(column =>
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var rowItems = rows[i];
                    var tableItem = rowItems.FirstOrDefault(x =>
                        string.Equals(x.type, "table", StringComparison.OrdinalIgnoreCase));

                    // ── Normal row ─────────────────────────────────────────────
                    if (tableItem == null)
                    {
                        column.Item().Row(r => RenderItemsRow(r, rowItems, totalChars));
                        continue;
                    }

                    // ── Row with a table ───────────────────────────────────────────
                    var ordered = rowItems.OrderBy(x => x.position).ToList();
                    var leftFirst = ordered.Where(x => x.position < tableItem.position).ToList();
                    var rightItems = ordered.Where(x => x.position > tableItem.position).ToList();

                    int leftUnits = leftFirst.Sum(x => x.width);
                    int rightUnits = Math.Max(0, totalChars - leftUnits - tableItem.width);

                    bool stackOnLeft = leftUnits > 0;
                    bool declaresPanel = !stackOnLeft && rightItems.Any();   // panel must be declared in the table's row

                    int stackUnits = stackOnLeft ? leftUnits : (declaresPanel ? rightUnits : 0);

                    var extraRows = new List<List<ReportItem>>();
                    while (stackUnits > 0 && i + 1 < rows.Count &&
                           !rows[i + 1].Any(x => string.Equals(x.type, "table", StringComparison.OrdinalIgnoreCase)) &&
                           !rows[i + 1].Any(x => string.Equals(x.type, "spacer", StringComparison.OrdinalIgnoreCase) ||
                                                 string.Equals(x.type, "image", StringComparison.OrdinalIgnoreCase)) &&
                           rows[i + 1].Sum(x => x.width) <= stackUnits)
                    {
                        extraRows.Add(rows[++i]);
                    }

                    bool sidePanel = declaresPanel;
                    bool leftBand = stackOnLeft;

                    if (sidePanel)
                    {
                        var sideRows = new List<List<ReportItem>>();
                        if (rightItems.Any()) sideRows.Add(rightItems);   // "Statement dated" + value
                        sideRows.AddRange(extraRows);                     // DPName, label, ...

                        var headerSide = sideRows.FirstOrDefault() ?? new List<ReportItem>();
                        var bodySide = sideRows.Skip(1).ToList();

                        column.Item().Element(c =>
                        RenderTableSlot(c, tableItem, tableItem.width, rightUnits, headerSide, bodySide)); // no ShowEntire
                    }
                    else if (leftBand)
                    {
                        column.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(leftUnits);                      // leftBand implies leftUnits > 0
                                cd.RelativeColumn(Math.Max(1, tableItem.width));
                                if (rightUnits > 0) cd.RelativeColumn(rightUnits);
                            });

                            // Left cell: draws the outline at full row height, items draw no lines of their own
                            IContainer leftCell = t.Cell().BorderLeft(1).BorderRight(1).BorderBottom(1);
                            leftCell.Column(left =>
                            {
                                if (leftFirst.Any())
                                    left.Item().Row(lr => RenderItemsRow(lr, leftFirst, leftUnits, true));

                                foreach (var extra in extraRows)
                                    left.Item().Row(lr => RenderItemsRow(lr, extra, leftUnits, true));
                            });

                            // Table cell: closes the right and bottom edges at the band height
                            IContainer tableCell = t.Cell().BorderRight(1).BorderBottom(1);
                            RenderTableSlot(tableCell, tableItem);

                            // Right cell (only if something is placed after the table)
                            if (rightUnits > 0)
                            {
                                t.Cell().Column(right =>
                                {
                                    if (rightItems.Any())
                                        right.Item().Row(rr => RenderItemsRow(rr, rightItems, rightUnits));
                                });
                            }
                        });
                    }
                    else
                    {
                        column.Item().Row(r =>
                        {
                            RenderTableSlot(r.RelativeItem(Math.Max(1, tableItem.width)), tableItem);

                            if (rightUnits > 0)
                                r.RelativeItem(rightUnits).Column(right =>
                                {
                                    if (rightItems.Any())
                                        right.Item().Row(rr => RenderItemsRow(rr, rightItems, rightUnits));
                                });
                        });
                    }

                    bool nextIsTable = i + 1 < rows.Count && rows[i + 1].Any(x => string.Equals(x.type, "table", StringComparison.OrdinalIgnoreCase));

                    if (!nextIsTable)
                        column.Item().Height(10);
                }
            });
        }

        // Your existing "normal row" logic, extracted so it can be reused inside any slot
        private void RenderItemsRow(RowDescriptor r, List<ReportItem> rowItems, int spanUnits, bool noBox = false)
        {
            int itemWidth = 0;

            foreach (var item in rowItems.OrderBy(x => x.position))
            {
                string type = item.type?.ToLowerInvariant();
                int w = Math.Max(1, item.width);

                if (type == "image")
                    r.RelativeItem(w).Image(item.datafield).FitArea();
                else if (type == "spacer")
                    r.RelativeItem(spanUnits).Height(item.width);
                else
                {
                    try { r.RelativeItem(w).Element(c => RenderItem(c, item, noBox)); }
                    catch (Exception)
                    {
                        r.RelativeItem(w).Text("NA: " + item.datafield)
                         .FontColor(Colors.Red.Darken1);
                    }
                }

                itemWidth += w;
            }

            if (itemWidth < spanUnits)
                r.RelativeItem(spanUnits - itemWidth);
        }

        private void RenderTableSlot(IContainer slot, ReportItem tableItem, int tableUnits = 0, int sideUnits = 0,
    List<ReportItem> headerSide = null, List<List<ReportItem>> bodySide = null)
        {
            var section = _layout.TableSections.FirstOrDefault(s =>
                s.TableName.Equals("transactiontable", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.TableMappingId, tableItem.datafield, StringComparison.OrdinalIgnoreCase));
            if (section == null) return;

            string tableKey = section.TableMappingId;
            if (!_detailTable.Tables.Contains(tableKey)) return;

            var tableData = _detailTable.Tables[tableKey];
            if (tableData.Rows.Count == 0) return;                   // this is also why HideIfEmpty is not needed
            if (!_layout.Tables.TryGetValue(tableKey, out var tableConfig)) return;

            if (string.Equals(section.RenderMode, "form", StringComparison.OrdinalIgnoreCase))
                slot.Element(x => RenderFormTable(x, tableData, tableConfig));
            else
                slot.Element(x => CreateTransactionTable(x, tableData, tableConfig,
                                                         tableUnits, sideUnits, headerSide, bodySide));
        }

        // One block per data row. The slot gets exactly ONE child (the Column); each data row is a new Item().
        private void RenderFormTable(IContainer slot, DataTable data, TableDefinition cfg)
        {
            var rowGroups = cfg.Columns
                .OrderBy(c => c.rowno).ThenBy(c => c.position)
                .GroupBy(c => c.rowno).OrderBy(g => g.Key)
                .Select(g => g.ToList()).ToList();

            slot.Column(col =>
            {
                foreach (DataRow dr in data.Rows)
                {
                    col.Item().ShowEntire().Column(block =>          // keep a holder/nominee block on one page
                    {
                        foreach (var cells in rowGroups)
                        {
                            var rowItems = cells.Select(c => ToReportItem(c, dr)).ToList();
                            block.Item().Row(r => RenderItemsRow(r, rowItems, totalChars));
                        }
                    });
                }
            });
        }

        private void RenderItem(IContainer container, ReportItem item, bool noBox = false)
        {
            switch (item.type?.ToLower())
            {
                case "text":
                    var box = noBox ? "" : (item.showboxline ?? "").Trim().ToUpperInvariant();
                    bool all = box.Contains('A');
                    const float line = 1f;

                    string showLabel = item.islablevisible ? (item.label ?? "").Replace("\\n", "\n") : "";
                    string value = (item.datafield ?? "").Replace("\\n", "\n");

                    IContainer cellBox = container;
                    if (!string.IsNullOrWhiteSpace(item.backcolor)) cellBox = cellBox.Background(item.backcolor);

                    cellBox.Layers(layers =>
                    {
                        var txt = layers.PrimaryLayer().Padding(2).Text(showLabel + value);

                        if (item.isbold) txt.Bold();
                        if (item.fontsize > 0) txt.FontSize(item.fontsize);

                        switch (item.align?.ToUpperInvariant())
                        {
                            case "C": txt.AlignCenter(); break;
                            case "R": txt.AlignRight(); break;
                            default: txt.AlignLeft(); break;
                        }

                        if (all || box.Contains('L')) layers.Layer().AlignLeft().LineVertical(line).LineColor(Colors.Black);
                        if (all || box.Contains('R')) layers.Layer().AlignRight().LineVertical(line).LineColor(Colors.Black);
                        if (all || box.Contains('T')) layers.Layer().AlignTop().LineHorizontal(line).LineColor(Colors.Black);
                        if (all || box.Contains('B')) layers.Layer().AlignBottom().LineHorizontal(line).LineColor(Colors.Black);
                    });
                    break;
            }
        }

        private static TextStyle ApplyHeaderStyle(TextStyle style, int? size, bool bold)
        {
            if (size.HasValue) style = style.FontSize(size.Value);
            return bold ? style.Bold() : style.NormalWeight();
        }

        private void CreateTransactionTable(IContainer container, DataTable dtlTable, TableDefinition tableConfig,
            int tableUnits = 0, int sideUnits = 0, List<ReportItem> headerSide = null, List<List<ReportItem>> bodySide = null)
        {
            System.Diagnostics.Debug.WriteLine($"tableUnits={tableUnits}, sideUnits={sideUnits}, group={tableConfig.GroupByColumn}");
            var columnsConfig = tableConfig.Columns.OrderBy(x => x.rowno).ToList();
            bool hasGroupCol = !string.IsNullOrWhiteSpace(tableConfig.GroupByColumn);

            var detailColumns = columnsConfig
                .Where(x => x.isvisible &&
                            (!hasGroupCol || !x.datafield.Equals(tableConfig.GroupByColumn, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            bool hasGrouping = hasGroupCol && dtlTable.Columns.Contains(tableConfig.GroupByColumn);
            bool hasSide = sideUnits > 0 && tableUnits > 0 && !hasGrouping;
            bool rowLines = !hasSide;                       // panel tables: column lines only, like your target

            if (hasSide) container = container.Border(1.5f);   // outer box

            container.Table(table =>
            {
                int detailWidth = detailColumns.Sum(x => x.width);
                float sideRel = hasSide ? (float)sideUnits * detailWidth / tableUnits : 0;

                table.ColumnsDefinition(columns =>
                {
                    foreach (var col in detailColumns) columns.RelativeColumn(col.width);
                    if (hasSide) columns.RelativeColumn(sideRel);
                });

                table.Header(header =>
                {
                    foreach (var row in tableConfig.HeaderRows)
                        foreach (var headerCell in row)
                        {
                            var cell = header.Cell();
                            if (headerCell.colspan > 1) cell = cell.ColumnSpan((uint)headerCell.colspan);
                            if (headerCell.rowspan > 1) cell = cell.RowSpan((uint)headerCell.rowspan);

                            int? size = headerCell.fontsize;
                            bool bold = headerCell.isbold ?? true;

                            string bg = string.IsNullOrWhiteSpace(headerCell.backcolor) ? "#E0E0E0" : headerCell.backcolor;  // #E0E0E0 = Grey.Lighten2

                            cell.Border(1).Background(bg).Padding(3).AlignCenter()
                                .DefaultTextStyle(x => ApplyHeaderStyle(x, size, bold))
                                .Text(headerCell.label);
                        }

                    // "Statement dated" sits in the header row, so it has the header's height
                    if (hasSide)
                        header.Cell().Border(1)
                              .Element(c => c.Row(r => RenderItemsRow(r, headerSide ?? new List<ReportItem>(), sideUnits, true)));
                });

                if (hasGrouping)
                {
                    foreach (var group in dtlTable.AsEnumerable()
                                 .GroupBy(r => r[tableConfig.GroupByColumn]?.ToString() ?? ""))
                    {
                        table.Cell().ColumnSpan((uint)detailColumns.Count).Border(1)
                             .Background(Colors.Blue.Lighten4).Padding(4)
                             .Text($"{tableConfig.GroupByColumn}: {group.Key}").Bold();

                        foreach (var row in group) RenderDetailRow(table, row, detailColumns);
                        if (tableConfig.ShowGroupTotal) RenderGroupTotal(table, group, detailColumns);
                    }
                }
                else
                {
                    int n = dtlTable.Rows.Count;
                    for (int k = 0; k < n; k++)
                    {
                        RenderDetailRow(table, dtlTable.Rows[k], detailColumns, rowLines);

                        // one cell spanning all data rows, however many there are
                        if (hasSide && k == 0)
                        {
                            table.Cell().RowSpan((uint)n).Border(1).Padding(2).Column(col =>
                            {
                                foreach (var rowItems in bodySide ?? new List<List<ReportItem>>())
                                    col.Item().Row(r => RenderItemsRow(r, rowItems, sideUnits, true));
                            });
                        }
                    }
                }

                var allRows = dtlTable.AsEnumerable();
                bool hasTotal = detailColumns.Any(x => x.showtotal);
                if (hasTotal)
                {
                    foreach (var col in detailColumns)
                    {
                        var cell = table.Cell()
                            .Border(1)
                            .Background(Colors.Grey.Lighten3)
                            .Padding(3);

                        if (col == detailColumns.First())
                        {
                            cell.Text("Grand Total").Bold();
                        }
                        else if (col.showtotal)
                        {
                            decimal total = allRows.Sum(r =>
                            {
                                decimal.TryParse(
                                    r[col.datafield]?.ToString(),
                                    out decimal value);

                                return value;
                            });

                            cell.AlignRight()
                                .Text(total.ToString("N2"))
                                .Bold();
                        }
                        else
                        {
                            cell.Text("");
                        }
                    }
                }
            });

        }

        private void RenderDetailRow(TableDescriptor table, DataRow row, List<TableColumnConfig> detailColumns, bool rowLines = true)
        {
            foreach (var col in detailColumns)
            {
                string value = !string.IsNullOrEmpty(col.datafield) && row.Table.Columns.Contains(col.datafield)
                    ? (row[col.datafield]?.ToString() ?? "")
                    : "";

                // the ONLY table.Cell() call in this loop
                IContainer boxed = rowLines ? table.Cell().Border(1) : table.Cell().BorderVertical(1);
                if (!string.IsNullOrWhiteSpace(col.backcolor)) boxed = boxed.Background(col.backcolor);
                var padded = boxed.Padding(3);

                IContainer aligned;
                switch (col.align?.ToUpper())
                {
                    case "R": aligned = padded.AlignRight(); break;
                    case "C": aligned = padded.AlignCenter(); break;
                    default: aligned = padded.AlignLeft(); break;
                }

                aligned.Text(text =>
                {
                    var span = text.Span(value);
                    if (col.fontsize > 0) span.FontSize(col.fontsize);
                    if (col.isbold) span.Bold();
                });
            }
        }


        private void RenderGroupTotal(TableDescriptor table, IGrouping<string, DataRow> group, List<TableColumnConfig> detailColumns)
        {
            foreach (var col in detailColumns)
            {
                var cell = table.Cell()
                    .Border(1)
                    .Padding(3);

                if (col == detailColumns.First())
                {
                    cell.Text("Group Total").Bold();
                }
                else if (col.showtotal)
                {
                    decimal total = group.Sum(r =>
                    {
                        decimal.TryParse(
                            r[col.datafield]?.ToString(),
                            out decimal value);

                        return value;
                    });

                    cell.AlignRight()
                        .Text(total.ToString("N2"))
                        .Bold();
                }
                else
                {
                    cell.Text("");
                }
            }
        }


        private static double NFloat(JObject obj, string key, double defaultValue = 0) =>
                    double.TryParse(GetValueIgnoreCase(obj, key)?.ToString(), out var value)
                        ? value
                        : defaultValue;

        private static string SString(JObject obj, string key, string defaultValue = "") =>
                         GetValueIgnoreCase(obj, key)?.ToString() ?? defaultValue;

        private static readonly Regex TokenRx = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        private static string CellValue(DataRow row, string field)
        {
            if (string.IsNullOrWhiteSpace(field) || !row.Table.Columns.Contains(field)) return "";
            var v = row[field];
            return v == DBNull.Value ? "" : Convert.ToString(v);
        }

        // '{NomType} Name' -> 'Nominee Name'; '{CorrAdd1} {CorrAdd2}' -> joined address
        private static string ExpandTokens(string text, DataRow row) =>
            string.IsNullOrEmpty(text) || text.IndexOf('{') < 0
                ? text
                : TokenRx.Replace(text, m => (CellValue(row, m.Groups[1].Value) ?? "").Trim());

        private ReportItem ToReportItem(TableColumnConfig x, DataRow row) => new ReportItem
        {
            type = x.type,
            label = ExpandTokens(x.label, row),
            datafield = string.Equals(x.type, "text", StringComparison.OrdinalIgnoreCase)
                            ? CellValue(row, x.datafield) : x.datafield,
            position = x.position,
            rowno = x.rowno,
            width = x.width,
            align = x.align,
            showboxline = x.showboxline,
            fontsize = x.fontsize,
            isbold = x.isbold,
            islablevisible = x.islablevisible,
            isvaluevisible = x.isvaluevisible,
            backcolor = x.backcolor
        };


    }

    public class ReportItem
    {
        public string type { get; set; }
        public string label { get; set; }
        public string datafield { get; set; }
        public int position { get; set; }   // 1-12
        public int rowno { get; set; }
        public int width { get; set; }      // 1-12     
        public string align { get; set; }
        public string showboxline { get; set; }
        public int fontsize { get; set; }
        public bool isvaluevisible { get; set; }
        public bool islablevisible { get; set; }
        public bool isbold { get; set; }
        public string backcolor { get; set; }
    }

    public class TableColumnConfig
    {
        public string datafield { get; set; }
        public int width { get; set; }
        public string align { get; set; }
        public bool isvisible { get; set; }
        public bool showtotal { get; set; }
        public int rowno { get; set; }
        public string type { get; set; }
        public string label { get; set; }
        public int position { get; set; }   // 1-12
        public string showboxline { get; set; }
        public int fontsize { get; set; }
        public bool isvaluevisible { get; set; }
        public bool islablevisible { get; set; }
        public bool isbold { get; set; }
        public string backcolor { get; set; }
    }

    public class HeaderCell
    {
        public string label { get; set; }
        public int colspan { get; set; } = 1;
        public int rowspan { get; set; } = 1;
        public int? fontsize { get; set; }
        public bool? isbold { get; set; }
        public string backcolor { get; set; }
    }

    public class TableDefinition
    {
        public List<List<HeaderCell>> HeaderRows { get; set; }
        public List<TableColumnConfig> Columns { get; set; }

        public string GroupByColumn { get; set; }
        public bool ShowGroupTotal { get; set; }
        public int? headerfontsize { get; set; }
        public string headerbold { get; set; }
    }

    public class TableSection
    {
        public string TableName { get; set; }
        public string TableMappingId { get; set; }
        public string DataSource { get; set; }
        public string UpdateQuery { get; set; }
        public bool ShowHeader { get; set; }
        public string RenderMode { get; set; }
    }
}