namespace Kursovaya_ToP;

/// <summary>
/// Представляет ошибку с информацией о строке и сообщении.
/// </summary>
public class Error
{
    public int? LineNumber { get; set; }

    public string Message { get; set; } = string.Empty;

    public override string ToString()
    {
        return LineNumber != null
            ? $"Ошибка в строке {LineNumber}: {Message}"
            : $"Ошибка: {Message}";
    }
}

/// <summary>
/// Содержит расширения для работы со списками ошибок.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Добавляет ошибку в список ошибок с возможностью указать номер строки.
    /// </summary>
    /// <param name="errors">Список ошибок.</param>1
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="lineIndex">Индекс строки (опционально).</param>
    public static void AddError(this List<Error> errors, string message, int? lineIndex = null)
    {
        errors.Add(new Error
        {
            LineNumber = lineIndex == null ? lineIndex : lineIndex + 1,
            Message = message
        });
    }
}