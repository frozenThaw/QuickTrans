using QuickTrans.App.Models;

namespace QuickTrans.App.Services;

public interface ITranslationService
{
    Task<TranslationResult> TranslateToChineseAsync(string text, CancellationToken cancellationToken);
}
