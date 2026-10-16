using Monero.IntegrationTests.Utils;

namespace Monero.IntegrationTests;

/// <summary>
/// Base test class with common set up. Tests are marked with the categories <see cref="NonRelays"/>, <see cref="Relays"/> and
/// <see cref="Notifications"/>, and ignored when <c>Config/config.json</c> disables them.
/// </summary>
public abstract class BaseTestClass
{
    /// <summary>Category of the tests that don't relay transactions.</summary>
    public const string NonRelays = "NonRelays";

    /// <summary>Category of the tests that relay transactions.</summary>
    public const string Relays = "Relays";

    /// <summary>Category of the tests of notifications.</summary>
    public const string Notifications = "Notifications";

    /// <summary>Executed once before all tests.</summary>
    [OneTimeSetUp]
    public async Task GlobalSetUp()
    {
        TestContext.Progress.WriteLine($"Setup test class {GetType().Name}");
        await BeforeAll();
    }

    /// <summary>Executed once after all tests.</summary>
    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        await AfterAll();
        TestContext.Progress.WriteLine($"Teardown test class {GetType().Name}");
    }

    /// <summary>Executed before each test.</summary>
    [SetUp]
    public void SetUp()
    {
        var categories = TestContext.CurrentContext.Test.Properties["Category"].Cast<string>().ToList();

        if (categories.Contains(NonRelays) && !TestUtils.TestNonRelays) Assert.Ignore("TEST_NON_RELAYS disabled");
        if (categories.Contains(Relays) && (!TestUtils.TestRelays || TestUtils.LiteMode)) Assert.Ignore("TEST_RELAYS disabled");
        if (categories.Contains(Notifications) && (!TestUtils.TestNotifications || TestUtils.LiteMode)) Assert.Ignore("TEST_NOTIFICATIONS disabled");

        TestContext.Progress.WriteLine($"Before {TestContext.CurrentContext.Test.Name}");
    }

    /// <summary>Executed once before all tests.</summary>
    protected virtual Task BeforeAll() => Task.CompletedTask;

    /// <summary>Executed once after all tests.</summary>
    protected virtual Task AfterAll() => Task.CompletedTask;
}
