using System;

public class InvalidActionException : Exception
{
    public InvalidActionException()
    {
    }

    public InvalidActionException(string message) : base(message)
    {
    }

    public InvalidActionException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class NotFoundComponentException : Exception
{
    public NotFoundComponentException()
    {
    }

    public NotFoundComponentException(string message) : base(message)
    {
    }

    public NotFoundComponentException(string message, Exception inner) : base(message, inner)
    {
    }
}
