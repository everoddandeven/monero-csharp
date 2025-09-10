
namespace Monero.Wallet.Common;

public class MoneroSubaddress
{
    private uint? accountIndex;
    private uint? index;
    private string? address;
    private string? label;
    private ulong? balance;
    private ulong? unlockedBalance;
    private ulong? numUnspentOutputs;
    private bool? isUsed;
    private ulong? numBlocksToUnlock;

    public bool Equals(MoneroSubaddress? other)
    {
        if (other == null) return false;
        if (this == other) return true;
        return accountIndex == other.accountIndex &&
                index == other.index &&
                address == other.address &&
                label == other.label &&
                balance == other.balance &&
                unlockedBalance == other.unlockedBalance &&
                numUnspentOutputs == other.numUnspentOutputs &&
                isUsed == other.isUsed &&
                numBlocksToUnlock == other.numBlocksToUnlock;
    }

    public MoneroSubaddress() { }

    public MoneroSubaddress(string address)
    {
        this.address = address;
    }

    public MoneroSubaddress(uint accountIndex, uint index)
    {
        this.accountIndex = accountIndex;
        this.index = index;
    }

    public MoneroSubaddress(MoneroSubaddress other)
    {
        accountIndex = other.accountIndex;
        index = other.index;
        address = other.address;
        label = other.label;
        balance = other.balance;
        unlockedBalance = other.unlockedBalance;
        numUnspentOutputs = other.numUnspentOutputs;
        isUsed = other.isUsed;
        numBlocksToUnlock = other.numBlocksToUnlock;
    }

    public MoneroSubaddress Clone()
    {
        return new MoneroSubaddress(this);
    }

    public uint? GetAccountIndex()
    {
        return accountIndex;
    }

    public MoneroSubaddress SetAccountIndex(uint? accountIndex)
    {
        this.accountIndex = accountIndex;
        return this;
    }

    public uint? GetIndex()
    {
        return index;
    }

    public MoneroSubaddress SetIndex(uint index)
    {
        this.index = index;
        return this;
    }

    public string? GetAddress()
    {
        return address;
    }

    public MoneroSubaddress SetAddress(string? address)
    {
        this.address = address;
        return this;
    }

    public string? GetLabel()
    {
        return label;
    }

    public MoneroSubaddress SetLabel(string? label)
    {
        this.label = label;
        return this;
    }

    public ulong? GetBalance()
    {
        return balance;
    }

    public MoneroSubaddress SetBalance(ulong? balance)
    {
        this.balance = balance;
        return this;
    }

    public ulong? GetUnlockedBalance()
    {
        return unlockedBalance;
    }

    public MoneroSubaddress SetUnlockedBalance(ulong? unlockedBalance)
    {
        this.unlockedBalance = unlockedBalance;
        return this;
    }

    public ulong? GetNumUnspentOutputs()
    {
        return numUnspentOutputs;
    }

    public MoneroSubaddress SetNumUnspentOutputs(ulong? numUnspentOutputs)
    {
        this.numUnspentOutputs = numUnspentOutputs;
        return this;
    }

    public bool? IsUsed()
    {
        return isUsed;
    }

    public MoneroSubaddress SetIsUsed(bool? isUsed)
    {
        this.isUsed = isUsed;
        return this;
    }

    public ulong? GetNumBlocksToUnlock()
    {
        return numBlocksToUnlock;
    }

    public MoneroSubaddress SetNumBlocksToUnlock(ulong? numBlocksToUnlock)
    {
        this.numBlocksToUnlock = numBlocksToUnlock;
        return this;
    }
}
