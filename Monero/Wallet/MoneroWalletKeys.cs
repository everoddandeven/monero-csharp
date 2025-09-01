using Monero.Common;
using Monero.Wallet.Common;
using System.Runtime.ConstrainedExecution;

namespace Monero.Wallet
{
    public class MoneroWalletKeys : MoneroWalletDefault
    {
        protected static readonly MoneroWalletManager walletManager = MoneroWalletManager.Instance;
        protected readonly IntPtr walletHandle;
        protected Dictionary<uint, Dictionary<uint, string?>?> addressCache = [];
        protected string seedOffset;

        protected MoneroWalletKeys(IntPtr walletHandle, string seedOffset = "")
        {
            this.walletHandle = walletHandle;
            this.seedOffset = seedOffset;

            Init();
        }

        protected virtual void Init()
        {
            SetOffline();
        }

        protected void SetOffline(bool offline = true) 
        {
            MoneroWallet2Api.MONERO_Wallet_setOffline(walletHandle, offline);
        }

        public static MoneroWalletKeys CreateWalletFromKeys(MoneroWalletConfig config)
        {
            var path = config.GetPath();
            var password = config.GetPassword();
            var restoreHeight = config.GetRestoreHeight();
            var primaryAddress = config.GetPrimaryAddress();
            var privateViewKey = config.GetPrivateViewKey();
            var privateSpendKey = config.GetPrivateSpendKey();
            var networkType = config.GetNetworkType();
            var language = config.GetLanguage();

            if (path == null || path.Length == 0) throw new MoneroError("Must provide a valid wallet path");
            if (primaryAddress == null || primaryAddress.Length == 0) throw new MoneroError("Must provide a valid wallet primary address");
            if (privateViewKey == null) privateViewKey = "";
            if (privateSpendKey == null) privateSpendKey = "";
            if (password == null) password = "";
            if (language == null || !MoneroUtils.IsValidLanguage(language)) throw new MoneroError("Must provide a valid wallet language");
            if (networkType == null) throw new MoneroError("Must provide wallet network type");
            if (restoreHeight == null) restoreHeight = 0;

            var walletHandle = walletManager.CreateWalletFromKeys(path, password, language, (int)networkType, (ulong)restoreHeight, primaryAddress, privateViewKey, privateSpendKey, 1);

            CheckWalletStatus(walletHandle);

            return new MoneroWalletKeys(walletHandle);
        }

        public static MoneroWalletKeys CreateWalletFromSeed(MoneroWalletConfig config)
        {
            var path = config.GetPath();
            var password = config.GetPassword();
            var mnemonic = config.GetSeed();
            var networkType = config.GetNetworkType();
            var restoreHeight = config.GetRestoreHeight();

            if (path == null || path.Length == 0) throw new MoneroError("Must provide a valid wallet path");
            if (mnemonic == null || mnemonic.Length == 0) throw new MoneroError("No seed provided");
            if (networkType == null) throw new MoneroError("Must provide a valid wallet network type.");
            if (restoreHeight == null) restoreHeight = 0;
            if (password == null) password = "";

            var walletHandle = walletManager.RecoveryWallet(path, password, mnemonic, (int)networkType, (ulong)restoreHeight, 1, "");

            CheckWalletStatus(walletHandle);

            return new MoneroWalletKeys(walletHandle);
        }

        public static MoneroWalletKeys CreateWalletRandom(MoneroWalletConfig config)
        {
            var path = config.GetPath();
            var password = config.GetPassword();
            var mnemonic = config.GetSeed();
            var seedOffset = config.GetSeedOffset();
            var networkType = config.GetNetworkType();
            var restoreHeight = config.GetRestoreHeight();
            var language = config.GetLanguage();

            if (path == null || path.Length == 0) throw new MoneroError("Must provide a valid wallet path");
            if (password == null) password = "";
            if (networkType == null) throw new MoneroError("Must provide a valid wallet network type");
            if (language == null || !MoneroUtils.IsValidLanguage(language)) throw new MoneroError("Must provide a valid wallet language");
            if (mnemonic != null) throw new MoneroError("Cannot specify seed when creating random wallet");
            if (seedOffset != null) throw new MoneroError("Cannot specify seed offset when creating random wallet");
            if (restoreHeight != null) throw new MoneroError("specify restore height when creating random wallet");

            var walletHandle = walletManager.CreateWallet(path, password, language, (int)networkType);

            CheckWalletStatus(walletHandle);

            return new MoneroWalletKeys(walletHandle);
        }

        #region Override Base Methods

        public override uint AddAddressBookEntry(string address, string description)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void ChangePassword(string oldPassword, string newPassword)
        {
            var currentPassword = MoneroWallet2Api.MONERO_Wallet_getPassword(walletHandle);
            if (currentPassword != oldPassword) throw new MoneroError("Cannot change wallet password: password mismatch");
            MoneroWallet2Api.MONERO_Wallet_setPassword(walletHandle, newPassword);
        }

        public override MoneroCheckReserve CheckReserveProof(string address, string message, string signature)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override bool CheckSpendProof(string txHash, string message, string signature)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroCheckTx CheckTxKey(string txHash, string txKey, string address)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroCheckTx CheckTxProof(string txHash, string address, string message, string signature)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroAccount CreateAccount(string? label = null)
        {
            if (label == null) label = "";
            uint accountIdx = MoneroWallet2Api.MONERO_Wallet_numSubaddressAccounts(walletHandle);
            
            CheckWalletStatus();
            
            MoneroWallet2Api.MONERO_Wallet_addSubaddressAccount(walletHandle, label);

            CheckWalletStatus();

            var account = new MoneroAccount();

            account.SetIndex(accountIdx)
                .SetPrimaryAddress(GetAddress(accountIdx, 0))
                .SetBalance(0)
                .SetUnlockedBalance(0)
                .SetTag(label);

            return account;
        }

        public override MoneroSubaddress CreateSubaddress(uint accountIdx, string? label = null)
        {
            if (label == null) label = "";
            uint subaddressIdx = MoneroWallet2Api.MONERO_Wallet_numSubaddresses(walletHandle, accountIdx);

            CheckWalletStatus();

            MoneroWallet2Api.MONERO_Wallet_addSubaddressAccount(walletHandle, label);

            CheckWalletStatus();

            var subaddress = new MoneroSubaddress();

            subaddress
                .SetAccountIndex(accountIdx)
                .SetIndex(subaddressIdx)
                .SetAddress(GetAddress(accountIdx, subaddressIdx))
                .SetLabel(label)
                .SetBalance(0)
                .SetUnlockedBalance(0);

            return subaddress;
        }

        public override List<MoneroTxWallet> CreateTxs(MoneroTxConfig config)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroIntegratedAddress DecodeIntegratedAddress(string integratedAddress)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void DeleteAddressBookEntry(uint entryIdx)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroTxSet DescribeTxSet(MoneroTxSet txSet)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void EditAddressBookEntry(uint index, bool setAddress, string address, bool setDescription, string description)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroMultisigInitResult ExchangeMultisigKeys(List<string> multisigHexes, string password)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroKeyImage> ExportKeyImages(bool all = false)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string ExportMultisigHex()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string ExportOutputs(bool all = false)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void FreezeOutput(string keyImage)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroAccount GetAccount(uint accountIdx, bool includeSubaddresses = false)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroAccount> GetAccounts(bool includeSubaddresses = false, string? tag = null)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroAccountTag> GetAccountTags()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string GetAddress(uint accountIdx, uint subaddressIdx)
        {
            var addressMap = addressCache[accountIdx];

            if (addressMap != null)
            {
                var cachedAddress = addressMap[subaddressIdx];

                if (cachedAddress != null) return cachedAddress;
            }

            var address = MoneroWallet2Api.MONERO_Wallet_address(walletHandle, accountIdx, subaddressIdx);

            CheckWalletStatus();

            if (address == null) throw new MoneroError("Could not get subaddress at index" + subaddressIdx + " from subaddress " + accountIdx);

            addressMap = addressCache[accountIdx] = [];
            addressMap[subaddressIdx] = address;

            return address;
        }

        public override List<MoneroAddressBookEntry> GetAddressBookEntries(List<uint>? entryIndices)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroSubaddress GetAddressIndex(string address)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string? GetAttribute(string key)
        {
            var value = MoneroWallet2Api.MONERO_Wallet_getCacheAttribute(walletHandle, key);

            CheckWalletStatus();

            if (value == null || value.Length == 0) return null;

            return value;
        }

        public override ulong GetBalance(uint? accountIdx = null, uint? subaddressIdx = null)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroRpcConnection? GetDaemonConnection()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override ulong GetDaemonHeight()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroTxPriority GetDefaultFeePriority()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override ulong GetHeight()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override ulong GetHeightByDate(int year, int month, int day)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroIntegratedAddress GetIntegratedAddress(string? standardAddress = null, string? paymentId = null)
        {
            if (standardAddress != null && standardAddress.Length > 0) throw new MoneroError("Non null standard address parameter is not supported by keys only wallet");
            paymentId ??= "";

            var integratedAddress = MoneroWallet2Api.MONERO_Wallet_integratedAddress(walletHandle, paymentId);

            var result = new MoneroIntegratedAddress();

            result
                .SetStandardAddress(GetPrimaryAddress())
                .SetIntegratedAddress(integratedAddress)
                .SetPaymentId(paymentId);

            return result;
        }

        public override MoneroMultisigInfo GetMultisigInfo()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroKeyImage> GetNewKeyImagesFromLastImport()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroOutputWallet> GetOutputs(MoneroOutputQuery query)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string GetPath()
        {
            string path = MoneroWallet2Api.MONERO_Wallet_path(walletHandle);

            CheckWalletStatus();

            return path;
        }

        public override string GetPaymentUri(MoneroTxConfig config)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string GetPrivateSpendKey()
        {
            var privateSpendKey = MoneroWallet2Api.MONERO_Wallet_secretSpendKey(walletHandle);

            CheckWalletStatus();

            if (privateSpendKey == null) throw new MoneroError("Cannot retrieve private view key");

            return privateSpendKey;
        }

        public override string GetPrivateViewKey()
        {
            var privateViewKey = MoneroWallet2Api.MONERO_Wallet_secretViewKey(walletHandle);

            CheckWalletStatus();

            if (privateViewKey == null) throw new MoneroError("Cannot retrieve private view key");

            return privateViewKey;
        }

        public override string GetPublicSpendKey()
        {
            var publicSpendKey = MoneroWallet2Api.MONERO_Wallet_publicSpendKey(walletHandle);

            CheckWalletStatus();

            if (publicSpendKey == null) throw new MoneroError("Cannot retrieve private view key");

            return publicSpendKey;
        }

        public override string GetPublicViewKey()
        {
            var publicViewKey = MoneroWallet2Api.MONERO_Wallet_publicViewKey(walletHandle);

            CheckWalletStatus();

            if (publicViewKey == null) throw new MoneroError("Cannot retrieve private view key");

            return publicViewKey;
        }

        public override string GetReserveProofAccount(uint accountIdx, ulong amount, string message)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string GetReserveProofWallet(string message)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string GetSeed()
        {
            var seed = MoneroWallet2Api.MONERO_Wallet_seed(walletHandle, seedOffset);
            CheckWalletStatus();

            if (seed == null || seed.Length == 0) throw new MoneroError("Cannot retrieve wallet seed");

            return seed;
        }

        public override string GetSeedLanguage()
        {
            var language = MoneroWallet2Api.MONERO_Wallet_getSeedLanguage(walletHandle);

            CheckWalletStatus();

            if (language == null || language.Length == 0) throw new MoneroError("Could not retrieve seed language");

            return language;
        }

        public override string GetSpendProof(string txHash, string? message = null)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroSubaddress> GetSubaddresses(uint accountIdx, List<uint>? subaddressIndices = null)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroTransfer> GetTransfers(MoneroTransferQuery query)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string GetTxKey(string txHash)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<string> GetTxNotes(List<string> txHashes)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string GetTxProof(string txHash, string address, string? message = null)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroTxWallet> GetTxs(MoneroTxQuery query)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override ulong GetUnlockedBalance(uint? accountIdx = null, uint? subaddressIdx = null)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroVersion GetVersion()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroNetworkType GetNetworkType()
        {
            var nettype = MoneroWallet2Api.MONERO_Wallet_nettype(walletHandle);

            return (MoneroNetwork.Parse(nettype));
        }

        public override MoneroWalletType GetWalletType()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroKeyImageImportResult ImportKeyImages(List<MoneroKeyImage> keyImages)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override int ImportMultisigHex(List<string> multisigHexes)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override int ImportOutputs(string outputsHex)
        {
            var coinsPtr = MoneroWallet2Api.MONERO_Wallet_coins(walletHandle);
            CheckWalletStatus();
            var before = MoneroWallet2Api.MONERO_Coins_count(coinsPtr);
            CheckWalletStatus();
            var result = MoneroWallet2Api.MONERO_Wallet_importOutputsUR(walletHandle, outputsHex);
            CheckWalletStatus();
            coinsPtr = MoneroWallet2Api.MONERO_Wallet_coins(walletHandle);
            var after = MoneroWallet2Api.MONERO_Coins_count(coinsPtr);
            CheckWalletStatus();
            return after - before;
        }

        public override bool IsConnectedToDaemon()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override bool IsMultisigImportNeeded()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override bool IsOutputFrozen(string keyImage)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override bool IsViewOnly()
        {
            var result = MoneroWallet2Api.MONERO_Wallet_watchOnly(walletHandle);

            CheckWalletStatus();

            return result == true;
        }

        public override string MakeMultisig(List<string> multisigHexes, int threshold, string password)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroTxConfig ParsePaymentUri(string uri)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string PrepareMultisig()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<string> RelayTxs(List<string> txMetadatas)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void RescanBlockchain()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void RescanSpent()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void Save()
        {
            MoneroWallet2Api.MONERO_Wallet_store(walletHandle, GetPath());
        }

        public override void ScanTxs(List<string> txHashes)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void SetAccountTagLabel(string tag, string label)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void SetAttribute(string key, string val)
        {
            MoneroWallet2Api.MONERO_Wallet_setCacheAttribute(walletHandle, key, val);

            CheckWalletStatus();
        }

        public override void SetDaemonConnection(MoneroRpcConnection? daemonConnection)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void SetProxyUri(string? uri)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void SetSubaddressLabel(uint accountIdx, uint subaddressIdx, string? label)
        {
            if (label == null) label = "";
            MoneroWallet2Api.MONERO_Wallet_setSubaddressLabel(walletHandle, accountIdx, subaddressIdx, label);
        }

        public override void SetTxNotes(List<string> txHashes, List<string> notes)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override string SignMessage(string message, MoneroMessageSignatureType signatureType = MoneroMessageSignatureType.SIGN_WITH_SPEND_KEY, uint accountIdx = 0, uint subaddressIdx = 0)
        {
            var address = GetAddress(accountIdx, subaddressIdx);
            var signedMessage = MoneroWallet2Api.MONERO_Wallet_signMessage(walletHandle, message, address);

            CheckWalletStatus();

            if (signedMessage == null || signedMessage.Length == 0) throw new MoneroError("Could not sign message");

            return signedMessage;
        }

        public override MoneroMultisigSignResult SignMultisigTxHex(string multisigTxHex)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroTxSet SignTxs(string unsignedTxHex)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void StartMining(ulong numThreads, bool backgroundMining, bool ignoreBattery)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void StartSyncing(ulong? SyncPeriodInMs = null)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void StopMining()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void StopSyncing()
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<string> SubmitMultisigTxHex(string signedMultisigTxHex)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<string> SubmitTxs(string signedTxHex)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroTxWallet> SweepDust(bool relay)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroTxWallet SweepOutput(MoneroTxConfig config)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override List<MoneroTxWallet> SweepUnlocked(MoneroTxConfig config)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroSyncResult Sync(ulong? startHeight = null, MoneroWalletListener? listener = null)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void TagAccounts(string tag, List<uint> accountIndices)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void ThawOutput(string keyImage)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override void UntagAccounts(List<uint> accountIndices)
        {
            throw new NotImplementedException("Not supported by keys only wallet");
        }

        public override MoneroMessageSignatureResult VerifyMessage(string message, string address, string signature)
        {
            var verification = MoneroWallet2Api.MONERO_Wallet_verifySignedMessage(walletHandle, message, address, signature);

            CheckWalletStatus();

            var result = new MoneroMessageSignatureResult();

            result.SetIsGood(verification);

            return result;
        }

        #endregion

        #region Private Methods

        protected void CheckWalletStatus()
        {
            CheckWalletStatus(walletHandle);
        }

        protected static void CheckWalletStatus(IntPtr? walletHandle)
        {
            if (walletHandle == IntPtr.Zero || walletHandle == null) throw new MoneroError("Wallet handle is null");
            var walletStatus = MoneroWallet2Api.MONERO_Wallet_status((IntPtr)walletHandle);

            if (walletStatus != 0)
            {
                var errorString = MoneroWallet2Api.MONERO_WalletManager_errorString((IntPtr)walletHandle);
                if (errorString != null && errorString.Length > 0) throw new MoneroError(errorString);
                else throw new MoneroError("Unkown wallet error");
            }
        }

        #endregion
    }

    internal static class MoneroWalletManagerNative
    {
        private readonly static string DllName = "monero_x86_64-w64-mingw32_libwallet2_api_c";


    }
}
