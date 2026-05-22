using CellApi.DTOs;
using CellApi.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using ZXing;
using ZXing.QrCode;
using ZXing.Rendering;

namespace CellApi.Services;

public class PdfService : IPdfService
{
    private readonly IWebHostEnvironment      _env;
    private readonly IConfiguracionRepository _configRepo;
    private readonly ILogger<PdfService>      _logger;

    public PdfService(
        IWebHostEnvironment      env,
        IConfiguracionRepository configRepo,
        ILogger<PdfService>      logger)
    {
        _env        = env;
        _configRepo = configRepo;
        _logger     = logger;
    }

    // ════════════════════════════════════════════════════════════════
    // FACTURA
    // ════════════════════════════════════════════════════════════════

    public async Task<string> GenerarFacturaPdfAsync(FacturaDto factura)
    {
        var config  = await GetConfigAsync();
        var formato = config.GetValueOrDefault("ticket_formato", "a4");
        var logo    = await GetLogoAsync(config);
        var pie     = config.GetValueOrDefault("factura_pie_texto", "");

        var outputDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "facturas");
        Directory.CreateDirectory(outputDir);

        var fileName = $"Factura_{factura.NumeroFactura.Replace("-", "_").Replace("/", "-")}.pdf";
        var filePath = Path.Combine(outputDir, fileName);

        byte[] pdfBytes = formato switch
        {
            "ticket_80mm" => GenerarFacturaTermica(config, factura, 80f, pie, logo),
            "ticket_58mm" => GenerarFacturaTermica(config, factura, 58f, pie, logo),
            _             => GenerarFacturaA4(config, factura, pie, logo)
        };

        await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);
        _logger.LogInformation("Factura PDF generada: {FilePath}", filePath);
        return filePath;
    }

    public async Task<byte[]> GenerarFacturaPdfBytesAsync(FacturaDto factura, string? formatoOverride = null)
    {
        var config  = await GetConfigAsync();
        var formato = formatoOverride ?? config.GetValueOrDefault("ticket_formato", "a4");
        var logo    = await GetLogoAsync(config);
        var pie     = config.GetValueOrDefault("factura_pie_texto", "");
        return formato switch
        {
            "ticket_80mm" => GenerarFacturaTermica(config, factura, 80f, pie, logo),
            "ticket_58mm" => GenerarFacturaTermica(config, factura, 58f, pie, logo),
            _             => GenerarFacturaA4(config, factura, pie, logo)
        };
    }

    // ── Factura A4 ──────────────────────────────────────────────────
    private byte[] GenerarFacturaA4(
        Dictionary<string, string> config, FacturaDto factura,
        string pie, byte[]? logo)
    {
        var ivaFactor = 1m + factura.PorcentajeIva / 100m;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    // Cabecera empresa
                    CabeceraPagina(col, config, "FACTURA",
                        factura.NumeroFactura,
                        factura.FechaEmision.ToString("dd/MM/yyyy"),
                        logo);

                    // Cliente (izquierda) + Resumen fiscal (derecha)
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(10).Column(c =>
                            {
                                c.Item().Text("DATOS DEL CLIENTE")
                                    .Bold().FontSize(8).FontColor(Colors.Blue.Darken2);
                                c.Item().PaddingTop(4)
                                    .Text(factura.ClienteNombreCompleto ?? "—").Bold().FontSize(11);
                                if (!string.IsNullOrEmpty(factura.ClienteNif))
                                    c.Item().PaddingTop(2)
                                        .Text($"NIF/CIF: {factura.ClienteNif}").FontSize(9);
                                if (!string.IsNullOrEmpty(factura.ClienteDireccion))
                                    c.Item().Text(factura.ClienteDireccion).FontSize(9);
                                if (!string.IsNullOrEmpty(factura.ClienteEmail))
                                    c.Item().Text(factura.ClienteEmail).FontSize(9);
                            });

                        row.ConstantItem(10);

                        row.RelativeItem().Border(1).BorderColor(Colors.Blue.Darken2)
                            .Padding(10).Column(c =>
                            {
                                c.Item().Text("DATOS DE LA FACTURA")
                                    .Bold().FontSize(8).FontColor(Colors.Blue.Darken2);
                                c.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Text("Número:").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    r.AutoItem().Text(factura.NumeroFactura).Bold().FontSize(9);
                                });
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Fecha:").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    r.AutoItem().Text(factura.FechaEmision.ToString("dd/MM/yyyy")).FontSize(9);
                                });
                                if (factura.ReparacionId.HasValue)
                                    c.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Orden rep.:").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        r.AutoItem().Text($"#{factura.ReparacionId}").FontSize(9);
                                    });
                                c.Item().PaddingTop(6).Row(r =>
                                {
                                    r.RelativeItem().Text("Base imponible:").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    r.AutoItem().Text($"{factura.BaseImponible:F2} €").FontSize(9);
                                });
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text($"IVA ({factura.PorcentajeIva:0}%):").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    r.AutoItem().Text($"{factura.ImporteIva:F2} €").FontSize(9);
                                });
                            });
                    });

                    col.Item().PaddingTop(14);

                    // Tabla de líneas — precios CON IVA (PVP)
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(5);
                            cols.ConstantColumn(40);
                            cols.ConstantColumn(85);
                            cols.ConstantColumn(85);
                        });

                        table.Header(header =>
                        {
                            static IContainer H(IContainer c) =>
                                c.Background(Colors.Blue.Darken2).Padding(7)
                                 .DefaultTextStyle(t => t.FontColor(Colors.White).Bold().FontSize(9));
                            header.Cell().Element(H).Text("Descripción");
                            header.Cell().Element(H).AlignCenter().Text("Cant.");
                            header.Cell().Element(H).AlignRight().Text("Precio c/IVA");
                            header.Cell().Element(H).AlignRight().Text("Total c/IVA");
                        });

                        bool par = false;
                        foreach (var linea in factura.Lineas)
                        {
                            var bg      = par ? Colors.Grey.Lighten4 : Colors.White;
                            par         = !par;
                            var pvpUnit = Math.Round(linea.PrecioUnitario * ivaFactor, 2);
                            var pvpTot  = Math.Round(linea.Subtotal * ivaFactor, 2);

                            static IContainer R(IContainer c, string bg) => c.Background(bg).Padding(6);
                            table.Cell().Element(c => R(c, bg)).Text(linea.Descripcion);
                            table.Cell().Element(c => R(c, bg)).AlignCenter().Text(linea.Cantidad.ToString());
                            table.Cell().Element(c => R(c, bg)).AlignRight().Text($"{pvpUnit:F2} €");
                            table.Cell().Element(c => R(c, bg)).AlignRight().Text($"{pvpTot:F2} €");
                        }
                    });

                    col.Item().PaddingTop(2).AlignRight()
                        .Text("* Precios con IVA incluido.")
                        .FontSize(7).FontColor(Colors.Grey.Darken1).Italic();

                    // Bloque total
                    col.Item().PaddingTop(10).AlignRight().Width(265).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });

                        t.Cell().Padding(5).Text("Base imponible:").FontSize(9).FontColor(Colors.Grey.Darken2);
                        t.Cell().Padding(5).AlignRight().Text($"{factura.BaseImponible:F2} €").FontSize(9);

                        t.Cell().Padding(5).Text($"IVA ({factura.PorcentajeIva:0}%):").FontSize(9).FontColor(Colors.Grey.Darken2);
                        t.Cell().Padding(5).AlignRight().Text($"{factura.ImporteIva:F2} €").FontSize(9);

                        t.Cell().ColumnSpan(2).LineHorizontal(2).LineColor(Colors.Blue.Darken2);

                        static IContainer Tot(IContainer c) =>
                            c.Background(Colors.Blue.Darken2).Padding(9)
                             .DefaultTextStyle(s => s.FontColor(Colors.White).Bold().FontSize(13));
                        t.Cell().Element(Tot).Text("TOTAL  (IVA incluido)");
                        t.Cell().Element(Tot).AlignRight().Text($"{factura.Total:F2} €");
                    });

                    // QR digital
                    var urlPublicaF = config.GetValueOrDefault("empresa_url_publica", "").TrimEnd('/');
                    if (!string.IsNullOrEmpty(urlPublicaF))
                    {
                        var qrUrlF = $"{urlPublicaF}/api/facturas/{factura.Id}/pdf";
                        var qrPngF = GenerarQrPng(qrUrlF, 150);
                        col.Item().PaddingTop(14).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Ver factura digital").FontSize(9).FontColor(Colors.Grey.Darken2);
                                c.Item().Text(qrUrlF).FontSize(7).FontColor(Colors.Grey.Darken1);
                            });
                            r.ConstantItem(28, Unit.Millimetre).Image(qrPngF);
                        });
                    }

                    if (!string.IsNullOrEmpty(pie))
                    {
                        col.Item().PaddingTop(20).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(4).Text(pie)
                            .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                    }
                });
            });
        }).GeneratePdf();
    }

    // ── Factura térmica ─────────────────────────────────────────────
    private byte[] GenerarFacturaTermica(
        Dictionary<string, string> config, FacturaDto factura,
        float anchoPapelMm, string pie, byte[]? logo)
    {
        var empresaNombre = config.GetValueOrDefault("empresa_nombre",   "CellShop");
        var empresaTel    = config.GetValueOrDefault("empresa_telefono", "");
        var empresaEmail  = config.GetValueOrDefault("empresa_email",    "");
        var ivaFactor     = 1m + factura.PorcentajeIva / 100m;
        float fs    = anchoPapelMm >= 70 ? 8f : 7f;
        float fsBig = fs + 2;
        float fsMed = fs + 1;
        float fsTiny= Math.Max(6f, fs - 1);

        var urlPubFact = config.GetValueOrDefault("empresa_url_publica", "").TrimEnd('/');
        float altoFacturaMm = 45f
            + (logo != null ? 14f : 0f)
            + (!string.IsNullOrEmpty(empresaTel) ? 4f : 0f)
            + (!string.IsNullOrEmpty(empresaEmail) ? 4f : 0f)
            + (!string.IsNullOrEmpty(factura.ClienteNombreCompleto) ? 14f : 0f)
            + factura.Lineas.Count * 7f
            + 22f
            + (!string.IsNullOrEmpty(urlPubFact) ? 28f : 0f)
            + (!string.IsNullOrEmpty(pie) ? 12f : 0f)
            + 10f;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(anchoPapelMm, altoFacturaMm, Unit.Millimetre);
                page.Margin(3, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(fs));

                page.Content().Column(col =>
                {
                    // Logo
                    if (logo != null)
                    {
                        col.Item().AlignCenter()
                           .Width(Math.Min(anchoPapelMm * 0.45f, 22), Unit.Millimetre)
                           .Image(logo);
                        col.Item().PaddingBottom(2);
                    }

                    col.Item().AlignCenter().Text(empresaNombre).Bold().FontSize(fsBig);
                    if (!string.IsNullOrEmpty(empresaTel))
                        col.Item().AlignCenter().Text(empresaTel).FontSize(fsTiny);
                    if (!string.IsNullOrEmpty(empresaEmail))
                        col.Item().AlignCenter().Text(empresaEmail).FontSize(fsTiny);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);

                    col.Item().AlignCenter().Text("FACTURA").Bold().FontSize(fsMed);
                    col.Item().AlignCenter().Text(factura.NumeroFactura).Bold().FontSize(fsBig);
                    col.Item().AlignCenter().Text(factura.FechaEmision.ToString("dd/MM/yyyy")).FontSize(fsTiny);

                    // Cliente
                    if (!string.IsNullOrEmpty(factura.ClienteNombreCompleto))
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().Text("CLIENTE").Bold().FontSize(fs);
                        col.Item().Text(factura.ClienteNombreCompleto).FontSize(fs);
                        if (!string.IsNullOrEmpty(factura.ClienteNif))
                            col.Item().Text($"NIF: {factura.ClienteNif}").FontSize(fsTiny);
                    }

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);

                    // Líneas — precios CON IVA (PVP)
                    foreach (var linea in factura.Lineas)
                    {
                        var pvpTot = Math.Round(linea.Subtotal * ivaFactor, 2);
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(linea.Descripcion).FontSize(fs);
                            r.AutoItem().AlignRight().Text($"{pvpTot:F2} €").FontSize(fs);
                        });
                        if (linea.Cantidad != 1)
                        {
                            var pvpUnit = Math.Round(linea.PrecioUnitario * ivaFactor, 2);
                            col.Item().Text($"  {linea.Cantidad} × {pvpUnit:F2} €")
                               .FontSize(fsTiny).FontColor(Colors.Grey.Darken1);
                        }
                    }

                    col.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Black);

                    // Desglose fiscal
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Base imponible:").FontSize(fsTiny);
                        r.AutoItem().AlignRight().Text($"{factura.BaseImponible:F2} €").FontSize(fsTiny);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"IVA {factura.PorcentajeIva:0}%:").FontSize(fsTiny);
                        r.AutoItem().AlignRight().Text($"{factura.ImporteIva:F2} €").FontSize(fsTiny);
                    });
                    col.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL (IVA incl.):").Bold().FontSize(fsMed);
                        r.AutoItem().AlignRight().Text($"{factura.Total:F2} €").Bold().FontSize(fsMed);
                    });

                    // QR digital (si hay URL pública configurada)
                    if (!string.IsNullOrEmpty(urlPubFact))
                    {
                        var qrUrlFT = $"{urlPubFact}/api/facturas/{factura.Id}/pdf";
                        var qrPngFT = GenerarQrPng(qrUrlFT, 120);
                        col.Item().PaddingTop(4).AlignCenter().Width(22, Unit.Millimetre).Image(qrPngFT);
                        col.Item().AlignCenter().Text("Ver factura digital").FontSize(fsTiny);
                    }

                    if (!string.IsNullOrEmpty(pie))
                    {
                        col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().AlignCenter().Text(pie).FontSize(fsTiny).FontColor(Colors.Grey.Darken1).Italic();
                    }
                });
            });
        }).GeneratePdf();
    }

    // ════════════════════════════════════════════════════════════════
    // ETIQUETA REPARACIÓN (90×55 mm — sin QR)
    // ════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerarEtiquetaReparacionPdfAsync(ReparacionDto rep)
    {
        var falla = rep.DescripcionFalla.Length > 90
            ? rep.DescripcionFalla[..87] + "..."
            : rep.DescripcionFalla;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(90, 55, Unit.Millimetre);
                page.Margin(3, Unit.Millimetre);
                page.DefaultTextStyle(x => x.Bold());

                page.Content().Column(col =>
                {
                    col.Item().Text(rep.NumeroOrden)
                        .Bold().FontSize(20).FontColor(Colors.Black);

                    col.Item().PaddingTop(2).Text(rep.ClienteNombreCompleto ?? "")
                        .Bold().FontSize(13).FontColor(Colors.Blue.Darken4);

                    if (!string.IsNullOrEmpty(rep.ClienteTelefono))
                        col.Item().PaddingTop(1).Text(rep.ClienteTelefono)
                            .Bold().FontSize(16).FontColor(Colors.Black);

                    col.Item().PaddingTop(1)
                        .Text(rep.FechaRecepcion.ToString("dd/MM/yyyy"))
                        .Bold().FontSize(9).FontColor(Colors.Grey.Darken4);

                    var precio = rep.PrecioFinal ?? rep.PrecioEstimado;
                    if (precio.HasValue)
                        col.Item().PaddingTop(1)
                            .Text($"{precio.Value:N2} €")
                            .Bold().FontSize(13).FontColor(Colors.Green.Darken4);

                    col.Item().PaddingTop(2).Text(falla)
                        .Bold().FontSize(11).FontColor(Colors.Black);
                });
            });
        }).GeneratePdf();
    }

    // ════════════════════════════════════════════════════════════════
    // ORDEN DE REPARACIÓN
    // ════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerarOrdenReparacionPdfAsync(ReparacionDto rep, string? formatoOverride = null)
    {
        var config    = await GetConfigAsync();
        var formato   = formatoOverride ?? config.GetValueOrDefault("ticket_formato", "a4");
        var clausRep  = config.GetValueOrDefault("ticket_clausula_reparacion", "");
        var clausRec  = config.GetValueOrDefault("ticket_clausula_recogida",   "");
        var mostrarQr = config.GetValueOrDefault("ticket_mostrar_qr", "true")
                              .Equals("true", StringComparison.OrdinalIgnoreCase);
        var pie       = config.GetValueOrDefault("factura_pie_texto", "");
        var logo      = await GetLogoAsync(config);

        return formato switch
        {
            "ticket_80mm" => GenerarOrdenReparacionTermica(config, rep, 80f, clausRep, clausRec, mostrarQr, pie, logo),
            "ticket_58mm" => GenerarOrdenReparacionTermica(config, rep, 58f, clausRep, clausRec, mostrarQr, pie, logo),
            _             => GenerarOrdenReparacionA4(config, rep, clausRep, clausRec, mostrarQr, pie, logo)
        };
    }

    private byte[] GenerarOrdenReparacionA4(
        Dictionary<string, string> config, ReparacionDto rep,
        string clausRep, string clausRec, bool mostrarQr, string pie, byte[]? logo)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    CabeceraPagina(col, config, "ORDEN DE REPARACIÓN",
                        rep.NumeroOrden,
                        rep.FechaRecepcion.ToString("dd/MM/yyyy"),
                        logo);

                    // Cliente
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("CLIENTE").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text(rep.ClienteNombreCompleto ?? "").Bold();
                        if (!string.IsNullOrEmpty(rep.ClienteTelefono))
                            c.Item().Text($"Tel: {rep.ClienteTelefono}").FontSize(9);
                        if (!string.IsNullOrEmpty(rep.ClienteEmail))
                            c.Item().Text(rep.ClienteEmail).FontSize(9);
                    });

                    col.Item().PaddingTop(10);

                    // Dispositivo
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("DISPOSITIVO").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Column(ci =>
                            {
                                ci.Item().Text($"Tipo: {rep.Dispositivo}");
                                if (!string.IsNullOrEmpty(rep.Marca) || !string.IsNullOrEmpty(rep.Modelo))
                                    ci.Item().Text($"Marca/Modelo: {rep.Marca} {rep.Modelo}".Trim());
                                if (!string.IsNullOrEmpty(rep.Imei))
                                    ci.Item().Text($"IMEI/S/N: {rep.Imei}");
                            });
                            r.RelativeItem().Column(ci =>
                            {
                                ci.Item().AlignRight().Text($"Estado: {EstadoLabel(rep.Estado)}").Bold();
                                ci.Item().AlignRight().Text($"Prioridad: {rep.Prioridad}");
                                if (!string.IsNullOrEmpty(rep.TecnicoAsignado))
                                    ci.Item().AlignRight().Text($"Técnico: {rep.TecnicoAsignado}");
                                if (rep.FechaEstimadaEntrega.HasValue)
                                    ci.Item().AlignRight().Text($"Entrega est.: {rep.FechaEstimadaEntrega:dd/MM/yyyy}");
                            });
                        });
                    });

                    col.Item().PaddingTop(10);

                    // Descripción falla
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("DESCRIPCIÓN DE LA FALLA").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text(rep.DescripcionFalla);
                        if (!string.IsNullOrEmpty(rep.ObservacionesTecnico))
                        {
                            c.Item().PaddingTop(4).Text("OBSERVACIONES DEL TÉCNICO")
                             .Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                            c.Item().Text(rep.ObservacionesTecnico);
                        }
                    });

                    // Repuestos
                    if (rep.Detalles.Any())
                    {
                        col.Item().PaddingTop(10);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(5); cols.RelativeColumn(1);
                                cols.RelativeColumn(2); cols.RelativeColumn(2);
                            });
                            table.Header(header =>
                            {
                                static IContainer H(IContainer c) =>
                                    c.Background(Colors.Blue.Darken2).Padding(5)
                                     .DefaultTextStyle(t => t.FontColor(Colors.White).Bold().FontSize(9));
                                header.Cell().Element(H).Text("Repuesto / Servicio");
                                header.Cell().Element(H).AlignCenter().Text("Cant.");
                                header.Cell().Element(H).AlignRight().Text("Precio unit.");
                                header.Cell().Element(H).AlignRight().Text("Subtotal");
                            });
                            bool par = false;
                            foreach (var d in rep.Detalles)
                            {
                                var bg = par ? Colors.Grey.Lighten4 : Colors.White;
                                par = !par;
                                static IContainer R(IContainer c, string bg) => c.Background(bg).Padding(5);
                                table.Cell().Element(c => R(c, bg)).Text(d.Descripcion);
                                table.Cell().Element(c => R(c, bg)).AlignCenter().Text(d.Cantidad.ToString());
                                table.Cell().Element(c => R(c, bg)).AlignRight().Text($"{d.PrecioUnitario:F2} €");
                                table.Cell().Element(c => R(c, bg)).AlignRight().Text($"{d.Subtotal:F2} €");
                            }
                        });

                        col.Item().PaddingTop(8).AlignRight().Width(220).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                            if (rep.BaseImponible.HasValue)
                            {
                                t.Cell().Padding(3).Text("Base imponible:");
                                t.Cell().Padding(3).AlignRight().Text($"{rep.BaseImponible:F2} €");
                                t.Cell().Padding(3).Text($"IVA ({rep.PorcentajeIva:0}%):");
                                t.Cell().Padding(3).AlignRight().Text($"{rep.ImporteIva:F2} €");
                                t.Cell().ColumnSpan(2).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                            }
                            t.Cell().Padding(3).Text("TOTAL:").Bold();
                            t.Cell().Padding(3).AlignRight().Text($"{rep.Total:F2} €").Bold();
                        });
                    }

                    // Precio estimado siempre visible aunque no haya detalles
                    if (rep.PrecioEstimado.HasValue && !rep.BaseImponible.HasValue && !rep.Detalles.Any())
                    {
                        col.Item().PaddingTop(10).AlignRight().Width(220).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                            t.Cell().Padding(3).Text("Precio estimado:").Bold();
                            t.Cell().Padding(3).AlignRight().Text($"{rep.PrecioEstimado:F2} €").Bold();
                        });
                    }

                    // Firmas
                    col.Item().PaddingTop(30).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            c.Item().AlignCenter().Text("Firma del cliente").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        r.ConstantItem(60);
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            c.Item().AlignCenter().Text("Firma del técnico").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    // Cláusulas
                    if (!string.IsNullOrEmpty(clausRep))
                    {
                        col.Item().PaddingTop(12).Column(c =>
                        {
                            c.Item().Text("CLÁUSULA DE REPARACIÓN").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                            c.Item().PaddingTop(2).Text(clausRep).FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                        });
                    }
                    if (!string.IsNullOrEmpty(clausRec))
                    {
                        col.Item().PaddingTop(8).Column(c =>
                        {
                            c.Item().Text("CONDICIONES DE RECOGIDA").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                            c.Item().PaddingTop(2).Text(clausRec).FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                        });
                    }

                    // QR (URL digital si está configurada, o número de orden)
                    if (mostrarQr)
                    {
                        var urlPublicaR = config.GetValueOrDefault("empresa_url_publica", "").TrimEnd('/');
                        var qrContentR  = string.IsNullOrEmpty(urlPublicaR)
                            ? rep.NumeroOrden
                            : $"{urlPublicaR}/api/reparaciones/{rep.Id}/pdf";
                        var qr = GenerarQrPng(qrContentR, 120);
                        col.Item().PaddingTop(8).AlignRight().Width(28, Unit.Millimetre).Image(qr);
                    }

                    if (!string.IsNullOrEmpty(pie))
                    {
                        col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(4).Text(pie).FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                    }
                });
            });
        }).GeneratePdf();
    }

    private byte[] GenerarOrdenReparacionTermica(
        Dictionary<string, string> config, ReparacionDto rep, float anchoPapelMm,
        string clausRep, string clausRec, bool mostrarQr, string pie, byte[]? logo)
    {
        var empresaNombre = config.GetValueOrDefault("empresa_nombre",   "CellShop");
        var empresaTel    = config.GetValueOrDefault("empresa_telefono", "");
        var empresaEmail  = config.GetValueOrDefault("empresa_email",    "");
        float fs    = anchoPapelMm >= 70 ? 8f : 7f;
        float fsBig = fs + 2;
        float fsMed = fs + 1;
        float fsTiny= Math.Max(6f, fs - 1);
        var urlPublicaRT = config.GetValueOrDefault("empresa_url_publica", "").TrimEnd('/');
        var qrContentRT  = !string.IsNullOrEmpty(urlPublicaRT)
            ? $"{urlPublicaRT}/api/reparaciones/{rep.Id}/pdf"
            : rep.NumeroOrden;
        byte[]? qrPng = mostrarQr ? GenerarQrPng(qrContentRT, 160) : null;

        float altoRepMm = 95f
            + (logo != null ? 14f : 0f)
            + (!string.IsNullOrEmpty(empresaTel) ? 4f : 0f)
            + (!string.IsNullOrEmpty(empresaEmail) ? 4f : 0f)
            + (!string.IsNullOrEmpty(rep.Imei) ? 4f : 0f)
            + (!string.IsNullOrEmpty(rep.TecnicoAsignado) ? 4f : 0f)
            + (rep.FechaEstimadaEntrega.HasValue ? 4f : 0f)
            + (!string.IsNullOrEmpty(rep.ObservacionesTecnico) ? 12f : 0f)
            + rep.Detalles.Count * 7f
            + 22f
            + (mostrarQr ? 30f : 0f)
            + (!string.IsNullOrEmpty(clausRep) ? Math.Min(clausRep.Length / 45f * 4f, 60f) : 0f)
            + (!string.IsNullOrEmpty(clausRec) ? Math.Min(clausRec.Length / 45f * 4f, 60f) : 0f)
            + (!string.IsNullOrEmpty(pie) ? 12f : 0f)
            + 12f;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(anchoPapelMm, altoRepMm, Unit.Millimetre);
                page.Margin(3, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(fs));

                page.Content().Column(col =>
                {
                    // Logo
                    if (logo != null)
                    {
                        col.Item().AlignCenter()
                           .Width(Math.Min(anchoPapelMm * 0.4f, 22), Unit.Millimetre)
                           .Image(logo);
                        col.Item().PaddingBottom(2);
                    }

                    col.Item().AlignCenter().Text(empresaNombre).Bold().FontSize(fsBig);
                    if (!string.IsNullOrEmpty(empresaTel))
                        col.Item().AlignCenter().Text(empresaTel).FontSize(fsTiny);
                    if (!string.IsNullOrEmpty(empresaEmail))
                        col.Item().AlignCenter().Text(empresaEmail).FontSize(fsTiny);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().AlignCenter().Text("ORDEN DE REPARACIÓN").Bold().FontSize(fsMed);
                    col.Item().AlignCenter().Text(rep.NumeroOrden).Bold().FontSize(fsBig);
                    col.Item().AlignCenter().Text(rep.FechaRecepcion.ToString("dd/MM/yyyy  HH:mm")).FontSize(fsTiny);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().Text("CLIENTE").Bold().FontSize(fs);
                    col.Item().Text(rep.ClienteNombreCompleto ?? "").FontSize(fs);
                    if (!string.IsNullOrEmpty(rep.ClienteTelefono))
                        col.Item().Text($"Tel: {rep.ClienteTelefono}").FontSize(fsTiny).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrEmpty(rep.ClienteEmail))
                        col.Item().Text(rep.ClienteEmail).FontSize(fsTiny).FontColor(Colors.Grey.Darken1);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().Text("DISPOSITIVO").Bold().FontSize(fs);
                    col.Item().Text($"{rep.Dispositivo} {rep.Marca} {rep.Modelo}".Trim()).FontSize(fs);
                    if (!string.IsNullOrEmpty(rep.Imei))
                        col.Item().Text($"IMEI/S/N: {rep.Imei}").FontSize(fsTiny);
                    col.Item().Text($"Estado: {EstadoLabel(rep.Estado)}  |  Prio: {rep.Prioridad}").FontSize(fsTiny);
                    if (!string.IsNullOrEmpty(rep.TecnicoAsignado))
                        col.Item().Text($"Técnico: {rep.TecnicoAsignado}").FontSize(fsTiny);
                    if (rep.FechaEstimadaEntrega.HasValue)
                        col.Item().Text($"Entrega est.: {rep.FechaEstimadaEntrega:dd/MM/yyyy}").FontSize(fsTiny);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().Text("DESCRIPCIÓN").Bold().FontSize(fs);
                    col.Item().Text(rep.DescripcionFalla).FontSize(fs);
                    if (!string.IsNullOrEmpty(rep.ObservacionesTecnico))
                    {
                        col.Item().PaddingTop(2).Text("OBS. TÉCNICO").Bold().FontSize(fsTiny);
                        col.Item().Text(rep.ObservacionesTecnico).FontSize(fsTiny);
                    }

                    // Repuestos
                    if (rep.Detalles.Any())
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().Text("REPUESTOS / SERVICIOS").Bold().FontSize(fs);
                        foreach (var d in rep.Detalles)
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{d.Descripcion} ×{d.Cantidad}").FontSize(fsTiny);
                                r.AutoItem().AlignRight().Text($"{d.Subtotal:F2} €").FontSize(fsTiny);
                            });
                        }
                        col.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Black);
                        if (rep.BaseImponible.HasValue)
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Base:").FontSize(fsTiny);
                                r.AutoItem().AlignRight().Text($"{rep.BaseImponible:F2} €").FontSize(fsTiny);
                            });
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"IVA {rep.PorcentajeIva:0}%:").FontSize(fsTiny);
                                r.AutoItem().AlignRight().Text($"{rep.ImporteIva:F2} €").FontSize(fsTiny);
                            });
                        }
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL:").Bold().FontSize(fsMed);
                            r.AutoItem().AlignRight().Text($"{rep.Total:F2} €").Bold().FontSize(fsMed);
                        });
                    }

                    // Precio / presupuesto (siempre visible si hay valor)
                    if (rep.PrecioEstimado.HasValue && !rep.BaseImponible.HasValue)
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("PRESUPUESTO:").Bold().FontSize(fsMed);
                            r.AutoItem().AlignRight().Text($"{rep.PrecioEstimado:F2} €").Bold().FontSize(fsMed);
                        });
                    }

                    // Firmas
                    col.Item().PaddingTop(10).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            c.Item().AlignCenter().Text("Firma cliente").FontSize(fsTiny).FontColor(Colors.Grey.Darken1);
                        });
                        r.ConstantItem(5, Unit.Millimetre);
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            c.Item().AlignCenter().Text("Firma técnico").FontSize(fsTiny).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    // QR
                    if (qrPng != null)
                    {
                        col.Item().PaddingTop(5).AlignCenter().Width(22, Unit.Millimetre).Image(qrPng);
                        col.Item().AlignCenter().Text(rep.NumeroOrden).FontSize(fsTiny);
                    }

                    // Cláusulas
                    if (!string.IsNullOrEmpty(clausRep))
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().Text(clausRep).FontSize(fsTiny).FontColor(Colors.Grey.Darken1).Italic();
                    }
                    if (!string.IsNullOrEmpty(clausRec))
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().Text(clausRec).FontSize(fsTiny).FontColor(Colors.Grey.Darken1).Italic();
                    }
                    if (!string.IsNullOrEmpty(pie))
                    {
                        col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().AlignCenter().Text(pie).FontSize(fsTiny).FontColor(Colors.Grey.Darken1).Italic();
                    }
                });
            });
        }).GeneratePdf();
    }

    // ════════════════════════════════════════════════════════════════
    // TICKET DE VENTA
    // ════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerarTicketVentaPdfAsync(VentaDto venta, string? formatoOverride = null)
    {
        var config  = await GetConfigAsync();
        var formato = formatoOverride ?? config.GetValueOrDefault("ticket_formato", "a4");
        var pie     = config.GetValueOrDefault("factura_pie_texto", "");
        var logo    = await GetLogoAsync(config);

        return formato switch
        {
            "ticket_80mm" => GenerarTicketVentaTermica(config, venta, 80f, pie, logo),
            "ticket_58mm" => GenerarTicketVentaTermica(config, venta, 58f, pie, logo),
            _             => GenerarTicketVentaA4(config, venta, pie, logo)
        };
    }

    private byte[] GenerarTicketVentaA4(
        Dictionary<string, string> config, VentaDto venta, string pie, byte[]? logo)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    CabeceraPagina(col, config, "TICKET DE VENTA",
                        venta.NumeroVenta,
                        venta.Fecha.ToString("dd/MM/yyyy HH:mm"),
                        logo);

                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("CLIENTE").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text(venta.ClienteNombreCompleto ?? "").Bold();
                        if (!string.IsNullOrEmpty(venta.ClienteNif))
                            c.Item().Text($"NIF: {venta.ClienteNif}").FontSize(9);
                        if (!string.IsNullOrEmpty(venta.ClienteEmail))
                            c.Item().Text(venta.ClienteEmail).FontSize(9);
                    });

                    col.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text($"Método de pago: {venta.MetodoPago ?? "—"}").FontSize(9);
                        if (!string.IsNullOrEmpty(venta.NumeroFactura))
                            r.RelativeItem().AlignRight().Text($"Factura: {venta.NumeroFactura}").FontSize(9);
                    });

                    col.Item().PaddingTop(10);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(5); cols.RelativeColumn(1);
                            cols.RelativeColumn(2); cols.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            static IContainer H(IContainer c) =>
                                c.Background(Colors.Blue.Darken2).Padding(5)
                                 .DefaultTextStyle(t => t.FontColor(Colors.White).Bold().FontSize(9));
                            header.Cell().Element(H).Text("Descripción");
                            header.Cell().Element(H).AlignCenter().Text("Cant.");
                            header.Cell().Element(H).AlignRight().Text("Precio unit.");
                            header.Cell().Element(H).AlignRight().Text("Subtotal");
                        });
                        bool par = false;
                        foreach (var d in venta.Detalles)
                        {
                            var bg = par ? Colors.Grey.Lighten4 : Colors.White;
                            par = !par;
                            static IContainer R(IContainer c, string bg) => c.Background(bg).Padding(5);
                            table.Cell().Element(c => R(c, bg)).Text(d.Descripcion);
                            table.Cell().Element(c => R(c, bg)).AlignCenter().Text(d.Cantidad.ToString());
                            table.Cell().Element(c => R(c, bg)).AlignRight().Text($"{d.PrecioUnitario:F2} €");
                            table.Cell().Element(c => R(c, bg)).AlignRight().Text($"{d.Subtotal:F2} €");
                        }
                    });

                    col.Item().PaddingTop(8).AlignRight().Width(220).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                        t.Cell().Padding(3).Text("Base imponible:");
                        t.Cell().Padding(3).AlignRight().Text($"{venta.BaseImponible:F2} €");
                        t.Cell().Padding(3).Text($"IVA ({venta.PorcentajeIva:0}%):");
                        t.Cell().Padding(3).AlignRight().Text($"{venta.ImporteIva:F2} €");
                        t.Cell().ColumnSpan(2).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                        t.Cell().Padding(3).Text("TOTAL:").Bold();
                        t.Cell().Padding(3).AlignRight().Text($"{venta.Total:F2} €").Bold();
                    });

                    if (!string.IsNullOrEmpty(venta.Observaciones))
                    {
                        col.Item().PaddingTop(10).Text($"Observaciones: {venta.Observaciones}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1).Italic();
                    }

                    // QR digital (si hay URL pública configurada)
                    var urlPublicaV = config.GetValueOrDefault("empresa_url_publica", "").TrimEnd('/');
                    if (!string.IsNullOrEmpty(urlPublicaV))
                    {
                        var qrUrlV = $"{urlPublicaV}/api/ventas/{venta.Id}/pdf";
                        var qrPngV = GenerarQrPng(qrUrlV, 150);
                        col.Item().PaddingTop(12).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Ver ticket digital").FontSize(9).FontColor(Colors.Grey.Darken2);
                                c.Item().Text(qrUrlV).FontSize(7).FontColor(Colors.Grey.Darken1);
                            });
                            r.ConstantItem(28, Unit.Millimetre).Image(qrPngV);
                        });
                    }

                    if (!string.IsNullOrEmpty(pie))
                    {
                        col.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(4).Text(pie).FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                    }
                });
            });
        }).GeneratePdf();
    }

    private byte[] GenerarTicketVentaTermica(
        Dictionary<string, string> config, VentaDto venta,
        float anchoPapelMm, string pie, byte[]? logo)
    {
        var empresaNombre = config.GetValueOrDefault("empresa_nombre",   "CellShop");
        var empresaTel    = config.GetValueOrDefault("empresa_telefono", "");
        var empresaEmail  = config.GetValueOrDefault("empresa_email",    "");
        float fs    = anchoPapelMm >= 70 ? 8f : 7f;
        float fsBig = fs + 2;
        float fsMed = fs + 1;
        float fsTiny= Math.Max(6f, fs - 1);

        var urlPubVenta = config.GetValueOrDefault("empresa_url_publica", "").TrimEnd('/');
        float altoVentaMm = 42f
            + (logo != null ? 14f : 0f)
            + (!string.IsNullOrEmpty(empresaTel) ? 4f : 0f)
            + (!string.IsNullOrEmpty(empresaEmail) ? 4f : 0f)
            + (!string.IsNullOrEmpty(venta.ClienteNombreCompleto) ? 14f : 0f)
            + venta.Detalles.Count * 7f
            + 22f
            + (!string.IsNullOrEmpty(venta.MetodoPago) ? 4f : 0f)
            + (!string.IsNullOrEmpty(venta.NumeroFactura) ? 4f : 0f)
            + (!string.IsNullOrEmpty(venta.Observaciones) ? 8f : 0f)
            + (!string.IsNullOrEmpty(urlPubVenta) ? 28f : 0f)
            + (!string.IsNullOrEmpty(pie) ? 12f : 0f)
            + 10f;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(anchoPapelMm, altoVentaMm, Unit.Millimetre);
                page.Margin(3, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(fs));

                page.Content().Column(col =>
                {
                    if (logo != null)
                    {
                        col.Item().AlignCenter()
                           .Width(Math.Min(anchoPapelMm * 0.4f, 22), Unit.Millimetre)
                           .Image(logo);
                        col.Item().PaddingBottom(2);
                    }

                    col.Item().AlignCenter().Text(empresaNombre).Bold().FontSize(fsBig);
                    if (!string.IsNullOrEmpty(empresaTel))
                        col.Item().AlignCenter().Text(empresaTel).FontSize(fsTiny);
                    if (!string.IsNullOrEmpty(empresaEmail))
                        col.Item().AlignCenter().Text(empresaEmail).FontSize(fsTiny);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().AlignCenter().Text("TICKET DE VENTA").Bold().FontSize(fsMed);
                    col.Item().AlignCenter().Text(venta.NumeroVenta).Bold().FontSize(fsBig);
                    col.Item().AlignCenter().Text(venta.Fecha.ToString("dd/MM/yyyy  HH:mm")).FontSize(fsTiny);

                    if (!string.IsNullOrEmpty(venta.ClienteNombreCompleto))
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().Text("CLIENTE").Bold().FontSize(fs);
                        col.Item().Text(venta.ClienteNombreCompleto).FontSize(fs);
                        if (!string.IsNullOrEmpty(venta.ClienteNif))
                            col.Item().Text($"NIF: {venta.ClienteNif}").FontSize(fsTiny);
                    }

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                    foreach (var d in venta.Detalles)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(d.Descripcion).FontSize(fs);
                            r.AutoItem().AlignRight().Text($"{d.Subtotal:F2} €").FontSize(fs);
                        });
                        if (d.Cantidad != 1)
                            col.Item().Text($"  {d.Cantidad} × {d.PrecioUnitario:F2} €")
                               .FontSize(fsTiny).FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Base imponible:").FontSize(fsTiny);
                        r.AutoItem().AlignRight().Text($"{venta.BaseImponible:F2} €").FontSize(fsTiny);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"IVA {venta.PorcentajeIva:0}%:").FontSize(fsTiny);
                        r.AutoItem().AlignRight().Text($"{venta.ImporteIva:F2} €").FontSize(fsTiny);
                    });
                    col.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL:").Bold().FontSize(fsMed);
                        r.AutoItem().AlignRight().Text($"{venta.Total:F2} €").Bold().FontSize(fsMed);
                    });

                    if (!string.IsNullOrEmpty(venta.MetodoPago))
                        col.Item().Text($"Pago: {venta.MetodoPago}").FontSize(fsTiny);
                    if (!string.IsNullOrEmpty(venta.NumeroFactura))
                        col.Item().Text($"Factura: {venta.NumeroFactura}").FontSize(fsTiny);
                    if (!string.IsNullOrEmpty(venta.Observaciones))
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().Text(venta.Observaciones).FontSize(fsTiny).FontColor(Colors.Grey.Darken1).Italic();
                    }

                    // QR digital (si hay URL pública configurada)
                    if (!string.IsNullOrEmpty(urlPubVenta))
                    {
                        var qrUrlVT = $"{urlPubVenta}/api/ventas/{venta.Id}/pdf";
                        var qrPngVT = GenerarQrPng(qrUrlVT, 120);
                        col.Item().PaddingTop(4).AlignCenter().Width(22, Unit.Millimetre).Image(qrPngVT);
                        col.Item().AlignCenter().Text("Ver ticket digital").FontSize(fsTiny);
                    }

                    if (!string.IsNullOrEmpty(pie))
                    {
                        col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(Colors.Black);
                        col.Item().AlignCenter().Text(pie).FontSize(fsTiny).FontColor(Colors.Grey.Darken1).Italic();
                    }
                });
            });
        }).GeneratePdf();
    }

    // ════════════════════════════════════════════════════════════════
    // ORDEN DE COMPRA (siempre A4)
    // ════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerarOrdenCompraPdfAsync(CompraDto compra)
    {
        var config = await GetConfigAsync();
        var pie    = config.GetValueOrDefault("factura_pie_texto", "");
        var logo   = await GetLogoAsync(config);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    CabeceraPagina(col, config, "ORDEN DE COMPRA",
                        compra.NumeroCompra,
                        compra.Fecha.ToString("dd/MM/yyyy"),
                        logo);

                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("PROVEEDOR").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text(compra.ProveedorNombre ?? "").Bold();
                    });

                    if (!string.IsNullOrEmpty(compra.Observaciones))
                    {
                        col.Item().PaddingTop(4).Text($"Observaciones: {compra.Observaciones}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1).Italic();
                    }

                    col.Item().PaddingTop(10);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(5); cols.RelativeColumn(1);
                            cols.RelativeColumn(2); cols.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            static IContainer H(IContainer c) =>
                                c.Background(Colors.Blue.Darken2).Padding(5)
                                 .DefaultTextStyle(t => t.FontColor(Colors.White).Bold().FontSize(9));
                            header.Cell().Element(H).Text("Producto");
                            header.Cell().Element(H).AlignCenter().Text("Cant.");
                            header.Cell().Element(H).AlignRight().Text("Costo unit.");
                            header.Cell().Element(H).AlignRight().Text("Subtotal");
                        });
                        bool par = false;
                        foreach (var d in compra.Detalles)
                        {
                            var bg = par ? Colors.Grey.Lighten4 : Colors.White;
                            par = !par;
                            static IContainer R(IContainer c, string bg) => c.Background(bg).Padding(5);
                            table.Cell().Element(c => R(c, bg)).Text(d.ProductoNombre ?? "");
                            table.Cell().Element(c => R(c, bg)).AlignCenter().Text(d.Cantidad.ToString());
                            table.Cell().Element(c => R(c, bg)).AlignRight().Text($"{d.CostoUnitario:F2} €");
                            table.Cell().Element(c => R(c, bg)).AlignRight().Text($"{d.Subtotal:F2} €");
                        }
                    });

                    col.Item().PaddingTop(8).AlignRight().Width(220).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                        t.Cell().ColumnSpan(2).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                        t.Cell().Padding(3).Text("TOTAL:").Bold();
                        t.Cell().Padding(3).AlignRight().Text($"{compra.Total:F2} €").Bold();
                    });

                    if (!string.IsNullOrEmpty(pie))
                    {
                        col.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(4).Text(pie).FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                    }
                });
            });
        }).GeneratePdf();
    }

    // ════════════════════════════════════════════════════════════════
    // HELPERS PRIVADOS
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cabecera estándar A4: logo (opcional) + datos empresa + título/número/fecha
    /// </summary>
    private static void CabeceraPagina(
        ColumnDescriptor col,
        Dictionary<string, string> config,
        string titulo,
        string numero,
        string fecha,
        byte[]? logo = null)
    {
        var empresaNombre = config.GetValueOrDefault("empresa_nombre",    "CellShop");
        var empresaCif    = config.GetValueOrDefault("empresa_cif",       "");
        var empresaDirec  = config.GetValueOrDefault("empresa_direccion", "");
        var empresaCp     = config.GetValueOrDefault("empresa_cp",        "");
        var empresaCiudad = config.GetValueOrDefault("empresa_ciudad",    "");
        var empresaTel    = config.GetValueOrDefault("empresa_telefono",  "");
        var empresaEmail  = config.GetValueOrDefault("empresa_email",     "");

        col.Item().Row(row =>
        {
            // ── Logo (izquierda, si existe) ──────────────────────
            if (logo != null)
            {
                row.ConstantItem(38, Unit.Millimetre)
                   .AlignMiddle()
                   .Image(logo);
                row.ConstantItem(6);
            }

            // ── Datos empresa ────────────────────────────────────
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(empresaNombre).Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                if (!string.IsNullOrEmpty(empresaCif))
                    c.Item().Text($"CIF: {empresaCif}").FontSize(9);
                if (!string.IsNullOrEmpty(empresaDirec))
                    c.Item().Text(empresaDirec).FontSize(9);
                if (!string.IsNullOrEmpty(empresaCp) || !string.IsNullOrEmpty(empresaCiudad))
                    c.Item().Text($"{empresaCp} {empresaCiudad}".Trim()).FontSize(9);
                if (!string.IsNullOrEmpty(empresaTel))
                    c.Item().Text($"Tel: {empresaTel}").FontSize(9);
                if (!string.IsNullOrEmpty(empresaEmail))
                    c.Item().Text(empresaEmail).FontSize(9);
            });

            // ── Título / número / fecha ──────────────────────────
            row.RelativeItem().Column(c =>
            {
                c.Item().AlignRight().Text(titulo)
                    .Bold().FontSize(16).FontColor(Colors.Grey.Darken2);
                c.Item().AlignRight().Text(numero).Bold().FontSize(14);
                c.Item().AlignRight().Text($"Fecha: {fecha}");
            });
        });

        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
    }

    /// <summary>
    /// Carga los bytes del logo desde wwwroot según la ruta guardada en config.
    /// Devuelve null si no hay logo configurado o el archivo no existe.
    /// </summary>
    private async Task<byte[]?> GetLogoAsync(Dictionary<string, string> config)
    {
        var logoRel = config.GetValueOrDefault("empresa_logo", "");
        if (string.IsNullOrEmpty(logoRel)) return null;

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var path    = Path.Combine(webRoot,
            logoRel.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(path)) return null;

        try   { return await File.ReadAllBytesAsync(path); }
        catch { return null; }
    }

    public byte[] GenerarQrPng(string contenido, int px)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format  = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width  = px, Height = px, Margin = 1,
                ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M
            }
        };
        var pixelData = writer.Write(contenido);
        var info   = new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888);
        using var bmp  = new SKBitmap(info);
        System.Runtime.InteropServices.Marshal.Copy(
            pixelData.Pixels, 0, bmp.GetPixels(), pixelData.Pixels.Length);
        using var image = SKImage.FromBitmap(bmp);
        using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private async Task<Dictionary<string, string>> GetConfigAsync() =>
        await _configRepo.GetAllAsync();

    private static string EstadoLabel(string e) => e switch
    {
        "recibido"      => "Recibido",
        "diagnosticado" => "Diagnosticado",
        "en_reparacion" => "En reparación",
        "reparado"      => "Reparado",
        "entregado"     => "Entregado",
        "no_reparable"  => "No reparable",
        _               => e
    };

    // ════════════════════════════════════════════════════════════════
    // ETIQUETAS DE PRECIO
    // ════════════════════════════════════════════════════════════════
    public Task<byte[]> GenerarEtiquetasPrecioPdfAsync(
        IEnumerable<ProductoDto> productos, string formato = "50x30")
    {
        var lista = productos.ToList();
        // Dimensiones según formato (mm)
        var (anchoMm, altoMm) = formato switch {
            "40x20" => (40f, 20f),
            "30x20" => (30f, 20f),
            _       => (50f, 30f)   // 50x30 por defecto
        };

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(5, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(7));

                int cols = (int)(190f / anchoMm);
                page.Content().Column(mainCol =>
                {
                    for (int i = 0; i < lista.Count; i += cols)
                    {
                        mainCol.Item().Row(row =>
                        {
                            for (int j = i; j < Math.Min(i + cols, lista.Count); j++)
                            {
                                var p  = lista[j];
                                var qr = GenerarQrPng(p.Codigo ?? p.Nombre, 80);
                                row.ConstantItem(anchoMm, Unit.Millimetre)
                                   .Border(0.5f).Padding(1, Unit.Millimetre)
                                   .Height(altoMm, Unit.Millimetre)
                                   .Column(col =>
                                   {
                                       col.Item().Text(p.Nombre)
                                          .FontSize(altoMm >= 30 ? 7f : 6f)
                                          .Bold().LineHeight(1.1f);
                                       if (!string.IsNullOrEmpty(p.Codigo))
                                           col.Item().Text(p.Codigo).FontSize(5)
                                              .FontColor(Colors.Grey.Darken1);
                                       col.Item().Row(r =>
                                       {
                                           r.RelativeItem().AlignLeft()
                                            .Text($"{p.PrecioVenta:F2} €")
                                            .FontSize(altoMm >= 30 ? 9f : 7f).Bold();
                                           r.ConstantItem(altoMm >= 30 ? 14f : 10f, Unit.Millimetre)
                                            .Image(qr);
                                       });
                                   });
                            }
                        });
                    }
                });
            });
        }).GeneratePdf();

        return Task.FromResult(pdf);
    }
}
