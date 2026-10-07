using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;

namespace TradeWeb.API.Repository
{
    public class LetterTemplate : IDocument
    {
        private readonly LetterData _data;
        //private readonly TemplateData _template;
        private string logoPath;
        private string headerImagePath;

        public LetterTemplate(LetterData data, string headerImage, string logoImagePath)
        {
            _data = data;
            logoPath = logoImagePath;
            headerImagePath = headerImage;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        private static byte[] LoadImage(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            catch { /* ignore image errors */ }
            return null;
        }

        public DocumentSettings GetSettings()
        {
            return DocumentSettings.Default;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                // ================= HEADER =================
                page.Header().Column(column =>
                    {
                        column.Spacing(3);

                        // Logo and Company name + address
                        column.Item().Row(row =>
                        {
                            var headerImageBytes = LoadImage(headerImagePath);
                            var logoImageBytes = LoadImage(logoPath);

                            if (logoImageBytes != null)
                            {
                                row.ConstantItem(80).Height(50).Image(logoImageBytes).FitArea();
                            }

                            if (headerImageBytes != null)
                            {
                                // Letterhead banner beside the logo (8pt gutter after the logo box)
                                row.RelativeItem().Element(container =>
                                {
                                    container
                                        .PaddingLeft(8)
                                        .MaxWidth(490)
                                        .MaxHeight(105)
                                        .Image(headerImageBytes);
                                });
                            }
                            else
                            {
                                // CENTER: Company Details
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().AlignCenter()
                                            .Text(_data.CompanyName)
                                            .SemiBold().FontSize(13);

                                        col.Item().AlignCenter().Text(_data.Address1).FontSize(10);
                                        col.Item().AlignCenter().Text(_data.Address2).FontSize(10);
                                        col.Item().AlignCenter().Text(_data.Address3).FontSize(10);
                                        col.Item().AlignCenter().Text(_data.CINNo).FontSize(10);
                                        col.Item().AlignCenter().Text(_data.ReportName).FontSize(10).SemiBold();
                                    });
                                    // RIGHT EMPTY SPACE (for balance)
                                    row.ConstantItem(80);
                                }
                            });
                    });


                // ================= CONTENT =================
                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(5);

                    column.Item().Text(text =>
                    {
                        text.Line("To,");
                        text.Line(_data.Client + "[" + _data.ClientCode + "]");
                        text.Line(_data.ClientAddress1);
                        text.Line(_data.ClientAddress2);
                        text.Line(_data.ClientAddress3);
                        text.Line("Pin: " + _data.Clientpin);
                        text.Line("Mobile: " + _data.Telephone);
                    });

                    column.Item().PaddingTop(15)
                        .Text(_data.Subject);

                    column.Item().PaddingTop(10).Text(text =>
                    {
                        text.Line(_data.HeaderText);
                        text.Line("");
                        text.Line("");
                        text.Line("");
                        text.Line(_data.BodyText);
                    });

                    column.Item().PaddingTop(25).Text(text =>
                    {
                        text.Line(_data.FooterText);
                    });
                });

                // ================= FOOTER =================
                page.Footer().Row(row =>
                {
                    row.RelativeItem().AlignLeft()
                        .Text($"Print Date : {DateTime.Now:dd/MM/yyyy}");

                    row.RelativeItem().AlignRight()
                        .Text(text =>
                        {
                            text.Span(_data.FooterModuleName + " | Page ");
                            text.CurrentPageNumber();
                        });
                });
            });

        }
    }
}
