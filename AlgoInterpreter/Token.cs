namespace AlgoInterpreter;

public interface INodeConverter
{
    public Token.TokenPriority Priority { get; set; }
    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens);
    public void ExecuteNode(Node node, NodeInterpreter interpreter);
}

public interface INodeInterpreter : INodeConverter
{
    public void ExecuteNode(Node node);
}

public interface ITokenLexer
{
    public void Lex(ref GroupToken currentGroupToken, ref Token[] nextTokens);
}

public class Token
{
    public static Dictionary<TokenType,Dictionary<string, Func<Token>>> TokenFactoryRegister = new Dictionary<TokenType,Dictionary<string, Func<Token>>>();
    public enum TokenType
    {
        Keyword,
        Variable,
        Type,
        Number,
        Operator,
        Group,
        Separator,
        EndOfLine,
        String,
        Parenthesis,
        Function,
        Boolean,
        BooleanOperator,
        Special
    }
    
    public enum TokenPriority
    {
        Lowest,
        Low,
        Medium,
        High,
        Highest
    }
    
    public static void RegisterTokenFactory(TokenType tokenType, string tokenValue, Func<Token> tokenFactory)
    {
        if(!TokenFactoryRegister.ContainsKey(tokenType))
            TokenFactoryRegister.Add(tokenType, new Dictionary<string, Func<Token>>());
        TokenFactoryRegister[tokenType].Add(tokenValue, tokenFactory);
    }
    
    public static Token CreateToken(TokenType tokenType, string tokenValue)
    {
        if(TokenFactoryRegister.ContainsKey(tokenType))
        {
            if(TokenFactoryRegister[tokenType].ContainsKey(tokenValue))
                return TokenFactoryRegister[tokenType][tokenValue]();
        }
        return new Token(tokenType, tokenValue);
    }

    public static void InitTokenFactoryRegistry()
    {
        RegisterTokenFactory(TokenType.Operator, ":", () => new DeclarationToken());
        RegisterTokenFactory(TokenType.Operator, "←", () => new AssignationToken());
        
        RegisterTokenFactory(TokenType.Parenthesis, "(", () => new ParenthesisToken("("));
        RegisterTokenFactory(TokenType.Parenthesis, ")", () => new ParenthesisToken(")"));
        RegisterTokenFactory(TokenType.Parenthesis, "[", () => new SquareBracketToken("["));
        RegisterTokenFactory(TokenType.Parenthesis, "]", () => new SquareBracketToken("]"));
        
        RegisterTokenFactory(TokenType.Separator, "\"", () => new QuoteToken("\""));
        RegisterTokenFactory(TokenType.Separator, "'", () => new QuoteToken("'"));

        RegisterTokenFactory(TokenType.Operator, "+", () => new AdditionToken());
        RegisterTokenFactory(TokenType.Operator, "-", () => new SubtractionToken());
        RegisterTokenFactory(TokenType.Operator, "*", () => new MultiplicationToken());
        RegisterTokenFactory(TokenType.Operator, "/", () => new DivisionToken());
        
        RegisterTokenFactory(TokenType.Operator, "=", () => new EqualToken());
        RegisterTokenFactory(TokenType.Operator, "<", () => new InferiorToken());
        RegisterTokenFactory(TokenType.Operator, "<=", () => new InferiorEqualToken());
        RegisterTokenFactory(TokenType.Operator, ">", () => new SuperiorToken());
        RegisterTokenFactory(TokenType.Operator, ">=", () => new SuperiorEqualToken());
        
        RegisterTokenFactory(TokenType.BooleanOperator, "ET", () => new AndOperatorToken());
        RegisterTokenFactory(TokenType.BooleanOperator, "OU", () => new OrOperatorToken());
        
        RegisterTokenFactory(TokenType.Keyword, "Si", () => new IfToken());
        RegisterTokenFactory(TokenType.Keyword, "Sinon", () => new ElseToken());
        RegisterTokenFactory(TokenType.Keyword, "Tant", () => new WhileToken());
        RegisterTokenFactory(TokenType.Keyword, "Pour", () => new ForToken());
        RegisterTokenFactory(TokenType.Keyword, "Fin", () => new EndToken());
        RegisterTokenFactory(TokenType.Keyword, "Algorithme", () => new AlgorithmToken());
        RegisterTokenFactory(TokenType.Keyword, "Variables", () => new VariableDeclarationToken());
        RegisterTokenFactory(TokenType.Keyword, "Début", () => new StartToken());
    }

    public TokenType Type { get; set; }
    public object Value { get; set; }
    
    public Token(TokenType type, object value)
    {
        Type = type;
        Value = value;
    }
    
    public static bool operator ==(Token? token1, Token? token2)
    {
        if (token1 is null && token2 is null)
            return true;
        if (token1 is null || token2 is null)
            return false;
        return token1.Type == token2.Type && token1.Value.Equals(token2.Value);
    }
    
    public static bool operator !=(Token? token1, Token? token2)
    {
        return !(token1 == token2);
    }
    
    public static Token[][] SplitTokens(Token[] tokens)
    {
        List<Token[]> linesTokens = new List<Token[]>();
        int lastSeparatorIndex = -1;
        for (int i = 0; i < tokens.Length; i++)
        {
            if(tokens[i].Type == TokenType.EndOfLine)
            {
                Token[] lineTokens = new Token[i - lastSeparatorIndex - 1];
                Array.Copy(tokens, lastSeparatorIndex + 1, lineTokens, 0, lineTokens.Length);
                linesTokens.Add(lineTokens);
                lastSeparatorIndex = i;
            }
        }
        
        return linesTokens.ToArray();
    }
    
    public static Token[] GetTokenTo(Token[] tokens, int startIndex, TokenType tokenType)
    {
        List<Token> tokensTo = new List<Token>();
        for (int i = startIndex; i < tokens.Length; i++)
        {
            if(tokens[i].Type == tokenType)
                break;
            tokensTo.Add(tokens[i]);
        }
        return tokensTo.ToArray();
    }
}

public class ParenthesisToken : Token, ITokenLexer
{
    public ParenthesisToken(string value) : base(TokenType.Parenthesis, value) { }
    
    public void Lex(ref GroupToken currentGroupToken, ref Token[] nextTokens)
    {
        if ((string) Value == "(")
        {
            GroupToken groupToken = new GroupToken();
            groupToken.Parent = currentGroupToken;
                    
            if(currentGroupToken.Tokens.Last().Type == Token.TokenType.Variable)
            {
                currentGroupToken.Tokens.Add(new FunctionCallToken((string)currentGroupToken.Tokens.Last().Value, groupToken));
                currentGroupToken.Tokens.RemoveAt(currentGroupToken.Tokens.Count - 2);
            }
            else
            {
                currentGroupToken.Tokens.Add(groupToken);
            }

            currentGroupToken = groupToken;
        }
        else if ((string) Value == ")")
        {
            currentGroupToken = currentGroupToken.Parent;
        }
    }
}

public class SquareBracketToken : Token, ITokenLexer    
{
    public SquareBracketToken(string value) : base(TokenType.Parenthesis, value) { }
    
    public void Lex(ref GroupToken currentGroupToken, ref Token[] nextTokens)
    {
        if ((string) Value == "[")
        {
            GroupToken groupToken = new GroupToken();
            groupToken.Parent = currentGroupToken;
                    
            if(currentGroupToken.Tokens.Last() is VariableToken variableToken)
            {
                variableToken.DimensionsTokens.Add(groupToken);
            }
            else
            {
                currentGroupToken.Tokens.Add(groupToken);
            }

            currentGroupToken = groupToken;
        }
        else if ((string) Value == "]")
        {
            currentGroupToken = currentGroupToken.Parent;
        }
    }
}

public class QuoteToken : Token, ITokenLexer
{
    public QuoteToken(string value) : base(TokenType.Separator, value) { }

    public void Lex(ref GroupToken currentGroupToken, ref Token[] nextTokens)
    {
        int i = 0;
        while (i < nextTokens.Length && nextTokens[i] is not QuoteToken && nextTokens[i].Value as string != Value as string)
        {
            i++;
        }
        
        string value = string.Join("", nextTokens[..i].Select(t => t.Value as string));
        currentGroupToken.Tokens.Add(new StringToken(value));
        nextTokens = nextTokens[(i + 1)..];
    }
}

public class GroupToken : Token
{
    public GroupToken Parent { get; set; }

    public List<Token> Tokens
    {
        get => Value as List<Token>;
        set => Value = value;
    }
    
    public GroupToken() : base(TokenType.Group, null)
    {
        Tokens = new List<Token>();
    }
    
    public void Push(Token token)
    {
        Tokens.Add(token);
    }
}

public class FunctionDeclarationToken : Token, INodeConverter, ITokenLexer
{
    public struct Parameter
    {
        private bool reference;
        private string name;
        private Type type;
    }
    
    public TokenPriority Priority
    {
        get => TokenPriority.Highest; 
        set => throw new NotImplementedException();
    }

    public string Name
    {
        get => Value as string;
        set => Value = value;
    }
    
    public TypeToken ReturnType { get; set; }

    public FunctionDeclarationToken(string name) : base(TokenType.Keyword, name) { }


    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        throw new NotImplementedException();
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        
    }

    public void Lex(ref GroupToken currentGroupToken, ref Token[] nextTokens)
    {
        VariableToken functionName = nextTokens[0] as VariableToken;
        GroupToken parametersGroup = nextTokens[1] as GroupToken;
        if(nextTokens[2].Value != ":")
            throw new Exception("Expected ':' after function parameters");
        Token[] typeTokens = GetTokenTo(nextTokens, 3, TokenType.EndOfLine);
        ReturnType = NodeInterpreter.GetAlgoType(typeTokens);
        
    }

    public Parameter[] ParseParameters(Token[] tokens)
    {
        throw new NotImplementedException();
    }
}

public class FunctionCallToken : Token, INodeConverter
{
    public string Name
    {
        get => (Value as Tuple<string, GroupToken>).Item1;
        set => Value = new Tuple<string, GroupToken>(value, (Value as Tuple<string, GroupToken>).Item2);
    }
    
    public GroupToken Arguments
    {
        get => (Value as Tuple<string, GroupToken>).Item2;
        set => Value = new Tuple<string, GroupToken>(Name, value);
    }
    
    public TokenPriority Priority
    {
        get => TokenPriority.Lowest;
        set => throw new NotImplementedException();
    }
    
    public FunctionCallToken(string name, GroupToken arguments) : base(TokenType.Function, null)
    {
        Value = new Tuple<string, GroupToken>(name, arguments);
        
    }
    
    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        if(leftTokens.Length != 0 || rightTokens.Length != 0)
            throw new ArgumentException("Function should not have any left or right tokens");
        List<Node> argumentNodeTrees = new List<Node>();
        List<Token> argumentTokens = Arguments.Tokens;
        int lastSeparator = -1;
        for (int i = 0; i < argumentTokens.Count; i++)
        {
            if(argumentTokens[i].Type == TokenType.Separator)
            {
                argumentNodeTrees.Add(NodeConverter.CreateNodeTree(argumentTokens.GetRange(lastSeparator + 1, i - lastSeparator - 1).ToArray()));
                lastSeparator = i;
            }
        }
        argumentNodeTrees.Add(NodeConverter.CreateNodeTree(argumentTokens.GetRange(lastSeparator + 1, argumentTokens.Count - lastSeparator - 1).ToArray()));
        Node node = new Node
        {
            Token = this
        };
        node.Children.AddRange(argumentNodeTrees);
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteFunction(Name, node.Children.ToArray());
    }
}

public class DeclarationToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest; 
        set => throw new NotImplementedException();
    }
    
    public DeclarationToken() : base(TokenType.Operator, ":") { }

    public Node ConvertToNodeTree(Token[] variables, Token[] type)
    {
        Node node = new Node();
        node.Token = this;
        node.Children.Add(new Node(){Token = NodeInterpreter.GetAlgoType(type)});

        for (int i = 0; i < variables.Length; i++)
        {
            if(i % 2 == 0)
            {
                node.Children.Add(new Node(){Token = variables[i]});
            }
            else
            {
                if(variables[i].Type != TokenType.Separator)
                    throw new Exception("Variable declaration is not valid (probably missing comma)");
            }
        }
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        TypeToken typeToken = node.Children[0].Token as TypeToken;
        if (typeToken == null) throw new Exception("Invalid type declaration");
        VariableToken[] vars = new VariableToken[node.Children.Count - 1];
        for (int i = 1; i < node.Children.Count; i++)
        {
            vars[i - 1] = node.Children[i].Token as VariableToken;
        }
        
        foreach (VariableToken var in vars)
        {
            if (typeToken.IsArray)
            {
                int[] dimensions = var.GetDimensions(interpreter);
                interpreter.RegisterVariableArray(var.Value as string, typeToken.Type, dimensions);
            }
            else
            {
                interpreter.RegisterVariable(var.Value as string, typeToken.Type);
            }
        }
    }
}

public class AssignationToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public AssignationToken() : base(TokenType.Operator, "←") { }
    
    public Node ConvertToNodeTree(Token[] assigned, Token[] operation)
    {
        Node node = new Node();
        node.Token = this;
        if (assigned.Length == 1)
        {
            if(assigned[0] is VariableToken variableToken)
            {
                node.Children.Add(new Node(){Token = variableToken.PrepareDimensionsIfPresent()});
            }
            else
            {
                throw new ArgumentException("Invalid assignment (variable expected)");
            }
        }
        else
        {
            throw new Exception("Assignation operation must have only one assigned variable");
        }
        node.Children.Add(NodeConverter.CreateNodeTree(operation));

        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.SetVariable(node.Children[0].Token as VariableToken, interpreter.ExtractValue(node.Children[1]));
    }
}

public class TypeToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Medium;
        set => throw new NotImplementedException();
    }
    
    public bool IsArray { get; set; }

    public Type Type
    {
        get => (Type) Value;
        set => Value = value;
    }

    public TypeToken(Type value) : base(TokenType.Type, value) { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        throw new NotImplementedException();
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        throw new NotImplementedException();
    }
}

public class VariableToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Low; 
        set => throw new NotImplementedException();
    }

    public Node[] Dimensions { get; set; }
    public List<Token> DimensionsTokens { get; set; }

    public VariableToken(string value) : base(TokenType.Variable, value)
    {
        DimensionsTokens = new List<Token>();
    }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        PrepareDimensionsIfPresent();
        return new Node(){Token = this};
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        if (DimensionsTokens.Count == 0)
        {
            interpreter.Push(interpreter.GetVariable(node.Token.Value as string));
        }
        else
        {
            int[] dimensions = GetDimensions(interpreter);
            interpreter.Push(interpreter.GetVariableArray(node.Token.Value as string, dimensions));
        }
    }

    public VariableToken PrepareDimensionsIfPresent()
    {
        Dimensions = DimensionsTokens.Select(dimensionToken => NodeConverter.CreateNodeTree((dimensionToken as GroupToken).Tokens.ToArray())).ToArray();
        return this;
    }
    
    public int[] GetDimensions(NodeInterpreter interpreter)
    {
        return DimensionsTokens.Select(dimension => (int) interpreter.ExtractValue(NodeConverter.CreateNodeTree((dimension as GroupToken).Tokens.ToArray()))).ToArray();
    }
}

public class BooleanToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Low; 
        set => throw new NotImplementedException();
    }

    public BooleanToken(bool value) : base(TokenType.Boolean, value) { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        return new Node(){Token = this};
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.Push(node.Token.Value);
    }
}

public class NumberToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Low;
        set => throw new NotImplementedException();
    }

    public NumberToken(object value) : base(TokenType.Number, value) { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        return new Node() { Token = this };
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.Push(ParseIntOrFloat(node.Token.Value as string));
    }

    public static object ParseIntOrFloat(string value)
    {
        int intValue;
        if (int.TryParse(value, out intValue))
        {
            return intValue;
        }

        float floatValue;
        if (float.TryParse(value, out floatValue))
        {
            return floatValue;
        }

        throw new Exception("Value is neither a float neither an int");
    }
}

public class StringToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Low;
        set => throw new NotImplementedException();
    }

    public StringToken(string value) : base(TokenType.String, value) { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        return new Node() { Token = this };
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.Push(node.Token.Value);
    }
}

public class IfToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest; 
        set => throw new NotImplementedException();
    }

    public IfToken() : base(TokenType.Keyword, "Si") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        int index = 0;
        while (rightTokens[index] != new Token(TokenType.Keyword, "alors"))
        {
            index++;
        }
        Token[] condition = rightTokens[..index];
        Node node = new Node();
        node.Token = this;
        node.Children.Add(NodeConverter.CreateNodeTree(condition));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteNode(node.Children[0]);
        bool condition = interpreter.Pop<bool>();
        if (condition)
        {
            interpreter.MetaDataSet("jumpElse", true);
        }
        else
        {
            interpreter.MetaDataSet("jumpElse", false);
            interpreter.JumpToElse();
        }
    }
}

public class ElseToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public ElseToken() : base(TokenType.Keyword, "Sinon") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        return new Node() { Token = this };
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        bool shouldPass = interpreter.MetaDataGet<bool>("jumpElse");
        if (shouldPass)
        {
            interpreter.SkipToEnd();
        }
    }
}

public class EndOfLineToken : Token
{
    public EndOfLineToken() : base(TokenType.EndOfLine, "") { }
}

#region ArithmeticOperators
public class AdditionToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Medium; 
        set => throw new NotImplementedException();
    }
    
    public AdditionToken() : base(TokenType.Operator, "+") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node();
        node.Token = this;
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        object valueA = interpreter.ExtractArithmeticValue(node.Children[0]);

        object valueB = interpreter.ExtractArithmeticValue(node.Children[1]);
        

        if (valueA.GetType() != valueB.GetType())
        {
            throw new Exception($"Incompatible type between two operand, a:{valueA.GetType()}, b:{valueB.GetType()}");
        }

        object result = null;
        if (valueA is int)
        {
            result = (int)valueA + (int) valueB;
        }
        else if(valueA is float)
        {
            result = (float)valueA + (float)valueB;
        }
        else
        {
            throw new Exception("Unable to add two values of type " + valueA.GetType());
        }
        
        interpreter.Push(result);
    }
}

public class SubtractionToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Medium; 
        set => throw new NotImplementedException();
    }
    
    public SubtractionToken() : base(TokenType.Operator, "-") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node();
        node.Token = this;
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        object valueA = interpreter.ExtractArithmeticValue(node.Children[0]);

        object valueB = interpreter.ExtractArithmeticValue(node.Children[1]);

        if (valueA.GetType() != valueB.GetType())
        {
            throw new Exception("Incompatible type between two operand");
        }

        object result = null;
        if (valueA is int)
        {
            result = (int)valueA - (int) valueB;
        }
        else if(valueA is float)
        {
            result = (float)valueA - (float)valueB;
        }
        else
        {
            throw new NotImplementedException();
        }
        
        interpreter.Push(result);
    }
}

public class MultiplicationToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.High; 
        set => throw new NotImplementedException();
    }
    
    public MultiplicationToken() : base(TokenType.Operator, "*") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node();
        node.Token = this;
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        object valueA = interpreter.ExtractArithmeticValue(node.Children[0]);

        object valueB = interpreter.ExtractArithmeticValue(node.Children[1]);

        if (valueA.GetType() != valueB.GetType())
        {
            throw new Exception("Incompatible type between two operand");
        }

        object result = null;
        if (valueA is int)
        {
            result = (int)valueA * (int) valueB;
        }
        else if(valueA is float)
        {
            result = (float)valueA * (float)valueB;
        }
        else
        {
            throw new NotImplementedException();
        }
        
        interpreter.Push(result);
    }
}

public class DivisionToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.High; 
        set => throw new NotImplementedException();
    }
    
    public DivisionToken() : base(TokenType.Operator, "/") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node();
        node.Token = this;
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        object valueA = interpreter.ExtractArithmeticValue(node.Children[0]);

        object valueB = interpreter.ExtractArithmeticValue(node.Children[1]);

        if (valueA.GetType() != valueB.GetType())
        {
            throw new Exception("Incompatible type between two operand");
        }

        object result = null;
        if (valueA is int)
        {
            result = (int)valueA / (int) valueB;
        }
        else if(valueA is float)
        {
            result = (float)valueA / (float)valueB;
        }
        else
        {
            throw new NotImplementedException();
        }
        
        interpreter.Push(result);
    }
}

#endregion

#region LoopInstruction

public class WhileToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest; 
        set => throw new NotImplementedException();
    }

    public WhileToken() : base(TokenType.Keyword, "Tant") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        if (rightTokens[0] == new Token(TokenType.Keyword, "que"))
        {
            int index = 1;
            while (rightTokens[index] != new Token(TokenType.Keyword, "faire"))
            {
                index++;
            }
            Token[] condition = rightTokens[1..index];
            Node node = new Node();
            node.Token = this;
            node.Children.Add(NodeConverter.CreateNodeTree(condition));
            return node;
        }
        
        throw new Exception("While condition is not valid");
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteNode(node.Children[0]);
        bool condition = interpreter.Pop<bool>();
        if (condition)
        {
            interpreter.SetInstructionLoopIndex();
        }
        else
        {
            interpreter.SkipToEnd();
        }
    }
}

public class ForToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest; 
        set => throw new NotImplementedException();
    }

    public ForToken() : base(TokenType.Keyword, "Pour") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Token iterator = rightTokens[0];
        int tokenIndex = 1;
        while (rightTokens[tokenIndex] != new Token(TokenType.Keyword, "à"))
        {
            tokenIndex++;
        }
        Token[] startAssignation = rightTokens[..tokenIndex];
        int endValueStartIndex = tokenIndex + 1;
        while (rightTokens[tokenIndex] != new Token(TokenType.Keyword, "faire"))
        {
            tokenIndex++;
        }
        Token[] endValue = rightTokens[endValueStartIndex..tokenIndex];
        
        Node node = new Node();
        node.Token = this;
        node.Children.Add(new Node() { Token = iterator });
        node.Children.Add(NodeConverter.CreateNodeTree(startAssignation));
        node.Children.Add(NodeConverter.CreateNodeTree(endValue));
        NodeConverter.specialOperationStack.Push(node);
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        if (interpreter.MetaDataContain("for" + interpreter.GetInstructionIndex()))
        {
            string iteratorName = node.Children[0].Token.Value as string;
            int currentValue = interpreter.GetVariable<int>(iteratorName);
            int endValue = interpreter.MetaDataGet<int>("for" + interpreter.GetInstructionIndex());
            if (currentValue < endValue)
            {
                interpreter.SetInstructionLoopIndex();
                interpreter.SetVariable(iteratorName, currentValue + 1);
            }
            else
            {
                interpreter.SkipToEnd();
            }
        }
        else
        {
            (node.Children[1].Token as INodeConverter).ExecuteNode(node.Children[1], interpreter);
            int endValue = 0;
            
            switch (node.Children[2].Token.Type)
            {
                case TokenType.Number:
                    endValue = int.Parse(node.Children[2].Token.Value as string);
                    break;
                case TokenType.Operator:
                    (node.Children[2].Token as INodeConverter).ExecuteNode(node.Children[2], interpreter);
                    endValue = interpreter.Pop<int>();
                    break;
                case TokenType.Variable:
                    endValue = interpreter.GetVariable<int>(node.Children[2].Token.Value as string);
                    break;
                default:
                    throw new Exception($"Unable to interprete end value to for loop at {interpreter.GetInstructionIndex()}");
            }
            interpreter.MetaDataSet("for"+interpreter.GetInstructionIndex(), endValue);
            interpreter.SetInstructionLoopIndex();
        }
    }
}

public class EndToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest; 
        set => throw new NotImplementedException();
    }

    public EndToken() : base(TokenType.Keyword, "Fin") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node();
        node.Token = this;
        if(rightTokens.Length > 0)
        {
            node.Children.Add(new Node(){Token = rightTokens[0]});
        }
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        if (node.Children.Count > 0)
        {
            switch ((node.Children[0].Token.Value as string).ToLower())
            {
                case "pour":
                    interpreter.JumpToLastLoop();
                    break;
                case "tant":
                    interpreter.JumpToLastLoop();
                    break;
                case "si":
                    break;
                default:
                    throw new Exception($"Unable to interprete end value at {interpreter.GetInstructionIndex()}");
            }
        }
        else
        {
            interpreter.Stop();
        }
        
        
    }
}

#endregion

#region AlgoInstruction
public class AlgorithmToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public AlgorithmToken() : base(TokenType.Keyword, "Algorithme") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node(){Token = this};
        if (rightTokens.Length == 1)
        {
            node.Children.Add(new Node(){Token = rightTokens[0]});
        }
        else
        {
            throw new Exception("Algorithm name is not valid");
        }

        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.MetaDataSet("AlgoName", node.Children[0].Token.Value as string);
    }
}

public class VariableDeclarationToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public VariableDeclarationToken() : base(TokenType.Keyword, "Variables") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        return null;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        throw new NotImplementedException();
    }
}

public class StartToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public StartToken() : base(TokenType.Keyword, "Début") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        return null;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        throw new NotImplementedException();
    }
}

#endregion

#region BinaryOperator

public class SuperiorToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public SuperiorToken() : base(TokenType.Operator, ">") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node(){Token = this};
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteNode(node.Children[0]);
        interpreter.ExecuteNode(node.Children[1]);
        object rightValue = interpreter.Pop();
        object leftValue = interpreter.Pop();
        interpreter.Push(IsSuperior(leftValue, rightValue));
    }

    public static bool IsSuperior(object a, object b)
    {
        if (a is int && b is int)
        {
            return (int)a > (int)b;
        }
        if (a is float && b is float)
        {
            return (float)a > (float)b;
        }
        if (a is int && b is float)
        {
            return (int)a > (float)b;
        }
        if (a is float && b is int)
        {
            return (float)a > (int)b;
        }
        if(a is char && b is char)
        {
            return (char) a > (char) b;
        }
        throw new Exception("Unable to compare values");
    }
}

public class InferiorToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public InferiorToken() : base(TokenType.Operator, "<") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node(){Token = this};
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteNode(node.Children[0]);
        interpreter.ExecuteNode(node.Children[1]);
        object rightValue = interpreter.Pop();
        object leftValue = interpreter.Pop();
        
        interpreter.Push(IsInferior(leftValue, rightValue));
    }
    
    public static bool IsInferior(object a, object b)
    {
        if (a is int && b is int)
        {
            return (int)a < (int)b;
        }
        if (a is float && b is float)
        {
            return (float)a < (float)b;
        }
        if (a is int && b is float)
        {
            return (int)a < (float)b;
        }
        if (a is float && b is int)
        {
            return (float)a < (int)b;
        }
        if(a is char && b is char)
        {
            return (char) a < (char) b;
        }
        throw new Exception("Unable to compare values");
    }
}

public class EqualToken : Token, INodeConverter, ITokenLexer
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public EqualToken() : base(TokenType.Operator, "=") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node(){Token = this};
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteNode(node.Children[0]);
        interpreter.ExecuteNode(node.Children[1]);
        object rightValue = interpreter.Pop();
        object leftValue = interpreter.Pop();
        interpreter.Push(AreEqual(leftValue, rightValue));
    }

    public static bool AreEqual(object a, object b)
    {
        if (a is int && b is int)
        {
            return (int)a == (int)b;
        }
        if (a is float && b is float)
        {
            return (float)a == (float)b;
        }
        if (a is int && b is float)
        {
            return (int)a == (float)b;
        }
        if (a is float && b is int)
        {
            return (float)a == (int)b;
        }
        if (a is string && b is string)
        {
            return (string)a == (string)b;
        }
        if(a is char && b is char)
        {
            return (char) a == (char) b;
        }
        if (a.GetType() != b.GetType())
        {
            return false;
        }
        throw new Exception("Unable to compare values");
    }

    public void Lex(ref GroupToken currentGroupToken, ref Token[] nextTokens)
    {
        if (currentGroupToken.Tokens.Last() is InferiorToken)
        {
            currentGroupToken.Tokens.RemoveAt(currentGroupToken.Tokens.Count - 1);
            currentGroupToken.Tokens.Add(new InferiorEqualToken());
        }
        else if (currentGroupToken.Tokens.Last() is SuperiorToken)
        {
            currentGroupToken.Tokens.RemoveAt(currentGroupToken.Tokens.Count - 1);
            currentGroupToken.Tokens.Add(new SuperiorEqualToken());
        }
        else
        {
            currentGroupToken.Tokens.Add(this);
        }
    }
}

public class SuperiorEqualToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public SuperiorEqualToken() : base(TokenType.Operator, ">=") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node(){Token = this};
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteNode(node.Children[0]);
        interpreter.ExecuteNode(node.Children[1]);
        object rightValue = interpreter.Pop();
        object leftValue = interpreter.Pop();
        interpreter.Push(IsSuperiorOrEqual(leftValue, rightValue));
    }

    private static bool IsSuperiorOrEqual(object a, object b)
    {
        return SuperiorToken.IsSuperior(a,b) || EqualToken.AreEqual(a,b);
    }
}

public class InferiorEqualToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest;
        set => throw new NotImplementedException();
    }

    public InferiorEqualToken() : base(TokenType.Operator, "<=") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node(){Token = this};
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteNode(node.Children[0]);
        interpreter.ExecuteNode(node.Children[1]);
        object rightValue = interpreter.Pop();
        object leftValue = interpreter.Pop();
        interpreter.Push(IsInferiorOrEqual(leftValue, rightValue));
    }

    private static bool IsInferiorOrEqual(object a, object b)
    {
        return InferiorToken.IsInferior(a,b) || EqualToken.AreEqual(a,b);
    }
}

#endregion

#region BooleanOperators

public class AndOperatorToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest; 
        set => throw new NotImplementedException();
    }

    public AndOperatorToken() : base(TokenType.BooleanOperator, "ET") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node(){Token = this};
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteNode(node.Children[0]);
        interpreter.ExecuteNode(node.Children[1]);
        bool rightValue = interpreter.Pop<bool>();
        bool leftValue = interpreter.Pop<bool>();
        interpreter.Push(leftValue && rightValue);
    }
}

public class OrOperatorToken : Token, INodeConverter
{
    public TokenPriority Priority
    {
        get => TokenPriority.Highest; 
        set => throw new NotImplementedException();
    }

    public OrOperatorToken() : base(TokenType.BooleanOperator, "OU") { }

    public Node ConvertToNodeTree(Token[] leftTokens, Token[] rightTokens)
    {
        Node node = new Node(){Token = this};
        node.Children.Add(NodeConverter.CreateNodeTree(leftTokens));
        node.Children.Add(NodeConverter.CreateNodeTree(rightTokens));
        return node;
    }

    public void ExecuteNode(Node node, NodeInterpreter interpreter)
    {
        interpreter.ExecuteNode(node.Children[0]);
        interpreter.ExecuteNode(node.Children[1]);
        bool rightValue = interpreter.Pop<bool>();
        bool leftValue = interpreter.Pop<bool>();
        interpreter.Push(leftValue || rightValue);
    }
}

#endregion