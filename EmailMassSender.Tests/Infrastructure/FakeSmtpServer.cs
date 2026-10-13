using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EmailMassSender.Tests.Infrastructure;

public enum FakeSmtpMode
{
    /// <summary>Accepts all mails.</summary>
    Accept,

    /// <summary>Closes the connection immediately.</summary>
    Reject,

    /// <summary>Accepts the connection and never answers.</summary>
    Hang
}

public sealed record ReceivedMail(string From, IReadOnlyList<string> To, string Data);

/// <summary>
/// Minimal local SMTP server (no TLS, no AUTH) for tests.
/// </summary>
internal sealed class FakeSmtpServer : IDisposable
{
    private readonly TcpListener _listener;

    private readonly CancellationTokenSource _cts = new();

    private readonly FakeSmtpMode _mode;

    private int _connections;

    public int Port { get; }

    public int Connections => Volatile.Read(ref _connections);

    public ConcurrentQueue<ReceivedMail> Mails { get; } = new();

    public FakeSmtpServer(FakeSmtpMode mode = FakeSmtpMode.Accept)
    {
        _mode = mode;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _ = AcceptLoopAsync();
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                Interlocked.Increment(ref _connections);
                _ = HandleClientAsync(client);
            }
        }
        catch
        {
            // Listener stopped.
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using var _ = client;

        try
        {
            switch (_mode)
            {
                case FakeSmtpMode.Reject:
                    return;
                case FakeSmtpMode.Hang:
                    await Task.Delay(Timeout.Infinite, _cts.Token);
                    return;
            }

            var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

            await writer.WriteLineAsync("220 localhost Fake SMTP");

            string from = null;
            var to = new List<string>();

            while (await reader.ReadLineAsync(_cts.Token) is { } line)
            {
                var command = line.ToUpperInvariant();

                if (command.StartsWith("EHLO") || command.StartsWith("HELO"))
                {
                    await writer.WriteLineAsync("250 localhost");
                }
                else if (command.StartsWith("MAIL FROM:"))
                {
                    from = ExtractAddress(line);
                    await writer.WriteLineAsync("250 OK");
                }
                else if (command.StartsWith("RCPT TO:"))
                {
                    to.Add(ExtractAddress(line));
                    await writer.WriteLineAsync("250 OK");
                }
                else if (command == "DATA")
                {
                    await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                    var data = new StringBuilder();
                    while (await reader.ReadLineAsync(_cts.Token) is { } dataLine && dataLine != ".")
                        data.AppendLine(dataLine);
                    Mails.Enqueue(new ReceivedMail(from, to.ToArray(), data.ToString()));
                    from = null;
                    to.Clear();
                    await writer.WriteLineAsync("250 OK");
                }
                else if (command == "QUIT")
                {
                    await writer.WriteLineAsync("221 Bye");
                    return;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }
        }
        catch
        {
            // Connection closed by the client or server stopped.
        }
    }

    private static string ExtractAddress(string line)
    {
        var start = line.IndexOf('<');
        var end = line.IndexOf('>');
        return start >= 0 && end > start ? line.Substring(start + 1, end - start - 1) : line;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
    }
}
