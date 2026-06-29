using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using TradeWeb.API.Repository;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TradeWeb.API.QuestPdfServicesClass
{
    public class DynamicDocument : IDocument
    {
        private readonly JObject _data;

        public DynamicDocument(JObject data)
        {
            _data = data;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        // ✅ HEADER
        void ComposeHeader(IContainer container)
        {
            container.AlignCenter().Text("Dynamic PDF Report")
                .FontSize(20).Bold().FontColor(Colors.Blue.Medium);
        }

        // ✅ CONTENT (Body + Tables)
        void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                foreach (var property in _data.Properties())
                {
                    if (property.Value.Type == JTokenType.Array)
                    {
                        var array = (JArray)property.Value;

                        if (array.Count > 0 && array.First.Type == JTokenType.Object)
                        {
                            // Table Section
                            column.Item().PaddingTop(10)
                                .Text(property.Name)
                                .Bold().FontSize(14);

                            column.Item().Element(c => ComposeTable(c, array));
                        }
                    }
                    else
                    {
                        // Key-Value Body Section
                        column.Item().Row(row =>
                        {
                            row.RelativeColumn(1)
                               .Text(property.Name).SemiBold();

                            row.RelativeColumn(2)
                               .Text(property.Value.ToString());
                        });
                    }
                }
            });
        }

        // ✅ TABLE BUILDER
        void ComposeTable(IContainer container, JArray array)
        {
            var firstObj = (JObject)array.First;

            container.Table(table =>
            {
                // Columns
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in firstObj.Properties())
                        columns.RelativeColumn();
                });

                // Header
                table.Header(header =>
                {
                    foreach (var prop in firstObj.Properties())
                    {
                        header.Cell()
                            .Background(Colors.Grey.Lighten2)
                            .Padding(5)
                            .Text(prop.Name).Bold();
                    }
                });

                // Rows
                foreach (JObject obj in array)
                {
                    foreach (var prop in obj.Properties())
                    {
                        table.Cell()
                            .Padding(5)
                            .Text(prop.Value.ToString());
                    }
                }
            });
        }

        // ✅ FOOTER
        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
            });
        }

        public DocumentSettings GetSettings()
        {
            return DocumentSettings.Default;
        }
    }
}
