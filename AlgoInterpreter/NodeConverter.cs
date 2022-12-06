using System.Collections;

namespace AlgoInterpreter;

public class NodeConverter
{
    public static Stack<Node> specialOperationStack = new Stack<Node>();
    public static Node CreateNodeTree(Token[] tokens)
    {
        Node node;
        
        (Token mainToken,Token[] leftTokens, Token[] rightTokens) = GetMainNode(tokens);
        if (mainToken is INodeConverter)
        {
            node = ((INodeConverter)mainToken).ConvertToNodeTree(leftTokens, rightTokens);
        }
        else
        {
            Console.Error.WriteLine("Error: Main token is not a node converter");
            return null;
        }

        return node;
    }

    public static (Token mainToken, Token[] leftTokens, Token[] rightTokens) GetMainNode(Token[] tokens)
    {
        if(tokens.Length == 1)
        {
            if (tokens[0] is GroupToken)
            {
                return GetMainNode((tokens[0] as GroupToken).Tokens.ToArray());
            }
            
            return (tokens[0], new Token[0], new Token[0]);
        }
        int mainTokenIndex = 0;
        Token.TokenPriority mainTokenTokenPriority = Token.TokenPriority.Lowest;
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] is INodeConverter)
            {
                Token.TokenPriority tokenPriority = ((INodeConverter) tokens[i]).Priority;
                if ((int) tokenPriority > (int) mainTokenTokenPriority)
                {
                    mainTokenIndex = i;
                    mainTokenTokenPriority = tokenPriority;
                }
            }
        }
        
        Token mainToken = tokens[mainTokenIndex];
        Token[] leftTokens = tokens.Take(mainTokenIndex).ToArray();
        Token[] rightTokens = tokens.Skip(mainTokenIndex + 1).ToArray();
        
        return (mainToken, leftTokens, rightTokens);
    }
    
    
}