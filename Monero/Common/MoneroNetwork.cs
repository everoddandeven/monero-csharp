
namespace Monero.Common
{
    public abstract class MoneroNetwork
    {
        private readonly int _primaryAddressCode;
        private readonly int _integratedAddressCode;
        private readonly int _subaddressCode;

        public readonly MoneroNetworkType Type;

        public MoneroNetwork(int primaryAddressCode, int integratedAddressCode, int subaddressCode, MoneroNetworkType type)
        {
            _primaryAddressCode = primaryAddressCode;
            _integratedAddressCode = integratedAddressCode;
            _subaddressCode = subaddressCode;
            Type = type;
        }

        public int GetPrimaryAddressCode()
        {
            return _primaryAddressCode;
        }

        public int GetIntegratedAddressCode()
        {
            return _integratedAddressCode;
        }

        public int GetSubaddressCode()
        {
            return _subaddressCode;
        }

        public static MoneroNetworkType Parse(string? networkTypeStr)
        {
            if (networkTypeStr == null) throw new MoneroError("Cannot parse null network type");
            return networkTypeStr.ToLower() switch
            {
                "mainnet" => MoneroNetworkType.MAINNET,
                "testnet" => MoneroNetworkType.TESTNET,
                "stagenet" => MoneroNetworkType.STAGENET,
                _ => throw new MoneroError("Invalid network type to parse: " + networkTypeStr),
            };
        }

        public static MoneroNetworkType Parse(int? nettype)
        {
            if (nettype == null) throw new MoneroError("Cannot parse null network type");
            if (nettype == 0) return MoneroNetworkType.MAINNET;
            else if (nettype == 1) return MoneroNetworkType.TESTNET;
            else return MoneroNetworkType.STAGENET;
        }

        public static readonly MoneroNetwork[] TYPES = [new MoneroNetworkMainnet(), new MoneroNetworkTestnet(), new MoneroNetworkStagenet()];
    }

    public class MoneroNetworkMainnet : MoneroNetwork
    {
        public MoneroNetworkMainnet(): base(18, 19, 42, MoneroNetworkType.MAINNET) { }
    }

    public class MoneroNetworkTestnet : MoneroNetwork
    {
        public MoneroNetworkTestnet() : base(53, 54, 63, MoneroNetworkType.TESTNET) { }
    }

    public class MoneroNetworkStagenet : MoneroNetwork
    {
        public MoneroNetworkStagenet() : base(24, 25, 36, MoneroNetworkType.STAGENET) { }
    }
}
