
namespace Monero.Wallet.Common
{
    public class MoneroAccountTag
    {
        private string? _tag;
        private string? _label;
        private List<uint>? _accountIndices;

        public MoneroAccountTag(string? tag = null, string? label = null, List<uint>? accountIndices = null)
        {
            _tag = tag;
            _label = label;
            _accountIndices = accountIndices;
        }

        public bool Equals(MoneroAccountTag other)
        {
            if (_accountIndices == null)
            {
                if (other._accountIndices != null) return false;
            }
            else
            {
                if (other._accountIndices == null || _accountIndices.Count != other._accountIndices.Count) return false;

                int i = 0;

                foreach (var index in _accountIndices)
                {
                    if (index != other._accountIndices[i]) return false;
                    i++;
                }
            }
            
            return _tag == other._tag &&
                   _label == other._label;
        }

        public string? GetTag()
        {
            return _tag;
        }

        public MoneroAccountTag SetTag(string? tag)
        {
            _tag = tag;
            return this;
        }

        public string? GetLabel()
        {
            return _label;
        }

        public MoneroAccountTag SetLabel(string? label = null)
        {
            _label = label;
            return this;
        }

        public List<uint>? GetAccountIndices()
        {
            return _accountIndices;
        }

        public MoneroAccountTag SetAccountIndices(List<uint>? accountIndices)
        {
            _accountIndices = accountIndices;
            return this;
        }
    }
}
