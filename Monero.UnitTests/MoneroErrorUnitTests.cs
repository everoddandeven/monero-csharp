using Monero.Common;

namespace Monero.UnitTests;

/// <summary>
/// Unit tests of <see cref="MoneroError"/> and <see cref="MoneroErrorCode"/>. They run without monero-c, a node or the network.
/// </summary>
[TestFixture]
public class MoneroErrorUnitTests
{
    [Test]
    public void AnErrorIsGenericUnlessItHasAnotherKind()
    {
        Assert.That(new MoneroError("a message").Code, Is.EqualTo(MoneroErrorCode.Generic));
        Assert.That(new MoneroError("a message", new InvalidOperationException()).Code, Is.EqualTo(MoneroErrorCode.Generic));
    }

    [TestCase(MoneroErrorCode.Generic)]
    [TestCase(MoneroErrorCode.InvalidArgument)]
    [TestCase(MoneroErrorCode.OutOfMemory)]
    [TestCase(MoneroErrorCode.Rpc)]
    [TestCase(MoneroErrorCode.Io)]
    public void AnErrorKeepsItsKindAndItsMessage(MoneroErrorCode code)
    {
        var error = new MoneroError("a message", code);

        Assert.That(error.Code, Is.EqualTo(code));
        Assert.That(error.Message, Is.EqualTo("a message"));
    }

    // the numbers are the ones of monero_error_code of monero-c, which Check reads as they are
    [Test]
    public void TheKindsHaveTheNumbersOfMoneroC()
    {
        Assert.That((int)MoneroErrorCode.Generic, Is.EqualTo(1));
        Assert.That((int)MoneroErrorCode.InvalidArgument, Is.EqualTo(2));
        Assert.That((int)MoneroErrorCode.OutOfMemory, Is.EqualTo(3));
        Assert.That((int)MoneroErrorCode.Rpc, Is.EqualTo(4));
        Assert.That((int)MoneroErrorCode.Io, Is.EqualTo(5));
        Assert.That(Enum.GetValues<MoneroErrorCode>(), Has.Length.EqualTo(5));
    }
}
