namespace JobScanner.Application.Pipeline;

/// <summary>
/// Run başına LLM devre kesici (saf, sağlayıcıdan bağımsız): üst üste N extraction hatasında açılır.
/// Başarılı extraction sayacı sıfırlar. Açıldıktan sonra run boyunca açık kalır.
/// </summary>
public sealed class ExtractionCircuitBreaker(int maxConsecutiveErrors)
{
    private int _consecutive;

    /// <summary>true ise bu run'da artık extraction denenmez.</summary>
    public bool IsOpen { get; private set; }

    public void RecordSuccess() => _consecutive = 0;

    /// <summary>Hatayı kaydeder; bu çağrı devreyi AÇTIYSA true döner (WARN'ı bir kez yazmak için).</summary>
    public bool RecordFailure()
    {
        if (IsOpen) return false;
        _consecutive++;
        if (maxConsecutiveErrors > 0 && _consecutive >= maxConsecutiveErrors)
        {
            IsOpen = true;
            return true;
        }
        return false;
    }
}
