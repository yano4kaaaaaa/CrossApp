namespace Core;

// Технічна помилка інфраструктури (зіпсований JSON тощо); оригінал лишається в InnerException.
public sealed class StorageException : Exception
{
    public StorageException(string message, Exception inner) : base(message, inner) { }
}