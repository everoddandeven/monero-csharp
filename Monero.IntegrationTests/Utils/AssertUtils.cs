using Monero.Common;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Assert utilities.
/// </summary>
public static class AssertUtils
{
    /// <summary>
    /// Assert that two models are equal: they have the same JSON, which is what monero-c writes for a model.
    /// </summary>
    /// <param name="expected">the expected model.</param>
    /// <param name="actual">the model to check.</param>
    /// <param name="message">failure message.</param>
    /// <typeparam name="T">type of the models.</typeparam>
    public static void AssertEquals<T>(T? expected, T? actual, string message = "assertion failed")
    {
        Assert.That(MoneroJson.Serialize(actual), Is.EqualTo(MoneroJson.Serialize(expected)), message);
    }
}
