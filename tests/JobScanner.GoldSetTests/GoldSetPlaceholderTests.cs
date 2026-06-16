namespace JobScanner.GoldSetTests;

/// <summary>
/// Gold-set (extraction regresyon) testleri Faz 2'de gelir: ~20 etiketli ilan uzerinde
/// IEligibilityExtractor dogrulugunu olcer. Faz 1'de LLM yok, bu yuzden iceriksiz.
/// </summary>
public sealed class GoldSetPlaceholderTests
{
    [Fact(Skip = "Faz 2: LLM fact-extraction gold-set regresyonu (PromptVersion/ModelVersion ile).")]
    public void Extraction_gold_set_regression() { }
}
