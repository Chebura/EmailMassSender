using System.Text;
using EmailMassSender.Configuration;
using EmailMassSender.Service;
using EmailMassSender.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmailMassSender.Tests;

/// <summary>
/// Test cases S23-S27 (see TEST-CASES.md). Mails are sent to a local fake SMTP server.
/// </summary>
public class EmailMassSendingServiceSmtpTests : IDisposable
{
    private const string Group = "g";

    private const string Body = "Hello world";

    private readonly TestWorkspace _ws = new(Body);

    public void Dispose() => _ws.Dispose();

    private RootConfiguration CreateConfiguration(FakeSmtpServer server, params string[] receivers)
    {
        var configuration = _ws.CreateConfiguration();
        configuration.SmtpClient = new SmtpClientConfiguration
        {
            Enable = true,
            Host = "127.0.0.1",
            Port = server.Port,
            UseSsl = false,
            Username = "user",
            Password = "password"
        };
        configuration.Groups[Group] = _ws.CreateGroup(receivers);
        return configuration;
    }

    private static Task RunAsync(RootConfiguration configuration, CancellationToken cancellationToken)
    {
        return new EmailMassSendingService(configuration, NullLogger.Instance).SendAsync(cancellationToken);
    }

    [Fact]
    public async Task S23_Accepted_AllMailsSent()
    {
        using var server = new FakeSmtpServer();
        var configuration = CreateConfiguration(server, "a@example.com", "b@example.com", "c@example.com");

        await RunAsync(configuration, TestContext.Current.CancellationToken);

        Assert.Equal(3, server.Mails.Count);
        Assert.All(_ws.ReadTask(Group, configuration.Groups[Group]).Items, item =>
        {
            Assert.NotNull(item.Attempt);
            Assert.False(item.Failed);
            Assert.Null(item.Failure);
        });
    }

    [Fact]
    public async Task S24_Accepted_MailContentIsCorrect()
    {
        using var server = new FakeSmtpServer();
        var configuration = CreateConfiguration(server, "a@example.com");

        await RunAsync(configuration, TestContext.Current.CancellationToken);

        var mail = Assert.Single(server.Mails);
        Assert.Equal("sender@example.com", mail.From);
        Assert.Equal(new[] { "a@example.com" }, mail.To);
        Assert.Contains("Subject: Test subject", mail.Data);
        Assert.True(mail.Data.Contains(Body) || mail.Data.Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes(Body))),
            mail.Data);
    }

    [Fact]
    public async Task S25_Rejected_ItemFailedAndWaitsForRetry()
    {
        using var server = new FakeSmtpServer(FakeSmtpMode.Reject);
        var configuration = CreateConfiguration(server, "a@example.com");
        configuration.RollUpStrategy = new RollUpStrategyConfiguration
        {
            Enable = true,
            MaxCount = 3,
            Delay = TimeSpan.FromMinutes(10)
        };

        await RunAsync(configuration, TestContext.Current.CancellationToken);

        var item = Assert.Single(_ws.ReadTask(Group, configuration.Groups[Group]).Items);
        Assert.True(item.Failed);
        Assert.False(string.IsNullOrEmpty(item.Failure));
        Assert.Equal(2, item.AttemptsLeft);
        Assert.NotNull(item.WaitFor);
        var expectedWaitFor = item.Attempt!.Value.AddMinutes(10);
        Assert.InRange(item.WaitFor.Value, expectedWaitFor, expectedWaitFor.AddSeconds(30));
    }

    [Fact]
    public async Task S26_Rejected_RetriedUntilAttemptsExhausted()
    {
        using var server = new FakeSmtpServer(FakeSmtpMode.Reject);
        var configuration = CreateConfiguration(server, "a@example.com");
        configuration.RollUpStrategy = new RollUpStrategyConfiguration
        {
            Enable = true,
            MaxCount = 3,
            Delay = TimeSpan.FromMilliseconds(300)
        };
        configuration.HostRecycle = new HostRecycleConfiguration
        {
            Enable = true,
            Delay = TimeSpan.FromMilliseconds(100),
            RecyclingWindow = TimeSpan.FromMinutes(1)
        };

        await RunAsync(configuration, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        var item = Assert.Single(_ws.ReadTask(Group, configuration.Groups[Group]).Items);
        Assert.True(item.Failed);
        Assert.Equal(0, item.AttemptsLeft);
        Assert.Equal(3, server.Connections);
    }

    [Fact]
    public async Task S27_Cancelled_CompletesAndReleasesFiles()
    {
        using var server = new FakeSmtpServer(FakeSmtpMode.Hang);
        var configuration = CreateConfiguration(server, "a@example.com");
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        await RunAsync(configuration, cts.Token)
            .WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // Task file must be closed by the service.
        await using var fs = new FileStream(_ws.GetTaskFilePath(Group, configuration.Groups[Group]),
            FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }
}
