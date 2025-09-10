
namespace Monero.Wallet.Common;

public class MoneroSyncResult
{
    private ulong? numBlocksFetched;
    private bool? receivedMoney;

    public MoneroSyncResult(ulong? numBlocksFetched = null, bool? receivedMoney = null)
    {
        this.numBlocksFetched = numBlocksFetched;
        this.receivedMoney = receivedMoney;
    }

    public MoneroSyncResult(MoneroSyncResult syncResult)
    {
        numBlocksFetched = syncResult.numBlocksFetched;
        receivedMoney = syncResult.receivedMoney;
    }

    public ulong? GetNumBlocksFetched()
    {
        return numBlocksFetched;
    }

    public MoneroSyncResult SetNumBlocksFetched(ulong? numBlocksFetched)
    {
        this.numBlocksFetched = numBlocksFetched;
        return this;
    }

    public bool? GetReceivedMoney()
    {
        return receivedMoney;
    }

    public MoneroSyncResult SetReceivedMoney(bool? receivedMoney)
    {
        this.receivedMoney = receivedMoney;
        return this;
    }

    public MoneroSyncResult Clone()
    {
        return new MoneroSyncResult(this);
    }
}
