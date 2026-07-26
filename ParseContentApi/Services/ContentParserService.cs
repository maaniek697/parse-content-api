namespace ParseContentApi.Services;

public sealed class ContentParsingException : Exception
{
    public ContentParsingException(string message) : base(message) { }

    public ContentParsingException(string message, Exception inner) : base(message, inner) { }
}