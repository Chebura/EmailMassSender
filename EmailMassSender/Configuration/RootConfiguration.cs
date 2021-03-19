using System;
using System.Collections.Generic;

namespace EmailMassSender.Configuration
{
    public class RootConfiguration
    {
        public string DefaultGroups { get; set; }

        public bool ResetTasks { get; set; }

        public bool DeleteObsoleteTasks { get; set; }

        public TimeSpan? TaskFileLifetime { get; set; }

        public int Threads { get; set; }

        public RollUpStrategyConfiguration RollUpStrategy { get; set; }

        public IDictionary<string, GroupConfiguration> Groups { get; set; }

        public TimeSpan? ExecutionTimeout { get; set; }

        public SmtpClientConfiguration SmtpClient { get; set; }

        public HostRecycleConfiguration HostRecycle { get; set; }

        public string FromAddress { get; set; }

        public string FromTitle { get; set; }

        public string Encoding { get; set; }

        public string TasksPath { get; set; }

        public TimeSpan? DelayBetweenSendsInThread { get; set; }
    }
}