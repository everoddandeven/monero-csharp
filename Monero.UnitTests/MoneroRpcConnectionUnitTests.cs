using Monero.Common;
using Monero.Wallet.Model;
using static Monero.UnitTests.ModelTestSupport;

namespace Monero.UnitTests;

/// <summary>
/// Unit tests of <see cref="MoneroRpcConnection"/>. They run without monero-c, a node or the network.
/// </summary>
[TestFixture]
public class MoneroRpcConnectionUnitTests
{
    private const string NodeUri = "http://127.0.0.1:18081";
    private const string WalletUri = "http://127.0.0.1:18082";
    private const uint Timeout = 15000;

    private static MoneroRpcConnection FullConnection()
    {
        return new MoneroRpcConnection("test_node:18081", "user", "abc123")
        {
            ProxyUri = "127.0.0.1:9050",
            ZmqUri = "test_node:18084",
            Priority = 1,
            TimeoutMs = 1000,
            ResponseTime = 42,
            SslVerify = false,
            IsOnline = true,
            IsAuthenticated = true
        };
    }

    private static MoneroRpcConnection Read(Engine engine, string json)
    {
        return (MoneroRpcConnection)ReadWith(engine, json, typeof(MoneroRpcConnection), MoneroWalletJsonContext.Default);
    }

    private static string Write(Engine engine, MoneroRpcConnection connection)
    {
        return WriteWith(engine, connection, typeof(MoneroRpcConnection), MoneroWalletJsonContext.Default);
    }

    // construction

    [Test]
    public void NewConnection_HasNothingSet()
    {
        var connection = new MoneroRpcConnection();

        Assert.Multiple(() =>
        {
            Assert.That(connection.Uri, Is.Null);
            Assert.That(connection.Username, Is.Null);
            Assert.That(connection.Password, Is.Null);
            Assert.That(connection.ProxyUri, Is.Null);
            Assert.That(connection.ZmqUri, Is.Null);
            Assert.That(connection.Priority, Is.Null);
            Assert.That(connection.TimeoutMs, Is.Null);
            Assert.That(connection.ResponseTime, Is.Null);
            Assert.That(connection.SslVerify, Is.Null);
            Assert.That(connection.IsOnline, Is.Null);
            Assert.That(connection.IsAuthenticated, Is.Null);
        });
    }

    [Test]
    public void Constructor_WithAUri_HasNoCredentials()
    {
        var connection = new MoneroRpcConnection(NodeUri);

        Assert.Multiple(() =>
        {
            Assert.That(connection.Uri, Is.EqualTo(NodeUri));
            Assert.That(connection.Username, Is.Null);
            Assert.That(connection.Password, Is.Null);
        });
    }

    [Test]
    public void Constructor_WithCredentials_KeepsThem()
    {
        var connection = new MoneroRpcConnection(NodeUri, "rpc_daemon_user", "abc123");

        Assert.Multiple(() =>
        {
            Assert.That(connection.Uri, Is.EqualTo(NodeUri));
            Assert.That(connection.Username, Is.EqualTo("rpc_daemon_user"));
            Assert.That(connection.Password, Is.EqualTo("abc123"));
        });
    }

    [Test]
    public void Credentials_CanBeSetAndCleared()
    {
        var connection = new MoneroRpcConnection(NodeUri);

        connection.Username = "rpc_daemon_user";
        connection.Password = "abc123";
        Assert.Multiple(() =>
        {
            Assert.That(connection.Username, Is.EqualTo("rpc_daemon_user"));
            Assert.That(connection.Password, Is.EqualTo("abc123"));
        });

        connection.Username = null;
        connection.Password = null;
        Assert.Multiple(() =>
        {
            Assert.That(connection.Username, Is.Null);
            Assert.That(connection.Password, Is.Null);
        });
    }

    [Test]
    public void Uri_CanBeChanged()
    {
        var connection = new MoneroRpcConnection(NodeUri);

        connection.Uri = "http://moneronkvv2hu2anvcc5b4qd5y7strnc2ob6khqsrtikmhocyvjpdjyd.onion:18089";
        Assert.That(connection.Uri, Does.EndWith(".onion:18089"));

        connection.Uri = null;
        Assert.That(connection.Uri, Is.Null);
    }

    // serialization

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Serialize_NodeConnection(Engine engine)
    {
        var connection = new MoneroRpcConnection(NodeUri, "rpc_daemon_user", "abc123") { Priority = 0, TimeoutMs = Timeout };

        Assert.That(Write(engine, connection),
            Is.EqualTo("""{"uri":"http://127.0.0.1:18081","username":"rpc_daemon_user","password":"abc123","priority":0,"timeoutMs":15000}"""));
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Serialize_WalletConnection(Engine engine)
    {
        var connection = new MoneroRpcConnection(WalletUri, "rpc_user", "abc123") { Priority = 0, TimeoutMs = Timeout };

        Assert.That(Write(engine, connection),
            Is.EqualTo("""{"uri":"http://127.0.0.1:18082","username":"rpc_user","password":"abc123","priority":0,"timeoutMs":15000}"""));
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Serialize_EmptyConnection_LeavesEverythingOut(Engine engine)
    {
        Assert.That(Write(engine, new MoneroRpcConnection()), Is.EqualTo("{}"));
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Serialize_ConnectionWithAProxyAndAZmqUri(Engine engine)
    {
        var connection = new MoneroRpcConnection("test_node:18081", "user", "abc123")
        {
            ProxyUri = "127.0.0.1:9050",
            ZmqUri = "test_node:18084",
            Priority = 1,
            TimeoutMs = 1000
        };

        Assert.That(Write(engine, connection), Is.EqualTo(
            """{"uri":"test_node:18081","username":"user","password":"abc123","proxyUri":"127.0.0.1:9050","zmqUri":"test_node:18084","priority":1,"timeoutMs":1000}"""));
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Serialize_ConnectionOnlyWritesWhatIsSet(Engine engine)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Write(engine, new MoneroRpcConnection(NodeUri)), Is.EqualTo("""{"uri":"http://127.0.0.1:18081"}"""));
            Assert.That(Write(engine, new MoneroRpcConnection { Priority = 0 }), Is.EqualTo("""{"priority":0}"""));
            Assert.That(Write(engine, new MoneroRpcConnection { SslVerify = false }), Is.EqualTo("""{"sslVerify":false}"""));
            Assert.That(Write(engine, new MoneroRpcConnection { IsOnline = false }), Is.EqualTo("""{"isOnline":false}"""));
        });
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Serialize_WritesTheStateOfTheLastCheck(Engine engine)
    {
        var connection = new MoneroRpcConnection(NodeUri) { ResponseTime = 42, IsOnline = true, IsAuthenticated = false };

        AssertSameJson("""{"uri":"http://127.0.0.1:18081","responseTime":42,"isOnline":true,"isAuthenticated":false}""", Write(engine, connection));
    }

    [Test]
    public void Serialize_WithBothEngines_GivesTheSameJson()
    {
        var connection = FullConnection();

        Assert.That(Write(Engine.SourceGenerated, connection), Is.EqualTo(Write(Engine.Reflection, connection)));
    }

    [Test]
    public void Serialize_WithTheHelperOfTheLibrary_GivesTheSameJsonAsTheSerializer()
    {
        var connection = FullConnection();

        Assert.That(MoneroJson.Serialize(connection), Is.EqualTo(Write(Engine.Reflection, connection)));
    }

    // deserialization

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Deserialize_ReadsEveryProperty(Engine engine)
    {
        const string json = """
                            {"uri":"test_node:18081","username":"user","password":"abc123","proxyUri":"127.0.0.1:9050","zmqUri":"test_node:18084",
                            "priority":1,"timeoutMs":1000,"responseTime":42,"sslVerify":false,"isOnline":true,"isAuthenticated":true}
                            """;

        var connection = Read(engine, json);

        Assert.Multiple(() =>
        {
            Assert.That(connection.Uri, Is.EqualTo("test_node:18081"));
            Assert.That(connection.Username, Is.EqualTo("user"));
            Assert.That(connection.Password, Is.EqualTo("abc123"));
            Assert.That(connection.ProxyUri, Is.EqualTo("127.0.0.1:9050"));
            Assert.That(connection.ZmqUri, Is.EqualTo("test_node:18084"));
            Assert.That(connection.Priority, Is.EqualTo(1));
            Assert.That(connection.TimeoutMs, Is.EqualTo(1000u));
            Assert.That(connection.ResponseTime, Is.EqualTo(42ul));
            Assert.That(connection.SslVerify, Is.False);
            Assert.That(connection.IsOnline, Is.True);
            Assert.That(connection.IsAuthenticated, Is.True);
        });
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Deserialize_EmptyObject_HasNothingSet(Engine engine)
    {
        var connection = Read(engine, "{}");

        AssertSameJson("{}", Write(engine, connection));
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Deserialize_DoesNotMixUpAPriorityOfZeroWithNoPriority(Engine engine)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Read(engine, """{"priority":0}""").Priority, Is.EqualTo(0));
            Assert.That(Read(engine, "{}").Priority, Is.Null);
        });
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Deserialize_NodeConnection_AndSerializeItAgain_GiveTheSameJson(Engine engine)
    {
        const string json = """{"uri":"http://127.0.0.1:18081","username":"rpc_daemon_user","password":"abc123","priority":0,"timeoutMs":15000}""";

        Assert.That(Write(engine, Read(engine, json)), Is.EqualTo(json));
    }

    [TestCase(Engine.Reflection)]
    [TestCase(Engine.SourceGenerated)]
    public void Deserialize_TheFixtureOfAConnection_WritesItBackUnchanged(Engine engine)
    {
        var json = Fixture("Wallet", "rpc_connection");

        AssertSameJson(json, Write(engine, Read(engine, json)));
    }

    [Test]
    public void Deserialize_AUriThatIsNotAString_Throws()
    {
        Assert.That(() => MoneroJson.Deserialize<MoneroRpcConnection>("""{"uri":5}"""), Throws.InstanceOf<System.Text.Json.JsonException>());
    }

    // copy

    [Test]
    public void CopyConstructor_CopiesTheNodeConnection()
    {
        var connection = new MoneroRpcConnection(NodeUri, "rpc_daemon_user", "abc123") { Priority = 0, TimeoutMs = Timeout };

        var copy = new MoneroRpcConnection(connection);

        Assert.That(MoneroJson.Serialize(copy), Is.EqualTo(MoneroJson.Serialize(connection)));
    }

    [Test]
    public void CopyConstructor_CopiesEveryProperty()
    {
        var connection = new Sampler().Populate<MoneroRpcConnection>();

        var copy = new MoneroRpcConnection(connection);

        Assert.Multiple(() =>
        {
            foreach (var property in JsonProperties(typeof(MoneroRpcConnection)))
            {
                Assert.That(property.GetValue(connection), Is.Not.Null, $"the sample of {property.Name} is empty");
                Assert.That(property.GetValue(copy), Is.EqualTo(property.GetValue(connection)), property.Name);
            }
        });
    }

    [Test]
    public void CopyConstructor_CopiesAnEmptyConnection()
    {
        var copy = new MoneroRpcConnection(new MoneroRpcConnection());

        Assert.That(MoneroJson.Serialize(copy), Is.EqualTo("{}"));
    }

    [Test]
    public void CopyConstructor_MakesAnotherObject_ThatDoesNotChangeTheOriginal()
    {
        var connection = FullConnection();
        var copy = new MoneroRpcConnection(connection);

        Assert.That(copy, Is.Not.SameAs(connection));

        copy.Uri = "other:18081";
        copy.Username = null;
        copy.Password = null;
        copy.Priority = 9;
        copy.IsOnline = false;

        Assert.Multiple(() =>
        {
            Assert.That(connection.Uri, Is.EqualTo("test_node:18081"));
            Assert.That(connection.Username, Is.EqualTo("user"));
            Assert.That(connection.Password, Is.EqualTo("abc123"));
            Assert.That(connection.Priority, Is.EqualTo(1));
            Assert.That(connection.IsOnline, Is.True);
        });
    }

    [Test]
    public void Copy_GivesAnEqualConnection()
    {
        var connection = FullConnection();

        var copy = connection.Copy();

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(connection));
            Assert.That(copy, Is.TypeOf<MoneroRpcConnection>());
            Assert.That(MoneroJson.Serialize(copy), Is.EqualTo(MoneroJson.Serialize(connection)));
        });
    }

    [Test]
    public void Copy_CanBeOverriddenByAClassThatExtendsTheConnection()
    {
        MoneroRpcConnection connection = new CustomConnection(NodeUri) { Label = "main" };

        var copy = connection.Copy();

        Assert.That(copy, Is.TypeOf<CustomConnection>());
        Assert.Multiple(() =>
        {
            Assert.That(copy.Uri, Is.EqualTo(NodeUri));
            Assert.That(((CustomConnection)copy).Label, Is.EqualTo("main"));
        });
    }

    private sealed class CustomConnection : MoneroRpcConnection
    {
        public CustomConnection(string uri) : base(uri)
        {
        }

        private CustomConnection(CustomConnection connection) : base(connection)
        {
            Label = connection.Label;
        }

        public string? Label { get; init; }

        public override MoneroRpcConnection Copy()
        {
            return new CustomConnection(this);
        }
    }
}
