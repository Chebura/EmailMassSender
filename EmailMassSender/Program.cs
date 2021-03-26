using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmailMassSender
{
    using Configuration;
    using Service;

    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Environment.ExitCode = 1;

            Console.WriteLine(
                $"Email Mass Sender (EMS) v.{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0"} by Kalianov Dmitry (http://mrald.narod.ru). Read README.md for details.");

            var configuration = new RootConfiguration();

            ILoggerFactory loggerFactory;

            try
            {
                var configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, false)
                    .AddCommandLine(args);

                var configBuilt = configurationBuilder
                    .Build();

                configBuilt.Bind(configuration);

                loggerFactory = LoggerFactory.Create((builder) =>
                    builder.AddConfiguration(configBuilt.GetSection("Logging")).AddConsole());

                var configBuilder2 = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(configuration.UserSettingsFilePath ?? "usersettings.json", false, false);

                configBuilder2.Build().Bind(configuration);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Loading settings failed!");
                Console.Error.WriteLine(e.ToString());
                return 1;
            }

            var logger = loggerFactory.CreateLogger("Host");

            var service =
                new EmailMassSendingService(configuration, loggerFactory.CreateLogger("EmailMassSendingService"));

            using var cts = configuration.ExecutionTimeout != null
                ? new CancellationTokenSource(configuration.ExecutionTimeout.Value)
                : new CancellationTokenSource();

            try
            {
                await service.SendAsync(cts.Token);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Sending process error. UNDONE.");
                return 2;
            }

            return 0;
        }
    }
}