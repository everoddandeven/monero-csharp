
using System.Text;
using Monero.Common;

namespace Monero.Wallet.Common;

public class MoneroOutgoingTransfer : MoneroTransfer
{
    private List<uint>? subaddressIndices;
    private List<string>? addresses;
    private List<MoneroDestination>? destinations;

    public MoneroOutgoingTransfer()
    {
        // nothing to initialize
    }

    public bool Equals(MoneroOutgoingTransfer? other)
    {
        if (other == null) return false;
        if (other == this) return true;

        if (subaddressIndices == null)
        {
            if (other.subaddressIndices != null) return false;
        }
        else
        {
            if (other.subaddressIndices == null) return false;
            if (subaddressIndices.Count != other.subaddressIndices.Count) return false;
            int i = 0;

            foreach (var index in subaddressIndices)
            {
                var otherIndex = other.subaddressIndices[i]!;
                if (index != otherIndex) return false;
                i++;
            }
        }

        if (addresses == null)
        {
            if (other.addresses != null) return false;
        }
        else
        {
            if (other.addresses == null) return false;
            if (addresses.Count != other.addresses.Count) return false;
            int i = 0;

            foreach (var address in addresses)
            {
                var otherAddress = other.addresses[i]!;
                if (address != otherAddress) return false;
                i++;
            }
        }

        if (destinations == null)
        {
            if (other.destinations != null) return false;
        }
        else
        {
            if (other.destinations == null) return false;
            if (destinations.Count != other.destinations.Count) return false;

            int i = 0;
            foreach (var destination in destinations)
            {
                var otherDest = other.destinations[i];
                if (destination.Equals(otherDest)) return false;
                i++;
            }
        }

        return true;
    }

    public MoneroOutgoingTransfer(MoneroOutgoingTransfer transfer) : base(transfer)
    {
        if (transfer.subaddressIndices != null) subaddressIndices = [.. transfer.subaddressIndices];
        if (transfer.addresses != null) addresses = [.. transfer.addresses];
        if (transfer.destinations != null)
        {
            destinations = new List<MoneroDestination>();
            foreach (MoneroDestination destination in transfer.GetDestinations()!)
            {
                destinations.Add(destination.Clone());
            }
        }
    }

    public override MoneroOutgoingTransfer Clone()
    {
        return new MoneroOutgoingTransfer(this);
    }

    public override MoneroOutgoingTransfer SetTx(MoneroTxWallet? tx)
    {
        base.SetTx(tx);
        return this;
    }

    public override bool? IsIncoming()
    {
        return false;
    }

    public List<uint>? GetSubaddressIndices()
    {
        return subaddressIndices;
    }

    public MoneroOutgoingTransfer SetSubaddressIndices(List<uint>? subaddressIndices)
    {
        this.subaddressIndices = subaddressIndices;
        return this;
    }

    public List<string>? GetAddresses()
    {
        return addresses;
    }

    public MoneroOutgoingTransfer SetAddresses(List<string>? addresses)
    {
        this.addresses = addresses;
        return this;
    }

    public List<MoneroDestination>? GetDestinations()
    {
        return destinations;
    }

    public MoneroOutgoingTransfer SetDestinations(List<MoneroDestination>? destinations)
    {
        this.destinations = destinations;
        return this;
    }

    public override string ToString(int indent)
    {
        var sb = new StringBuilder();
        sb.Append(base.ToString(indent) + "\n");
        sb.Append(GenUtils.KvLine("Subaddress indices", GetSubaddressIndices(), indent));
        sb.Append(GenUtils.KvLine("Addresses", GetAddresses(), indent));
        if (GetDestinations() != null)
        {
            sb.Append(GenUtils.KvLine("Destinations", "", indent));
            for (int i = 0; i < GetDestinations()!.Count; i++)
            {
                sb.Append(GenUtils.KvLine(i + 1, "", indent + 1));
                sb.Append(GetDestinations()[i].ToString(indent + 2) + "\n");
            }
        }
        string str = sb.ToString();
        return str.Substring(0, str.Length - 1);
    }

}
