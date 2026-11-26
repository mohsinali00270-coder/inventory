using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FontManager = QuestPDF.Drawing.FontManager;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace ShopLedger.Services;

public interface IPdfService { string CreateStatementPdf(Statement s); }

public class PdfService : IPdfService
{
    private static bool _fontTried;
    private static bool _fontOk;

    /// <summary>Registers the Nastaliq font from the app package (if you added it). Otherwise the PDF engine
    /// falls back to a system font that has Arabic-script glyphs.</summary>
    private static void EnsureUrduFont()
    {
        if (_fontTried) return;
        _fontTried = true;
        try
        {
            using var src = Task.Run(() => FileSystem.OpenAppPackageFileAsync(Loc.UrduFontFile)).GetAwaiter().GetResult();
            using var ms = new MemoryStream();
            src.CopyTo(ms);
            ms.Position = 0;
            FontManager.RegisterFontFromStream(ms);
            _fontOk = true;
        }
        catch { _fontOk = false; }
    }

    public string CreateStatementPdf(Statement s)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var urdu = Loc.IsUrdu;
        if (urdu) EnsureUrduFont();

        var safeName = new string(s.Customer.Name.Where(c => c < 128 && char.IsLetterOrDigit(c)).ToArray());
        if (safeName.Length == 0) safeName = "customer" + s.Customer.Id;
        var path = Path.Combine(FileSystem.CacheDirectory, $"statement-{safeName}-{DateTime.Now:yyyyMMdd-HHmm}.pdf");

        Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(x => urdu && _fontOk ? x.FontSize(10).FontFamily(Loc.UrduFontFamily) : x.FontSize(10));
            if (urdu) page.ContentFromRightToLeft();

            page.Header().Column(col =>
            {
                col.Item().Text(Loc.T("Pdf_Title")).FontSize(20).Bold();
                col.Item().Text(s.Customer.Name).FontSize(14);
                if (!string.IsNullOrWhiteSpace(s.Customer.Phone)) col.Item().Text(s.Customer.Phone!);
                col.Item().Text($"{Loc.T("Pdf_Period")}: {s.Period}").FontColor("#666666");
            });

            page.Content().PaddingVertical(12).Column(col =>
            {
                col.Spacing(6);
                col.Item().Text($"{Loc.T("Lbl_Opening")}: {Loc.Money(s.OpeningBalance)}").Bold();

                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(62); c.RelativeColumn(); c.ConstantColumn(58); c.ConstantColumn(58); c.ConstantColumn(66);
                    });
                    t.Header(h =>
                    {
                        foreach (var title in new[] { Loc.T("Col_Date"), Loc.T("Col_Desc"), Loc.T("Col_Debit"), Loc.T("Col_Credit"), Loc.T("Col_Balance") })
                            h.Cell().Element(Head).Text(title).Bold();
                    });
                    foreach (var r in s.Rows)
                    {
                        t.Cell().Element(Body).Text(r.Date.ToString("dd/MM/yy", Loc.Culture));
                        t.Cell().Element(Body).Text(r.Description);
                        t.Cell().Element(Body).AlignRight().Text(r.Debit == 0 ? "" : r.Debit.ToString("N0", Loc.Culture));
                        t.Cell().Element(Body).AlignRight().Text(r.Credit == 0 ? "" : r.Credit.ToString("N0", Loc.Culture));
                        t.Cell().Element(Body).AlignRight().Text(r.Balance.ToString("N0", Loc.Culture));
                    }
                });

                col.Item().PaddingTop(8).Text($"{Loc.T("Lbl_TotalDebit")}: {Loc.Money(s.TotalDebit)}    {Loc.T("Lbl_TotalCredit")}: {Loc.Money(s.TotalCredit)}");
                col.Item().Text($"{Loc.T("Lbl_Closing")}: {Loc.Money(s.ClosingBalance)}").FontSize(13).Bold();
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span(Loc.T("Pdf_Footer"));
                x.CurrentPageNumber();
            });
        })).GeneratePdf(path);

        return path;

        static IContainer Head(IContainer c) => c.Background("#EEEEEE").Padding(4);
        static IContainer Body(IContainer c) => c.BorderBottom(0.5f).BorderColor("#CCCCCC").Padding(4);
    }
}
