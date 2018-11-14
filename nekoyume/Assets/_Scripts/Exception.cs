public class InvalidMoveException : System.Exception
{
    public InvalidMoveException()
    { }

    public InvalidMoveException(string message) : base(message)
    { }

    public InvalidMoveException(string message, System.Exception inner) : base(message, inner)
    { }
}
