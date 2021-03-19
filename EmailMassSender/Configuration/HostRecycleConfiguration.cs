using System;

namespace EmailMassSender.Configuration
{
    public class HostRecycleConfiguration
    {
        public bool? Enable { get; set; }
 
        public int? CountOfRecycles { get; set; }

        public TimeSpan? RecyclingWindow { get; set; }

        public TimeSpan? Delay { get; set; }
    }
}