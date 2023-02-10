namespace AlgoInterpreter;

public class Syntax
{
    public static string[] keywords = new []{"algorithme","variables","début","fin","si","alors","sinon","tant","que","à","faire","pour","alors","fonction","paramètres","parametres"};
    public static string[] types = new []{"entier","réel","reel","booléen","booleen","caractère","chaine"};
    public static string[] booleans = new []{"vrai","faux","Vrai","Faux"};
    public static string[] booleanOperators = new[] { "ET", "OU" };
    public static char[] operators = new []{'<','>','=','+','-','*','/','←',':'};
    public static char[] parenthesis = new []{'(',')','{','}','[',']'};
    public static char[] textDelimiters = new []{'"','\''};
    public static char[] separators = new []{','};
}