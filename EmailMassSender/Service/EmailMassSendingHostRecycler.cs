using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmailMassSender.Service
{
    using Configuration;

    internal class EmailMassSendingHostRecycler
    {
        public bool Enable { get; set; } = false;

        public int CountOfRecycles { get; set; } = int.MaxValue;

        public TimeSpan RecyclingWindow { get; set; } = TimeSpan.FromHours(1);

        public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(10);

        private int _countOfRecycles = 1;

        private readonly DateTime _till;

        public EmailMassSendingHostRecycler(HostRecycleConfiguration configuration)
        {
            if (configuration == null)
                return;

            if (configuration.Enable != null)
                Enable = configuration.Enable.Value;

            if (configuration.CountOfRecycles != null)
                CountOfRecycles = configuration.CountOfRecycles.Value;

            if (configuration.RecyclingWindow != null)
                RecyclingWindow = configuration.RecyclingWindow.Value;

            if (configuration.Delay != null)
                Delay = configuration.Delay.Value;

            _till = DateTime.UtcNow.Add(RecyclingWindow);
        }

        public async Task<bool> TryRecycleAsync(CancellationToken cancellationToken)
        {
            if (!Enable)
                return false;

            if (cancellationToken.IsCancellationRequested)
                return false;

            if (_countOfRecycles >= CountOfRecycles)
                return false;

            if (DateTime.Compare(DateTime.UtcNow, _till) > 0)
                return false;

            await Task.Delay(Delay, cancellationToken);

            _countOfRecycles++;

            return true;
        }
    }
}