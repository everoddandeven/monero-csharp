using System.Text.RegularExpressions;
using Monero.Common;
using Monero.Wallet.Model;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Wallet test utilities.
/// </summary>
public static partial class WalletUtils
{
    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Hex64();

    [GeneratedRegex("^[0-9A-Za-z]+$")]
    private static partial Regex Base58();

    /// <summary>
    /// Assert that an address is a valid standard address, subaddress or integrated address.
    /// </summary>
    public static void ValidateAddress(string? address)
    {
        Assert.That(address, Is.Not.Null.And.Not.Empty);
        Assert.That(address!.Length, Is.EqualTo(95).Or.EqualTo(106), "Invalid address length");
        Assert.That(Base58().IsMatch(address), Is.True, "Invalid address characters");
    }

    /// <summary>
    /// Assert that a key is a valid key of 32 bytes in hex.
    /// </summary>
    public static void ValidateKey(string? key)
    {
        Assert.That(key, Is.Not.Null);
        Assert.That(Hex64().IsMatch(key!), Is.True, "Invalid key");
    }

    /// <summary>
    /// Assert that a seed is a mnemonic of 25 words.
    /// </summary>
    public static void ValidateMnemonic(string? seed)
    {
        Assert.That(seed, Is.Not.Null.And.Not.Empty);
        Assert.That(seed!.Split(' ', StringSplitOptions.RemoveEmptyEntries), Has.Length.EqualTo(25), "Invalid mnemonic");
    }

    /// <summary>
    /// Test an account.
    /// </summary>
    /// <param name="account">the account to test.</param>
    /// <param name="full">tests also the balances and the subaddresses.</param>
    public static void TestAccount(MoneroAccount? account, bool full = true)
    {
        Assert.That(account, Is.Not.Null);
        Assert.That(account!.Index, Is.Not.Null);
        ValidateAddress(account.PrimaryAddress);

        if (full)
        {
            GenUtils.TestUnsignedBigInteger(account.Balance);
            GenUtils.TestUnsignedBigInteger(account.UnlockedBalance);

            // if given, test the subaddresses and that their balances add up to the balances of the account
            var subaddresses = account.Subaddresses ?? [];
            if (subaddresses.Count > 0)
            {
                ulong balance = 0;
                ulong unlockedBalance = 0;

                for (var i = 0; i < subaddresses.Count; i++)
                {
                    TestSubaddress(subaddresses[i]);
                    Assert.That(subaddresses[i].AccountIndex, Is.EqualTo(account.Index));
                    Assert.That(subaddresses[i].Index, Is.EqualTo((uint)i));
                    balance += subaddresses[i].Balance!.Value;
                    unlockedBalance += subaddresses[i].UnlockedBalance!.Value;
                }

                Assert.That(account.Balance, Is.EqualTo(balance), $"Subaddress balances {balance} != account {account.Index} balance {account.Balance}");
                Assert.That(account.UnlockedBalance, Is.EqualTo(unlockedBalance), $"Subaddress unlocked balances {unlockedBalance} != account {account.Index} unlocked balance {account.UnlockedBalance}");
            }
        }

        // the tag is not set or is not empty
        Assert.That(account.Tag, Is.Null.Or.Not.Empty);
    }

    /// <summary>
    /// Test a subaddress.
    /// </summary>
    /// <param name="subaddress">the subaddress to test.</param>
    /// <param name="full">tests also the balances, the outputs and the blocks to unlock.</param>
    public static void TestSubaddress(MoneroSubaddress? subaddress, bool full = true)
    {
        Assert.That(subaddress, Is.Not.Null);
        Assert.That(subaddress!.AccountIndex, Is.Not.Null);
        Assert.That(subaddress.Index, Is.Not.Null);

        if (full)
        {
            Assert.That(subaddress.Balance, Is.Not.Null);
            Assert.That(subaddress.NumUnspentOutputs, Is.Not.Null);
            Assert.That(subaddress.NumBlocksToUnlock, Is.Not.Null);
            GenUtils.TestUnsignedBigInteger(subaddress.Balance);
            GenUtils.TestUnsignedBigInteger(subaddress.UnlockedBalance);
            Assert.That(subaddress.IsUsed, Is.Not.Null);
            if (subaddress.Balance > 0) Assert.That(subaddress.IsUsed, Is.True);
        }

        Assert.That(subaddress.Address, Is.Not.Null.And.Not.Empty);
        Assert.That(subaddress.Label, Is.Null.Or.Not.Empty);
    }

    /// <summary>
    /// Test a message signature result.
    /// </summary>
    public static void TestMessageSignatureResult(MoneroMessageSignatureResult? result, bool isGood)
    {
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsGood, Is.EqualTo(isGood));
        Assert.That(result.IsOld, Is.False);
        Assert.That(result.Version, Is.EqualTo(isGood ? 2u : 0u));
    }

    /// <summary>
    /// Test an address book entry.
    /// </summary>
    public static void TestAddressBookEntry(MoneroAddressBookEntry? entry)
    {
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.Index, Is.Not.Null);
        ValidateAddress(entry.Address);
        Assert.That(entry.Description, Is.Not.Null);
    }

    /// <summary>
    /// A random string, for the names of wallets and labels.
    /// </summary>
    public static string GetRandomString() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Build a transaction config to test the conversion of payment URIs.
    /// </summary>
    public static MoneroTxConfig BuildPaymentUriConfig(string address) => new(address, 250000000000UL)
    {
        RecipientName = "John Doe",
        Note = "My transfer to wallet",
    };

    /// <summary>The message of the errors of a wallet that is not connected.</summary>
    public const string NotConnectedError = "Wallet is not connected to daemon";

    /// <summary>Assert that an error is the one of an invalid address.</summary>
    public static void TestInvalidAddressError(Exception? error, string? address = null)
    {
        Assert.That(error, Is.InstanceOf<MoneroError>());
        Assert.That(error!.Message, Is.EqualTo(address == null ? "Invalid address" : $"Invalid address: {address}"));
    }

    /// <summary>Assert that an error is the one of an invalid transaction hash.</summary>
    public static void TestInvalidTxHashError(Exception? error) => AssertError(error, "TX hash has invalid format");

    /// <summary>Assert that an error is the one of an invalid transaction key.</summary>
    public static void TestInvalidTxKeyError(Exception? error) => AssertError(error, "Tx key has invalid format");

    /// <summary>Assert that an error is the one of an invalid signature.</summary>
    public static void TestInvalidSignatureError(Exception? error) => AssertError(error, "Signature size mismatch with additional tx pubkeys");

    /// <summary>Assert that an error is the one of an address that must not be a subaddress.</summary>
    public static void TestNoSubaddressError(Exception? error) => AssertError(error, "Address must not be a subaddress");

    /// <summary>Assert that an error is the one of a signature with a wrong header.</summary>
    public static void TestSignatureHeaderError(Exception? error) => AssertError(error, "Signature header check error");

    /// <summary>Assert that an error is the one of a server without a wallet.</summary>
    public static void TestNoWalletFileError(Exception? error) => AssertError(error, "No wallet file");

    /// <summary>Assert that an error is the one of a wallet that is not connected to its daemon.</summary>
    public static void TestWalletIsNotConnectedError(Exception? error)
    {
        Assert.That(error, Is.InstanceOf<MoneroError>());
        Assert.That(error!.Message, Is.EqualTo(NotConnectedError).Or.Contains("Network error"), error.Message);
    }

    private static void AssertError(Exception? error, string message)
    {
        Assert.That(error, Is.InstanceOf<MoneroError>());
        Assert.That(error!.Message, Is.EqualTo(message));
    }
}
