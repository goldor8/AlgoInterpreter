namespace AlgoInterpreter;

public class Node
{
    public Token Token { get; set; }
    public List<Node> Children { get; set; }
        
    public Node()
    {
        Children = new List<Node>();
    }
}