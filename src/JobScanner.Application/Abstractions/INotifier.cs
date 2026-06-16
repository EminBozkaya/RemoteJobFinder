namespace JobScanner.Application.Abstractions;

/// <summary>REZERVE: bildirim soyutlaması. Faz 1-2'de implementasyon YOK (Telegram yok, UI yok).</summary>
public interface INotifier
{
    Task NotifyAsync(string message, CancellationToken ct);
}
