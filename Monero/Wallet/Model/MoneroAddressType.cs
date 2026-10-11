namespace Monero.Wallet.Model;

/// <summary>
/// Enumerates the types of address. The numbers are the ones of monero-cpp, and the JSON has them as numbers.
/// </summary>
public enum MoneroAddressType
{
    /// <summary>
    /// The primary address of an account.
    /// </summary>
    PrimaryAddress = 0,

    /// <summary>
    /// A standard address with a payment id.
    /// </summary>
    IntegratedAddress,

    /// <summary>
    /// A subaddress of an account.
    /// </summary>
    Subaddress
}
