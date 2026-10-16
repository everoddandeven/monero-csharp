using Monero.Common;
using Monero.Daemon;
using Monero.Daemon.Model;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Daemon test utilities.
/// </summary>
public static class DaemonUtils
{
    /// <summary>
    /// Test a known daemon peer.
    /// </summary>
    /// <param name="peer">daemon peer to test.</param>
    /// <param name="fromConnection">indicates if <paramref name="peer"/> is obtained from daemon connections.</param>
    public static void TestKnownPeer(MoneroPeer peer, bool fromConnection)
    {
        // common peer validation
        Assert.That(peer.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(peer.Host, Is.Not.Null.And.Not.Empty);
        Assert.That(peer.Port, Is.GreaterThan(0));
        Assert.That(peer.RpcPort == null || peer.RpcPort >= 0, Is.True);
        Assert.That(peer.IsOnline, Is.Not.Null);
        Assert.That(peer.PruningSeed == null || peer.PruningSeed >= 0, Is.True);

        if (fromConnection)
        {
            // validate peer connection
            Assert.That(peer.LastSeenTimestamp, Is.Null);
        }
        else
        {
            // validate known peer
            Assert.That(peer.LastSeenTimestamp, Is.Not.Null);
        }
    }

    /// <summary>
    /// Test a daemon connection peer.
    /// </summary>
    /// <param name="peer">peer connection to test.</param>
    public static void TestPeer(MoneroPeer peer)
    {
        TestKnownPeer(peer, true);
        Assert.That(peer.Hash, Is.Not.Null.And.Not.Empty);
        Assert.That(peer.AvgDownload, Is.Not.Null);
        Assert.That(peer.AvgUpload, Is.Not.Null);
        Assert.That(peer.CurrentDownload, Is.Not.Null);
        Assert.That(peer.CurrentUpload, Is.Not.Null);
        Assert.That(peer.Height, Is.Not.Null);
        Assert.That(peer.LiveTime, Is.Not.Null);
        Assert.That(peer.IsLocalIp, Is.Not.Null);
        Assert.That(peer.IsLocalHost, Is.Not.Null);
        Assert.That(peer.NumReceives, Is.Not.Null);
        Assert.That(peer.ReceiveIdleTime, Is.Not.Null);
        Assert.That(peer.NumSends, Is.Not.Null);
        Assert.That(peer.SendIdleTime, Is.Not.Null);
        Assert.That(peer.State, Is.Not.Null);
        Assert.That(peer.NumSupportFlags, Is.GreaterThanOrEqualTo(0));
        Assert.That(peer.ConnectionType, Is.Not.Null);
    }

    /// <summary>
    /// Test daemon information.
    /// </summary>
    /// <param name="info">daemon info to test.</param>
    public static void TestInfo(MoneroDaemonInfo info)
    {
        Assert.That(info.Version, Is.Not.Null);
        Assert.That(info.NumAltBlocks, Is.Not.Null);
        Assert.That(info.BlockSizeLimit, Is.GreaterThan(0UL));
        Assert.That(info.BlockSizeMedian, Is.GreaterThan(0UL));
        Assert.That(info.BootstrapDaemonAddress == null || info.BootstrapDaemonAddress.Length > 0, Is.True);
        GenUtils.TestUnsignedBigInteger(info.DifficultyLow);
        GenUtils.TestUnsignedBigInteger(info.DifficultyHigh);
        GenUtils.TestUnsignedBigInteger(info.CumulativeDifficultyLow);
        GenUtils.TestUnsignedBigInteger(info.CumulativeDifficultyHigh);
        GenUtils.TestUnsignedBigInteger(info.FreeSpace);
        Assert.That(info.NumOfflinePeers, Is.GreaterThanOrEqualTo(0));
        Assert.That(info.NumOnlinePeers, Is.GreaterThanOrEqualTo(0));
        Assert.That(info.Height, Is.Not.Null);
        Assert.That(info.HeightWithoutBootstrap, Is.GreaterThan(0UL));
        Assert.That(info.NumIncomingConnections, Is.GreaterThanOrEqualTo(0));
        Assert.That(info.NumOutgoingConnections, Is.GreaterThanOrEqualTo(0));
        Assert.That(info.NetworkType, Is.Not.Null);
        Assert.That(info.IsOffline, Is.Not.Null);
        Assert.That(info.NumRpcConnections, Is.GreaterThanOrEqualTo(0));
        Assert.That(info.StartTimestamp, Is.GreaterThan(0UL));
        Assert.That(info.AdjustedTimestamp, Is.GreaterThan(0UL));
        Assert.That(info.Target, Is.GreaterThan(0UL));
        Assert.That(info.TargetHeight, Is.Not.Null);
        Assert.That(info.NumTxs, Is.GreaterThanOrEqualTo(0));
        Assert.That(info.NumTxsPool, Is.GreaterThanOrEqualTo(0));
        Assert.That(info.WasBootstrapEverUsed, Is.Not.Null);
        Assert.That(info.BlockWeightLimit, Is.GreaterThan(0UL));
        Assert.That(info.BlockWeightMedian, Is.GreaterThan(0UL));
        Assert.That(info.DatabaseSize, Is.GreaterThan(0UL));
        Assert.That(info.UpdateAvailable, Is.Not.Null);
        // 0 credits
        GenUtils.TestUnsignedBigInteger(info.Credits, false);
        Assert.That(info.TopBlockHash, Is.Not.Null.And.Not.Empty);
        Assert.That(info.IsBusySyncing, Is.Not.Null);
        Assert.That(info.IsSynchronized, Is.Not.Null);
        Assert.That(info.IsRegtest, Is.Not.Null);
    }

    /// <summary>
    /// Test daemon synchronization info.
    /// </summary>
    /// <param name="syncInfo">daemon sync info to test.</param>
    public static void TestSyncInfo(MoneroDaemonSyncInfo syncInfo)
    {
        Assert.That(syncInfo.Height, Is.Not.Null);

        // test peers
        Assert.That(syncInfo.Peers, Is.Not.Null.And.Not.Empty, "No peers found in daemon sync info to test.");
        foreach (var connection in syncInfo.Peers!) TestPeer(connection);

        Assert.That(syncInfo.NextNeededPruningSeed, Is.GreaterThanOrEqualTo(0));
        Assert.That(syncInfo.Overview, Is.Null);
        // 0 credits
        GenUtils.TestUnsignedBigInteger(syncInfo.Credits, false);
        Assert.That(syncInfo.TopBlockHash, Is.Null);
    }

    /// <summary>
    /// Test daemon hard fork information.
    /// </summary>
    /// <param name="hardForkInfo">daemon hard fork information to test.</param>
    public static void TestHardForkInfo(MoneroHardForkInfo hardForkInfo)
    {
        Assert.That(hardForkInfo.EarliestHeight, Is.Not.Null);
        Assert.That(hardForkInfo.IsEnabled, Is.Not.Null);
        Assert.That(hardForkInfo.State, Is.Not.Null);
        Assert.That(hardForkInfo.Threshold, Is.Not.Null);
        Assert.That(hardForkInfo.Version, Is.Not.Null);
        Assert.That(hardForkInfo.NumVotes, Is.Not.Null);
        Assert.That(hardForkInfo.Voting, Is.Not.Null);
        Assert.That(hardForkInfo.Window, Is.Not.Null);
        // 0 credits
        GenUtils.TestUnsignedBigInteger(hardForkInfo.Credits, false);
        Assert.That(hardForkInfo.TopBlockHash, Is.Null);
    }

    /// <summary>
    /// Test a daemon ban.
    /// </summary>
    /// <param name="ban">daemon ban to test.</param>
    public static void TestBan(MoneroBan ban)
    {
        Assert.That(ban.Host, Is.Not.Null);
        Assert.That(ban.Ip, Is.Not.Null);
        Assert.That(ban.Seconds, Is.Not.Null);
    }

    /// <summary>
    /// Test daemon miner data.
    /// </summary>
    /// <param name="data">miner data to test.</param>
    public static void TestMinerData(MoneroMinerData data)
    {
        Assert.That(data.MajorVersion, Is.GreaterThan(0U));
        Assert.That(data.Height, Is.GreaterThan(0UL));
        Assert.That(data.PrevHash, Is.Not.Null.And.Length.EqualTo(64));
        Assert.That(data.SeedHash, Is.Not.Null.And.Length.EqualTo(64));
        // difficulty comes back as a hex string, e.g. "0x1f4"
        Assert.That(data.Difficulty, Is.Not.Null);
        Assert.That(Convert.ToUInt64(data.Difficulty![2..], 16), Is.GreaterThan(0UL));
        Assert.That(data.MedianWeight, Is.Not.Null);
        Assert.That(data.AlreadyGeneratedCoins, Is.GreaterThan(0UL));
        foreach (var tx in data.TxPoolBacklog ?? []) Assert.That(tx.Hash, Is.Not.Null.And.Length.EqualTo(64));
    }

    /// <summary>
    /// Test daemon network statistics.
    /// </summary>
    /// <param name="stats">network statistics to test.</param>
    public static void TestNetworkStats(MoneroDaemonNetworkStats stats)
    {
        Assert.That(stats.StartTime, Is.GreaterThan(0UL));
        Assert.That(stats.TotalPacketsIn, Is.Not.Null);
        Assert.That(stats.TotalBytesIn, Is.Not.Null);
        Assert.That(stats.TotalPacketsOut, Is.Not.Null);
        Assert.That(stats.TotalBytesOut, Is.Not.Null);
    }

    /// <summary>
    /// Test the miner tx sum result.
    /// </summary>
    /// <param name="txSum">miner tx sum to test.</param>
    public static void TestMinerTxSum(MoneroMinerTxSum txSum)
    {
        GenUtils.TestUnsignedBigInteger(txSum.EmissionSumLow);
        GenUtils.TestUnsignedBigInteger(txSum.EmissionSumHigh);
        GenUtils.TestUnsignedBigInteger(txSum.FeeSumLow);
        GenUtils.TestUnsignedBigInteger(txSum.FeeSumHigh);
    }

    /// <summary>
    /// Test daemon tx pool statistics.
    /// </summary>
    /// <param name="stats">daemon tx pool statistics to test.</param>
    public static void TestTxPoolStats(MoneroTxPoolStats stats)
    {
        Assert.That(stats.NumTxs, Is.GreaterThanOrEqualTo(0));

        if (stats.NumTxs > 0)
        {
            Assert.That(stats.BytesMax, Is.GreaterThan(0UL));
            Assert.That(stats.BytesMed, Is.GreaterThan(0UL));
            Assert.That(stats.BytesMin, Is.GreaterThan(0UL));
            Assert.That(stats.BytesTotal, Is.GreaterThan(0UL));
            Assert.That(stats.OldestTimestamp, Is.GreaterThan(0UL));
            Assert.That(stats.Histo98Pc == null || stats.Histo98Pc > 0, Is.True);
            Assert.That(stats.Num10M, Is.GreaterThanOrEqualTo(0));
            Assert.That(stats.NumDoubleSpends, Is.GreaterThanOrEqualTo(0));
            Assert.That(stats.NumFailing, Is.GreaterThanOrEqualTo(0));
            Assert.That(stats.NumNotRelayed, Is.GreaterThanOrEqualTo(0));
        }
        else
        {
            Assert.That(stats.BytesMax, Is.Null);
            Assert.That(stats.BytesMed, Is.Null);
            Assert.That(stats.BytesMin, Is.Null);
            Assert.That(stats.BytesTotal, Is.EqualTo(0UL));
            Assert.That(stats.Histo98Pc, Is.Null);
            Assert.That(stats.OldestTimestamp, Is.Null);
            Assert.That(stats.Num10M, Is.EqualTo(0));
            Assert.That(stats.NumDoubleSpends, Is.EqualTo(0));
            Assert.That(stats.NumFailing, Is.EqualTo(0));
            Assert.That(stats.NumNotRelayed, Is.EqualTo(0));
            Assert.That(stats.Histo ?? [], Is.Empty);
        }
    }

    /// <summary>
    /// Test a daemon update check result.
    /// </summary>
    /// <param name="result">daemon update check result to test.</param>
    public static void TestUpdateCheckResult(MoneroDaemonUpdateCheckResult result)
    {
        Assert.That(result.IsUpdateAvailable, Is.Not.Null);

        if (result.IsUpdateAvailable == true)
        {
            const string errMsg = "No auto uri is daemon online?";
            Assert.That(result.AutoUri, Is.Not.Null.And.Not.Empty, errMsg);
            Assert.That(result.UserUri, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Version, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Hash, Is.Not.Null.And.Length.EqualTo(64));
        }
        else
        {
            Assert.That(result.AutoUri, Is.Null);
            Assert.That(result.UserUri, Is.Null);
            Assert.That(result.Version, Is.Null);
            Assert.That(result.Hash, Is.Null);
        }
    }

    /// <summary>
    /// Test a daemon update download result.
    /// </summary>
    /// <param name="result">daemon update download result to test.</param>
    public static void TestUpdateDownloadResult(MoneroDaemonUpdateDownloadResult result)
    {
        TestUpdateCheckResult(result);

        if (result.IsUpdateAvailable == true)
        {
            // TODO monero-project daemon returning empty status string on download update error
            if (result.DownloadPath == null) TestContext.Progress.WriteLine("TODO Result path is null");
        }
        else
        {
            Assert.That(result.DownloadPath, Is.Null);
        }
    }

    private static void TestSubmitTxResultCommon(MoneroSubmitTxResult result)
    {
        Assert.That(result.IsGood, Is.Not.Null);
        Assert.That(result.IsRelayed, Is.Not.Null);
        Assert.That(result.IsDoubleSpend, Is.Not.Null);
        Assert.That(result.IsFeeTooLow, Is.Not.Null);
        Assert.That(result.IsMixinTooLow, Is.Not.Null);
        Assert.That(result.HasInvalidInput, Is.Not.Null);
        Assert.That(result.HasInvalidOutput, Is.Not.Null);
        Assert.That(result.IsOverspend, Is.Not.Null);
        Assert.That(result.IsTooBig, Is.Not.Null);
        Assert.That(result.SanityCheckFailed, Is.Not.Null);
        Assert.That(result.Reason == null || result.Reason.Length > 0, Is.True);
    }

    /// <summary>
    /// Test a successful daemon submit tx result.
    /// </summary>
    /// <param name="result">daemon submit tx result to test.</param>
    public static void TestSubmitTxResultGood(MoneroSubmitTxResult result)
    {
        TestSubmitTxResultCommon(result);
        Assert.That(result.IsDoubleSpend, Is.False, "tx submission is double spend.");
        Assert.That(result.IsFeeTooLow, Is.False, "fee is too low.");
        Assert.That(result.IsMixinTooLow, Is.False, "mixin is too low.");
        Assert.That(result.HasInvalidInput, Is.False, "tx has invalid input.");
        Assert.That(result.HasInvalidOutput, Is.False, "tx has invalid output.");
        Assert.That(result.HasTooFewOutputs, Is.False, "tx has too few outputs.");
        Assert.That(result.IsOverspend, Is.False, "tx is overspend.");
        Assert.That(result.IsTooBig, Is.False, "tx is too big.");
        Assert.That(result.SanityCheckFailed, Is.False, "tx sanity check failed.");
        // 0 credits
        GenUtils.TestUnsignedBigInteger(result.Credits, false);
        Assert.That(result.TopBlockHash, Is.Null);
        Assert.That(result.IsTxExtraTooBig, Is.False, "tx extra is too big.");
        Assert.That(result.IsGood, Is.True);
        Assert.That(result.IsNonZeroUnlockTime, Is.False, "tx has non-zero unlock time.");
    }

    /// <summary>
    /// Test a double spend daemon submit tx result.
    /// </summary>
    /// <param name="result">daemon submit tx result to test.</param>
    public static void TestSubmitTxResultDoubleSpend(MoneroSubmitTxResult result)
    {
        TestSubmitTxResultCommon(result);
        Assert.That(result.IsGood, Is.False);
        Assert.That(result.IsDoubleSpend, Is.True);
        Assert.That(result.IsFeeTooLow, Is.False);
        Assert.That(result.IsMixinTooLow, Is.False);
        Assert.That(result.HasInvalidInput, Is.False);
        Assert.That(result.HasInvalidOutput, Is.False);
        Assert.That(result.IsOverspend, Is.False);
        Assert.That(result.IsTooBig, Is.False);
    }

    /// <summary>
    /// Test key images spent statuses.
    /// </summary>
    /// <param name="daemon">daemon to get key image spent statuses from.</param>
    /// <param name="keyImages">list of key image hex to get spent status from daemon.</param>
    /// <param name="expectedStatus">expected key images spent status.</param>
    public static async Task TestSpentStatuses(MoneroDaemon daemon, IReadOnlyList<string> keyImages, MoneroKeyImageSpentStatus expectedStatus)
    {
        // test image
        foreach (var keyImage in keyImages)
        {
            Assert.That(await daemon.GetKeyImageSpentStatusAsync(keyImage), Is.EqualTo(expectedStatus));
        }

        // test array of images
        var statuses = keyImages.Count > 0 ? await daemon.GetKeyImageSpentStatusesAsync(keyImages) : [];
        Assert.That(statuses, Has.Count.EqualTo(keyImages.Count));
        Assert.That(statuses, Is.All.EqualTo(expectedStatus));
    }

    /// <summary>
    /// Get confirmed txs on the blockchain.
    /// </summary>
    /// <param name="daemon">daemon to use to query the blockchain.</param>
    /// <param name="numTxs">number of confirmed transactions to get from the blockchain.</param>
    /// <returns>confirmed transactions.</returns>
    public static async Task<List<MoneroTx>> GetConfirmedTxs(MoneroDaemon daemon, int numTxs)
    {
        var txs = new List<MoneroTx>();
        const ulong numBlocksPerReq = 50;
        var height = await daemon.GetHeightAsync();
        if (height <= numBlocksPerReq + 1) return txs;

        var startIdx = (long)(height - numBlocksPerReq - 1);
        while (startIdx >= 0)
        {
            var blocks = await daemon.GetBlocksByRangeAsync((ulong)startIdx, (ulong)startIdx + numBlocksPerReq);
            foreach (var tx in blocks.SelectMany(block => block.Txs ?? []))
            {
                txs.Add(tx);
                if (txs.Count == numTxs) return txs;
            }

            startIdx -= (long)numBlocksPerReq;
        }

        return txs;
    }

    /// <summary>
    /// Get the hashes of the confirmed txs of the last blocks, without the miner txs, which only a wallet can create.
    /// </summary>
    /// <param name="daemon">daemon instance to get confirmed txs from.</param>
    /// <returns>the confirmed tx hashes, five at most.</returns>
    public static async Task<List<string>> GetConfirmedTxHashes(MoneroDaemon daemon)
    {
        var hashes = new List<string>();
        const ulong numBlocksPerReq = 50;
        var end = await daemon.GetHeightAsync();

        // from the last blocks back, in ranges of blocks, since a chain that no wallet used has none
        while (hashes.Count < 5 && end > 0)
        {
            var start = end > numBlocksPerReq ? end - numBlocksPerReq : 0;
            var blocks = await daemon.GetBlocksByRangeAsync(start, end - 1);
            foreach (var block in Enumerable.Reverse(blocks))
            {
                hashes.AddRange((block.Txs ?? []).Select(tx => tx.Hash!));
                if (hashes.Count >= 5) break;
            }

            end = start;
        }

        return hashes.Take(5).ToList();
    }

    /// <summary>
    /// Get the hashes of the miner txs of the last blocks. A regtest chain that no wallet used has no other txs.
    /// </summary>
    /// <param name="daemon">daemon instance to get miner txs from.</param>
    /// <param name="numTxs">number of miner tx hashes to get.</param>
    /// <returns>the miner tx hashes, from the last block.</returns>
    public static async Task<List<string>> GetMinerTxHashes(MoneroDaemon daemon, int numTxs = 5)
    {
        var hashes = new List<string>();
        var height = await daemon.GetHeightAsync();

        while (hashes.Count < numTxs && height > 0)
        {
            height--;
            var header = await daemon.GetBlockHeaderByHeightAsync(height);
            hashes.Add(header.MinerTxHash!);
        }

        return hashes;
    }

    /// <summary>
    /// Run a call that needs the update servers of Monero, which a daemon in a sandbox without a DNS can't reach.
    /// </summary>
    /// <param name="call">the call.</param>
    /// <typeparam name="T">type of the result.</typeparam>
    /// <returns>the result of the call.</returns>
    public static async Task<T> RequireUpdateServers<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (MoneroError e) when (e.Message == "Error checking for updates")
        {
            Assert.Inconclusive("The daemon can't check for updates: it can't reach the update servers of Monero (is there a DNS?)");
            throw;
        }
    }
}
