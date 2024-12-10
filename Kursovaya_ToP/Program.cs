using Kursovaya_ToP;

const string filePath = "code.txt";
try
{
    var codeAnalyzer = new CodeAnalyzer(filePath);
    codeAnalyzer.ErrorResults();
    if (codeAnalyzer.Errors.Count > 0)
        return;

    Console.WriteLine("Start program\n");
    var interpreter = new Interpreter(codeAnalyzer);
    interpreter.Run();
    Console.WriteLine("\nProgram finished correct\n");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}