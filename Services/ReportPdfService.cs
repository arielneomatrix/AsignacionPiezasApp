using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// QuestPDF
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Alias para evitar choque con System.Reflection.Metadata.Document
using Document = QuestPDF.Fluent.Document;
using IContainer = QuestPDF.Infrastructure.IContainer;

using AsignacionPiezasApp.Models;


namespace AsignacionPiezasApp.Services
{
    // NUEVO: Generación de PDF para informes
    public static class ReportPdfService
    {
        public class ReportRow
        {
            public string Codigo { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public string Usuario { get; set; } = "";
            public string Estatus { get; set; } = "";
            public string Fecha { get; set; } = "";
        }

        public class ReportInfo
        {
            public string Titulo { get; set; } = "Informe de piezas";
            public string FiltrosAplicados { get; set; } = "";
            public DateTime GeneradoEl { get; set; } = DateTime.Now;
        }

        public static void GeneratePiezasReportPdf(string filePath, IEnumerable<ReportRow> rows, ReportInfo info)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var data = rows?.ToList() ?? new List<ReportRow>();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Column(col =>
                    {
                        col.Item().Text(info?.Titulo ?? "Informe de piezas").Bold().FontSize(18);

                        var filtros = info?.FiltrosAplicados;
                        if (!string.IsNullOrWhiteSpace(filtros))
                            col.Item().Text(filtros).FontSize(10).FontColor(Colors.Grey.Darken2);

                        col.Item().Text($"Generado: {(info?.GeneradoEl ?? DateTime.Now):yyyy-MM-dd HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2); // Código
                            cols.RelativeColumn(4); // Descripción
                            cols.RelativeColumn(3); // Usuario
                            cols.RelativeColumn(3); // Estatus
                            cols.RelativeColumn(3); // Fecha
                        });

                        // Encabezados
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Código");
                            header.Cell().Element(HeaderCell).Text("Descripción");
                            header.Cell().Element(HeaderCell).Text("Usuario");
                            header.Cell().Element(HeaderCell).Text("Estatus");
                            header.Cell().Element(HeaderCell).Text("Fecha");
                        });

                        // Filas
                        foreach (var r in data)
                        {
                            table.Cell().Element(BodyCell).Text(r?.Codigo ?? "");
                            table.Cell().Element(BodyCell).Text(r?.Descripcion ?? "");
                            table.Cell().Element(BodyCell).Text(r?.Usuario ?? "");
                            table.Cell().Element(BodyCell).Text(r?.Estatus ?? "");
                            table.Cell().Element(BodyCell).Text(r?.Fecha ?? "");
                        }
                    });

                    // Footer: API correcta (TextDescriptor tiene CurrentPageNumber / TotalPages)
                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);

            // 👉 Helpers de estilo (sin tipos internos de QuestPDF)
            static IContainer HeaderCell(IContainer c) =>
                c.DefaultTextStyle(x => x.SemiBold())
                 .PaddingVertical(6)
                 .BorderBottom(1)
                 .BorderColor(Colors.Grey.Medium);

            static IContainer BodyCell(IContainer c) =>
                c.PaddingVertical(5)
                 .BorderBottom(0.5f)
                 .BorderColor(Colors.Grey.Lighten2);
        }

    }
}
