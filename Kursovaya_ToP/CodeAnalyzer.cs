using Kursovaya_ToP.Exceptions;
using System.Text;
using System.Text.RegularExpressions;

namespace Kursovaya_ToP;
public partial class CodeAnalyzer
    {
        private const int MaxIdentLength = 11;

        [GeneratedRegex(@"^[a-zA-Z]+$")]
        private static partial Regex ValidCharacters();
        
        public List<string> ProgramText { get; private set; }

        public int CurrentLineNumber { get; private set; }
        
        public List<string> KeyWords { get; private set; }
        
        public Dictionary<string, string?> Idents { get; private set; }
        
        public HashSet<string> UnaryOperators { get; private set; }
        
        public HashSet<string> BinaryOperators { get; private set; }
        
        public HashSet<string> Operators { get; private set; }

        public List<Error> Errors { get; private set; }
        
        public CodeAnalyzer(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файла {filePath} не существует");
            //string[][] rules =
            // {
            //    new string[]{"<F>","<E>=<G>;"},
            //    new string[]{"<O>","<A><B>"},
            //    new string[]{"<A>","VAR<D>:logical;"},
            //    new string[]{"<B>","BEGIN<C>END"},
            //    new string[] {"<C>","<M><C>"},
            //    new string[]{"<C>","<F><C>"},
            //    new string[]{"<C>","<F>"},
            //    new string[]{"<C>","<M>"},
            //    new string[]{"<D>","<E>,<D>"},
            //    new string[]{"<D>","<E>"},
            //    new string[]{"<E>","<L><E>"},
            //    new string[]{"<E>","<L>"},
            //    new string[] {"<G>",".NOT.<H>"},
            //    new string[]{"<G>","<H>"},
            //    new string[]{"<H>","(<G>)"},
            //    new string[]{"<H>","<H><J><H>"},
            //    new string[]{"<H>","<I>"},
            //    new string[]{"<I>","<K>"},
            //    new string[]{"<I>","<E>"},
            //    new string[]{"<J>",".AND."},
            //    new string[]{"<J>",".OR."},
            //    new string[]{"<J>",".EQU."},
            //    new string[]{"<K>","0"},
            //    new string[]{"<K>","1"},
            //    new string[]{"<O>","READ(<D>)"},
            //    new string[]{"<O>","IF<G>THEN<C>ELSE<C>END_IF;"},
            //    new string[]{"<O>","WRITE(<D>)"}

            //}; 
            string[][] rules =
            {
                new string[]{"<O>","<A><B>"},
                new string[]{"<A>","VAR<D>:LOGICAL"},
                new string[]{"<B>","BEGIN<C>END"},
                new string[] {"<C>","<M><C>"},
                new string[]{"<C>","<F><C>"},
                new string[]{"<C>","<C><C>"},
                new string[]{"<D>","<E>,<D>"},
                new string[]{"<E>","<L><E>"},
                
                new string[]{ "<D>", "(<G>)"},
                new string[]{ "<D>", "<G><J><G>"},
                new string[]{"<F>","<E>=<G>"},
                new string[] {"<G>",".NOT.<G>"},
                new string[]{"<J>",".AND."},
                new string[]{"<J>",".OR."},
                new string[]{"<J>",".EQU."},
                new string[]{"<K>","0"},
                new string[]{"<K>","1"},
                new string[]{ "<M>", "READ(<D>)"},
                new string[]{ "<M>", "IF<G>THEN<C>ELSE<C>END_IF"},
                new string[]{ "<M>", "WRITE(<D>)"},
                new string[]{"<I>","<K>"},
                new string[]{"<E>","<L>"},
                new string[]{ "<D>", "<I>"},

                new string[]{"<G>","<D>"},
                new string[]{"<D>","<E>"},
                
                new string[]{"<I>","<E>"},
                new string[]{"<G>","(<F>)" },
                new string[]{"<C>","<M>"},
                new string[]{"<C>","<F>"},
            };
            ruls = new List<string[]>();
            for (int i = 97; i < 123; i++)
                ruls.Add(new string[] { "<L>", char.ConvertFromUtf32(i) });
            ReadFile(filePath);
            for (int i = 0; i < rules.Length; i++)
                ruls.Add(rules[i]);

            InitializeLists();
            //восходящий анализ
            DownUP();
            ParseProgramText();
        }
        List<string[]> ruls;
        private void DownUP()
        {
            string program = "";
            for (int i = 0;i< ProgramText.Count; i++)
            {
                program += ProgramText[i];
            }
            program = program.Replace(" ", "");
            program = program.Replace("var", "VAR");
            program = program.Replace("logical", "LOGICAL");
            program = program.Replace("begin", "BEGIN");
            program = program.Replace("end", "END");
            program = program.Replace("not.", ".NOT.");
            program = program.Replace(".and.", ".AND.");
            program = program.Replace(".or.", ".OR.");
            program = program.Replace(".equ.", ".EQU.");
            program = program.Replace("read", "READ");
            program = program.Replace("if", "IF");
            program = program.Replace("then", "THEN");
            program = program.Replace("else", "ELSE");
            program = program.Replace("end_if", "END_IF");
            program = program.Replace("write", "WRITE");
            string tmplast = "";
            while (program != tmplast)
            {
                tmplast = program;
                for (int i = 0; i < ruls.Count; i++)
                {
                    if (program.Contains(ruls[i][1]))
                    {
                        program = ReplaceLast(program,ruls[i][1], ruls[i][0]);
                        break;    
                    }
                }
            }
            if (program != "<O>")
                Errors.AddError($"Непредвиденная ошибка");
            // return (program == "<O>"); 
        }
        string ReplaceLast(string text, string search, string replace)
        {
            int pos = text.LastIndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        /// <summary>
        /// Выводит в консоль результат работы лексического  анализатора
        /// </summary>
        public void ErrorResults()
        {
            if (Errors.Count > 0)
            {
                Errors.ForEach(Console.WriteLine);
            }
        }

        private void InitializeLists()
        {
            KeyWords = [];
            Idents = [];
            UnaryOperators = [];
            BinaryOperators = [];
            Operators = [];
            Errors = [];
        }

        /// <summary>
        /// Читает файл программы, разбивает его по строкам и записывает в список <see cref="ProgramText"/>
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        private void ReadFile(string filePath)
        {
            using (FileStream fstream = new FileStream(filePath, FileMode.Open))
            {
                byte[] buffer = new byte[fstream.Length];
                fstream.Read(buffer);

                string fileContent = Encoding.Default.GetString(buffer);
                
                if (fileContent.Length == 0)
                    throw new EmptyFileException(filePath);

                ProgramText = [..fileContent.Split(";").Select(s => s.Trim().Replace(Environment.NewLine, string.Empty))];
            }
        }
         
        //
        private void CheckBeginEnd(List<string> calculationBlock)
        {
            if (calculationBlock[0].StartsWith("begin", StringComparison.OrdinalIgnoreCase))
            {
                CurrentLineNumber++;
                KeyWords.Add("begin");
                calculationBlock[0] = calculationBlock[0].Replace("begin", string.Empty);
            }
            else
                Errors.AddError("Программа должна содержать ключевое слово begin перед началом блока вычислений");

            if (calculationBlock[^1].EndsWith("end", StringComparison.OrdinalIgnoreCase))
            {
                KeyWords.Add("end");
                calculationBlock.RemoveAt(calculationBlock.Count - 1);
            }
            else
                Errors.AddError("Блок вычислений должен заканчиваться ключевым словом end");
        }

        private void CheckReadWrite(string expr)
        {
            bool isRead = false;
            
            if (expr.EndsWith(")"))
            {
                Operators.Add(")");
                expr = expr.Replace(")", string.Empty);
            }
            else
                Errors.AddError("Вызов функции должен завершаться )", CurrentLineNumber);

            if (expr.StartsWith("read"))
            {
                isRead = true;
                Operators.Add("read");
                expr = expr.Replace("read", string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            else if (expr.StartsWith("write"))
            {
                Operators.Add("write");
                expr = expr.Replace("write", string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            else
                Errors.AddError("Оператор должен быть в начале строки", CurrentLineNumber);

            if (expr.StartsWith("("))
            {
                Operators.Add("(");
                expr = expr.Replace("(", string.Empty);
            }
            else
                Errors.AddError("Вызов функции должен начинаться с (", CurrentLineNumber);

            foreach (var ident in expr.Split(',').Select(ident => ident.Trim()))
            {
                if (!Idents.ContainsKey(ident))
                    Errors.AddError($"Неизвестная переменная {ident}", CurrentLineNumber);
                else
                {
                    if (isRead)
                        Idents[ident] = "read from console";
                }
            }
        }

        private void CheckAssignment(string expr, bool shouldBeAssigned = true)
        {
            expr = expr.Replace("(", string.Empty).Replace(")", string.Empty);
            List<string> constants = ["1", "0"];
            List<string> unaryBinaryOperators = ["not", "or", "and", "equ"];

            List<string> parts = [.. expr.Split('=').Select(i => i.Trim())];

            if (!Idents.ContainsKey(parts[0]))
                Errors.AddError($"Переменная {parts[0]} не была объявлена в блоке переменных", CurrentLineNumber);

            string[] rvalue = parts[1].Split(".");
            string previousValueType = "";

            foreach (string value in rvalue)
            {
                if (value.Length == 0)
                {
                    Errors.AddError("Неверное завершение операции", CurrentLineNumber);
                    continue;
                }

                if (constants.Contains(value) || Idents.ContainsKey(value))
                {
                    if (previousValueType.Equals("const"))
                    {
                        Errors.AddError("Ошибка в использовании переменных", CurrentLineNumber);
                        continue;
                    }

                    previousValueType = "const";

                    if (Idents.ContainsKey(value) && Idents[value] == null)
                    {
                        Errors.AddError($"Переменная {value} должна быть инициализирована перед использованием", CurrentLineNumber);
                        continue;
                    }
                }
                else if (unaryBinaryOperators.Contains(value))
                {
                    if (value.Equals("not"))
                    {
                        UnaryOperators.Add("not");
                        if (previousValueType.Equals("const"))
                        {
                            Errors.AddError("Оператор .not. является унарным", CurrentLineNumber);
                            continue;
                        }
                    }
                    else
                    {
                        BinaryOperators.Add(value);
                        if (previousValueType.Equals("operation"))
                        {
                            Errors.AddError($"Оператор {value} является бинарным", CurrentLineNumber);
                            continue;
                        }
                    }
                    previousValueType = "operation";
                }
                else
                {
                    Errors.AddError($"{value} не определён", CurrentLineNumber);
                    continue;
                }
            }

            if (previousValueType.Equals("operation"))
                Errors.AddError("Неверное завершение операции", CurrentLineNumber);
            
            if (shouldBeAssigned)
                Idents[parts[0]] = parts[1];
        }
    }