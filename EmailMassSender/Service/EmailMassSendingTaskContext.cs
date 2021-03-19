using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace EmailMassSender.Service
{
    using Configuration;

    public sealed class EmailMassSendingTaskContext
    {
        public string TaskId { get; }

        public FileStream File { get; }

        public byte[] Text { get; }

        public GroupConfiguration GroupConfiguration { get; }

        public bool Success { get; set; }

        public int Sent { get; set; }

        public int Failed { get; set; }

        public int Total { get; set; }

        public ILogger Logger { get; }

        public EmailMassSendingTaskContext(string taskId, FileStream file, byte[] text,
            GroupConfiguration groupConfiguration)
        {
            TaskId = taskId ?? throw new ArgumentNullException(nameof(taskId));
            File = file ?? throw new ArgumentNullException(nameof(file));
            Text = text;
            GroupConfiguration = groupConfiguration;
        }
    }
}