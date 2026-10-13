using EmailMassSender.Configuration;
using EmailMassSender.Service;
using EmailMassSender.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmailMassSender.Tests;

/// <summary>
/// Test cases S01-S22 (see TEST-CASES.md). SMTP is disabled in these tests.
/// </summary>
public class EmailMassSendingServiceTests : IDisposable
{
    private const string Group = "g";

    private readonly TestWorkspace _ws = new();

    public void Dispose() => _ws.Dispose();

    private static Task RunAsync(RootConfiguration configuration)
    {
        return new EmailMassSendingService(configuration, NullLogger.Instance)
            .SendAsync(TestContext.Current.CancellationToken);
    }

    private static EmailMassSendingTaskItem Item(string receiver, Action<EmailMassSendingTaskItem> setup = null)
    {
        var item = new EmailMassSendingTaskItem
        {
            Receiver = receiver,
            Lifetime = DateTimeOffset.UtcNow.AddHours(1)
        };
        setup?.Invoke(item);
        return item;
    }

    private static readonly DateTimeOffset PastAttempt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Constructor

    [Fact]
    public void S01_NullConfiguration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailMassSendingService(null, NullLogger.Instance));
    }

    [Fact]
    public void S02_ZeroThreads_SetToOne()
    {
        var configuration = _ws.CreateConfiguration();
        configuration.Threads = 0;

        _ = new EmailMassSendingService(configuration, NullLogger.Instance);

        Assert.Equal(1, configuration.Threads);
    }

    [Fact]
    public void S03_NullRollUpStrategy_DefaultCreated()
    {
        var configuration = _ws.CreateConfiguration();
        configuration.RollUpStrategy = null;

        _ = new EmailMassSendingService(configuration, NullLogger.Instance);

        Assert.NotNull(configuration.RollUpStrategy);
        Assert.False(configuration.RollUpStrategy.Enable);
        Assert.Equal(TimeSpan.Zero, configuration.RollUpStrategy.Delay);
        Assert.Equal(1, configuration.RollUpStrategy.MaxCount);
    }

    // Tasks preparation

    [Fact]
    public async Task S04_NoGroups_NothingCreated()
    {
        var configuration = _ws.CreateConfiguration();
        configuration.Groups = null;

        await RunAsync(configuration);

        Assert.Empty(_ws.GetTaskFiles());
    }

    [Fact]
    public async Task S05_Group_TaskFileCreated()
    {
        var configuration = _ws.CreateConfiguration();
        var group = _ws.CreateGroup("a@example.com", "b@example.com");
        configuration.Groups[Group] = group;

        await RunAsync(configuration);

        Assert.True(File.Exists(_ws.GetTaskFilePath(Group, group)));
        var task = _ws.ReadTask(Group, group);
        Assert.Equal(Path.GetFileName(_ws.GetTaskFilePath(Group, group)), task.TaskId);
        Assert.Equal(new[] { "a@example.com", "b@example.com" }, task.Items.Select(x => x.Receiver));
    }

    [Fact]
    public async Task S06_DuplicateReceivers_Removed()
    {
        var configuration = _ws.CreateConfiguration();
        var group = _ws.CreateGroup("a@example.com", "a@example.com", "b@example.com");
        configuration.Groups[Group] = group;

        await RunAsync(configuration);

        Assert.Equal(2, _ws.ReadTask(Group, group).Items.Count);
    }

    [Fact]
    public async Task S07_DefaultGroups_FiltersGroupsIgnoringCase()
    {
        var configuration = _ws.CreateConfiguration();
        configuration.Groups["g1"] = _ws.CreateGroup("a@example.com");
        configuration.Groups["g2"] = _ws.CreateGroup("a@example.com");
        configuration.Groups["g3"] = _ws.CreateGroup("a@example.com");
        configuration.DefaultGroups = "G1;g3";

        await RunAsync(configuration);

        Assert.True(File.Exists(_ws.GetTaskFilePath("g1", configuration.Groups["g1"])));
        Assert.False(File.Exists(_ws.GetTaskFilePath("g2", configuration.Groups["g2"])));
        Assert.True(File.Exists(_ws.GetTaskFilePath("g3", configuration.Groups["g3"])));
    }

    [Theory]
    [InlineData("*")]
    [InlineData(null)]
    [InlineData("")]
    public async Task S08_AllGroups_AllProcessed(string defaultGroups)
    {
        var configuration = _ws.CreateConfiguration();
        configuration.Groups["g1"] = _ws.CreateGroup("a@example.com");
        configuration.Groups["g2"] = _ws.CreateGroup("a@example.com");
        configuration.DefaultGroups = defaultGroups;

        await RunAsync(configuration);

        Assert.Equal(2, _ws.GetTaskFiles().Length);
    }

    [Fact]
    public async Task S09_NullReceivers_NoTaskFile()
    {
        var configuration = _ws.CreateConfiguration();
        var group = _ws.CreateGroup();
        group.Receivers = null;
        configuration.Groups[Group] = group;

        await RunAsync(configuration);

        Assert.Empty(_ws.GetTaskFiles());
    }

    [Fact]
    public async Task S10_MissingTextFile_GroupSkipped()
    {
        var configuration = _ws.CreateConfiguration();
        var missing = _ws.CreateGroup("a@example.com");
        missing.PlainTextFileName = Path.Combine(_ws.Root, "missing.txt");
        var ok = _ws.CreateGroup("a@example.com");
        configuration.Groups["missing"] = missing;
        configuration.Groups["ok"] = ok;

        await RunAsync(configuration);

        var files = _ws.GetTaskFiles();
        Assert.Single(files);
        Assert.StartsWith("ok_", files[0]);
    }

    [Fact]
    public async Task S11_MissingTasksDirectory_Created()
    {
        var configuration = _ws.CreateConfiguration();
        configuration.TasksPath = Path.Combine(_ws.Root, "a", "b", "tasks");
        configuration.Groups[Group] = _ws.CreateGroup("a@example.com");

        await RunAsync(configuration);

        Assert.Single(Directory.GetFiles(configuration.TasksPath, "*.task"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task S12_S13_ObsoleteTasks_DeletedOnlyWhenEnabled(bool deleteObsoleteTasks)
    {
        Directory.CreateDirectory(_ws.TasksPath);
        var oldTask = Path.Combine(_ws.TasksPath, "old.task");
        var freshTask = Path.Combine(_ws.TasksPath, "fresh.task");
        File.WriteAllText(oldTask, "{}");
        File.WriteAllText(freshTask, "{}");
        File.SetCreationTimeUtc(oldTask, DateTime.UtcNow.AddDays(-2));

        var configuration = _ws.CreateConfiguration();
        configuration.DeleteObsoleteTasks = deleteObsoleteTasks;
        configuration.TaskFileLifetime = TimeSpan.FromDays(1);
        configuration.Groups[Group] = _ws.CreateGroup("a@example.com");

        await RunAsync(configuration);

        Assert.Equal(!deleteObsoleteTasks, File.Exists(oldTask));
        Assert.True(File.Exists(freshTask));
    }

    [Fact]
    public async Task S14_NullSmtpClient_Throws()
    {
        var configuration = _ws.CreateConfiguration();
        configuration.SmtpClient = null;
        var group = _ws.CreateGroup("a@example.com");
        configuration.Groups[Group] = group;

        await Assert.ThrowsAsync<InvalidOperationException>(() => RunAsync(configuration));

        // Task file must be closed before the exception is thrown.
        await using var fs = new FileStream(_ws.GetTaskFilePath(Group, group),
            FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    // Tasks processing (SMTP disabled)

    [Fact]
    public async Task S15_SmtpDisabled_ItemsMarkedAsNotSent()
    {
        var configuration = _ws.CreateConfiguration();
        var group = _ws.CreateGroup("a@example.com", "b@example.com");
        configuration.Groups[Group] = group;

        await RunAsync(configuration);

        Assert.All(_ws.ReadTask(Group, group).Items, item =>
        {
            Assert.NotNull(item.Attempt);
            Assert.False(item.Failed);
            Assert.Equal("Not sent (disabled)", item.Failure);
        });
    }

    [Fact]
    public async Task S16_ExistingTask_SentItemNotProcessedAgain()
    {
        var configuration = _ws.CreateConfiguration();
        var group = _ws.CreateGroup("a@example.com", "b@example.com");
        configuration.Groups[Group] = group;
        _ws.WriteTask(Group, group,
            Item("a@example.com", x => x.Attempt = PastAttempt),
            Item("b@example.com"));

        await RunAsync(configuration);

        var items = _ws.ReadTask(Group, group).Items.ToDictionary(x => x.Receiver);
        Assert.Equal(PastAttempt, items["a@example.com"].Attempt);
        Assert.NotNull(items["b@example.com"].Attempt);
    }

    [Fact]
    public async Task S17_ResetTasks_TaskRecreated()
    {
        var configuration = _ws.CreateConfiguration();
        configuration.ResetTasks = true;
        var group = _ws.CreateGroup("a@example.com");
        configuration.Groups[Group] = group;
        _ws.WriteTask(Group, group, Item("a@example.com", x => x.Attempt = PastAttempt));

        await RunAsync(configuration);

        var item = Assert.Single(_ws.ReadTask(Group, group).Items);
        Assert.NotEqual(PastAttempt, item.Attempt);
        Assert.NotNull(item.Attempt);
    }

    [Fact]
    public async Task S18_RollUpEnabled_AttemptsLeftDecremented()
    {
        var configuration = _ws.CreateConfiguration();
        configuration.RollUpStrategy = new RollUpStrategyConfiguration { Enable = true, MaxCount = 5 };
        var group = _ws.CreateGroup("a@example.com");
        configuration.Groups[Group] = group;

        await RunAsync(configuration);

        Assert.Equal(4, Assert.Single(_ws.ReadTask(Group, group).Items).AttemptsLeft);
    }

    [Fact]
    public async Task S19_NoAttemptsLeft_NotProcessed()
    {
        var configuration = _ws.CreateConfiguration();
        var group = _ws.CreateGroup("a@example.com");
        configuration.Groups[Group] = group;
        _ws.WriteTask(Group, group, Item("a@example.com", x =>
        {
            x.Attempt = PastAttempt;
            x.Failed = true;
            x.AttemptsLeft = 0;
        }));

        await RunAsync(configuration);

        var item = Assert.Single(_ws.ReadTask(Group, group).Items);
        Assert.Equal(PastAttempt, item.Attempt);
        Assert.Equal(0, item.AttemptsLeft);
    }

    [Fact]
    public async Task S20_LifetimeExpired_NotProcessed()
    {
        var configuration = _ws.CreateConfiguration();
        var group = _ws.CreateGroup("a@example.com");
        configuration.Groups[Group] = group;
        _ws.WriteTask(Group, group, Item("a@example.com", x => x.Lifetime = DateTimeOffset.UtcNow.AddMinutes(-1)));

        await RunAsync(configuration);

        Assert.Null(Assert.Single(_ws.ReadTask(Group, group).Items).Attempt);
    }

    [Fact]
    public async Task S21_WaitForInFuture_NotProcessed()
    {
        var configuration = _ws.CreateConfiguration();
        var group = _ws.CreateGroup("a@example.com");
        configuration.Groups[Group] = group;
        _ws.WriteTask(Group, group, Item("a@example.com", x =>
        {
            x.Attempt = PastAttempt;
            x.Failed = true;
            x.AttemptsLeft = 3;
            x.WaitFor = DateTimeOffset.UtcNow.AddHours(1);
        }));

        await RunAsync(configuration);

        var item = Assert.Single(_ws.ReadTask(Group, group).Items);
        Assert.Equal(PastAttempt, item.Attempt);
        Assert.Equal(3, item.AttemptsLeft);
    }

    [Fact]
    public async Task S22_MultipleThreads_AllItemsProcessed()
    {
        var configuration = _ws.CreateConfiguration();
        configuration.Threads = 3;
        var receivers = Enumerable.Range(0, 10).Select(i => $"r{i}@example.com").ToArray();
        var group = _ws.CreateGroup(receivers);
        configuration.Groups[Group] = group;

        await RunAsync(configuration);

        var items = _ws.ReadTask(Group, group).Items;
        Assert.Equal(10, items.Count);
        Assert.All(items, x => Assert.NotNull(x.Attempt));
    }
}
