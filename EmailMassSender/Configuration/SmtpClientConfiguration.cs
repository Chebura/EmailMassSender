namespace EmailMassSender.Configuration
{
    public class SmtpClientConfiguration
    {
        public bool? Enable { get; set; }

        public string Host { get; set; }

        public int? Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool? UseSsl { get; set; }
    }
}