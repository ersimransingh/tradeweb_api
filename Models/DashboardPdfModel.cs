using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradeWeb.API.Models
{
    public class DashboardPdfRequest
    {
        public string Title { get; set; } = "";
        public string DateRange { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string CompanyLogoBase64 { get; set; } = "";
        // "base64" (default) wraps the PDF in the JSON response; "file" returns the PDF file directly.
        public string Output { get; set; } = "base64";
        public string ActionName { get; set; } = "TradeWeb";

        // Optional: overrides the UserId in every tab's DataApi J_Api block when set
        // (the page always stamps the logged-in user the same way).
        public string UserId { get; set; } = "";

        [JsonConverter(typeof(FlexibleArrayConverter<DashboardWidget>))]
        public List<DashboardWidget> Widgets { get; set; } = new List<DashboardWidget>();
    }

    public class DashboardWidget
    {
        public string Type { get; set; } = "";
        public int Column { get; set; } = 12;
        public string Title { get; set; } = "";

        // gridWidget
        public List<GridItem> GridItems { get; set; } = new List<GridItem>();
        public WidgetSettings Settings { get; set; } = new WidgetSettings();

        // tableWithGraph
        public List<GridTab> GridTabs { get; set; } = new List<GridTab>();
    }

    public class GridItem
    {
        public LabelValue Label { get; set; } = new LabelValue();
        public LabelValue Value { get; set; } = new LabelValue();
    }

    public class LabelValue
    {
        public string Text { get; set; } = "";
        public double Size { get; set; } = 12;
        public string Color { get; set; } = "black";
        public double MarginTop { get; set; } = 0;
    }

    public class WidgetSettings
    {
        public string TitleColor { get; set; } = "";
        public string TitleAlign { get; set; } = "center";
        public string BorderType { get; set; } = "left";
        public string BorderColor { get; set; } = "#3B82F6";
        public string BorderWidth { get; set; } = "4px";
        public bool ShowInPdf { get; set; } = false;
    }

    public class GridTab
    {
        public string TabName { get; set; } = "";
        public GridTabSetting Setting { get; set; } = new GridTabSetting();

        // How the page fetches this tab's rows (the "DataAPI" block from the dashboard
        // layout). When Rows is empty, the PDF endpoint replays this server-side via the
        // same ActionName/Option -> stored procedure lookup the main TradeWeb API uses.
        public TabDataApi DataApi { get; set; }

        // Caller-supplied, pre-fetched data for this tab. Wins over DataApi when present.
        [JsonConverter(typeof(FlexibleArrayConverter<Dictionary<string, JsonElement>>))]
        public List<Dictionary<string, JsonElement>> Rows { get; set; } = new List<Dictionary<string, JsonElement>>();
    }

    public class TabDataApi
    {
        [JsonPropertyName("J_Ui")]
        public TabDataApiJUi JUi { get; set; }

        [JsonPropertyName("X_Filter")]
        public Dictionary<string, JsonElement> XFilter { get; set; }

        [JsonPropertyName("X_Filter_Multiple")]
        public Dictionary<string, JsonElement> XFilterMultiple { get; set; }

        [JsonPropertyName("J_Api")]
        public Dictionary<string, JsonElement> JApi { get; set; }
    }

    public class TabDataApiJUi
    {
        public string ActionName { get; set; } = "";
        public string Option { get; set; } = "";
    }

    // Mirrors the DashBoardNew tab "Setting" block from the tradewebx layout API
    // (see DASHBOARD_NEW_API.md in tradewebx), so the frontend can pass its tab
    // settings straight through unchanged. Property names match the frontend fields;
    // JSON binding is case-insensitive, so PascalCase payloads bind as-is.
    public class GridTabSetting
    {
        // PDF-only: a tab is only rendered in the PDF when this is explicitly true -
        // opt-in, not opt-out, so a pass-through payload that never mentions ShowInPdf
        // renders nothing until the caller turns tabs on deliberately.
        public bool? ShowInPdf { get; set; }

        // PDF-only: charts to render for this tab ("type": "pie"|"bar"|"line").
        // labelColumn/valueColumns may be omitted - they then default the same way the
        // page's chart does (group-by/first text column, first numeric column). A chart's
        // labelColumn/valueColumns may reference any column in the tab's raw data, even
        // one PdfColumns doesn't display in the table itself (e.g. charting by "Month"
        // while the table is grouped by branch).
        public List<PdfChartConfig> PdfCharts { get; set; } = new List<PdfChartConfig>();

        // PDF-only explicit overrides - when set, these win outright over the
        // auto-detected table shape below (GroupBy / auto-numeric-columns), because the
        // PDF often needs a fixed report shape independent of the web page's live
        // Group-By dropdown state.
        public string PdfColumns { get; set; } = "";     // CSV, exact columns + order to display
        public string PdfGroupBy { get; set; } = "";      // single column name to group by
        public string PdfSumColumns { get; set; } = "";   // CSV, columns to sum per group
        public string PdfSortBy { get; set; } = "";        // single column name to sort by (row column, or a sum column when grouped)
        public string PdfSortOrder { get; set; } = "";      // "asc" (default) or "desc"

        // --- pass-through fields, same names/meaning as the frontend tab Setting ---
        public string GroupBy { get; set; } = "";
        public string HideColumnLabels { get; set; } = "";   // CSV
        public string HideEntireColumn { get; set; } = "";   // CSV
        public string LeftAlignedColums { get; set; } = "";  // CSV (sic - frontend typo, accepted)
        public string LeftAlignedColumns { get; set; } = ""; // CSV (alias)
        public List<ColumnWidthConfig> ColumnWidth { get; set; } = new List<ColumnWidthConfig>();
        public List<DecimalColumnConfig> DecimalColumns { get; set; } = new List<DecimalColumnConfig>();
        public List<ValueColorConfig> ValueBasedTextColor { get; set; } = new List<ValueColorConfig>();

        [JsonConverter(typeof(FlexibleArrayConverter<DateFormatConfig>))]
        public List<DateFormatConfig> DateFormat { get; set; } = new List<DateFormatConfig>();
    }

    public class PdfChartConfig
    {
        public string Type { get; set; } = "";
        public string LabelColumn { get; set; } = "";
        public string ValueColumns { get; set; } = "";
    }

    public class DecimalColumnConfig
    {
        public string Key { get; set; } = "";
        public int DecimalPlaces { get; set; } = 2;
    }

    public class ColumnWidthConfig
    {
        public string Key { get; set; } = "";
        public double Width { get; set; } = 0;
    }

    public class ValueColorConfig
    {
        public string Key { get; set; } = "";
        public double CheckNumber { get; set; } = 0;
        public string LessThanColor { get; set; } = "";
        public string GreaterThanColor { get; set; } = "";
        public string EqualToColor { get; set; } = "";
    }

    public class DateFormatConfig
    {
        public string Key { get; set; } = "";
        public string Format { get; set; } = "";
    }
}
