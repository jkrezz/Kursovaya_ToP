namespace Kursovaya_ToP.Exceptions;

public class EmptyFileException(string filePath) : Exception($"Файл {filePath} пустой");