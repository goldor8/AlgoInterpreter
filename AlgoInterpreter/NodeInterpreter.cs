namespace AlgoInterpreter;

public class NodeInterpreter
{
    public Dictionary<string, object> metaDataRegistry = new Dictionary<string, object>();
    public Dictionary<string, object> registry = new Dictionary<string, object>();
    public Dictionary<string, Type> typeRegistry = new Dictionary<string, Type>();
    public Dictionary<string, Action<Node[]>> functionRegistry = new Dictionary<string, Action<Node[]>>();
    private Stack<object> stack = new Stack<object>();
    private Stack<int> loopIndexStack = new Stack<int>();
    private Node[] scriptInstruction;
    private int instructionIndex = 0;
    
    public NodeInterpreter(Node[] scriptInstruction)
    {
        this.scriptInstruction = scriptInstruction;
        InitFunction();
    }

    private void InitFunction()
    {
        functionRegistry.Add("Afficher", arguments =>
        {
            string[] values = new string[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                values[i] = ExtractValue(arguments[i]).ToString();
            }

            Console.WriteLine(string.Join(" ", values));
        });
        functionRegistry.Add("Saisir", arguments =>
        {
            if(arguments.Length != 1)
                throw new Exception("Saisir() takes only one argument");
            if (arguments[0].Token is VariableToken variableToken)
            {
                Type variableType = GetVariableType(variableToken.Value as string);
                if(variableType == typeof(int))
                {
                    int value = int.Parse(Console.ReadLine());
                    registry[variableToken.Value as string] = value;
                }
                else if(variableType == typeof(string))
                {
                    string value = Console.ReadLine();
                    registry[variableToken.Value as string] = value;
                }
                else if(variableType == typeof(bool))
                {
                    bool value = Lexer.ParseBoolean(Console.ReadLine());
                    registry[variableToken.Value as string] = value;
                }
                else
                {
                    throw new Exception("Type not supported");
                }
            }
        });
        functionRegistry.Add("longueur", arguments =>
        {
            if (arguments.Length != 1)
                throw new Exception("longueur() takes only one argument");
            if (arguments[0].Token is VariableToken variableToken)
            {
                object variable = GetVariable(variableToken.Value as string);
                if (variable is string str)
                {
                    stack.Push(str.Length);
                }
                else if(variable is Array array)
                {
                    stack.Push(array.Length);
                }
                else
                {
                    throw new Exception("Type not supported");
                }
            }
        });
    }
    
    public void ExecuteFunction(string functionName, Node[] arguments)
    {
        if (functionRegistry.ContainsKey(functionName))
        {
            functionRegistry[functionName](arguments);
        }
    }

    public void ExecuteNode(Node node)
    {
        if (node.Token is INodeConverter)
        {
            (node.Token as INodeConverter).ExecuteNode(node, this);
        }
        else
        {
            throw new Exception($"Token '{node.Token}' is not a INodeConverter, can't execute it");
        }
    }

    public void LoadScript(Node[] instructions)
    {
        scriptInstruction = instructions;
    }

    public void Execute()
    {
        while (instructionIndex < scriptInstruction. Length)
        {
            if (scriptInstruction[instructionIndex].Token is INodeConverter)
            {
                (scriptInstruction[instructionIndex].Token as INodeConverter).ExecuteNode(scriptInstruction[instructionIndex], this);
            }

            instructionIndex++;
        }
    }

    public int GetInstructionIndex()
    {
        return instructionIndex;
    }

    public void SetInstructionLoopIndex()
    {
        loopIndexStack.Push(instructionIndex);
    }

    public void JumpToLastLoop()
    {
        if(loopIndexStack.Count == 0) return; //It mean it is on last end node and this the end of the program 
        instructionIndex = loopIndexStack.Pop() - 1;
    }

    public void SkipToEnd()
    {
        while (scriptInstruction[instructionIndex].Token is not EndToken)
        {
            instructionIndex++;
        }
    }
    
    public void MetaDataSet(string key, object value)
    {
        if (metaDataRegistry.ContainsKey(key))
        {
            metaDataRegistry[key] = value;
            return;
        }
        metaDataRegistry.Add(key, value);
    }

    public bool MetaDataContain(string key)
    {
        return metaDataRegistry.ContainsKey(key);
    }

    public object MetaDataGet(string key)
    {
        if (metaDataRegistry.ContainsKey(key))
        {
            return metaDataRegistry[key];
        }

        return null;
    }
    
    public T MetaDataGet<T>(string key)
    {
        if (metaDataRegistry.ContainsKey(key))
        {
            return (T) metaDataRegistry[key];
        }

        throw new Exception($"Uncastable meta data for key {key}");
    }

    public void RegisterVariable(string varName, Type type)
    {
        typeRegistry.Add(varName, type);
        registry.Add(varName, null);
    }

    public void RegisterVariableArray(string varName, Type type, int[] size)
    {
        object[] array = PopulateDimensionalArray(size);
        typeRegistry.Add(varName, type.MakeArrayType(size.Length));
        registry.Add(varName, array);
    }

    private object[] PopulateDimensionalArray(int[] dimensions)
    {
        if(dimensions.Length < 1) throw new Exception("Array must have at least one dimension");
        object[] array = new object[dimensions[0]];
        if(dimensions.Length == 1) return array;
        for (int i = 0; i < dimensions[0]; i++)
        {
            array[i] = PopulateDimensionalArray(dimensions.Skip(1).ToArray());
        }

        return array;
    }

    public void SetVariable<T>(string varName, T value)
    {
        if (registry.ContainsKey(varName))
        {
            Type type = typeRegistry[varName];
            if (type == value.GetType())
            {
                registry[varName] = value;
            }
            else
            {
                //todo: implement cast
                throw new Exception("Wrong type assignation");
            }
        }
    }
    
    public void SetVariableArray<T>(string varName, int[] dimensions, T value)
    {
        (GetVariableArray(varName, dimensions[..^1]) as object[])[dimensions[^1] - 1] = value;//minus 1 because in algo array start at 1
    }

    public void SetVariable<T>(VariableToken variableToken, T value)
    {
        string varName = variableToken.Value as string;
        if (variableToken.DimensionsTokens.Count == 0)
        {
            SetVariable(varName, value);
        }
        else
        {
            SetVariableArray(varName, variableToken.Dimensions.Select(dimensionNode => (int) ExtractValue(dimensionNode)).ToArray(), value);
        }
    }

    public T GetVariable<T>(string varName)
    {
        return (T) GetVariable(varName);
    }
    
    public object GetVariable(string varName)
    {
        if (registry.ContainsKey(varName))
        {
            return registry[varName];
        }

        throw new Exception($"Undeclared variable '{varName}'");
    }
    
    public Type GetVariableType(string varName)
    {
        if (typeRegistry.ContainsKey(varName))
        {
            return typeRegistry[varName];
        }

        throw new Exception($"Undeclared variable type '{varName}'");
    }
    
    public object GetVariableArray(string varName, int[] dimensions)
    {
        object array = GetVariable(varName);
        
        if(array is string str)
        {
            if(dimensions.Length != 1) throw new Exception("String can only have one dimension");
            return str[dimensions[0] - 1];
        }
        
        for (int i = 0; i < dimensions.Length; i++)
        {
            array = (array as object[])[dimensions[i] - 1];//minus 1 because in algo array start at 1
        }

        return array;
    }
    
    public object GetVariable(VariableToken variableToken)
    {
        if(variableToken.DimensionsTokens.Count == 0) return GetVariable(variableToken.Value as string);
        return GetVariableArray(variableToken.Value as string, variableToken.Dimensions.Select(dimensionNode => (int) ExtractValue(dimensionNode)).ToArray());
    }

    public void Push(object value)
    {
        stack.Push(value);
    }

    public object Pop()
    {
        return stack.Pop();
    }
    
    public T Pop<T>()
    {
        return (T) stack.Pop();
    }

    public static Type GetAlgoType(Token token)
    {
        //todo: compete type
        if (token is TypeToken typeToken)
        {
            return typeToken.Type;
        }
        return GetAlgoType(token.Value as string);
    }

    public static TypeToken GetAlgoType(Token[] tokens)
    {
        if(tokens.Length == 1)
        {
            return new TypeToken(GetAlgoType(tokens[0].Value as string));
        }
        if (tokens.Length == 3)
        {
            if(tokens[1].Value as string != "de") throw new ArgumentException("Invalid type declaration");
            switch (tokens[0].Value as string)
            {
                case "chaine":
                case "chaîne":
                    switch (tokens[2].Value as string)
                    {
                        case "caractères":
                        case "caracteres":
                        case "caractère":
                        case "caractere":
                            return new TypeToken(GetAlgoType("string"));
                        default:
                            throw new ArgumentException("Invalid type declaration");
                    }
                case "tableau":
                    TypeToken typeToken = new TypeToken(GetAlgoType(tokens[2].Value as string));
                    typeToken.IsArray = true;
                    return typeToken;
            }
        }
        
        throw new Exception("Type declaration is not valid");
    }

    public static Type GetAlgoType(string typeName)
    {
        switch (typeName)
        {
            case "entier":
                return typeof(int);
            case "réel":
            case "reel":
                return typeof(float);
            case "booléen":
            case "booleen":
                return typeof(bool);
            case "string":
                return typeof(string);
            case "array":
                return typeof(Array);
            default:
                throw new Exception($"Unknown type {typeName}");
        }
    }
    
    public object ExtractArithmeticValue(Node value)
    {
        switch (value.Token.Type)
        {
            case Token.TokenType.Operator:
                (value.Token as INodeConverter).ExecuteNode(value, this);
                return Pop();
            case Token.TokenType.Number:
                return NumberToken.ParseIntOrFloat(value.Token.Value as string);
            case Token.TokenType.Variable:
                return GetVariable(value.Token.Value as string);
            default:
                throw new Exception($"Unable to extract arithmetic value of type {value.Token.Type}");
        }
    }
    
    public object ExtractValue(Node value)
    {
        switch (value.Token.Type)
        {
            case Token.TokenType.Operator:
            case Token.TokenType.Function:
                (value.Token as INodeConverter).ExecuteNode(value, this);
                return Pop();
            case Token.TokenType.Number:
                return NumberToken.ParseIntOrFloat(value.Token.Value as string);
            case Token.TokenType.Variable:
                VariableToken variableToken = value.Token as VariableToken;
                return GetVariable(variableToken);
            case Token.TokenType.String:
                return value.Token.Value;
            case Token.TokenType.Boolean:
                return (bool) value.Token.Value;
            case Token.TokenType.Group:
                return ExtractValue(value.Children[0]);
            default:
                throw new Exception($"Unable to extract value of type {value.Token.Type}");
        }
    }

    public void Stop()
    {
        instructionIndex = scriptInstruction.Length;
    }

    public void JumpToElse()
    {
        while (scriptInstruction[instructionIndex].Token is not ElseToken)
        {
            instructionIndex++;
        }
    }
}