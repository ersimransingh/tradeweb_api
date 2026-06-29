using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using Newtonsoft.Json.Linq;

namespace TradeWeb.API.QuestPdfServicesClass
{

    public static class QuestPdfGeneratorByJson
    {
        public static void Generate(string jsonFilePath, string outputFilePath)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var json = File.ReadAllText(jsonFilePath);
            var root = JsonDocument.Parse(json).RootElement;
            var jsonObject = JObject.Parse(json);

            //new JsonPdfDocument(root).GeneratePdf(outputFilePath);
            var document = new DynamicDocument(jsonObject);

            document.GeneratePdf("dynamic.pdf");

            Console.WriteLine("[JsonPdfGenerator] PDF saved → " + outputFilePath);
        }

        public static byte[] GenerateBytes(string jsonFilePath)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var json = File.ReadAllText(jsonFilePath);
            var root = JsonDocument.Parse(json).RootElement;

            //return new JsonPdfDocument(root).GeneratePdf();
            var doc = new JsonPdfDocument(root);

            using (var ms = new MemoryStream())
            {
                doc.GeneratePdf(ms);
                return ms.ToArray();
            }
        }
    }

    internal class JsonPdfDocument : IDocument
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

            public ResolvedCell(string value, JsonElement style, int colspan, int rowspan)
            {
                Value = value;
                Style = style;
                Colspan = colspan;
                Rowspan = rowspan;
            }
        }

        private readonly JsonElement _root;

        public JsonPdfDocument(JsonElement root)
        {
            _root = root;
        }

        public DocumentMetadata GetMetadata()
        {
            return DocumentMetadata.Default;
        }

        public void Compose(IDocumentContainer container)
        {
            var docCfg = J(_root, "document");
            var sections = J(_root, "sections");

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);

                page.DefaultTextStyle(x => x
                    .FontFamily(S(docCfg, "defaultFont", "Helvetica"))
                    .FontSize((float)N(docCfg, "defaultFontSize", 8)));

                page.Content().Column(col =>
                {
                    col.Spacing(0);

                    if (sections.ValueKind != JsonValueKind.Undefined)
                    {
                        foreach (var sec in sections.EnumerateArray())
                        {
                            col.Item().Element(c => Render(c, sec));
                        }
                    }
                });
            });
        }

        private void Render(IContainer container, JsonElement sec)
        {
            var type = S(sec, "type", "text");

            if (type == "text")
                RenderText(container, sec);
            else if (type == "table")
                RenderTable(container, sec);
            else if (type == "spacer")
                container.Height((float)N(sec, "height", 5));
        }

        private static void RenderText(IContainer container, JsonElement sec)
        {
            var value = S(sec, "value", "");

            container.Text(value);
        }

        private static void RenderTable(IContainer container, JsonElement sec)
        {
            var rows = Arr(sec, "rows");

            if (rows.ValueKind == JsonValueKind.Undefined)
                return;

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    int count = ResolveColumnCount(rows);

                    for (int i = 0; i < count; i++)
                        columns.RelativeColumn();
                });

                foreach (var row in rows.EnumerateArray())
                {
                    var cells = Arr(row, "cells");
                    if (cells.ValueKind == JsonValueKind.Undefined)
                        continue;

                    foreach (var cell in cells.EnumerateArray())
                    {
                        table.Cell().Element(c =>
                        {
                            c.Border(1);
                            c.Padding(2);

                            c.Text(S(cell, "value", ""));
                        });
                    }
                }
            });
        }

        private static int ResolveColumnCount(JsonElement rows)
        {
            foreach (var row in rows.EnumerateArray())
            {
                var cells = Arr(row, "cells");
                if (cells.ValueKind == JsonValueKind.Array)
                {
                    int count = 0;

                    foreach (var _ in cells.EnumerateArray())
                        count++;

                    return count;
                }
            }

            return 1;
        }

        // Helpers

        private static JsonElement J(JsonElement e, string k)
        {
            if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v))
                return v;

            return default(JsonElement);
        }

        private static string S(JsonElement e, string k, string d)
        {
            var v = J(e, k);
            return v.ValueKind == JsonValueKind.String ? v.GetString() : d;
        }

        private static double N(JsonElement e, string k, double d)
        {
            var v = J(e, k);
            return v.ValueKind == JsonValueKind.Number ? v.GetDouble() : d;
        }

        private static JsonElement Arr(JsonElement e, string k)
        {
            var v = J(e, k);
            return v.ValueKind == JsonValueKind.Array ? v : default(JsonElement);
        }

        public DocumentSettings GetSettings()
        {
            return DocumentSettings.Default;
        }
    }


}
