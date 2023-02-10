using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace AlgoInterpreter;

public class Lexer
{
    private static bool inQuotes;
    private static char quoteChar;
    
    enum StructureToken
    {
        ProgramStart = 0,
        VariableDeclaration = 1,
        CodeBlockStart = 2,
        CodeBlockEnd = 3,
    }
    
    static string[] structureTokens = new String[]
    {
        "Algorithme",
        "Variables",
        "Début",
        "Fin"
    };

    public static Token[] TokenizeFile(string path)
    {
        List<Token> tokens = new List<Token>();
        FileStream fileStream = new FileStream(path, FileMode.Open);
        StreamReader streamReader = new StreamReader(fileStream);
        string? line = streamReader.ReadLine();
        while (line != null)
        {
            Console.WriteLine(line);
            List<Token> lineTokens = TokenizeLine(line);
            Console.WriteLine(SerializeTokens(lineTokens));
            if (lineTokens != null && lineTokens.Count > 0)
            {
                tokens.AddRange(lineTokens);
            }
            
            line = streamReader.ReadLine();
        }
        streamReader.Close();
        fileStream.Close();
        
        return new NewTokenLineBuilder().Build(tokens);
    }

    private static string SerializeTokens(List<Token> tokens)
    {
        string result = "";
        foreach (Token token in tokens)
        {
            result += token.ToString() + " ";
        }
        return result;
    }

    public static List<Token> TokenizeLine(string line)
    {
        string[] words = SplitAlgoLine(line).ToArray();
        List<Token> lineTokens = new List<Token>();
        for (int i = 0; i < words.Length; i++)
        {
            if(words[i] == " ") continue;
            if(isTextDelimiter(words[i]))
            {
                lineTokens.Add(Token.CreateToken(Token.TokenType.Separator,words[i]));
            }
            else if(IsString(words[i]))
            {
                lineTokens.Add(new StringToken(words[i]));
            }
            else if (Syntax.keywords.Contains(words[i].ToLower()))
            {
                lineTokens.Add(Token.CreateToken(Token.TokenType.Keyword, words[i]));
            }
            else if (IsBoolean(words[i]))
            {
                lineTokens.Add(new BooleanToken(ParseBoolean(words[i].ToLower())));
            }
            else if(IsOperator(words[i]))
            {
                lineTokens.Add(Token.CreateToken(Token.TokenType.Operator, words[i]));
            }
            else if (IsBooleanOperator(words[i]))
            {
                lineTokens.Add(Token.CreateToken(Token.TokenType.BooleanOperator, words[i]));
            }
            else if(IsParenthesis(words[i]))
            {
                lineTokens.Add(Token.CreateToken(Token.TokenType.Parenthesis, words[i]));
            }
            else if(IsSeparator(words[i]))
            {
                lineTokens.Add(new Token(Token.TokenType.Separator, words[i]));
            }
            else if(IsNumber(words[i]))
            {
                lineTokens.Add(new NumberToken(words[i]));
            }
            else
            {
                lineTokens.Add(new VariableToken(words[i]));
            }
        }
        
        if(words.Length > 0)
            lineTokens.Add(new EndOfLineToken());
        
        return lineTokens;
    }

    public static bool ParseBoolean(string value)
    {
        return value.Equals("vrai");
    }

    private static bool IsBoolean(string word)
    {
        return Syntax.booleans.Contains(word);
    }

    private static bool isTextDelimiter(string word)
    {
        if(word.Length == 1)
        {
            if (Syntax.textDelimiters.Contains(word[0]) && (!inQuotes || word[0] == quoteChar))
            {
                inQuotes = !inQuotes;
                quoteChar = word[0];
                return true;
            }
        }
        return false;
    }

    private static bool IsOperator(string word)
    {
        if(word.Length == 1)
        {
            return Syntax.operators.Contains(word[0]);
        }
        return false;
    }

    private static List<string> SplitAlgoLine(string line)
    {
        string[] words = line.Trim().Split(" ");
        List<string> wordsList = new List<string>();
        char[] specialChars = Syntax.operators.Concat(Syntax.parenthesis.Concat(Syntax.separators.Concat(Syntax.textDelimiters))).ToArray();
        foreach (string word in words)
        {
            if(word == "") continue;
            if(word.Length == 1)
            {
                wordsList.Add(word);
                continue;
            }
            string currentWord = "";
            for (int i = 0; i < word.Length; i++)
            {
                if(specialChars.Contains(word[i]))
                {
                    if(currentWord != "")
                    {
                        wordsList.Add(currentWord);
                        currentWord = "";
                    }
                    wordsList.Add(word[i].ToString());
                }
                else
                {
                    currentWord += word[i];
                }
            }
            if(currentWord != "")
            {
                wordsList.Add(currentWord);
            }
        }
        return wordsList;
    }

    private static bool IsSeparator(string word)
    {
        if(word.Length == 1)
        {
            return Syntax.separators.Contains(word[0]);
        }
        return false;
    }

    private static bool IsParenthesis(string word)
    {
        if(word.Length == 1)
        {
            return Syntax.parenthesis.Contains(word[0]);
        }
        return false;
    }

    private static bool IsBooleanOperator(string word)
    {
        return Syntax.booleanOperators.Contains(word);
    }
    
    private static bool ContainSeparator(string word)
    {
        foreach (char c in word)
        {
            if(Syntax.separators.Contains(c))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsString(string word)
    {
        return inQuotes;
    }

    private static bool IsNumber(string word)
    {
        return int.TryParse(word, out _);
    }

    public class NewTokenLineBuilder
    {
        public readonly GroupToken rootGroup;
        public GroupToken CurrentGroup;

        public NewTokenLineBuilder()
        {
            rootGroup = new GroupToken();
            CurrentGroup = rootGroup;
        }

        public Token[] Build(List<Token> tokens)
        {
            Token[] tokenArray = tokens.ToArray();

            while (tokenArray.Length > 0)
            {
                Token currentToken = tokenArray[0];
                tokenArray = tokenArray[1..];

                if (currentToken is ITokenLexer tokenLexer)
                {
                    tokenLexer.Lex(ref CurrentGroup, ref tokenArray);
                }
                else
                {
                    CurrentGroup.Tokens.Add(currentToken);
                }
            }
            
            return rootGroup.Tokens.ToArray();
        }
    }
}