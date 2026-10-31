namespace Monero.Common;

/// <summary>
/// An error reported by monero-c, such as the message of a daemon or wallet that rejects a request.
/// </summary>
public class MoneroError : Exception
{
    /// <summary>
    /// Initialize a new error.
    /// </summary>
    /// <param name="message">the message of the error.</param>
    public MoneroError(string message) : base(message)
    {
    }

    /// <summary>
    /// Initialize a new error.
    /// </summary>
    /// <param name="message">the message of the error.</param>
    /// <param name="code">the kind of the error.</param>
    public MoneroError(string message, MoneroErrorCode code) : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Initialize a new error.
    /// </summary>
    /// <param name="message">the message of the error.</param>
    /// <param name="innerException">the error that caused this one.</param>
    public MoneroError(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// The kind of the error. It is <see cref="MoneroErrorCode.Generic"/> unless monero-c tells another one.
    /// </summary>
    public MoneroErrorCode Code { get; } = MoneroErrorCode.Generic;
}
