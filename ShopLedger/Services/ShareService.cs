using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace ShopLedger.Services;

public interface IShareService { Task ShareFileAsync(string path, string title); }

/// <summary>Opens the system share sheet (WhatsApp, Drive, email ...).</summary>
public class ShareService : IShareService
{
    public Task ShareFileAsync(string path, string title) =>
        Share.Default.RequestAsync(new ShareFileRequest { Title = title, File = new ShareFile(path) });
}
