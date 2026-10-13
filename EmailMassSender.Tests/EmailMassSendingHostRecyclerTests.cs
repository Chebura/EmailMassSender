using System.Diagnostics;
using EmailMassSender.Configuration;
using EmailMassSender.Service;

namespace EmailMassSender.Tests;

/// <summary>
/// Test cases R01-R08 (see TEST-CASES.md).
/// </summary>
public class EmailMassSendingHostRecyclerTests
{
    private static HostRecycleConfiguration Enabled(int? count = null, TimeSpan? window = null, TimeSpan? delay = null)
    {
        return new HostRecycleConfiguration
        {
            Enable = true,
            CountOfRecycles = count,
            RecyclingWindow = window ?? TimeSpan.FromHours(1),
            Delay = delay ?? TimeSpan.Zero
        };
    }

    [Fact]
    public async Task R01_Disabled_ReturnsFalse()
    {
        var recycler = new EmailMassSendingHostRecycler(new HostRecycleConfiguration { Enable = false });

        Assert.False(await recycler.TryRecycleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task R02_NullConfiguration_ReturnsFalse()
    {
        var recycler = new EmailMassSendingHostRecycler(null);

        Assert.False(recycler.Enable);
        Assert.False(await recycler.TryRecycleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task R03_Enabled_ReturnsTrue()
    {
        var recycler = new EmailMassSendingHostRecycler(Enabled());

        Assert.True(await recycler.TryRecycleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task R04_CountOfRecycles_LimitsRecycles()
    {
        var recycler = new EmailMassSendingHostRecycler(Enabled(count: 3));
        var ct = TestContext.Current.CancellationToken;

        Assert.True(await recycler.TryRecycleAsync(ct));
        Assert.True(await recycler.TryRecycleAsync(ct));
        Assert.False(await recycler.TryRecycleAsync(ct));
    }

    [Fact]
    public async Task R05_WindowExpired_ReturnsFalse()
    {
        var recycler = new EmailMassSendingHostRecycler(Enabled(window: TimeSpan.Zero));

        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.False(await recycler.TryRecycleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task R06_CancellationRequested_ReturnsFalse()
    {
        var recycler = new EmailMassSendingHostRecycler(Enabled(delay: TimeSpan.FromMinutes(1)));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sw = Stopwatch.StartNew();
        Assert.False(await recycler.TryRecycleAsync(cts.Token));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task R07_Delay_WaitsBeforeRecycle()
    {
        var recycler = new EmailMassSendingHostRecycler(Enabled(delay: TimeSpan.FromMilliseconds(200)));

        var sw = Stopwatch.StartNew();
        Assert.True(await recycler.TryRecycleAsync(TestContext.Current.CancellationToken));
        Assert.True(sw.Elapsed >= TimeSpan.FromMilliseconds(180), $"Elapsed: {sw.Elapsed}");
    }

    [Fact]
    public void R08_EmptyConfiguration_UsesDefaults()
    {
        var recycler = new EmailMassSendingHostRecycler(new HostRecycleConfiguration());

        Assert.False(recycler.Enable);
        Assert.Equal(int.MaxValue, recycler.CountOfRecycles);
        Assert.Equal(TimeSpan.FromHours(1), recycler.RecyclingWindow);
        Assert.Equal(TimeSpan.FromSeconds(10), recycler.Delay);
    }
}
