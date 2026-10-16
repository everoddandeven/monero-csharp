using Monero.Daemon;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Blockchain utilities to be used in integration tests.
/// </summary>
public static class BlockchainUtils
{
    /// <summary>
    /// Timeout in seconds to check blockchain mining progress.
    /// </summary>
    public const int CheckBlockTimeoutSeconds = 5;

    /// <summary>
    /// Number of blocks to generate with one request on regtest.
    /// </summary>
    public const ulong GenerateBatchSize = 20;

    /// <summary>
    /// Get the current blockchain height of the mining daemon.
    /// </summary>
    public static Task<ulong> GetHeight() => MiningUtils.GetDaemon().GetHeightAsync();

    /// <summary>
    /// Indicates if the blockchain has reached the minimum height for running tests.
    /// </summary>
    public static async Task<bool> BlockchainIsReady() => TestUtils.MinBlockHeight <= await GetHeight();

    /// <summary>
    /// Wait for the blockchain height. On regtest the blocks are generated, so there is nothing to wait for. Elsewhere mining starts if it is not
    /// active, and stops again when the height is reached.
    /// </summary>
    /// <param name="height">height to wait for.</param>
    /// <returns>the blockchain height.</returns>
    public static async Task<ulong> WaitForHeight(ulong height)
    {
        var daemon = MiningUtils.GetDaemon();
        var currentHeight = await daemon.GetHeightAsync();
        if (height <= currentHeight) return currentHeight;

        if (TestUtils.Regtest)
        {
            // in batches, so that no request waits longer than its timeout
            while (currentHeight < height)
            {
                await MiningUtils.GenerateBlocks(TestUtils.MiningAddress, Math.Min(height - currentHeight, GenerateBatchSize), daemon);
                currentHeight = await daemon.GetHeightAsync();
            }

            return currentHeight;
        }

        // start mining if not active
        var stopMining = false;
        if (!await MiningUtils.IsMining())
        {
            await MiningUtils.StartMining();
            stopMining = true;
        }

        // wait until blockchain reaches desired height
        while (currentHeight < height)
        {
            TestContext.Progress.WriteLine($"Waiting for blockchain height ({currentHeight}/{height})");
            var block = await daemon.WaitForNextBlockHeaderAsync();
            currentHeight = block.Height!.Value;
            await Task.Delay(TimeSpan.FromSeconds(CheckBlockTimeoutSeconds));
        }

        if (stopMining)
        {
            await MiningUtils.StopMining();
            await Task.Delay(TimeSpan.FromSeconds(CheckBlockTimeoutSeconds));
            currentHeight = await daemon.GetHeightAsync();
        }

        return currentHeight;
    }

    /// <summary>
    /// Wait for blocks.
    /// </summary>
    /// <param name="numBlocks">number of blocks to wait.</param>
    /// <returns>the blockchain height.</returns>
    public static async Task<ulong> WaitForBlocks(ulong numBlocks) => await WaitForHeight(await GetHeight() + numBlocks);

    /// <summary>
    /// Wait until the daemon of the tests has the blocks of the mining daemon.
    /// </summary>
    /// <param name="daemon">daemon of the tests.</param>
    /// <param name="timeout">time to wait.</param>
    public static async Task WaitForSync(MoneroDaemonRpc daemon, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromMinutes(2));

        while (true)
        {
            var target = await GetHeight();
            var height = await daemon.GetHeightAsync();
            if (height >= target) return;

            if (DateTime.UtcNow > deadline) throw new TimeoutException($"The daemon is at the height {height}, and the mining daemon is at {target}");
            await Task.Delay(500);
        }
    }

    /// <summary>
    /// Setup the blockchain for integration tests: it has at least the minimum height, and an alternative chain on regtest.
    /// </summary>
    public static async Task SetupBlockchain()
    {
        if (!await BlockchainIsReady())
        {
            await WaitForHeight(TestUtils.MinBlockHeight);
            await MiningUtils.TryStopMining();

            // create an alternative chain on regtest
            if (TestUtils.Regtest) await ReorgBlockchain();
        }

        await WaitForSync(TestUtils.GetDaemonRpc());
    }

    /// <summary>
    /// Force a blockchain reorganization (regtest only): mine a short chain on the tip, then a longer one from the same fork point.
    /// </summary>
    /// <param name="numBlocks">number of blocks to mine on the original chain before forking (the alternate chain mines one more).</param>
    /// <returns>the blockchain height after the reorg.</returns>
    public static async Task<ulong> ReorgBlockchain(ulong numBlocks = 3)
    {
        Assert.That(numBlocks, Is.GreaterThanOrEqualTo(1UL));

        var daemon = MiningUtils.GetDaemon();

        // mining must be stopped so it doesn't race with the forced reorg
        var stopMining = await MiningUtils.TryStopMining();

        string originalHash;
        ulong forkHeight;
        Daemon.Model.MoneroGenerateBlocksResult alt;

        try
        {
            // mark the fork point: the last common block between both chains
            var forkHeader = await daemon.GetLastBlockHeaderAsync();
            var forkHash = forkHeader.Hash!;
            forkHeight = forkHeader.Height!.Value;

            // mine the original, shorter chain on top of the fork point
            var original = await daemon.GenerateBlocksAsync(TestUtils.MiningAddress, numBlocks, forkHash);
            Assert.That(original.Height, Is.EqualTo(forkHeight + numBlocks), "Failed to build the original chain");
            originalHash = await daemon.GetBlockHashAsync(forkHeight + 1);

            // mine a longer alternate chain from the same fork point to force a reorg
            var altNumBlocks = numBlocks + 1;
            alt = await daemon.GenerateBlocksAsync(TestUtils.MiningAddress, altNumBlocks, forkHash);
            Assert.That(alt.Height, Is.EqualTo(forkHeight + altNumBlocks), "Failed to build the alternate chain");
        }
        finally
        {
            // restart mining, if it was active before
            if (stopMining) await MiningUtils.TryStartMining();
        }

        // ensure the reorg was accepted: the active chain must now follow the heavier alternate fork
        var activeHash = await daemon.GetBlockHashAsync(forkHeight + 1);
        Assert.That(activeHash, Is.Not.EqualTo(originalHash), "Reorg failed: block after the fork point was not replaced by the alternate chain");
        Assert.That(alt.BlockHashes, Does.Contain(activeHash), "Reorg failed: active chain block is not part of the generated alternate chain");

        var originalHeader = await daemon.GetBlockHeaderByHashAsync(originalHash);
        Assert.That(originalHeader.OrphanStatus, Is.True, "Reorg failed: original chain block was not marked as orphaned");

        return await daemon.GetHeightAsync();
    }
}
