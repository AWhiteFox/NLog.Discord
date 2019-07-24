using Discord;
using Discord.Webhook;
using NLog.Config;
using NLog.Targets;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NLog.Discord
{
    [Target("DiscordWebhook")]
    public sealed class WebhookTarget : TargetWithLayout
    {
        /// <summary>
        /// Discord webhook
        /// </summary>
        private DiscordWebhookClient webhook;

        /// <summary>
        /// Discord webhook ID
        /// </summary>
        [RequiredParameter]
        public ulong Id { get; set; }

        /// <summary>
        /// Discord webhook token
        /// </summary>
        [RequiredParameter]
        public string Token { get; set; }

        /// <summary>
        /// Use embeds or not
        /// </summary>
        public bool UseEmbeds { get; set; }

        /// <summary>
        /// Do mentions or not
        /// </summary>
        public bool DoMentions { get; set; }
        
        /// <summary>
        /// Mention string with @
        /// </summary>
        public string Mention { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public WebhookTarget()
        {
            UseEmbeds = true;
            DoMentions = false;
            Mention = "@everyone";
            Layout = "${message}";
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var webhook = GetWebhook();
            bool hasException = logEvent.Exception != null;

            string text = null;
            IEnumerable<Embed> embeds = null;

            if (UseEmbeds)
            {
                var embed = new EmbedBuilder
                {
                    Timestamp = logEvent.TimeStamp,
                    Description = this.Layout.Render(logEvent)
                };
                embed.WithFooter($"sec: {logEvent.TimeStamp.Second} ms: {logEvent.TimeStamp.Millisecond}");

                switch (logEvent.Level.Ordinal)
                {
                    case 0: embed.WithTitle($":page_with_curl: Trace message in {logEvent.LoggerName}"); break;
                    case 1: embed.WithTitle($":gear: Debug message in {logEvent.LoggerName}"); break;
                    case 2:
                        embed.WithTitle($":information_source: Info in {logEvent.LoggerName}");
                        embed.Color = new Color(3901635);
                        break;
                    case 3:
                        embed.WithTitle($":warning: Warning in {logEvent.LoggerName}");
                        embed.Color = new Color(16763981);
                        break;
                    case 4:
                        embed.WithTitle($":x: Error in {logEvent.LoggerName}");
                        embed.Color = new Color(14495300);
                        break;
                    case 5:
                        embed.WithTitle($":stop_sign: Fatal Error in {logEvent.LoggerName}");
                        embed.Color = new Color(9319490);
                        break;
                }

                if (hasException) embed.AddField("Exception", logEvent.Exception.Message);
                embeds = new[] { embed.Build() };
            }
            else
            {
                text = this.Layout.Render(logEvent);
            }

            if (DoMentions) text += Mention;

            if (hasException)
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new StreamWriter(stream, Encoding.UTF8);
                    try
                    {
                        writer.Write(logEvent.Exception.ToString());
                        writer.Flush();
                        stream.Seek(0, SeekOrigin.Begin);
                        webhook.SendFileAsync(stream, "stack-trace.txt", text, embeds: embeds).Wait();
                    }
                    finally { writer.Dispose(); }
                }
            }
            else webhook.SendMessageAsync(text, embeds: embeds).Wait();
        }

        private DiscordWebhookClient GetWebhook()
        {
            if (webhook == null) webhook = new DiscordWebhookClient(this.Id, this.Token);
            return webhook;
        }
    }
}
