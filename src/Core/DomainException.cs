namespace Core;

// Порушення бізнес-правила (примірник уже видано, видачу вже закрито, погані дані імпорту).
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}