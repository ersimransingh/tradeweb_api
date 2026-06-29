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
    public class TestQuestPdf
    {
        public static void Generate(string jsonFilePath, string outputFilePath, string logoPath)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var json = File.ReadAllText(jsonFilePath);
            var root = JsonDocument.Parse(json).RootElement;
            new QuestPdfJsonDoc(root, logoPath).GeneratePdf(outputFilePath);
            Console.WriteLine($"[JsonPdfGenerator] PDF saved → {outputFilePath}");
        }

        public static byte[] GenerateBytes(string jsonFilePath, string logoPath)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var json = File.ReadAllText(jsonFilePath);
            var root = JsonDocument.Parse(json).RootElement;
            return new QuestPdfJsonDoc(root, logoPath).GeneratePdf();
        }
    }

    // ─── Document ────────────────────────────────────────────────────────────────

    sealed class QuestPdfJsonDoc : IDocument
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
        public QuestPdfJsonDoc(JsonElement root, string imageLogo)
        {
            _root = root;
            _logoImgPath = imageLogo;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            var docCfg = J(_root, "document");
            var pnCfg = J(_root, "pageNumbering");
            var ghCfg = J(_root, "globalHeader");
            var gfCfg = J(_root, "globalFooter");
            var sections = J(_root, "sections");

            container.Page(page =>
            {
                // ── Page size ──────────────────────────────────────────────────
                switch (S(docCfg, "pageSize", "A4").ToUpperInvariant())
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

                // ── Margins ────────────────────────────────────────────────────
                var mCfg = J(docCfg, "margins");
                if (mCfg.ValueKind != JsonValueKind.Undefined)
                {
                    var mu = S(mCfg, "unit", "pt") == "mm" ? Unit.Millimetre : Unit.Point;
                    page.MarginTop((float)N(mCfg, "top", 20), mu);
                    page.MarginBottom((float)N(mCfg, "bottom", 20), mu);
                    page.MarginLeft((float)N(mCfg, "left", 20), mu);
                    page.MarginRight((float)N(mCfg, "right", 20), mu);
                }
                else
                {
                    page.Margin(20, Unit.Point);
                }

                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x
                    .FontFamily(S(docCfg, "defaultFont", "Helvetica"))
                    .FontSize((float)N(docCfg, "defaultFontSize", 8)));

                // ── Global header / top page numbers ──────────────────────────
                var pnPos = S(pnCfg, "position", "bottom-right");
                bool pnEnabled = B(pnCfg, "enabled", false);

                // ── Global header /
                if (B(ghCfg, "enabled", false))
                    page.Header().Element(c => RenderList(c, Arr(ghCfg, "content")));
                else if (pnEnabled && pnPos.StartsWith("top"))
                {
                    if (File.Exists(_logoImgPath))
                    {
                        var imageBytes = File.ReadAllBytes(_logoImgPath);
                        //row.ConstantItem(200).Height(90).Image(imageBytes).FitArea();
                        page.Header().Column(column =>
                        {
                            column.Spacing(3);
                            // Logo and Company name + address
                            column.Item().Row(row =>
                            {
                                if (!string.IsNullOrWhiteSpace(_logoImgPath))
                                {
                                    try
                                    {
                                        if (File.Exists(_logoImgPath))
                                        {
                                            var imageBytes = File.ReadAllBytes(_logoImgPath);
                                            //row.ConstantItem(200).Height(90).Image(imageBytes).FitArea();
                                            row.AutoItem().Element(container =>
                                            {
                                                container
                                                    .MaxWidth(490)
                                                    .MaxHeight(105)
                                                    .Image(imageBytes);
                                            });
                                        }
                                    }
                                    catch { /* ignore image errors */ }
                                }
                            });
                        });
                    }
                    page.Header().Element(c => RenderPageNumber(c, pnCfg));
                }
                // ── Global footer / bottom page numbers ───────────────────────
                if (B(gfCfg, "enabled", false))
                    page.Footer().Element(c => RenderList(c, Arr(gfCfg, "content")));
                else if (pnEnabled && pnPos.StartsWith("bottom"))
                    page.Footer().Element(c => RenderPageNumber(c, pnCfg));

                // ── Main content ───────────────────────────────────────────────
                page.Content().Column(col =>
                {
                    col.Spacing(0);
                    if (sections.ValueKind != JsonValueKind.Undefined)
                    {
                        foreach (var sec in sections.EnumerateArray())
                        {
                            if (S(sec, "type", "") == "page-break")
                                col.Item().PageBreak();
                            else
                                col.Item().Element(c => Render(c, sec));
                        }
                    }
                });
            });
        }

        // ─── Section Dispatcher ──────────────────────────────────────────────────

        private void Render(IContainer container, JsonElement sec)
        {
            switch (S(sec, "type", "text"))
            {
                case "text":
                case "section-title":
                    RenderText(container, sec);
                    break;
                case "columns":
                    RenderColumns(container, sec);
                    break;
                case "table":
                    RenderTable(container, sec);
                    break;
                case "keyvalue-list":
                    RenderKeyValueList(container, sec);
                    break;
                case "image":
                    RenderImage(container, sec);
                    break;
                case "spacer":
                    container.Height((float)N(sec, "height", 4));
                    break;
                case "divider":
                    container.LineHorizontal((float)N(sec, "thickness", 0.5f)).LineColor("#000000");
                    break;
            }
        }

        private void RenderList(IContainer container, JsonElement sections)
        {
            container.Column(col =>
            {
                col.Spacing(0);
                if (sections.ValueKind == JsonValueKind.Undefined) return;
                foreach (var s in sections.EnumerateArray())
                {
                    if (S(s, "type", "") == "page-break") col.Item().PageBreak();
                    else col.Item().Element(c => Render(c, s));
                }
            });
        }

        // ─── Text / Section Title ─────────────────────────────────────────────────

        private static void RenderText(IContainer container, JsonElement sec)
        {
            var value = S(sec, "value", "");
            var styleCfg = J(sec, "style");

            ContainerAlign(container, S(styleCfg, "align", "left"))
                .PaddingTop((float)N(styleCfg, "paddingTop", 0))
                .PaddingBottom((float)N(styleCfg, "paddingBottom", 0))
                .PaddingLeft((float)N(styleCfg, "paddingLeft", 0))
                .PaddingRight((float)N(styleCfg, "paddingRight", 0))
                .Text(txt =>
                {
                    txt.DefaultTextStyle(s => ApplyTextStyle(s, styleCfg));
                    var lines = value.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (i > 0) txt.EmptyLine();
                        if (!string.IsNullOrEmpty(lines[i])) txt.Span(lines[i]);
                    }
                });
        }

        // ─── Columns ─────────────────────────────────────────────────────────────

        private void RenderColumns(IContainer container, JsonElement sec)
        {
            var cols = Arr(sec, "columns");
            if (cols.ValueKind == JsonValueKind.Undefined) return;

            container.Row(row =>
            {
                foreach (var col in cols.EnumerateArray())
                {
                    var wStr = S(col, "width", "");
                    var content = Arr(col, "content");

                    IContainer item;
                    if (float.TryParse(wStr, out var grid))
                        item = row.RelativeItem(grid);
                    else if (wStr.EndsWith("%") && float.TryParse(wStr.TrimEnd('%'), out var pct))
                        item = row.RelativeItem(pct);
                    else if (float.TryParse(wStr.Replace("pt", "").Trim(), out var pts))
                        item = row.ConstantItem(pts);
                    else
                        item = row.RelativeItem();

                    item.Element(c => RenderList(c, content));
                }
            });
        }

        // ─── Key-Value List ───────────────────────────────────────────────────────

        private static void RenderKeyValueList(IContainer container, JsonElement sec)
        {
            var rows = Arr(sec, "rows");
            var styleCfg = J(sec, "style");
            var labelW = S(sec, "labelWidth", "5");
            var fontSize = (float)N(styleCfg, "size", 7);

            float labelPct = labelW.EndsWith("%") && float.TryParse(labelW.TrimEnd('%'), out var lp)
                ? lp : 40f;

            if (rows.ValueKind == JsonValueKind.Undefined) return;

            container.Column(col =>
            {
                col.Spacing(1);
                foreach (var row in rows.EnumerateArray())
                {
                    var label = S(row, "label", "");
                    var value = S(row, "value", "");
                    var rowStyle = J(row, "style");
                    var bold = B(rowStyle, "bold", false);

                    col.Item().Row(r =>
                    {
                        r.RelativeItem(labelPct).Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(fontSize));
                            t.Span($"{label} :").Bold();
                        });
                        r.RelativeItem(100f - labelPct).Text(t =>
                        {
                            t.DefaultTextStyle(s =>
                            {
                                s = s.FontSize(fontSize);
                                if (bold) s = s.Bold();
                                return s;
                            });
                            var lines = value.Split('\n');
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (i > 0) t.EmptyLine();
                                t.Span(lines[i]);
                            }
                        });
                    });
                }
            });
        }

        // ─── Image ───────────────────────────────────────────────────────────────

        private static void RenderLogoImage(IContainer container, JsonElement sec)
        {
            var src = S(sec, "src", "");
            var logoPath = S(sec, "filepath", "");
            var width = (float)N(sec, "imgwidth", 100);
            var height = (float)N(sec, "imgheight", 100);
            var align = S(sec, "align", "left");

            var aligned = ContainerAlign(container, align).Width(width).Height(height);

            if (!string.IsNullOrWhiteSpace(logoPath))
            {
                try
                {
                    if (File.Exists(logoPath))
                    {
                        var imageBytes = File.ReadAllBytes(logoPath);
                        aligned.Image(imageBytes).FitArea();
                    }
                }
                catch { /* ignore image errors */ }
            }
            else if (src.StartsWith("data:"))
            {
                var base64 = src.Contains(',') ? src.Split(',', 2)[1] : src;
                aligned.Image(Convert.FromBase64String(base64)).FitArea();
            }
            else if (File.Exists(src))
            {
                aligned.Image(src).FitArea();
            }
        }


        // ─── Image ───────────────────────────────────────────────────────────────

        private static void RenderImage(IContainer container, JsonElement sec)
        {
            var src = S(sec, "src", "");
            var logoPath = S(sec, "filepath", "");
            var width = (float)N(sec, "imgwidth", 100);
            var height = (float)N(sec, "imgheight", 100);
            var align = S(sec, "align", "left");

            var aligned = ContainerAlign(container, align).Width(width).Height(height);

            if (!string.IsNullOrWhiteSpace(logoPath))
            {
                try
                {
                    if (File.Exists(logoPath))
                    {
                        var imageBytes = File.ReadAllBytes(logoPath);
                        aligned.Image(imageBytes).FitArea();
                    }
                }
                catch { /* ignore image errors */ }
            }
            else if (src.StartsWith("data:"))
            {
                var base64 = src.Contains(',') ? src.Split(',', 2)[1] : src;
                aligned.Image(Convert.FromBase64String(base64)).FitArea();
            }
            else if (File.Exists(src))
            {
                aligned.Image(src).FitArea();
            }
        }


        // ─── Table ───────────────────────────────────────────────────────────────

        private static void RenderTable(IContainer container, JsonElement sec)
        {
            var rows = Arr(sec, "rows");
            var values = Arr(sec, "values");
            var data = values.ValueKind != JsonValueKind.Undefined ? values : Arr(sec, "data");
            var colWidths = Arr(sec, "columnWidths");
            var tStyle = J(sec, "style");

            if (rows.ValueKind == JsonValueKind.Undefined && data.ValueKind == JsonValueKind.Undefined) return;

            float borderW = (float)N(tStyle, "borderWidth", 0.5f);
            var borderC = S(tStyle, "borderColor", "#000000");
            var headerBg = S(tStyle, "headerBackground", "#d0d0d0");
            float cellFont = (float)N(tStyle, "cellFontSize", 7f);
            var altRowBg = tStyle.ValueKind != JsonValueKind.Undefined
                              && tStyle.TryGetProperty("alternateRowBackground", out var arb)
                              && arb.ValueKind == JsonValueKind.String
                              ? arb.GetString() : null;

            var headerColumns = ResolveHeaderColumns(rows);
            Console.WriteLine("========== HEADER MAP ==========");

            foreach (var col in headerColumns)
            {
                Console.WriteLine(col.Key);
            }

            Console.WriteLine("================================");
            int numCols = ResolveColumnCount(rows, colWidths, headerColumns.Count);

            container.Table(table =>
            {
                // Track which (rowIndex, colIndex) positions are occupied by a rowspan
                var occupied = new HashSet<(int, int)>();
                int rowIdx = 0;
                int dataIdx = 0; // for alternating row backgrounds

                void RenderResolvedCell(int rowIdx, int dataIdx, int colIdx, ResolvedCell cellData, bool isHeader, bool isSection, bool isSubtot, bool isTotal)
                {
                    int colspan = Math.Max(1, cellData.Colspan);
                    int rowspan = Math.Max(1, cellData.Rowspan);
                    var cellSty = cellData.Style;
                    var cellVal = cellData.Value;
                    var align = S(cellSty, "align", isHeader ? "center" : "left");

                    // Mark positions that will be occupied by this cell's rowspan
                    for (int r = 0; r < rowspan; r++)
                        for (int c = 0; c < colspan; c++)
                            if (r > 0 || c > 0)
                                occupied.Add((rowIdx + r, colIdx + c));

                    string? bg = null;
                    if (cellSty.ValueKind != JsonValueKind.Undefined
                        && cellSty.TryGetProperty("background", out var bgProp)
                        && bgProp.ValueKind == JsonValueKind.String)
                        bg = bgProp.GetString();

                    if (bg == null)
                    {
                        if (isHeader || isSection)
                            bg = headerBg;
                        else if (isTotal)
                            bg = "#f0f0f0";
                        else if (altRowBg != null && dataIdx % 2 == 1)
                            bg = altRowBg;
                    }

                    table.Cell()
                        .ColumnSpan((uint)colspan)
                        .RowSpan((uint)rowspan)
                        .Element(cell =>
                        {
                            IContainer c = cell;
                            if (bg != null) c = c.Background(bg);
                            c = c.Border(borderW).BorderColor(borderC).Padding(2);

                            if (rowspan > 1) c = c.AlignMiddle();

                            c.Text(txt =>
                            {
                                txt.DefaultTextStyle(s =>
                                {
                                    s = s.FontSize(cellFont);
                                    if (isHeader || isSection) s = s.Bold();
                                    return ApplyTextStyle(s, cellSty);
                                });

                                if (align == "right") txt.AlignRight();
                                else if (align == "center") txt.AlignCenter();

                                var lines = cellVal.Split('\n');
                                for (int i = 0; i < lines.Length; i++)
                                {
                                    if (i < lines.Length - 1)
                                        txt.Line(lines[i]);
                                    else if (!string.IsNullOrEmpty(lines[i]))
                                        txt.Span(lines[i]);
                                }
                            });
                        });
                }

                void RenderRow(JsonElement rowEl)
                {
                    var rowType = S(rowEl, "type", "data");

                    if (rowType == "header-spacer") { rowIdx++; return; }

                    bool isHeader = rowType == "header";
                    bool isSection = rowType == "section-row";
                    bool isSubtot = rowType == "subtotal-row";
                    bool isTotal = rowType == "total-row";

                    var cells = Arr(rowEl, "cells");
                    List<ResolvedCell>? resolvedCells = null;

                    if (cells.ValueKind == JsonValueKind.Undefined)
                        resolvedCells = ResolveMappedCells(rowEl, headerColumns);

                    if (cells.ValueKind == JsonValueKind.Undefined && (resolvedCells == null || resolvedCells.Count == 0))
                    {
                        rowIdx++;
                        return;
                    }

                    int colIdx = 0;

                    if (cells.ValueKind != JsonValueKind.Undefined)
                    {
                        foreach (var cellEl in cells.EnumerateArray())
                        {
                            while (occupied.Contains((rowIdx, colIdx))) colIdx++;

                            RenderResolvedCell(
                                rowIdx,
                                dataIdx,
                                colIdx,
                                new ResolvedCell(
                                    S(cellEl, "value", ""),
                                    J(cellEl, "style"),
                                    Math.Max(1, (int)N(cellEl, "colspan", 1)),
                                    Math.Max(1, (int)N(cellEl, "rowspan", 1))),
                                isHeader,
                                isSection,
                                isSubtot,
                                isTotal);

                            colIdx += Math.Max(1, (int)N(cellEl, "colspan", 1));
                        }
                    }
                    else
                    {
                        foreach (var cellData in resolvedCells!)
                        {
                            while (occupied.Contains((rowIdx, colIdx))) colIdx++;
                            RenderResolvedCell(rowIdx, dataIdx, colIdx, cellData, isHeader, isSection, isSubtot, isTotal);
                            colIdx += Math.Max(1, cellData.Colspan);
                        }
                    }

                    if (!isHeader && !isSection) dataIdx++;
                    rowIdx++;
                }

                // ── Column definitions ─────────────────────────────────────────
                table.ColumnsDefinition(cols =>
                {
                    if (colWidths.ValueKind != JsonValueKind.Undefined)
                    {
                        foreach (var w in colWidths.EnumerateArray())
                        {
                            var ws = w.GetString() ?? "";

                            if (float.TryParse(ws, out var grid))
                                cols.RelativeColumn(grid);
                            else if (ws.EndsWith("%") && float.TryParse(ws.TrimEnd('%'), out var rp))
                                cols.RelativeColumn(rp);
                            else if (float.TryParse(ws.Replace("pt", "").Trim(), out var fp))
                                cols.ConstantColumn(fp);
                            else
                                cols.RelativeColumn();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numCols; i++) cols.RelativeColumn();
                    }
                });

                if (rows.ValueKind != JsonValueKind.Undefined)
                    foreach (var rowEl in rows.EnumerateArray())
                        RenderRow(rowEl);

                if (data.ValueKind != JsonValueKind.Undefined)
                    foreach (var rowEl in data.EnumerateArray())
                        RenderRow(rowEl);
            });
        }

        // ─── Page Number ─────────────────────────────────────────────────────────

        private static void RenderPageNumber(IContainer container, JsonElement cfg)
        {
            var position = S(cfg, "position", "bottom-right");
            var format = S(cfg, "format", "Page {current} of {total}");
            var styCfg = J(cfg, "style");
            var fontSize = (float)N(styCfg, "size", 7);
            var bold = B(styCfg, "bold", false);
            var color = S(styCfg, "color", "#000000");
            var offsetX = (float)N(cfg, "offsetX", 0);
            var offsetY = (float)N(cfg, "offsetY", 0);

            ContainerAlign(
                container.PaddingHorizontal(offsetX).PaddingVertical(offsetY),
                position.EndsWith("right") ? "right" :
                position.EndsWith("center") ? "center" : "left"
            ).Text(txt =>
            {
                txt.DefaultTextStyle(s =>
                {
                    s = s.FontSize(fontSize).FontColor(color);
                    if (bold) s = s.Bold();
                    return s;
                });

                // Parse format: replace {current} and {total} with dynamic spans
                var remaining = format;
                while (remaining.Length > 0)
                {
                    int ci = remaining.IndexOf("{current}", StringComparison.Ordinal);
                    int ti = remaining.IndexOf("{total}", StringComparison.Ordinal);

                    if (ci == -1 && ti == -1) { txt.Span(remaining); break; }

                    bool useCurrent = ci != -1 && (ti == -1 || ci < ti);
                    int next = useCurrent ? ci : ti;

                    if (next > 0) txt.Span(remaining[..next]);

                    if (useCurrent) { txt.CurrentPageNumber(); remaining = remaining[(next + 9)..]; }
                    else { txt.TotalPages(); remaining = remaining[(next + 7)..]; }
                }
            });
        }

        // ─── Style Helpers ────────────────────────────────────────────────────────

        private static TextStyle ApplyTextStyle(TextStyle s, JsonElement cfg)
        {
            if (cfg.ValueKind == JsonValueKind.Undefined) return s;

            if (cfg.TryGetProperty("size", out var sz) && sz.ValueKind == JsonValueKind.Number) s = s.FontSize((float)sz.GetDouble());
            if (cfg.TryGetProperty("bold", out var bd) && bd.ValueKind == JsonValueKind.True) s = s.Bold();
            if (cfg.TryGetProperty("italic", out var it) && it.ValueKind == JsonValueKind.True) s = s.Italic();
            if (cfg.TryGetProperty("underline", out var ul) && ul.ValueKind == JsonValueKind.True) s = s.Underline();
            if (cfg.TryGetProperty("color", out var clr) && clr.ValueKind == JsonValueKind.String) s = s.FontColor(clr.GetString()!);
            if (cfg.TryGetProperty("font", out var fn) && fn.ValueKind == JsonValueKind.String) s = s.FontFamily(fn.GetString()!);
            return s;
        }

        private static IContainer ContainerAlign(IContainer c, string align) => align switch
        {
            "center" => c.AlignCenter(),
            "right" => c.AlignRight(),
            _ => c.AlignLeft()
        };

        // ─── Utilities ────────────────────────────────────────────────────────────

        private static List<TableColumnDefinition> ResolveHeaderColumns(JsonElement rows)
        {
            var mappedColumns = new List<(int Position, TableColumnDefinition Column)>();
            var columns = new List<TableColumnDefinition>();
            if (rows.ValueKind == JsonValueKind.Undefined) return columns;

            var headerRows = new List<JsonElement>();
            foreach (var row in rows.EnumerateArray())
            {
                var rowType = S(row, "type", "data");
                if (rowType == "header" || rowType == "header-spacer")
                {
                    headerRows.Add(row);
                    continue;
                }

                if (headerRows.Count > 0) break;
            }

            if (headerRows.Count == 0) return columns;

            int headerDepth = headerRows.Count;
            var occupied = new HashSet<(int, int)>();

            for (int headerRowIdx = 0; headerRowIdx < headerRows.Count; headerRowIdx++)
            {
                var row = headerRows[headerRowIdx];
                if (S(row, "type", "") == "header-spacer") continue;

                var cells = Arr(row, "cells");
                if (cells.ValueKind == JsonValueKind.Undefined) continue;

                int colIdx = 0;
                foreach (var cell in cells.EnumerateArray())
                {
                    while (occupied.Contains((headerRowIdx, colIdx))) colIdx++;

                    int colspan = Math.Max(1, (int)N(cell, "colspan", 1));
                    int rowspan = Math.Max(1, (int)N(cell, "rowspan", 1));

                    for (int r = 0; r < rowspan; r++)
                        for (int c = 0; c < colspan; c++)
                            if (r > 0 || c > 0)
                                occupied.Add((headerRowIdx + r, colIdx + c));


                    bool isLeaf = headerRowIdx + rowspan >= headerDepth;
                    if (isLeaf)
                    {
                        var keys = Arr(cell, "keys");
                        var dataStyle = J(cell, "dataStyle");
                        if (dataStyle.ValueKind == JsonValueKind.Undefined)
                            dataStyle = J(cell, "columnStyle");

                        if (keys.ValueKind != JsonValueKind.Undefined)
                        {
                            int localPos = 0;
                            foreach (var key in keys.EnumerateArray())
                            {
                                mappedColumns.Add(
                              (
                                  colIdx + localPos,
                                  new TableColumnDefinition(
                                      key.GetString() ?? "",
                                      dataStyle
                                  )
                              ));
                            }
                            //columns.Add(new TableColumnDefinition(key.GetString() ?? "", dataStyle));
                        }
                        else
                        {
                            var key = ResolveHeaderDataKey(cell);
                            for (int i = 0; i < colspan; i++)
                            {
                                mappedColumns.Add(
                                    (
                                        colIdx + i,
                                        new TableColumnDefinition(key, dataStyle)
                                    ));
                            }
                            //columns.Add(new TableColumnDefinition(key, dataStyle));
                        }
                    }
                    colIdx += colspan;
                }
            }
            columns = mappedColumns
                        .OrderBy(x => x.Position)
                        .Select(x => x.Column)
                        .ToList();
            return columns;
        }

        private static List<ResolvedCell> ResolveMappedCells(JsonElement rowEl, IReadOnlyList<TableColumnDefinition> headerColumns)
        {
            if (headerColumns.Count == 0 || rowEl.ValueKind != JsonValueKind.Object) return new List<ResolvedCell>();

            var values = J(rowEl, "values");
            var valueSource = values.ValueKind == JsonValueKind.Object ? values : rowEl;
            var resolved = new List<ResolvedCell>(headerColumns.Count);

            foreach (var column in headerColumns)
            {
                var value = ResolveValueByPath(valueSource, column.Key);
                resolved.Add(new ResolvedCell(JsonElementToText(value), column.DataStyle));
            }

            return resolved;
        }

        private static string ResolveHeaderDataKey(JsonElement cell)
        {
            var dataKey = S(cell, "dataKey", "");
            if (!string.IsNullOrWhiteSpace(dataKey)) return dataKey;

            var key = S(cell, "key", "");
            if (!string.IsNullOrWhiteSpace(key)) return key;

            var field = S(cell, "field", "");
            if (!string.IsNullOrWhiteSpace(field)) return field;

            return S(cell, "value", "");
        }

        private static JsonElement ResolveValueByPath(JsonElement source, string path)
        {
            if (source.ValueKind == JsonValueKind.Undefined || source.ValueKind == JsonValueKind.Null)
            {
                return default;
            }
            if (string.IsNullOrWhiteSpace(path)) return default;

            JsonElement current = source;
            //  foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            foreach (var segment in path
                          .Split('.', StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim()))
            {
                if (current.ValueKind == JsonValueKind.Object)
                {
                    if (!current.TryGetProperty(segment, out current))
                        return default;
                    continue;
                }

                if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index))
                {
                    if (index < 0 || index >= current.GetArrayLength())
                        return default;
                    current = current[index];
                    continue;
                }

                return default;
            }

            return current;
        }


        private static string JsonElementToText(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            JsonValueKind.Undefined => "",
            _ => value.GetRawText()
        };

        private static int ResolveColumnCount(JsonElement rows, JsonElement colWidths, int headerColumnCount)
        {
            if (colWidths.ValueKind != JsonValueKind.Undefined)
            {
                int n = 0;
                foreach (var _ in colWidths.EnumerateArray()) n++;
                return n;
            }
            if (headerColumnCount > 0) return headerColumnCount;
            if (rows.ValueKind == JsonValueKind.Undefined) return 1;
            foreach (var row in rows.EnumerateArray())
            {
                if (S(row, "type", "") == "header-spacer") continue;
                var cells = Arr(row, "cells");
                if (cells.ValueKind == JsonValueKind.Undefined) continue;
                int n = 0;
                foreach (var cell in cells.EnumerateArray())
                    n += Math.Max(1, (int)N(cell, "colspan", 1));
                return n;
            }
            return 1;
        }

        // Short accessor helpers (J=json prop, S=string, N=number, B=bool, Arr=array)
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

}
