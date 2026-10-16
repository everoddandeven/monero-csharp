using Monero.Daemon;
using Monero.Daemon.Model;

namespace Monero.IntegrationTests.Utils;

/// <summary>
/// Mining test utilities.
/// </summary>
public static class MiningUtils
{
    /// <summary>
    /// Get the internal mining daemon.
    /// </summary>
    public static MoneroDaemonRpc GetDaemon() => TestUtils.GetMiningDaemon();

    /// <summary>
    /// Generate blocks to a wallet address (regtest only).
    /// </summary>
    /// <param name="address">is the address of the wallet to receive miner transactions if block is successfully mined.</param>
    /// <param name="numBlocks">is the number of blocks to generate.</param>
    /// <param name="daemon">daemon to generate blocks with (default internal daemon).</param>
    /// <returns>the result of generating blocks; the height is the one of the last block generated.</returns>
    public static async Task<MoneroGenerateBlocksResult> GenerateBlocks(string address, ulong numBlocks, MoneroDaemonRpc? daemon = null)
    {
        Assert.That(TestUtils.Regtest, Is.True, "Generating blocks is supported only on regtest.");
        return await (daemon ?? GetDaemon()).GenerateBlocksAsync(address, numBlocks);
    }

    /// <summary>
    /// Check if mining is enabled.
    /// </summary>
    /// <param name="daemon">daemon to check (default internal daemon).</param>
    public static async Task<bool> IsMining(MoneroDaemonRpc? daemon = null)
    {
        // max tries 3
        daemon ??= GetDaemon();
        for (var i = 0; ; i++)
        {
            try
            {
                var status = await daemon.GetMiningStatusAsync();
                return status.IsActive == true;
            }
            catch (Exception) when (i < 2)
            {
                // try again
            }
        }
    }

    /// <summary>
    /// Start mining.
    /// </summary>
    /// <param name="daemon">daemon to start mining with (default internal daemon).</param>
    public static async Task StartMining(MoneroDaemonRpc? daemon = null)
    {
        if (await IsMining(daemon)) throw new InvalidOperationException("Mining already started");
        await (daemon ?? GetDaemon()).StartMiningAsync(TestUtils.MiningAddress, 1, false, false);
    }

    /// <summary>
    /// Stop mining.
    /// </summary>
    /// <param name="daemon">daemon to stop mining with (default internal daemon).</param>
    public static async Task StopMining(MoneroDaemonRpc? daemon = null)
    {
        if (!await IsMining(daemon)) throw new InvalidOperationException("Mining already stopped");
        await (daemon ?? GetDaemon()).StopMiningAsync();
    }

    /// <summary>
    /// Try to stop mining.
    /// </summary>
    /// <param name="daemon">daemon to stop mining with (default internal daemon).</param>
    /// <returns><c>true</c> if mining stopped, <c>false</c> otherwise.</returns>
    public static async Task<bool> TryStopMining(MoneroDaemonRpc? daemon = null)
    {
        try
        {
            await StopMining(daemon);
            return true;
        }
        catch (Exception e)
        {
            TestContext.Progress.WriteLine($"MiningUtils.StopMining(): {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Try to start mining.
    /// </summary>
    /// <param name="daemon">daemon to start mining with (default internal daemon).</param>
    /// <returns><c>true</c> if mining started, <c>false</c> otherwise.</returns>
    public static async Task<bool> TryStartMining(MoneroDaemonRpc? daemon = null)
    {
        try
        {
            await StartMining(daemon);
            return true;
        }
        catch (Exception e)
        {
            TestContext.Progress.WriteLine($"MiningUtils.StartMining(): {e.Message}");
            return false;
        }
    }
}
