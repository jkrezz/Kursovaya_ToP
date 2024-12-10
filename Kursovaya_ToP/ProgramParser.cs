namespace Kursovaya_ToP
{
    public partial class CodeAnalyzer
    {
        public void ParseProgramText()
        {
            for (CurrentLineNumber = 0; CurrentLineNumber < ProgramText.Count; CurrentLineNumber++)
            {
                string currentString = ProgramText[CurrentLineNumber];

                if (!currentString.StartsWith("var", StringComparison.OrdinalIgnoreCase))
                    Errors.AddError($"Нераспознанная строка: {currentString}");
                else
                    break;
            }

            if (CurrentLineNumber == ProgramText.Count)
            {
                Errors.Clear();
                Errors.AddError("Неверный синтаксис программы");
                return;
            }
            
            CheckAndParseVarBlock();
            CheckAndParseCalculationBlock();
        }

        private void CheckAndParseVarBlock()
        {
            string varBlock = ProgramText[CurrentLineNumber];

            if (varBlock.StartsWith("var", StringComparison.OrdinalIgnoreCase))
            {
                KeyWords.Add("var");
                varBlock = varBlock.Replace("var", string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            else
                Errors.AddError("Блок переменных должен начинаться со слова var", 1);

            if (varBlock.Count(i => i.Equals(':')) == 1)
            {
                Operators.Add(":");
                varBlock = varBlock.Replace(":", string.Empty);
            }
            else
                Errors.AddError("Оператор : должен встречаться один", 1);

            if (varBlock.EndsWith("logical", StringComparison.OrdinalIgnoreCase))
            {
                KeyWords.Add("logical");
                varBlock = varBlock.Replace("logical", string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            else
                Errors.AddError("Блок объявления заканчивается logical", 1);

            if (varBlock.Trim().Length == 0)
            {
                Errors.AddError("Не указано ни одной переменной", 1);
                return;
            }

            foreach (var ident in varBlock.Split(',').Select(ident => ident.Trim()))
            {
                if (!ValidCharacters().IsMatch(ident))
                    Errors.AddError($"Имя переменной {ident} содержит недопустимый символ", 1);

                if (Idents.ContainsKey(ident))
                {
                    Errors.AddError($"Идентификатор {ident} уже задан", 1);
                    continue;
                }

                if (ident.Length > MaxIdentLength)
                    Errors.AddError($"Длина идентификатора {ident} не должна превышать {MaxIdentLength}", 1);

                Idents.Add(ident, null);
            }

            CurrentLineNumber++;
        }
        
        private void CheckAndParseCalculationBlock()
        {
            List<string> calculationBlock = ProgramText[CurrentLineNumber..];

            CheckBeginEnd(calculationBlock);

            Dictionary<int, string> if_EndIfIndexes = [];
            
            for (int i = 0; i < calculationBlock.Count; i++, CurrentLineNumber++)
            {
                string expr = calculationBlock[i];
                
                if (expr.Contains("end_if", StringComparison.OrdinalIgnoreCase))
                {
                    if (!if_EndIfIndexes.ContainsValue("if"))
                    {
                        Errors.AddError("Отсутствует оператор if", CurrentLineNumber);
                        continue;
                    }
                    expr = expr.Replace("end_if", string.Empty, StringComparison.OrdinalIgnoreCase);
                    if (expr != "")
                        Errors.AddError("Некорректно написан оператор end_if", CurrentLineNumber);
                    if_EndIfIndexes.Clear();
                }
                else if (expr.Contains("if", StringComparison.OrdinalIgnoreCase))
                {
                    if (expr.StartsWith("if"))
                    {
                        Operators.Add("if");
                        expr = expr.Replace("if", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    }
                    else
                        Errors.AddError("Оператор if должен быть в начале строки", CurrentLineNumber);

                    if (expr.StartsWith("("))
                    {
                        Operators.Add("(");
                        expr = expr.Remove(expr.IndexOf("("), "(".Length).Insert(expr.IndexOf("("), string.Empty);
                    }
                    else
                        Errors.AddError("Вызов функции должен начинаться с (", CurrentLineNumber);

                    if (expr.Contains("then", StringComparison.OrdinalIgnoreCase))
                        Operators.Add("then");
                    else
                    {
                        Errors.AddError("Оператор if должен содержать оператор then", CurrentLineNumber);
                        return;
                    }

                    if (expr.Split("then")[0].Trim().EndsWith(")"))
                    {
                        Operators.Add(")");
                        expr = expr.Remove(expr.IndexOf(")"), ")".Length).Insert(expr.IndexOf(")"), string.Empty);
                    }
                    else
                        Errors.AddError("Вызов функции должен завершаться )", CurrentLineNumber);

                    if (expr.Split("then")[0].Trim().Contains("read", StringComparison.OrdinalIgnoreCase)
                        || expr.Split("then")[0].Trim().Contains("write", StringComparison.OrdinalIgnoreCase))
                        Errors.AddError("Оператор if не может содержать операторы read, write", CurrentLineNumber);
                    else
                        CheckAssignment(expr.Split("then")[0], false);

                    int endIfIndex;
                    if_EndIfIndexes[i] = "if";
                    for (endIfIndex = calculationBlock.Count - 1; endIfIndex >= 0; endIfIndex--)
                    {
                        if (!if_EndIfIndexes.ContainsValue("end_if") &&
                            calculationBlock[endIfIndex].StartsWith("end_if", StringComparison.OrdinalIgnoreCase))
                        {
                            Operators.Add("end_if");
                            if_EndIfIndexes[endIfIndex] = "end_if";
                            break;
                        }
                    }

                    if (endIfIndex == -1)
                        Errors.AddError("Отсутствует ключевое слово end_if для оператора if", CurrentLineNumber);
                    
                    for (; endIfIndex >= CurrentLineNumber; endIfIndex--)
                    {
                        if (!if_EndIfIndexes.ContainsValue("else") &&
                            calculationBlock[endIfIndex].StartsWith("else", StringComparison.OrdinalIgnoreCase))
                        {
                            Operators.Add("else");
                            if_EndIfIndexes[endIfIndex] = "else";
                            break;
                        }
                    }

                    CurrentLineNumber++;
                    if (expr.Split("then")[1].Trim().Contains("read", StringComparison.OrdinalIgnoreCase)
                        || expr.Split("then")[1].Trim().Contains("write", StringComparison.OrdinalIgnoreCase))
                        CheckReadWrite(expr.Split("then")[1].Trim());
                    else if (expr.Split("then")[1].Trim().Contains('='))
                        CheckAssignment(expr.Split("then")[1].Trim());
                    else
                        Errors.AddError("Нераспознаное выражение", CurrentLineNumber);
                }
                else if (expr.Contains("else", StringComparison.OrdinalIgnoreCase))
                {
                    if (!if_EndIfIndexes.ContainsValue("if"))
                    {
                        Errors.AddError("Отсутствует оператор if", CurrentLineNumber);
                        continue;
                    }
                    if (!if_EndIfIndexes.ContainsValue("else"))
                    {
                        Errors.AddError("Оператор else должен быть внутри блока if", CurrentLineNumber);
                    }
                    if (expr.StartsWith("else"))
                    {
                        expr = expr.Replace("else", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                        if (expr.Contains("read", StringComparison.OrdinalIgnoreCase)
                            || expr.Contains("write", StringComparison.OrdinalIgnoreCase))
                            CheckReadWrite(expr);
                        else if (expr.Contains('='))
                            CheckAssignment(expr);
                        else
                            Errors.AddError("Нераспознаное выражение", CurrentLineNumber);
                    }
                    else
                        Errors.AddError("Оператор else должен быть в начале строки", CurrentLineNumber);
                }
                else if (expr.Contains("read", StringComparison.OrdinalIgnoreCase)
                    || expr.Contains("write", StringComparison.OrdinalIgnoreCase))
                    CheckReadWrite(expr);
                else if (expr.Contains('='))
                    CheckAssignment(expr);
                else
                    Errors.AddError("Нераспознаное выражение", CurrentLineNumber);
            }
        }
    }
}
