using System.Text.RegularExpressions;
using Kursovaya_ToP.Exceptions;

namespace Kursovaya_ToP;
public class Interpreter
    {        
        private List<string> ProgramText { get; set; }

        private Dictionary<string, bool> Idents { get; set; }

        private int CurrentLine { get; set; }

        public Interpreter(CodeAnalyzer codeAnalyzer)
        {
            ProgramText = codeAnalyzer.ProgramText[1..^1];

            ProgramText[0] = ProgramText[0].Replace("begin", string.Empty, StringComparison.OrdinalIgnoreCase);

            Idents = codeAnalyzer.Idents.Keys.ToDictionary(ident => ident, ident => false);
            Idents["0"] = false;
            Idents["1"] = true;
        }

        public void Run()
        {
            while (CurrentLine < ProgramText.Count)
                ExecuteLine(ProgramText[CurrentLine]);
        }

        //
        private void ExecuteLine(string line)
        {
            if (line.Contains("if", StringComparison.OrdinalIgnoreCase))
            {
                ProgramText[CurrentLine] = ProgramText[CurrentLine].Replace("if", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

                int indexEnd = 0;
                int indexElse = -1;

                for (int i = ProgramText.Count - 1; i >= CurrentLine; i--)
                {
                    if (ProgramText[i].Contains("end_if"))
                    {
                        indexEnd = i;
                        break;
                    }
                }
                
                for (int i = indexEnd; i >= CurrentLine; i--)
                {
                    if (ProgramText[i].Contains("else"))
                    {
                        indexElse = i;
                        break;
                    }
                }

                string condition = ProgramText[CurrentLine].Split("then")[0].Trim();
                condition = condition.Replace("(", string.Empty, StringComparison.OrdinalIgnoreCase);
                condition = condition.Replace(")", string.Empty, StringComparison.OrdinalIgnoreCase);
                string command = ProgramText[CurrentLine].Split("then")[1].Trim();
                if (CheckIf(condition) && indexElse == -1)
                {
                    ExecuteLine(command);
                    for (int i = CurrentLine; i < indexEnd; i++)
                    {
                        ExecuteLine(ProgramText[CurrentLine]);
                    }
                }
                else if (CheckIf(condition) && indexElse != -1)
                {
                    ExecuteLine(command);
                    for (int i = CurrentLine; i < indexElse; i++, CurrentLine++)
                    {
                        ExecuteLine(ProgramText[CurrentLine]);
                    }
                    CurrentLine = indexEnd;
                }
                else if (!CheckIf(condition) && indexElse == -1)
                {
                    CurrentLine = indexEnd;
                }
                else if (!CheckIf(condition) && indexElse != -1)
                {
                    CurrentLine = indexElse;
                    ProgramText[CurrentLine] = ProgramText[CurrentLine].Replace("else", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    for (int i = CurrentLine; i < indexEnd; i++)
                    {
                        ExecuteLine(ProgramText[CurrentLine]);
                    }
                    CurrentLine = indexEnd;
                }
            }
            else if (line.Contains("read", StringComparison.OrdinalIgnoreCase))
                Read(line);
            else if (line.Contains("write", StringComparison.OrdinalIgnoreCase))
                Write(line);
            else
                Assignment(line);

            CurrentLine++;
        }

        private void Read(string line)
        {
            line = line.Replace("read(", string.Empty).Replace(")", string.Empty);

            foreach (var ident in line.Split(',').Select(i => i.Trim()))
            {
                Console.Write($"{ident} = ");

                Idents[ident] = Console.ReadLine()?.Trim() switch
                {
                    "0" => false,
                    "1" => true,
                    _ => throw new InvalidInputException()
                };
            }
        }

        private void Write(string line)
        {
            line = line.Replace("write(", string.Empty).Replace(")", string.Empty);

            foreach (var ident in line.Split(',').Select(i => i.Trim()))
                Console.WriteLine($"Значение {ident}: {(Idents[ident] ? "1" : "0")}");
        }

        private void Assignment(string line)
        {
            List<string> parts = [.. line.Split('=').Select(i => i.Trim())];

            Idents[parts[0]] = parts[1] switch
            {
                "0" => false,
                "1" => true,
                _ => CalculateExpr(parts[1])
            };
        }

        private bool CheckIf(string line)
        {
            if (line.Contains("=", StringComparison.OrdinalIgnoreCase))
            {
                List<string> parts = [.. line.Split('=').Select(i => i.Trim())];
                if (Idents[parts[0]] == Idents[parts[1]])
                    return true;
                else return false;
            }
            else return CalculateExpr(line);
        }

        private bool GetBoolValueFromString(string str)
        {
            return str switch
            {
                "0" => false,
                "1" => true,
                _ => Idents[str]
            };
        }

        private bool IsIdent(string token)
        {
            List<string> constants = ["0", "1"];

            return Idents.ContainsKey(token) || constants.Contains(token);
        }

        private bool CalculateExpr(string expr)
        {
            expr = expr.Replace("(", string.Empty).Replace(")", string.Empty);
            var queue = InfixToPrefix(expr.Split('.'));

            Stack<bool> stack = [];

            foreach (var token in queue)
            {
                if (IsIdent(token))
                    stack.Push(
                        token switch
                        {
                            "0" => false,
                            "1" => true,
                            _ => Idents[token]
                        }
                    );
                else
                {
                    if (token.Equals("not"))
                        stack.Push(!stack.Pop());
                    else
                        stack.Push(Evaluate(token, stack.Pop(), stack.Pop()));
                }
            }

            return stack.Pop();
        }
        
        private Queue<string> InfixToPrefix(IEnumerable<string> tokens)
        {
            List<string> operators = ["not", "and", "or", "equ"];

            var queue = new Queue<string>();

            var stack = new Stack<string>();

            foreach (var token in tokens)
            {
                if (IsIdent(token))
                {
                    queue.Enqueue(token);
                    continue;
                }

                switch (token)
                {
                    case "not":
                        while (stack.Count > 0 && stack.Peek().Equals("not"))
                            queue.Enqueue(stack.Pop());

                        stack.Push(token);
                        break;
                    case "and":
                        while (stack.Count > 0 && operators[..2].Contains(stack.Peek()))
                            queue.Enqueue(stack.Pop());

                        stack.Push(token);
                        break;
                    case "or":
                        while (stack.Count > 0 && operators[..3].Contains(stack.Peek()))
                            queue.Enqueue(stack.Pop());

                        stack.Push(token);
                        break;
                    case "equ":
                        while (stack.Count > 0 && operators.Contains(stack.Peek()))
                            queue.Enqueue(stack.Pop());

                        stack.Push(token);
                        break;
                }
            }

            while (stack.Count > 0)
                queue.Enqueue(stack.Pop());

            return queue;
        }

        private bool Evaluate(string op, bool right, bool left)
        {
            return op switch
            {
                "and" => left && right,
                "or" => left || right,
                "equ" => left == right
            };
        }
    }
