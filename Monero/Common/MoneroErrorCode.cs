namespace Monero.Common;

/// <summary>
/// The kind of a failure that monero-c reports. It comes from the type of the exception that ended the call in monero-c, not from its text,
/// so a failure that monero-cpp reports with its general exception is <see cref="Generic"/>, a network failure included. The values may grow
/// before monero-c reaches 1.0: treat a value that you don't know as <see cref="Generic"/>.
/// </summary>
public enum MoneroErrorCode
{
    /// <summary>Any other failure.</summary>
    Generic = 1,

    /// <summary>A null value, a value out of range, or a text that doesn't parse.</summary>
    InvalidArgument = 2,

    /// <summary>A memory allocation failed.</summary>
    OutOfMemory = 3,

    /// <summary>The daemon or the wallet RPC server answered with an error, or with an HTTP status that is not OK.</summary>
    Rpc = 4,

    /// <summary>A file or a network operation failed.</summary>
    Io = 5,
}
