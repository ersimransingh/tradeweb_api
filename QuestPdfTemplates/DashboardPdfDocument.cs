using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeWeb.API.Models;

namespace TradeWeb.API.QuestPdfTemplates
{
    public class DashboardPdfDocument : IDocument
    {
        // tradewebx default theme (ThemeContext.tsx) - kept in sync so the PDF matches the
        // on-screen dashboard's actual colors instead of arbitrary defaults.
        private static readonly Color ThemeText = Color.FromHex("#121212");
        private static readonly Color ThemePrimary = Color.FromHex("#5B627E");
        private static readonly Color ThemeBorder = Color.FromHex("#BED0C4");       // color3
        private static readonly Color ThemeCardBackground = Colors.White;

        // A4 landscape width (841.89pt) minus the page's own Margin(20) on both sides
        // minus the tab card's Padding(12) on both sides (measured available width in
        // practice: ~778pt), with extra safety margin beyond that. Column *weights* are
        // computed against this reference budget purely so a many/wide-column table
        // still gets every column above its own padding floor - RelativeColumn then
        // self-corrects to whatever the real container width turns out to be, so a
        // mismatch between this constant and reality just shifts proportions slightly
        // rather than causing a crash.
        private const float TabContentWidth = 750f;

        private static readonly Dictionary<string, string> NamedColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "black", "#000000" }, { "white", "#FFFFFF" }, { "green", "#16A34A" },
            { "red", "#DC2626" }, { "blue", "#2563EB" }, { "orange", "#EA580C" },
            { "gray", "#6B7280" }, { "grey", "#6B7280" }, { "purple", "#9333EA" }
        };

        private readonly DashboardPdfRequest _req;
        private readonly byte[] _logoBytes;

        public DashboardPdfDocument(DashboardPdfRequest request)
        {
            _req = request;

            if (!string.IsNullOrWhiteSpace(_req.CompanyLogoBase64))
            {
                try
                {
                    var base64 = _req.CompanyLogoBase64;
                    var commaIndex = base64.IndexOf(",", StringComparison.Ordinal);
                    if (base64.StartsWith("data:") && commaIndex >= 0)
                        base64 = base64.Substring(commaIndex + 1);

                    _logoBytes = Convert.FromBase64String(base64);
                }
                catch
                {
                    _logoBytes = null;
                }
            }
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily(FontSetup.FontFamily).FontSize(8));

                page.Header().Element(ComposeHeader);
                page.Content().PaddingTop(8).Element(ComposeContent);
                page.Footer().PaddingTop(4).Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.ConstantItem(120).Height(40).Element(logo =>
                {
                    if (_logoBytes != null)
                        logo.Image(_logoBytes).FitArea();
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Text(_req.CompanyName ?? "").Bold().FontSize(14).Underline();
                    col.Item().AlignCenter().Text($"{_req.Title} {_req.DateRange}".Trim()).FontSize(10);
                });

                row.ConstantItem(120);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // 1. Widgets (KPI cards) first.
                var cardWidgets = _req.Widgets
                    .Where(w => string.Equals(w.Type, "gridWidget", StringComparison.OrdinalIgnoreCase)
                                && w.Settings != null && w.Settings.ShowInPdf)
                    .ToList();

                if (cardWidgets.Count > 0)
                    col.Item().Element(c => ComposeCardGrid(c, cardWidgets));

                var tableWidgets = _req.Widgets
                    .Where(w => string.Equals(w.Type, "tableWithGraph", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // 2. Then every visible tab's table and chart(s) side by side. The first
                // tab continues right after the widgets on the same page; each tab after
                // that starts on its own new page (not every tab crowded onto one page).
                bool tabRendered = false;
                foreach (var widget in tableWidgets)
                {
                    // Opt-in: a tab only renders in the PDF when Setting.ShowInPdf is
                    // explicitly true - not merely "not false" - so tabs the caller never
                    // marked for PDF export stay out by default.
                    var visibleTabs = (widget.GridTabs ?? new List<GridTab>())
                        .Where(t => t.Setting != null && t.Setting.ShowInPdf == true)
                        .ToList();

                    if (visibleTabs.Count == 0)
                        continue;

                    foreach (var tab in visibleTabs)
                    {
                        // A little breathing room only where the first tab sits directly
                        // under the KPI cards on the same page; a fresh page needs none.
                        float topPadding = !tabRendered && cardWidgets.Count > 0 ? 10 : 0;

                        if (tabRendered)
                            col.Item().PageBreak();
                        tabRendered = true;

                        col.Item().PaddingTop(topPadding).Element(c => ComposeTabSection(c, tab));
                    }
                }
            });
        }

        // ---- KPI cards ----

        private void ComposeCardGrid(IContainer container, List<DashboardWidget> widgets)
        {
            container.Column(col =>
            {
                int used = 0;
                var currentRow = new List<DashboardWidget>();

                void Flush()
                {
                    if (currentRow.Count == 0) return;
                    var rowItems = currentRow.ToList();
                    col.Item().PaddingBottom(8).Row(row =>
                    {
                        row.Spacing(6, Unit.Point);
                        foreach (var w in rowItems)
                            row.RelativeItem(Math.Max(w.Column, 1)).Element(c => ComposeCard(c, w));
                    });
                    currentRow.Clear();
                    used = 0;
                }

                foreach (var w in widgets)
                {
                    int span = Math.Max(w.Column, 1);
                    if (used + span > 12 && used > 0)
                        Flush();

                    currentRow.Add(w);
                    used += span;
                }

                Flush();
            });
        }

        // KPI card font sizes are halved from the widget's requested sizes (and from the
        // previous fixed title size) to suit six-per-row print cards, per explicit request.
        private const float CardFontScale = 0.5f;

        private void ComposeCard(IContainer container, DashboardWidget widget)
        {
            var settings = widget.Settings ?? new WidgetSettings();
            var borderColor = string.IsNullOrWhiteSpace(settings.BorderColor) ? ThemePrimary : ParseColor(settings.BorderColor);
            var borderType = (settings.BorderType ?? "").Trim().ToLowerInvariant();
            float borderWidthPx = ParsePx(settings.BorderWidth, 4);
            const float cornerRadius = 6f;

            void RenderCardBody(IContainer body)
            {
                body.Padding(8).Column(col =>
                {
                    var titleContainer = col.Item();
                    var titleAligned = string.Equals(settings.TitleAlign, "center", StringComparison.OrdinalIgnoreCase) ? titleContainer.AlignCenter()
                        : string.Equals(settings.TitleAlign, "right", StringComparison.OrdinalIgnoreCase) ? titleContainer.AlignRight()
                        : titleContainer.AlignLeft();
                    titleAligned.Text(widget.Title ?? "").Bold().FontSize(11 * CardFontScale)
                        .FontColor(string.IsNullOrWhiteSpace(settings.TitleColor) ? ThemeText : ParseColor(settings.TitleColor));

                    foreach (var item in widget.GridItems ?? new List<GridItem>())
                    {
                        col.Item().PaddingTop(3).Row(itemRow =>
                        {
                            itemRow.RelativeItem(2).Text(item.Label?.Text ?? "")
                                .FontSize((float)(item.Label?.Size ?? 9) * CardFontScale)
                                .FontColor(ParseColor(item.Label?.Color, ThemeText));

                            string valueText = item.Value?.Text ?? "";
                            itemRow.RelativeItem(3).ScaleToFit().AlignRight().Text(valueText)
                                .Bold()
                                .FontSize(FitValueFontSize(valueText, (float)(item.Value?.Size ?? 12) * CardFontScale))
                                .FontColor(ParseColor(item.Value?.Color, ThemeText))
                                .WrapAnywhere(false);
                        });
                    }
                });
            }

            // Rounded corners + a subtle drop shadow, matching the web card's
            // "rounded-lg shadow-md" styling instead of the previous flat rectangle.
            var styledCard = container
                .Background(ThemeCardBackground)
                .CornerRadius(cornerRadius, Unit.Point)
                .Shadow(new BoxShadowStyle { OffsetX = 0, OffsetY = 1, Blur = 3, Spread = 0, Color = Colors.Grey.Lighten1 });

            if (borderType == "outline")
            {
                styledCard.Border(borderWidthPx).BorderColor(borderColor).Element(RenderCardBody);
            }
            else if (borderType == "left")
            {
                styledCard.Row(row =>
                {
                    row.ConstantItem(borderWidthPx).Background(borderColor);
                    row.RelativeItem().Element(RenderCardBody);
                });
            }
            else
            {
                styledCard.Element(RenderCardBody);
            }
        }

        // ---- Report tabs (table and its charts side by side) ----

        private void ComposeTabSection(IContainer container, GridTab tab)
        {
            // Card framing (rounded corners, border, subtle shadow) matches the KPI
            // cards' look so each report reads as a distinct, polished block.
            container
                .Background(ThemeCardBackground)
                .Border(1)
                .BorderColor(ThemeBorder)
                .CornerRadius(8, Unit.Point)
                .Shadow(new BoxShadowStyle { OffsetX = 0, OffsetY = 1, Blur = 3, Spread = 0, Color = Colors.Grey.Lighten2 })
                .Padding(12)
                .Column(col =>
            {
                col.Item().Text(tab.TabName ?? "").Bold().FontSize(12).FontColor(ThemePrimary);
                col.Item().PaddingTop(4).PaddingBottom(8).Height(1.5f).Background(ThemePrimary);

                var view = TabView.Build(tab);
                if (view.Columns.Count == 0)
                {
                    col.Item().PaddingTop(3).Text("No data available").FontSize(8).FontColor(Colors.Grey.Medium);
                    return;
                }

                // Table always gets the tab's full width, whether or not it has charts.
                col.Item().Element(c => ComposeReportTable(c, view, TabContentWidth));

                var charts = tab.Setting?.PdfCharts ?? new List<PdfChartConfig>();
                if (charts.Count == 0)
                    return;

                var rows = tab.Rows ?? new List<Dictionary<string, JsonElement>>();

                // Charts sit on their own line(s) below the table (not beside it) - a wide
                // table sitting next to a short chart stack left a large dead gap where the
                // table's cell had already finished but the row stayed tall to match the
                // charts. The grid is always 3 slots wide - every card is sized as if the
                // row were full (TabContentWidth / 3), even when fewer than 3 charts are
                // present, so a lone chart takes a single 1/3-width slot and leaves the
                // rest of the row blank instead of stretching to fill it. A 4th+ chart
                // wraps to another row of up to 3.
                const float chartGap = 12f;
                const float chartInset = 6f;
                const int chartsPerRow = 3;
                float chartCardWidth = (TabContentWidth - (chartsPerRow - 1) * chartGap) / chartsPerRow;

                // The SVG must be generated at exactly the size QuestPDF hands it after
                // padding is subtracted (Border() draws on the edge without consuming extra
                // layout space) - its Svg element fits to width and then requires the
                // matching height, so a mismatched container height can never be satisfied
                // (throws a layout error).
                float chartSvgWidth = chartCardWidth - 2 * chartInset;
                float chartSvgHeight = chartSvgWidth * 180f / 260f; // ChartSvgBuilder's baseline aspect ratio

                for (int i = 0; i < charts.Count; i += chartsPerRow)
                {
                    var rowCharts = charts.Skip(i).Take(chartsPerRow).ToList();

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        for (int j = 0; j < rowCharts.Count; j++)
                        {
                            if (j > 0)
                                row.ConstantItem(chartGap);

                            row.ConstantItem(chartCardWidth)
                                .Background(ThemeCardBackground).Border(1).BorderColor(ThemeBorder).CornerRadius(6, Unit.Point)
                                .Padding(chartInset).Height(chartSvgHeight).Element(c =>
                            {
                                var svg = BuildChartSvg(rowCharts[j], rows, view, chartSvgWidth, chartSvgHeight);
                                if (svg != null)
                                    c.Svg(svg);
                            });
                        }
                    });
                }
            });
        }

        private void ComposeReportTable(IContainer container, TabView view, float referenceBudget)
        {
            bool grouped = view.GroupCol != null && view.Rows.Count > 0;
            var displayRows = BuildDisplayRows(view, grouped);

            // Weights (not absolute widths) sized to each column's own content, so the
            // table always stretches to fill its full available width - like the web
            // page's table - while a short column (e.g. a two-word branch name) still
            // gets a smaller share than a long one instead of splitting evenly.
            var columnWeights = ComputeColumnWidths(view, displayRows, grouped, referenceBudget);

            container.Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    foreach (var w in columnWeights)
                        cd.RelativeColumn(w);
                });

                table.Header(header =>
                {
                    foreach (var colName in view.Columns)
                    {
                        var cell = header.Cell().Border(1).BorderColor(ThemeBorder).PaddingVertical(3).PaddingHorizontal(5);
                        var text = view.IsRightAligned(colName) ? cell.AlignRight() : cell.AlignLeft();
                        text.Text(view.HideLabels.Contains(colName) ? "" : PrettifyKey(colName))
                            .Bold().FontSize(8).FontColor(ThemeText).ClampLines(2);
                    }
                    if (grouped)
                        header.Cell().Border(1).BorderColor(ThemeBorder).PaddingVertical(3).PaddingHorizontal(5)
                            .AlignRight().Text("Count").Bold().FontSize(8).FontColor(ThemeText).ClampLines(2);
                });

                if (displayRows.Count == 0)
                {
                    table.Cell().ColumnSpan((uint)(view.Columns.Count + (grouped ? 1 : 0))).Padding(10).AlignCenter()
                        .Text("No data available").FontSize(8).FontColor(Colors.Grey.Medium);
                    return;
                }

                foreach (var dRow in displayRows)
                {
                    bool bold = false;
                    switch (dRow.Kind)
                    {
                        case DisplayRowKind.GrandTotal:
                        case DisplayRowKind.Total:
                            bold = true; break;
                    }

                    for (int i = 0; i < dRow.Cells.Count; i++)
                    {
                        bool isCountCell = grouped && i == dRow.Cells.Count - 1;
                        string colName = isCountCell ? null : view.Columns[i];

                        var cell = table.Cell().Border(1).BorderColor(ThemeBorder).PaddingVertical(2).PaddingHorizontal(5);

                        bool right = isCountCell || (colName != null && view.IsRightAligned(colName));
                        var text = right ? cell.AlignRight() : cell.AlignLeft();

                        Color color = ThemeText;
                        if (dRow.CellColors != null && dRow.CellColors.TryGetValue(i, out var cc))
                            color = cc;

                        var descriptor = text.Text(dRow.Cells[i]).FontSize(8).FontColor(color).ClampLines(2);
                        if (bold) descriptor.Bold();
                    }
                }
            });
        }

        // Per-column relative weights sized to the widest content (header label vs. every
        // display row's cell), clamped to a sane range. An explicit Setting.ColumnWidth
        // entry wins outright as its own weight. Used as RelativeColumn weights rather
        // than absolute widths, so the table always stretches to fill its full available
        // width - like the web page's table - while a short column like Branch Name still
        // gets less of that width than a long one instead of splitting evenly.
        //
        // RelativeColumn guarantees the *total* width always fits its container, but not
        // that every *individual* column does: with many columns and skewed weights, a
        // small-weight column can still be handed fewer pixels than its own 12pt cell
        // padding needs, which QuestPDF can't render ("conflicting size constraints") -
        // the exact same failure ConstantColumn had. So weights are computed against
        // referenceBudget (the page geometry this tab is expected to render into) via the
        // same floor-then-distribute allocation ConstantColumn used: every column first
        // gets a floor comfortably above its padding, then remaining budget is handed out
        // proportionally. If the real container width differs slightly from
        // referenceBudget, RelativeColumn just re-normalizes the same ratios to fit -
        // it can't overflow either way.
        private List<float> ComputeColumnWidths(TabView view, List<DisplayRow> displayRows, bool grouped, float referenceBudget)
        {
            const float minWidth = 50f;
            const float maxWidth = 220f;
            const float horizontalPadding = 12f; // 6pt each side, matches the cell Padding below
            const float safetyBuffer = 6f; // estimate is approximate; err wide, not narrow

            var widths = new List<float>();
            for (int i = 0; i < view.Columns.Count; i++)
            {
                var col = view.Columns[i];
                if (view.ColWidths.TryGetValue(col, out var configured) && configured > 0)
                {
                    widths.Add((float)configured);
                    continue;
                }

                string headerLabel = view.HideLabels.Contains(col) ? "" : PrettifyKey(col);
                float w = EstimateTextWidth(headerLabel, 8f, bold: true);
                foreach (var row in displayRows)
                {
                    if (i < row.Cells.Count)
                        w = Math.Max(w, EstimateTextWidth(row.Cells[i], 8f, row.Kind != DisplayRowKind.Data));
                }

                widths.Add(Math.Clamp(w + horizontalPadding + safetyBuffer, minWidth, maxWidth));
            }

            if (grouped)
            {
                float w = EstimateTextWidth("Count", 8f, bold: true);
                foreach (var row in displayRows)
                {
                    var cell = row.Cells.Count > 0 ? row.Cells[row.Cells.Count - 1] : "";
                    w = Math.Max(w, EstimateTextWidth(cell, 8f, bold: true));
                }
                widths.Add(Math.Clamp(w + horizontalPadding + safetyBuffer, minWidth, 90f));
            }

            float total = widths.Sum();
            if (total > referenceBudget && total > 0)
            {
                const float columnFloor = 20f; // > 12pt cell padding, leaves a sliver of content room
                int n = widths.Count;

                if (n * columnFloor >= referenceBudget)
                {
                    float equalShare = referenceBudget / n;
                    for (int i = 0; i < n; i++)
                        widths[i] = equalShare;
                }
                else
                {
                    float remaining = referenceBudget - n * columnFloor;
                    float extraDemandTotal = widths.Sum(w => Math.Max(w - columnFloor, 0f));
                    for (int i = 0; i < n; i++)
                    {
                        float extraDemand = Math.Max(widths[i] - columnFloor, 0f);
                        float extraShare = extraDemandTotal > 0
                            ? remaining * (extraDemand / extraDemandTotal)
                            : remaining / n;
                        widths[i] = columnFloor + extraShare;
                    }
                }
            }

            return widths;
        }

        // Rough proportional-font glyph-width estimate (no text-measurement API is
        // available here) - bold/uppercase runs skew wider, so they get a higher factor.
        // Deliberately generous: an overestimate just leaves a little extra cell padding,
        // an underestimate wraps/clips text, so err on the wide side.
        private static float EstimateTextWidth(string text, float fontSize, bool bold)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            float factor = bold ? 0.72f : 0.6f;
            return text.Length * fontSize * factor;
        }

        private enum DisplayRowKind { Data, GrandTotal, Total }

        private sealed class DisplayRow
        {
            public DisplayRowKind Kind;
            public List<string> Cells;
            public Dictionary<int, Color> CellColors;
        }

        // Row structure mirrors the page's DEFAULT "Group & Sum" view (DashBoardNew's
        // groupedData with no Sub column picked): ungrouped = raw rows + "Total (N)";
        // grouped = one collapsed row per group value (sum of every numeric column +
        // a row count), then a "Grand Total" row. The page's per-row raw-data drilldown
        // (its expand arrow / chevron) is deliberately not reproduced in the PDF.
        private List<DisplayRow> BuildDisplayRows(TabView view, bool grouped)
        {
            var result = new List<DisplayRow>();

            if (!grouped)
            {
                var rows = view.Rows;
                if (view.SortCol != null)
                {
                    bool numeric = view.Numeric.TryGetValue(view.SortCol, out var isNumeric) && isNumeric;
                    rows = numeric
                        ? rows.OrderBy(r => ToDouble(r.TryGetValue(view.SortCol, out var v) ? v : (JsonElement?)null)).ToList()
                        : rows.OrderBy(r => ExtractRaw(r, view.SortCol), StringComparer.OrdinalIgnoreCase).ToList();
                    if (view.SortDescending)
                        rows.Reverse();
                }

                foreach (var row in rows)
                {
                    var cells = new List<string>();
                    Dictionary<int, Color> colors = null;
                    for (int i = 0; i < view.Columns.Count; i++)
                    {
                        var col = view.Columns[i];
                        row.TryGetValue(col, out var raw);
                        cells.Add(FormatTabCell(view, col, raw));

                        var color = ValueColor(view, col, raw);
                        if (color.HasValue)
                        {
                            colors = colors ?? new Dictionary<int, Color>();
                            colors[i] = color.Value;
                        }
                    }
                    result.Add(new DisplayRow { Kind = DisplayRowKind.Data, Cells = cells, CellColors = colors });
                }

                if (view.SumCols.Count > 0 && view.Rows.Count > 0)
                {
                    var cells = new List<string>();
                    for (int i = 0; i < view.Columns.Count; i++)
                    {
                        var col = view.Columns[i];
                        if (i == 0)
                            cells.Add($"Total ({view.Rows.Count})");
                        else if (view.Numeric[col])
                            cells.Add(FormatSum(view, col, view.Rows.Sum(r => ToDouble(r.TryGetValue(col, out var v) ? v : (JsonElement?)null))));
                        else
                            cells.Add("");
                    }
                    result.Add(new DisplayRow { Kind = DisplayRowKind.Total, Cells = cells });
                }

                return result;
            }

            // Bucket rows by the group column's value. TabView.Build already trimmed
            // view.Columns down to [GroupCol, ...numeric columns] for a grouped tab, so
            // every non-group column here is a sum column.
            var buckets = new Dictionary<string, (Dictionary<string, double> Sums, int Count)>(StringComparer.Ordinal);
            var order = new List<string>();
            foreach (var row in view.Rows)
            {
                string key = ExtractRaw(row, view.GroupCol).Trim();
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = (view.Columns.Where(c => c != view.GroupCol).ToDictionary(c => c, c => 0.0), 0);
                    order.Add(key);
                }
                foreach (var col in bucket.Sums.Keys.ToList())
                    bucket.Sums[col] += ToDouble(row.TryGetValue(col, out var v) ? v : (JsonElement?)null);
                buckets[key] = (bucket.Sums, bucket.Count + 1);
            }

            // Default (no column explicitly sorted) order: alphabetical by group value,
            // same as the page's compareOnCols fallback. When PdfSortBy names a sum
            // column, order groups by that column's summed value instead.
            if (view.SortCol != null && view.SortCol != view.GroupCol
                && order.Count > 0 && buckets[order[0]].Sums.ContainsKey(view.SortCol))
                order = order.OrderBy(k => buckets[k].Sums[view.SortCol]).ToList();
            else
                order = order.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

            if (view.SortDescending)
                order.Reverse();

            var grandSums = view.Columns.Where(c => c != view.GroupCol).ToDictionary(c => c, c => 0.0);
            int grandCount = 0;

            foreach (var key in order)
            {
                var bucket = buckets[key];
                string label = key.Length > 0 ? key : "(blank)";

                var cells = new List<string>();
                for (int i = 0; i < view.Columns.Count; i++)
                {
                    var col = view.Columns[i];
                    cells.Add(col == view.GroupCol ? label : FormatSum(view, col, bucket.Sums[col]));
                }
                cells.Add(bucket.Count.ToString(CultureInfo.InvariantCulture));
                result.Add(new DisplayRow { Kind = DisplayRowKind.Data, Cells = cells });

                foreach (var col in bucket.Sums.Keys)
                    grandSums[col] += bucket.Sums[col];
                grandCount += bucket.Count;
            }

            var grandCells = new List<string>();
            for (int i = 0; i < view.Columns.Count; i++)
            {
                var col = view.Columns[i];
                if (i == 0)
                    grandCells.Add("Grand Total");
                else if (col == view.GroupCol)
                    grandCells.Add("");
                else
                    grandCells.Add(FormatSum(view, col, grandSums.TryGetValue(col, out var g) ? g : 0));
            }
            grandCells.Add(grandCount.ToString(CultureInfo.InvariantCulture));
            result.Add(new DisplayRow { Kind = DisplayRowKind.GrandTotal, Cells = grandCells });

            return result;
        }

        private Color? ValueColor(TabView view, string col, JsonElement raw)
        {
            if (!view.TextColors.TryGetValue(col, out var cfg))
                return null;
            if (!TryParseNumber(RawString(raw), out var num))
                return null;

            string colorValue = num < cfg.CheckNumber ? cfg.LessThanColor
                : num > cfg.CheckNumber ? cfg.GreaterThanColor
                : cfg.EqualToColor;

            if (string.IsNullOrWhiteSpace(colorValue))
                return null;
            return ParseColor(colorValue);
        }

        private string FormatSum(TabView view, string col, double sum)
        {
            return IndianNumberFormat.Format(sum, view.Decimals.TryGetValue(col, out var dp) ? dp : (int?)null);
        }

        private string FormatTabCell(TabView view, string col, JsonElement raw)
        {
            string str = RawString(raw);
            if (str.Length == 0)
                return "";

            if (view.DateFormats.TryGetValue(col, out var fmt))
            {
                var reformatted = TryFormatDate(str, fmt);
                if (reformatted != null)
                    return reformatted;
            }

            if (view.Numeric.TryGetValue(col, out var numeric) && numeric
                && TryParseNumber(str, out var num))
            {
                return IndianNumberFormat.Format(num, view.Decimals.TryGetValue(col, out var dp) ? dp : (int?)null);
            }

            return str;
        }

        // Analysis of one tab's rows, mirroring the DashBoardNew page logic
        // (src/apppages/DashBoardNew/index.tsx): columns = union of row keys in
        // first-seen order minus hideEntireColumn; a column is numeric when every
        // sampled non-empty value parses as a number; the group column defaults to
        // Setting.GroupBy (loose match) then any "branch name"-like text column.
        private sealed class TabView
        {
            public List<Dictionary<string, JsonElement>> Rows;
            public List<string> Columns = new List<string>();
            // Every column seen in the raw row data (minus HideEntireColumn), regardless
            // of which subset Columns ends up displaying - charts may reference a
            // dimension (e.g. "Month") that isn't part of the table's own display columns.
            public List<string> AllColumns = new List<string>();
            public Dictionary<string, bool> Numeric = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            public List<string> SumCols = new List<string>();
            public HashSet<string> LeftAligned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> HideLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Decimals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> DateFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, double> ColWidths = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, ValueColorConfig> TextColors = new Dictionary<string, ValueColorConfig>(StringComparer.OrdinalIgnoreCase);
            public string GroupCol;
            public string SortCol;
            public bool SortDescending;

            public bool IsRightAligned(string col) =>
                Numeric.TryGetValue(col, out var n) && n && !LeftAligned.Contains(col);

            public static TabView Build(GridTab tab)
            {
                var view = new TabView();
                var setting = tab.Setting ?? new GridTabSetting();
                var rows = tab.Rows ?? new List<Dictionary<string, JsonElement>>();
                view.Rows = rows;

                var hideCols = new HashSet<string>(SplitCsv(setting.HideEntireColumn), StringComparer.OrdinalIgnoreCase);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in rows)
                    foreach (var key in row.Keys)
                        if (seen.Add(key) && !hideCols.Contains(key))
                            view.AllColumns.Add(key);

                view.HideLabels = new HashSet<string>(SplitCsv(setting.HideColumnLabels), StringComparer.OrdinalIgnoreCase);
                view.LeftAligned = new HashSet<string>(
                    SplitCsv(setting.LeftAlignedColums).Concat(SplitCsv(setting.LeftAlignedColumns)),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var cfg in setting.DecimalColumns ?? new List<DecimalColumnConfig>())
                    foreach (var key in SplitCsv(cfg.Key))
                        view.Decimals[key] = cfg.DecimalPlaces;

                foreach (var cfg in setting.ColumnWidth ?? new List<ColumnWidthConfig>())
                    foreach (var key in SplitCsv(cfg.Key))
                        if (cfg.Width > 0)
                            view.ColWidths[key] = cfg.Width;

                foreach (var cfg in setting.ValueBasedTextColor ?? new List<ValueColorConfig>())
                    foreach (var key in SplitCsv(cfg.Key))
                        view.TextColors[key] = cfg;

                foreach (var cfg in setting.DateFormat ?? new List<DateFormatConfig>())
                    foreach (var key in SplitCsv(cfg.Key))
                        if (!string.IsNullOrWhiteSpace(cfg.Format))
                            view.DateFormats[key] = cfg.Format;

                foreach (var col in view.AllColumns)
                {
                    bool numeric = true;
                    foreach (var row in rows.Take(500))
                    {
                        if (!IsNumericValue(row.TryGetValue(col, out var v) ? v : (JsonElement?)null))
                        {
                            numeric = false;
                            break;
                        }
                    }
                    view.Numeric[col] = numeric;
                }

                string Norm(string s) => new string((s ?? "").Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToLowerInvariant();
                string ResolveRaw(string name) =>
                    string.IsNullOrWhiteSpace(name) ? null : view.AllColumns.FirstOrDefault(c => Norm(c) == Norm(name));

                // Explicit PDF overrides (pdfGroupBy / pdfSumColumns / pdfColumns) win
                // outright over the auto-detected shape below - the caller wants a fixed
                // report shape, independent of the web page's live Group-By dropdown state.
                var pdfGroupBy = (setting.PdfGroupBy ?? "").Trim();
                var explicitColumns = SplitCsv(setting.PdfColumns).Select(ResolveRaw).Where(c => c != null).ToList();
                var explicitSumColumns = SplitCsv(setting.PdfSumColumns).Select(ResolveRaw).Where(c => c != null).ToList();

                // PDF-only sort override: which column drives row/group order, and its
                // direction. Resolved the same loose way as pdfGroupBy/pdfColumns - the
                // configured name just needs to match a raw column, case/whitespace-insensitive.
                var pdfSortBy = (setting.PdfSortBy ?? "").Trim();
                if (pdfSortBy.Length > 0)
                    view.SortCol = ResolveRaw(pdfSortBy);
                view.SortDescending = string.Equals((setting.PdfSortOrder ?? "").Trim(), "desc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals((setting.PdfSortOrder ?? "").Trim(), "descending", StringComparison.OrdinalIgnoreCase);

                if (pdfGroupBy.Length > 0)
                    view.GroupCol = ResolveRaw(pdfGroupBy);

                if (view.GroupCol == null)
                {
                    var textCols = view.AllColumns.Where(c => !view.Numeric[c]).ToList();

                    // The PDF always groups by Branch Name when the data has one, regardless
                    // of Setting.GroupBy - that field mirrors the web page's live "Group By"
                    // dropdown selection, not a fixed report shape, and the PDF needs
                    // Branch-wise data every time. Setting.GroupBy is only a fallback for
                    // tabs with no branch column.
                    view.GroupCol = textCols.FirstOrDefault(c => Regex.IsMatch(c, @"branch\s*name", RegexOptions.IgnoreCase));
                    if (view.GroupCol == null)
                    {
                        var groupBy = (setting.GroupBy ?? "").Trim();
                        if (groupBy.Length > 0)
                            view.GroupCol = textCols.FirstOrDefault(c => Norm(c) == Norm(groupBy));
                    }
                }

                // An explicit sum-column list is trusted over auto-detected numeric-ness,
                // so a column with a few blank/oddly-formatted values still gets summed.
                foreach (var col in explicitSumColumns)
                    view.Numeric[col] = true;

                if (explicitColumns.Count > 0)
                {
                    view.Columns = explicitColumns;
                }
                else
                {
                    view.Columns = view.AllColumns;

                    // Grouped tabs show only the group column + every numeric (summed)
                    // column, same as the page's displayColumns when Group & Sum is on with
                    // no Sub column picked - other text columns (Client Code, Scrip, ...)
                    // only ever appear behind the page's per-row expand arrow, which the
                    // PDF doesn't reproduce.
                    if (view.GroupCol != null)
                        view.Columns = view.Columns.Where(c => c == view.GroupCol || view.Numeric[c]).ToList();
                }

                view.SumCols = view.Columns.Where(c => c != view.GroupCol && view.Numeric[c]).ToList();

                return view;
            }
        }

        private string BuildChartSvg(PdfChartConfig chart, List<Dictionary<string, JsonElement>> rows, TabView view, float width, float height)
        {
            // Same defaults as the page's per-tab chart state: label = group-by column
            // (else first text column), values = first numeric column.
            var labelCol = (chart.LabelColumn ?? "").Trim();
            if (labelCol.Length == 0)
                labelCol = view.GroupCol ?? view.Columns.FirstOrDefault(c => !view.Numeric[c]) ?? "";

            var valueCols = SplitCsv(chart.ValueColumns);
            if (valueCols.Count == 0)
            {
                var firstNumeric = view.Columns.FirstOrDefault(c => view.Numeric[c]);
                if (firstNumeric != null)
                    valueCols.Add(firstNumeric);
            }

            // Chart configs may use display names ("Branch Name") while row keys are
            // compact ("BranchName") - resolve loosely, the same way the page matches
            // Setting.GroupBy against actual columns.
            labelCol = ResolveColumn(view, labelCol) ?? "";
            valueCols = valueCols.Select(vc => ResolveColumn(view, vc)).Where(vc => vc != null).ToList();

            if (labelCol.Length == 0 || valueCols.Count == 0)
                return null;

            var grouped = GroupAndSum(rows, labelCol, valueCols, view);
            var labels = grouped.Select(g => Convert.ToString(g[labelCol]) ?? "").ToArray();
            string title = $"By {PrettifyKey(labelCol)}";

            switch ((chart.Type ?? "").Trim().ToLowerInvariant())
            {
                case "pie":
                    var pieValues = grouped.Select(g => ToDouble(g.TryGetValue(valueCols[0], out var v) ? v : null)).ToArray();
                    return ChartSvgBuilder.BuildPieChart(title, labels, pieValues, width, height);

                case "bar":
                    return ChartSvgBuilder.BuildBarChart(title, labels, valueCols.Select(vc => new ChartSvgBuilder.Series
                    {
                        Name = PrettifyKey(vc),
                        Values = grouped.Select(g => ToDouble(g.TryGetValue(vc, out var v) ? v : null)).ToArray()
                    }).ToList(), width, height);

                case "line":
                    return ChartSvgBuilder.BuildLineChart(title, labels, valueCols.Select(vc => new ChartSvgBuilder.Series
                    {
                        Name = PrettifyKey(vc),
                        Values = grouped.Select(g => ToDouble(g.TryGetValue(vc, out var v) ? v : null)).ToArray()
                    }).ToList(), width, height);

                default:
                    return null;
            }
        }

        private void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().AlignLeft()
                    .Text($"Printed on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8);

                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.DefaultTextStyle(y => y.FontSize(8));
                    x.Span($"{_req.ActionName}[Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                    x.Span("]");
                });
            });
        }

        // ---- helpers ----

        private static List<Dictionary<string, object>> GroupAndSum(List<Dictionary<string, JsonElement>> rows, string groupByCol, List<string> sumCols, TabView view)
        {
            var groups = new Dictionary<string, Dictionary<string, object>>();
            var order = new List<string>();

            foreach (var row in rows)
            {
                string key = ExtractRaw(row, groupByCol) ?? "";
                if (!groups.TryGetValue(key, out var g))
                {
                    g = new Dictionary<string, object> { [groupByCol] = key };
                    foreach (var sc in sumCols)
                        g[sc] = 0.0;
                    groups[key] = g;
                    order.Add(key);
                }

                foreach (var sc in sumCols)
                    g[sc] = (double)g[sc] + ToDouble(row.TryGetValue(sc, out var v) ? v : (JsonElement?)null);
            }

            // Same ordering as the table (Setting.PdfSortBy/PdfSortOrder): when the sort
            // column is one of this chart's summed value columns, order categories by that
            // summed value; otherwise fall back to alphabetical-by-label, the same default
            // BuildDisplayRows uses - so a chart's category/slice order lines up with the
            // table's row order instead of raw first-seen insertion order.
            if (view.SortCol != null && view.SortCol != groupByCol
                && order.Count > 0 && groups[order[0]].ContainsKey(view.SortCol))
                order = order.OrderBy(k => (double)groups[k][view.SortCol]).ToList();
            else
                order = order.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

            if (view.SortDescending)
                order.Reverse();

            return order.Select(k => groups[k]).ToList();
        }

        private static string ExtractRaw(Dictionary<string, JsonElement> row, string key)
        {
            if (row == null || !row.TryGetValue(key, out var val) || val.ValueKind == JsonValueKind.Null || val.ValueKind == JsonValueKind.Undefined)
                return "";
            return val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
        }

        private static double ToDouble(object value)
        {
            if (value is JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.Number) return el.GetDouble();
                if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
                return 0;
            }
            if (value is JsonElement?) return 0;
            if (value is double dv) return dv;
            if (value is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var ds)) return ds;
            return 0;
        }

        private static string RawString(JsonElement val)
        {
            if (val.ValueKind == JsonValueKind.Null || val.ValueKind == JsonValueKind.Undefined)
                return "";
            return val.ValueKind == JsonValueKind.String ? (val.GetString() ?? "") : val.ToString();
        }

        // Mirrors the page's isNumericValue: empty counts as numeric, commas are ignored.
        private static bool IsNumericValue(JsonElement? val)
        {
            if (val == null)
                return true;
            var el = val.Value;
            if (el.ValueKind == JsonValueKind.Null || el.ValueKind == JsonValueKind.Undefined)
                return true;
            if (el.ValueKind == JsonValueKind.Number)
                return true;
            if (el.ValueKind == JsonValueKind.String)
                return TryParseNumber(el.GetString() ?? "", out _);
            return false;
        }

        private static bool TryParseNumber(string str, out double num)
        {
            num = 0;
            if (string.IsNullOrWhiteSpace(str))
                return true; // empty counts as numeric, like the page
            return double.TryParse(str.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out num);
        }

        private static readonly string[] DateParseFormats =
        {
            "yyyyMMdd", "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy",
            "yyyy-MM-ddTHH:mm:ss", "dd MMM yyyy", "dd-MMM-yyyy"
        };

        // Reformats a date string with a moment-style format (e.g. "DD-MM-YYYY").
        // Returns null when the value isn't a recognizable date, so the raw text shows.
        private static string TryFormatDate(string value, string momentFormat)
        {
            DateTime dt;
            if (!DateTime.TryParseExact(value.Trim(), DateParseFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
                && !DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return null;

            // moment tokens -> .NET tokens (month "MM" is the same in both)
            var netFormat = momentFormat
                .Replace("YYYY", "yyyy")
                .Replace("YY", "yy")
                .Replace("DD", "dd")
                .Replace("A", "tt");

            try { return dt.ToString(netFormat, CultureInfo.InvariantCulture); }
            catch { return null; }
        }

        // Same key prettifying as the page: camelCase/digit boundaries become spaces.
        private static string PrettifyKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "";
            var s = Regex.Replace(key, @"([a-z0-9])([A-Z])", "$1 $2");
            s = Regex.Replace(s, @"([A-Za-z])(\d)", "$1 $2");
            s = Regex.Replace(s, @"(\d)([A-Za-z])", "$1 $2");
            return s.Trim();
        }

        // Finds the actual row key for a configured column name: exact (case-insensitive)
        // first, then whitespace/case-insensitive ("Branch Name" -> "BranchName"). Searches
        // every column in the tab's raw data (not just the table's own display columns) -
        // a chart may legitimately slice by a dimension the table doesn't show, e.g.
        // charting "By Month" while the table itself is grouped by branch.
        private static string ResolveColumn(TabView view, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var exact = view.AllColumns.FirstOrDefault(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
                return exact;

            string Norm(string s) => new string(s.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToLowerInvariant();
            return view.AllColumns.FirstOrDefault(c => Norm(c) == Norm(name));
        }

        private static List<string> SplitCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
            return csv.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        }

        private static Color ParseColor(string colorValue, Color? fallback = null)
        {
            var defaultColor = fallback ?? ThemeText;
            if (string.IsNullOrWhiteSpace(colorValue))
                return defaultColor;

            var v = colorValue.Trim();
            if (v.StartsWith("#"))
            {
                try { return Color.FromHex(v); }
                catch { return defaultColor; }
            }

            if (NamedColors.TryGetValue(v, out var hex))
                return Color.FromHex(hex);

            return defaultColor;
        }

        private static float ParsePx(string value, float fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var digits = new string(value.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
            return float.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out var px) ? px : fallback;
        }

        // QuestPDF's ScaleToFit()/WrapAnywhere() didn't reliably stop long unbroken values
        // (e.g. "247518614") from hard-wrapping mid-digit in narrow KPI cards even though
        // shorter comma-grouped values of similar length fit fine - so the size is capped
        // deterministically here instead of trusting layout heuristics to catch every case.
        private static float FitValueFontSize(string text, float requestedSize)
        {
            const int comfortableLength = 8;
            if (string.IsNullOrEmpty(text) || text.Length <= comfortableLength)
                return requestedSize;

            return Math.Max(7f, requestedSize * comfortableLength / text.Length);
        }
    }
}
