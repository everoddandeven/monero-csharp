namespace Monero.IntegrationTests.Utils;

/// <summary>
/// General test utilities.
/// </summary>
public static class GenUtils
{
    /// <summary>
    /// Wait for the specified time.
    /// </summary>
    /// <param name="milliseconds">milliseconds to wait for.</param>
    public static Task WaitFor(int milliseconds) => Task.Delay(milliseconds);

    /// <summary>
    /// Test that a number is an unsigned big integer.
    /// </summary>
    /// <param name="num">number to test.</param>
    /// <param name="nonZero"><c>true</c> to assert that the number is not zero, <c>false</c> to assert that it is zero, <c>null</c> to not check.</param>
    public static void TestUnsignedBigInteger(ulong? num, bool? nonZero = null)
    {
        Assert.That(num, Is.Not.Null, "Number is null");

        if (nonZero == true) Assert.That(num, Is.GreaterThan(0UL), "Number is zero");
        else if (nonZero == false) Assert.That(num, Is.EqualTo(0UL), $"Number is not zero: {num}");
    }

    /// <summary>
    /// Count the number of occurrences of each value in a list.
    /// </summary>
    /// <param name="instances">values to count occurrences of.</param>
    /// <returns>a map of each value to its number of occurrences.</returns>
    public static Dictionary<ulong, int> CountNumInstances(IEnumerable<ulong> instances)
    {
        var counts = new Dictionary<ulong, int>();
        foreach (var instance in instances) counts[instance] = counts.GetValueOrDefault(instance) + 1;
        return counts;
    }
}
