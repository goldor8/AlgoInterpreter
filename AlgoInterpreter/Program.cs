// See https://aka.ms/new-console-template for more information

using AlgoInterpreter;

if (args.Length >= 1)
{
    Console.WriteLine("Interpreting " + args[0] +"...");    
    Token.InitTokenFactoryRegistry();
    List<List<Token>> tokens = Lexer.TokenizeFile(args[0]);
    List<Node> bigTree = new List<Node>();
    foreach (List<Token> tokenList in tokens)
    {
        Node lineNode = NodeConverter.CreateNodeTree(tokenList.ToArray());
        if(lineNode != null)
        {
            bigTree.Add(lineNode);
        }
    }
    
    NodeInterpreter interpreter = new NodeInterpreter(bigTree.ToArray());
    interpreter.Execute();
    Console.WriteLine("Interpreting finished.");
}
else
{
    Console.WriteLine("No file specified");
}