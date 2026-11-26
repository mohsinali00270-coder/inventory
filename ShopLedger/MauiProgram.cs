using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.Logging;
using ShopLedger.Data;
using ShopLedger.Services;

namespace ShopLedger;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Loc.Init();   // language + culture before any XAML loads
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        Controls.FieldHandlers.Configure();   // borderless inputs inside the rounded field containers
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(f =>
            {
                f.AddFont("lucide.ttf", Controls.Icons.FontFamily);   // icon font (Lucide, ISC licence)
                f.AddFont(Loc.UrduFontFile, "UrduFont");   // needs Resources/Fonts/NotoNastaliqUrdu-Regular.ttf (see FONT-SETUP.txt)
            });

        // Data
        builder.Services.AddSingleton<AppDatabase>();
        builder.Services.AddSingleton<IProductRepository, ProductRepository>();
        builder.Services.AddSingleton<ICustomerRepository, CustomerRepository>();
        builder.Services.AddSingleton<ITransactionRepository, TransactionRepository>();
        builder.Services.AddSingleton<IStockRepository, StockRepository>();

        // Services
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<ILedgerService, LedgerService>();
        builder.Services.AddSingleton<IStatementService, StatementService>();
        builder.Services.AddSingleton<IReportService, ReportService>();
        builder.Services.AddSingleton<IPdfService, PdfService>();
        builder.Services.AddSingleton<IShareService, ShareService>();
        builder.Services.AddSingleton<IBackupService, BackupService>();
        builder.Services.AddSingleton<IDemoDataService, DemoDataService>();   // Phase 7

        // Phase 5: voice
        builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
        builder.Services.AddSingleton<IVoiceAssistant, VoiceAssistant>();
        builder.Services.AddSingleton<ITtsService, TtsService>();

        // Views / ViewModels (registered in later phases)
        builder.Services.AddTransient<AppShell>();   // rebuilt on every language switch
        builder.Services.AddTransient<Views.DashboardPage>();

        // Phase 2: products & customers
        builder.Services.AddTransient<ViewModels.ProductsViewModel>();
        builder.Services.AddTransient<ViewModels.ProductEditViewModel>();
        builder.Services.AddTransient<ViewModels.StockAdjustViewModel>();
        builder.Services.AddTransient<ViewModels.CustomersViewModel>();
        builder.Services.AddTransient<ViewModels.CustomerEditViewModel>();
        builder.Services.AddTransient<ViewModels.CustomerDetailViewModel>();
        builder.Services.AddTransient<Views.ProductsPage>();
        builder.Services.AddTransient<Views.ProductEditPage>();
        builder.Services.AddTransient<Views.StockAdjustPage>();
        builder.Services.AddTransient<Views.CustomersPage>();
        builder.Services.AddTransient<Views.CustomerEditPage>();
        builder.Services.AddTransient<Views.CustomerDetailPage>();

        // Phase 3: transactions & ledger
        builder.Services.AddTransient<ViewModels.SalesViewModel>();
        builder.Services.AddTransient<ViewModels.NewSaleViewModel>();
        builder.Services.AddTransient<ViewModels.LedgerEntryViewModel>();
        builder.Services.AddTransient<Views.SalesPage>();
        builder.Services.AddTransient<Views.NewSalePage>();
        builder.Services.AddTransient<Views.LedgerEntryPage>();

        // Phase 4: history, reports, PDF, backup
        builder.Services.AddTransient<ViewModels.DashboardViewModel>();
        builder.Services.AddTransient<ViewModels.HistoryViewModel>();
        builder.Services.AddTransient<ViewModels.StatementViewModel>();
        builder.Services.AddTransient<ViewModels.SettingsViewModel>();
        builder.Services.AddTransient<Views.HistoryPage>();
        builder.Services.AddTransient<Views.StatementPage>();
        builder.Services.AddTransient<Views.SettingsPage>();

        // Phase 5: voice entry
        builder.Services.AddTransient<ViewModels.VoiceEntryViewModel>();
        builder.Services.AddTransient<Views.VoiceEntryPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
