namespace Monero.Common;

/// <summary>
/// Joins and splits the 128-bit numbers of the daemon, which monero-c and monero-cpp write as two 64-bit halves.
/// </summary>
internal static class MoneroUInt128
{
    /// <summary>
    /// Join the two halves of a 128-bit number.
    /// </summary>
    /// <param name="low">least significant 64 bits.</param>
    /// <param name="high">most significant 64 bits.</param>
    /// <returns>the number, or <c>null</c> if neither half is set. A half that is missing counts as zero.</returns>
    public static UInt128? Combine(ulong? low, ulong? high)
    {
        if (low == null && high == null) return null;
        return new UInt128(high ?? 0UL, low ?? 0UL);
    }

    /// <summary>
    /// Split a 128-bit number in its two halves.
    /// </summary>
    /// <param name="value">the number, or <c>null</c> to clear both halves.</param>
    /// <returns>the least significant and the most significant 64 bits.</returns>
    public static (ulong? Low, ulong? High) Split(UInt128? value)
    {
        if (value == null) return (null, null);
        return ((ulong)(value.Value & ulong.MaxValue), (ulong)(value.Value >> 64));
    }
}
