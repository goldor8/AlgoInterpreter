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

    public static List<List<Token>> TokenizeFile(string path)
    {
        List<List<Token>> tokens = new List<List<Token>>();
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
                tokens.Add(lineTokens);
            }
            
            line = streamReader.ReadLine();
        }
        streamReader.Close();
        fileStream.Close();
        return tokens;
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
                lineTokens.Add(new Token(Token.TokenType.Separator,words[i]));
            }
            else if(IsString(words[i]))
            {
                lineTokens.Add(new StringToken(words[i]));
            }
            else if (Syntax.keywords.Contains(words[i]))
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
            else if(IsParenthesis(words[i]))
            {
                lineTokens.Add(new Token(Token.TokenType.Parenthesis, words[i]));
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

        return new TokenLineBuilder().PushToken(lineTokens).Build();
    }

    private static bool ParseBoolean(string value)
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

    private static bool IsStructureToken(string word)
    {
        return structureTokens.Contains(word);
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

    private static StructureToken GetStructureToken(string token)
    {
        return (StructureToken)Array.IndexOf(structureTokens, token);
    }

    private static TokenizeContext.BlockEmplacement GetBlockEmplacement(StructureToken token)
    {
        switch (token)
        {
            case StructureToken.ProgramStart:
                return TokenizeContext.BlockEmplacement.none;
            case StructureToken.VariableDeclaration:
                return TokenizeContext.BlockEmplacement.variableDeclaration;
            case StructureToken.CodeBlockStart:
                return TokenizeContext.BlockEmplacement.codeBlock;
            case StructureToken.CodeBlockEnd:
                return TokenizeContext.BlockEmplacement.none;
            default:
                throw new ArgumentOutOfRangeException(nameof(token), token, null);
        }
    }
    
    public class TokenizeContext
    {
        public enum BlockEmplacement
        {
            none,
            variableDeclaration,
            codeBlock
        }
        
        public BlockEmplacement blockEmplacement = BlockEmplacement.none;
        public string AlgorithmName = "";
        public string[] variables;
    }
    
    public class TokenLineBuilder
    {
        public readonly GroupToken rootTokens;
        public GroupToken CurrentGroup;
        public bool IndexMode = false;
        public bool Indexed = false;
        
        public TokenLineBuilder()
        {
            rootTokens = new GroupToken();
            CurrentGroup = rootTokens;
        }

        public TokenLineBuilder PushToken(Token token)
        {
            if(token.Type == Token.TokenType.Parenthesis)
            {
                if ((string)token.Value == "(")
                {
                    GroupToken groupToken = new GroupToken();
                    groupToken.Parent = CurrentGroup;
                    
                    if(CurrentGroup.Tokens.Last().Type == Token.TokenType.Variable)
                    {
                        CurrentGroup.Tokens.Add(new FunctionToken((string)CurrentGroup.Tokens.Last().Value, groupToken));
                        CurrentGroup.Tokens.RemoveAt(CurrentGroup.Tokens.Count - 2);
                    }
                    else
                    {
                        CurrentGroup.Tokens.Add(groupToken);
                    }

                    CurrentGroup = groupToken;
                }
                else if ((string)token.Value == ")")
                {
                    CurrentGroup = CurrentGroup.Parent;
                }

                if ((string)token.Value == "[")
                {
                    GroupToken groupToken = new GroupToken();
                    groupToken.Parent = CurrentGroup;
                    
                    if(CurrentGroup.Tokens.Last() is VariableToken variableToken)
                    {
                        variableToken.DimensionsTokens.Add(groupToken);
                    }
                    else
                    {
                        CurrentGroup.Tokens.Add(groupToken);
                    }

                    CurrentGroup = groupToken;
                }
                else if ((string)token.Value == "]")
                {
                    CurrentGroup = CurrentGroup.Parent;
                }
            }
            else if (token.Type == Token.TokenType.Separator && (token.Value as string == "\'" || token.Value as string == "\""))
            {
                if (inQuotes)
                {
                    inQuotes = false;
                }
                else
                {
                    inQuotes = true;
                    CurrentGroup.Tokens.Add(new StringToken(""));
                }
            }
            else if (token.Type == Token.TokenType.String)
            {
                Token lastToken = CurrentGroup.Tokens.Last();
                lastToken.Value = (string)lastToken.Value + " " +(string)token.Value;
            }
            else
            {
                if (token.Type == Token.TokenType.Operator && token.Value as string == "=")
                {
                    Token lastToken = CurrentGroup.Tokens.Last();
                    if(lastToken.Type is Token.TokenType.Operator && (lastToken.Value as string == "<" || lastToken.Value as string == ">"))
                    {
                        CurrentGroup.Tokens.RemoveAt(CurrentGroup.Tokens.Count - 1);
                        CurrentGroup.Tokens.Add(Token.CreateToken(Token.TokenType.Operator, lastToken.Value as string + token.Value));
                    }
                    else
                    {
                        CurrentGroup.Tokens.Add(token);
                    }
                }
                else
                {
                    CurrentGroup.Tokens.Add(token);
                }
            }
            
            return this;
        }
        
        public TokenLineBuilder PushToken(List<Token> tokens)
        {
            foreach (Token token in tokens)
            {
                PushToken(token);
            }
            
            return this;
        }
        
        public List<Token> Build()
        {
            return rootTokens.Tokens;
        }
    }
}