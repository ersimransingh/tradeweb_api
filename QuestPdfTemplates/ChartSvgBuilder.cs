using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TradeWeb.API.QuestPdfTemplates
{
    // Hand-rolled SVG chart rendering (pie/bar/line) so dashboard PDF charts can be drawn
    // natively server-side via QuestPDF's Svg() element. Styled to match tradewebx's
    // on-screen ApexCharts config (DashBoardNew/index.tsx: CHART_PALETTE, chartOptions) -
    // bottom legend, on-slice pie percentages, rounded bar corners, rotated x-axis labels,
    // en-IN number formatting, and the same slice/category caps with an "Others" fold.
    public static class ChartSvgBuilder
    {
        // Exact match to tradewebx's CHART_PALETTE (DashBoardNew/index.tsx), in order -
        // ApexCharts assigns these sequentially to pie slices / bar-line series.
        private static readonly string[] Palette =
        {
            "#3B82F6", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6",
            "#EC4899", "#14B8A6", "#F97316", "#6366F1", "#84CC16",
            "#06B6D4", "#F43F5E"
        };

        private const int MaxPieSlices = 10;
        // Was 30 - far too high to ever kick in for realistic data (30 skinny bars is
        // unreadable well before the fold-into-"Others" mechanism even applied). Matches
        // the pie chart's cap so both chart types fold the smallest categories the same way.
        private const int MaxBarCategories = 10;
        private const string OthersLabel = "Others";
        private const string AxisTextColor = "#121212";
        private const string GridColor = "#BED0C4";

        public class Series
        {
            public string Name;
            public double[] Values;
        }

        // Slices thinner than this share of the pie get their percentage pulled outside
        // the circle on a leader line instead of stamped on the slice - below this an
        // arc is too narrow for the on-slice text to fit without overlapping its
        // neighbours (the case a 10-slice pie with several small categories hits).
        // Was 7 - still let some borderline slices keep their on-slice label crammed
        // against its neighbours, so widened to catch more of them.
        private const double SmallSliceThresholdPct = 10.0;

        public static string BuildPieChart(string title, string[] labels, double[] values, float width, float height)
        {
            List<double[]> seriesValues;
            (labels, seriesValues) = CapAndFold(labels, new List<double[]> { values }, MaxPieSlices);
            values = seriesValues[0];

            var sb = new StringBuilder();
            sb.Append(Header(width, height));
            sb.Append(TitleText(title, width));

            double total = values.Sum();

            if (total <= 0 || labels.Length == 0)
            {
                sb.Append(NoDataText(width, height));
                sb.Append("</svg>");
                return sb.ToString();
            }

            bool hasSmallSlice = values.Any(v => v > 0 && v / total * 100 < SmallSliceThresholdPct);

            // The legend is the one place every slice's percentage is guaranteed to show
            // (small slices' on-slice text moves to a callout, big slices' stays on the
            // slice) - folding it into "Name (12.3%)" here means the reader never has to
            // cross-reference a slice's colour back to a number shown somewhere else.
            var legendLabels = labels.Select((l, i) => $"{Truncate(l, PieLegendNameMaxChars)} ({F1(values[i] / total * 100)}%)").ToArray();

            float legendTop = height - LegendHeight(legendLabels, width - 20, PieLegendEntryMaxChars);
            float chartAreaTop = 20;
            // A floor keeps the pie from collapsing to a near-zero/negative radius (a
            // garbled sliver of overlapping paths) when a long legend - many client names,
            // each forced onto its own row in a narrow chart - eats nearly the whole
            // height. A cramped legend is far more readable than a broken pie.
            const float minRadius = 22f;
            // Outside callout labels need horizontal clearance on both sides of the
            // circle - reserve it in the radius so the leader line + text never runs
            // off the edge of the chart.
            float sideMargin = hasSmallSlice ? 48f : 0f;
            float radius = Math.Max(minRadius, Math.Min(Math.Min(width * 0.28f, width / 2f - sideMargin), (legendTop - chartAreaTop) / 2f - 4));
            float cx = width / 2f;
            float cy = chartAreaTop + radius + 4;

            double angle = -90;
            var slices = new List<(double mid, double pct, bool small)>();

            for (int i = 0; i < labels.Length; i++)
            {
                double slice = values[i] <= 0 ? 0 : (values[i] / total) * 360.0;
                if (slice <= 0) { slices.Add((0, 0, false)); continue; }

                double startAngle = angle;
                double endAngle = angle + slice;
                double midAngle = (startAngle + endAngle) / 2;
                angle = endAngle;

                var (x0, y0) = PointOnCircle(cx, cy, radius, startAngle);
                var (x1, y1) = PointOnCircle(cx, cy, radius, endAngle);
                int largeArc = slice > 180 ? 1 : 0;
                string color = Palette[i % Palette.Length];

                sb.Append($"<path d=\"M {F(cx)},{F(cy)} L {F(x0)},{F(y0)} A {F(radius)},{F(radius)} 0 {largeArc} 1 {F(x1)},{F(y1)} Z\" fill=\"{color}\" stroke=\"white\" stroke-width=\"1\" />");

                double pct = values[i] / total * 100;
                slices.Add((midAngle, pct, pct < SmallSliceThresholdPct));
            }

            foreach (var s in slices)
            {
                if (s.pct <= 0 || s.small) continue;
                var (mx, my) = PointOnCircle(cx, cy, radius * 0.65f, s.mid);
                sb.Append($"<text x=\"{F(mx)}\" y=\"{F(my)}\" font-size=\"7\" font-weight=\"bold\" fill=\"white\" text-anchor=\"middle\" dominant-baseline=\"middle\">{F1(s.pct)}%</text>");
            }

            // Callouts are free to use the whole band between the title and the legend -
            // clamped to it below so a pie with several small slices stacks its labels
            // into that space instead of running down past the legend's own text.
            float calloutTop = chartAreaTop - 6f;
            float calloutBottom = legendTop - 4f;
            AppendCalloutLabels(sb, slices, cx, cy, radius, calloutTop, calloutBottom);

            AppendBottomLegend(sb, legendLabels, width, legendTop, width - 20, PieLegendEntryMaxChars);

            sb.Append("</svg>");
            return sb.ToString();
        }

        private const float CalloutMinGap = 8f;
        private const float CalloutElbowOffset = 6f;
        private const float CalloutLegLength = 12f;

        // Draws percentage labels for thin slices outside the circle, each linked back
        // to its slice by a bent leader line - mirrors how Highcharts/ApexCharts handle
        // pie data labels that don't fit on the slice itself.
        private static void AppendCalloutLabels(StringBuilder sb, List<(double mid, double pct, bool small)> slices, float cx, float cy, float radius, float top, float bottom)
        {
            var left = new List<(float y, double pct, double mid)>();
            var right = new List<(float y, double pct, double mid)>();

            foreach (var s in slices)
            {
                if (s.pct <= 0 || !s.small) continue;
                var (ex, ey) = PointOnCircle(cx, cy, radius, s.mid);
                (ex >= cx ? right : left).Add((ey, s.pct, s.mid));
            }

            AppendCalloutSide(sb, left, cx, cy, radius, top, bottom, isRight: false);
            AppendCalloutSide(sb, right, cx, cy, radius, top, bottom, isRight: true);
        }

        private static void AppendCalloutSide(StringBuilder sb, List<(float y, double pct, double mid)> items, float cx, float cy, float radius, float top, float bottom, bool isRight)
        {
            if (items.Count == 0) return;
            items.Sort((a, b) => a.y.CompareTo(b.y));

            // Spread stacked labels vertically so consecutive callouts on the same side
            // don't sit on top of each other - a top-down push against the previous
            // label's position, ...
            var ys = new float[items.Count];
            ys[0] = items[0].y;
            for (int i = 1; i < items.Count; i++)
                ys[i] = Math.Max(items[i].y, ys[i - 1] + CalloutMinGap);

            // ... then pulled back up if that push ran the last label past the legend -
            // this is what actually keeps a many-small-slice pie's callouts from
            // overlapping the legend text below it.
            if (ys[ys.Length - 1] > bottom)
            {
                ys[ys.Length - 1] = bottom;
                for (int i = ys.Length - 2; i >= 0; i--)
                    ys[i] = Math.Min(ys[i], ys[i + 1] - CalloutMinGap);
            }

            // If there are still more labels than the band can hold at MinGap even after
            // that (e.g. a great many small slices on one side), fall back to spacing
            // them evenly across the whole band - tighter gaps, but never off the edges.
            if (ys[0] < top)
            {
                float step = items.Count > 1 ? (bottom - top) / (items.Count - 1) : 0;
                for (int i = 0; i < ys.Length; i++)
                    ys[i] = top + step * i;
            }

            float columnX = isRight ? cx + radius + CalloutElbowOffset + CalloutLegLength : cx - radius - CalloutElbowOffset - CalloutLegLength;
            string anchor = isRight ? "start" : "end";
            float textX = isRight ? columnX + 2 : columnX - 2;

            for (int i = 0; i < items.Count; i++)
            {
                var (edgeX, edgeY) = PointOnCircle(cx, cy, radius, items[i].mid);
                var (bendX, bendY) = PointOnCircle(cx, cy, radius + CalloutElbowOffset, items[i].mid);
                float labelY = ys[i];

                sb.Append($"<polyline points=\"{F(edgeX)},{F(edgeY)} {F(bendX)},{F(bendY)} {F(columnX)},{F(labelY)}\" fill=\"none\" stroke=\"{AxisTextColor}\" stroke-width=\"0.5\" />");
                sb.Append($"<text x=\"{F(textX)}\" y=\"{F(labelY + 2.5f)}\" font-size=\"6.5\" fill=\"{AxisTextColor}\" text-anchor=\"{anchor}\">{F1(items[i].pct)}%</text>");
            }
        }

        public static string BuildBarChart(string title, string[] categories, List<Series> series, float width, float height)
        {
            var (cappedCategories, seriesValues) = CapAndFold(categories, series.Select(s => s.Values).ToList(), MaxBarCategories);
            var names = series.Select(s => s.Name).ToArray();

            var sb = new StringBuilder();
            sb.Append(Header(width, height));
            sb.Append(TitleText(title, width));

            if (cappedCategories.Length == 0)
            {
                sb.Append(NoDataText(width, height));
                sb.Append("</svg>");
                return sb.ToString();
            }

            double max = seriesValues.SelectMany(v => v).DefaultIfEmpty(0).Max();
            if (max <= 0) max = 1;

            // Was a fixed 42pt - too narrow for large Indian-formatted values (crores+
            // routinely run 12-14 digits with separators), clipping the leftmost digits
            // off the edge of the SVG canvas. Widens to fit the largest grid label.
            float axisLeft = AxisLeftMargin(max);
            float legendTop = height - LegendHeight(names, width - 20);
            // Category labels are capped at 15 chars and rotated -45deg (RotatedCategoryLabel);
            // at font-size 6 that needs ~38pt of vertical clearance plus the label's own
            // +8pt offset from axisBottom - the old fixed 26pt reservation was well short
            // of that, so long labels overlapped the legend text below them.
            float axisBottom = legendTop - 50;
            float axisTop = 20;
            float plotWidth = width - axisLeft - 10;
            float plotHeight = axisBottom - axisTop;

            sb.Append(GridLines(axisLeft, axisTop, width - 5, axisBottom, max));
            sb.Append($"<line x1=\"{F(axisLeft)}\" y1=\"{F(axisTop)}\" x2=\"{F(axisLeft)}\" y2=\"{F(axisBottom)}\" stroke=\"{GridColor}\" stroke-width=\"1\" />");
            sb.Append($"<line x1=\"{F(axisLeft)}\" y1=\"{F(axisBottom)}\" x2=\"{F(width - 5)}\" y2=\"{F(axisBottom)}\" stroke=\"{GridColor}\" stroke-width=\"1\" />");

            int seriesCount = Math.Max(seriesValues.Count, 1);
            float groupWidth = plotWidth / cappedCategories.Length;
            float barGap = 2;
            float barWidth = Math.Max(2, (groupWidth * 0.55f - barGap * (seriesCount - 1)) / seriesCount);
            float groupPadding = (groupWidth - (barWidth * seriesCount + barGap * (seriesCount - 1))) / 2f;

            for (int c = 0; c < cappedCategories.Length; c++)
            {
                float groupX = axisLeft + c * groupWidth;

                for (int s = 0; s < seriesValues.Count; s++)
                {
                    double val = c < seriesValues[s].Length ? seriesValues[s][c] : 0;
                    float barHeight = (float)(val / max * plotHeight);
                    if (barHeight < 0) barHeight = 0;

                    float barX = groupX + groupPadding + s * (barWidth + barGap);
                    float barY = axisBottom - barHeight;
                    string color = Palette[s % Palette.Length];

                    sb.Append($"<rect x=\"{F(barX)}\" y=\"{F(barY)}\" width=\"{F(barWidth)}\" height=\"{F(barHeight)}\" rx=\"2\" fill=\"{color}\" />");
                }

                sb.Append(RotatedCategoryLabel(groupX + groupWidth / 2, axisBottom + 8, cappedCategories[c]));
            }

            AppendBottomLegend(sb, names, width, legendTop, width - 20);

            sb.Append("</svg>");
            return sb.ToString();
        }

        public static string BuildLineChart(string title, string[] categories, List<Series> series, float width, float height)
        {
            var (cappedCategories, seriesValues) = CapAndFold(categories, series.Select(s => s.Values).ToList(), MaxBarCategories);
            var names = series.Select(s => s.Name).ToArray();

            var sb = new StringBuilder();
            sb.Append(Header(width, height));
            sb.Append(TitleText(title, width));

            if (cappedCategories.Length == 0)
            {
                sb.Append(NoDataText(width, height));
                sb.Append("</svg>");
                return sb.ToString();
            }

            double max = seriesValues.SelectMany(v => v).DefaultIfEmpty(0).Max();
            if (max <= 0) max = 1;

            // Was a fixed 42pt - too narrow for large Indian-formatted values (crores+
            // routinely run 12-14 digits with separators), clipping the leftmost digits
            // off the edge of the SVG canvas. Widens to fit the largest grid label.
            float axisLeft = AxisLeftMargin(max);
            float legendTop = height - LegendHeight(names, width - 20);
            // Category labels are capped at 15 chars and rotated -45deg (RotatedCategoryLabel);
            // at font-size 6 that needs ~38pt of vertical clearance plus the label's own
            // +8pt offset from axisBottom - the old fixed 26pt reservation was well short
            // of that, so long labels overlapped the legend text below them.
            float axisBottom = legendTop - 50;
            float axisTop = 20;
            float plotWidth = width - axisLeft - 10;
            float plotHeight = axisBottom - axisTop;

            sb.Append(GridLines(axisLeft, axisTop, width - 5, axisBottom, max));
            sb.Append($"<line x1=\"{F(axisLeft)}\" y1=\"{F(axisTop)}\" x2=\"{F(axisLeft)}\" y2=\"{F(axisBottom)}\" stroke=\"{GridColor}\" stroke-width=\"1\" />");
            sb.Append($"<line x1=\"{F(axisLeft)}\" y1=\"{F(axisBottom)}\" x2=\"{F(width - 5)}\" y2=\"{F(axisBottom)}\" stroke=\"{GridColor}\" stroke-width=\"1\" />");

            float step = cappedCategories.Length > 1 ? plotWidth / (cappedCategories.Length - 1) : 0;

            for (int s = 0; s < seriesValues.Count; s++)
            {
                string color = Palette[s % Palette.Length];
                var points = new List<(float x, float y)>();

                for (int c = 0; c < cappedCategories.Length; c++)
                {
                    double val = c < seriesValues[s].Length ? seriesValues[s][c] : 0;
                    float x = axisLeft + (cappedCategories.Length > 1 ? c * step : plotWidth / 2);
                    float y = axisBottom - (float)(val / max * plotHeight);
                    points.Add((x, y));
                }

                sb.Append($"<polyline points=\"{string.Join(" ", points.Select(p => $"{F(p.x)},{F(p.y)}"))}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"2\" stroke-linejoin=\"round\" />");

                foreach (var p in points)
                    sb.Append($"<circle cx=\"{F(p.x)}\" cy=\"{F(p.y)}\" r=\"2.5\" fill=\"{color}\" />");
            }

            for (int c = 0; c < cappedCategories.Length; c++)
            {
                float x = axisLeft + (cappedCategories.Length > 1 ? c * step : plotWidth / 2);
                sb.Append(RotatedCategoryLabel(x, axisBottom + 8, cappedCategories[c]));
            }

            AppendBottomLegend(sb, names, width, legendTop, width - 20);

            sb.Append("</svg>");
            return sb.ToString();
        }

        // Groups the smallest entries (by total across series) beyond `max` into a single
        // "Others" bucket, mirroring tradewebx's MAX_PIE_SLICES/MAX_BAR_CATEGORIES fold.
        private static (string[] labels, List<double[]> series) CapAndFold(string[] labels, List<double[]> series, int max)
        {
            if (labels.Length <= max)
                return (labels, series);

            var totals = new double[labels.Length];
            for (int i = 0; i < labels.Length; i++)
                foreach (var s in series)
                    totals[i] += i < s.Length ? s[i] : 0;

            var order = Enumerable.Range(0, labels.Length).OrderByDescending(i => totals[i]).ToArray();
            var keepIdx = order.Take(max - 1).OrderBy(i => i).ToList();
            var foldIdx = order.Skip(max - 1).ToList();

            var newLabels = keepIdx.Select(i => labels[i]).Concat(new[] { OthersLabel }).ToArray();
            var newSeries = series.Select(s =>
            {
                var kept = keepIdx.Select(i => i < s.Length ? s[i] : 0).ToList();
                double folded = foldIdx.Sum(i => i < s.Length ? s[i] : 0);
                kept.Add(folded);
                return kept.ToArray();
            }).ToList();

            return (newLabels, newSeries);
        }

        private const float LegendRowHeight = 11;
        // Long client/scrip names used to run to 25 chars each, meaning even one entry
        // could exceed a narrow chart's row width and force every single entry onto its
        // own row - a legend with many entries then ate nearly the whole chart height
        // (collapsing a pie's radius to near zero). Shorter entries fit 2+ per row.
        private const int LegendEntryMaxChars = 16;
        // Pie legend entries carry a "Name (12.3%)" suffix on top of the name (see
        // BuildPieChart) - the name itself is already shortened to PieLegendNameMaxChars
        // before the suffix is appended, so this just needs to be long enough not to
        // re-truncate that combined string.
        private const int PieLegendEntryMaxChars = 24;
        private const int PieLegendNameMaxChars = 12;

        private static void AppendBottomLegend(StringBuilder sb, string[] labels, float width, float top, float maxWidth, int maxChars = LegendEntryMaxChars)
        {
            float x = 10;
            float y = top + 10;

            for (int i = 0; i < labels.Length; i++)
            {
                string entry = Truncate(labels[i], maxChars);
                float entryWidth = 12 + entry.Length * 3.6f + 12;

                if (x + entryWidth > maxWidth)
                {
                    x = 10;
                    y += LegendRowHeight;
                }

                string color = Palette[i % Palette.Length];
                sb.Append($"<rect x=\"{F(x)}\" y=\"{F(y - 7)}\" width=\"7\" height=\"7\" fill=\"{color}\" />");
                sb.Append($"<text x=\"{F(x + 10)}\" y=\"{F(y)}\" font-size=\"6.5\" fill=\"{AxisTextColor}\">{Esc(entry)}</text>");

                x += entryWidth;
            }
        }

        // How many rows AppendBottomLegend will wrap `labels` onto at `maxWidth`, so the
        // caller can reserve exactly that much vertical space instead of a fixed
        // allowance that clips whenever a legend needs more than one row (which any
        // multi-series bar/line chart's legend, or a many-slice pie's, easily does).
        private static float LegendHeight(string[] labels, float maxWidth, int maxChars = LegendEntryMaxChars)
        {
            float x = 10;
            int rows = 1;

            foreach (var label in labels)
            {
                string entry = Truncate(label, maxChars);
                float entryWidth = 12 + entry.Length * 3.6f + 12;

                if (x + entryWidth > maxWidth)
                {
                    x = 10;
                    rows++;
                }

                x += entryWidth;
            }

            return 10 + rows * LegendRowHeight;
        }

        // How wide the y-axis gutter needs to be to fit its largest label (the top grid
        // line, formatted from `max`) without clipping - large Indian-formatted values
        // (crores+) can run well past the old fixed 42pt margin.
        private static float AxisLeftMargin(double max)
        {
            string label = IndianNumberFormat.Format(max, 0);
            float textWidth = label.Length * 6f * 0.6f; // matches GridLines' font-size="6"
            return Math.Max(42f, textWidth + 14f); // 4pt label-to-axis gap + 10pt buffer
        }

        private static string GridLines(float left, float top, float right, float bottom, double max)
        {
            var sb = new StringBuilder();
            int steps = 4;

            for (int i = 0; i <= steps; i++)
            {
                float y = top + (bottom - top) * i / steps;
                double value = max * (steps - i) / steps;

                sb.Append($"<line x1=\"{F(left)}\" y1=\"{F(y)}\" x2=\"{F(right)}\" y2=\"{F(y)}\" stroke=\"{GridColor}\" stroke-width=\"0.5\" stroke-opacity=\"0.5\" />");
                sb.Append($"<text x=\"{F(left - 4)}\" y=\"{F(y + 2)}\" font-size=\"6\" fill=\"{AxisTextColor}\" text-anchor=\"end\">{Esc(IndianNumberFormat.Format(value, 0))}</text>");
            }

            return sb.ToString();
        }

        private static string RotatedCategoryLabel(float x, float y, string text)
        {
            return $"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"6\" fill=\"{AxisTextColor}\" text-anchor=\"end\" transform=\"rotate(-45 {F(x)} {F(y)})\">{Esc(Truncate(text, 15))}</text>";
        }

        private static string Header(float width, float height)
        {
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(width)}\" height=\"{F(height)}\" viewBox=\"0 0 {F(width)} {F(height)}\">";
        }

        private static string TitleText(string title, float width)
        {
            if (string.IsNullOrWhiteSpace(title)) return "";
            int maxChars = Math.Max(10, (int)(width / 4.5f));
            return $"<text x=\"{F(width / 2)}\" y=\"12\" font-size=\"9\" font-weight=\"bold\" fill=\"{AxisTextColor}\" text-anchor=\"middle\">{Esc(Truncate(title, maxChars))}</text>";
        }

        private static string NoDataText(float width, float height)
        {
            return $"<text x=\"{F(width / 2)}\" y=\"{F(height / 2)}\" font-size=\"8\" fill=\"#999\" text-anchor=\"middle\">No data</text>";
        }

        private static (float, float) PointOnCircle(float cx, float cy, float r, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            return (cx + r * (float)Math.Cos(rad), cy + r * (float)Math.Sin(rad));
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max - 1) + "…");

        private static string Esc(string s) => System.Security.SecurityElement.Escape(s ?? "");

        private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static string F1(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
