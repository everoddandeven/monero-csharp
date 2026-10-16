using Monero.Daemon.Model;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Output test utilities.
/// </summary>
public static class OutputUtils
{
    /// <summary>
    /// Test a key image.
    /// </summary>
    public static void TestKeyImage(MoneroKeyImage? image)
    {
        Assert.That(image, Is.Not.Null);
        Assert.That(image!.Hex, Is.Not.Null.And.Not.Empty);
        if (image.Signature != null) Assert.That(image.Signature, Is.Not.Empty);
    }

    /// <summary>
    /// Test a daemon output distribution entry.
    /// </summary>
    public static void TestOutputDistributionEntry(MoneroOutputDistributionEntry entry)
    {
        GenUtils.TestUnsignedBigInteger(entry.Amount);
        Assert.That(entry.Base, Is.Not.Null);
        Assert.That(entry.Distribution, Is.Not.Null.And.Not.Empty);
        Assert.That(entry.StartHeight, Is.Not.Null);
    }

    /// <summary>
    /// Test a daemon output histogram entry.
    /// </summary>
    public static void TestOutputHistogramEntry(MoneroOutputHistogramEntry entry)
    {
        GenUtils.TestUnsignedBigInteger(entry.Amount);
        Assert.That(entry.NumInstances, Is.Not.Null);
        Assert.That(entry.UnlockedInstances, Is.Not.Null);
        Assert.That(entry.RecentInstances, Is.Not.Null);
    }

    /// <summary>
    /// Test an output.
    /// </summary>
    public static void TestOutput(MoneroOutput? output, ModelTestContext? context = null)
    {
        Assert.That(output, Is.Not.Null);
        GenUtils.TestUnsignedBigInteger(output!.Amount);
        if (context == null) return;

        Assert.That(output.Tx, Is.Not.Null);
        if (output.Tx!.InTxPool == true || context.HasOutputIndices == false)
        {
            Assert.That(output.Index, Is.Null);
        }
        else
        {
            Assert.That(output.Index, Is.Not.Null);
            Assert.That(output.StealthPublicKey, Is.Not.Null.And.Not.Empty);
        }
    }

    /// <summary>
    /// Test an input.
    /// </summary>
    public static void TestInput(MoneroOutput? input, ModelTestContext? context)
    {
        Assert.That(input, Is.Not.Null);
        TestOutput(input);
        TestKeyImage(input!.KeyImage);
        Assert.That(input.RingOutputIndices, Is.Not.Null.And.Not.Empty);
    }
}
