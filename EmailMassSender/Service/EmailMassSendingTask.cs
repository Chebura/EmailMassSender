using System.Collections.Generic;

namespace EmailMassSender.Service
{
    public class EmailMassSendingTask
    {
        public string TaskId { get; set; }

        public ICollection<EmailMassSendingTaskItem> Items { get; set; }
    }
}