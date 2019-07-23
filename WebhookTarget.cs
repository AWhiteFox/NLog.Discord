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
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var webhook = GetWebhook();
            string text = string.Empty;
            IEnumerable<Embed> embeds = null;

            if (!UseEmbeds)
            {
                logEvent.Exception = null;
                text = this.Layout.Render(logEvent);
            }
            else
            {
                var embed = new EmbedBuilder
                {
                    Timestamp = logEvent.TimeStamp,
                    Description = logEvent.Message
                };
                embed.WithFooter($"sec: {logEvent.TimeStamp.Second} ms: {logEvent.TimeStamp.Millisecond}");

                string level = string.Empty;
                switch (logEvent.Level.Ordinal)
                {
                    case 0: level = "Trace message"; break;
                    case 1: level = ":gear: Debug message"; break;
                    case 2:
                        level = ":information_source: Info";
                        embed.Color = new Color(3901635);
                        break;
                    case 3:
                        level = ":warning: Warning";
                        embed.Color = new Color(16763981);
                        break;
                    case 4:
                        level = ":x: Error";
                        embed.Color = new Color(14495300);
                        break;
                    case 5:
                        level = ":stop_sign: Fatal Error";
                        embed.Color = new Color(9319490);
                        break;
                }                
                embed.WithTitle($"{level} in {logEvent.LoggerName}");
                if (logEvent.Exception != null) embed.AddField("Exception", logEvent.Exception.Message);

                webhook.SendMessageAsync(embeds: new Embed[1] { embed.Build() }).Wait();
            }

            if (DoMentions) text += " " + Mention;

            if (logEvent.Exception.StackTrace != null)
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
                    finally
                    {
                        writer.Dispose();
                    }
                }
            }
            else
            {
                webhook.SendMessageAsync(text, embeds: embeds).Wait();
            }
        }

        private DiscordWebhookClient GetWebhook()
        {
            if (webhook == null)
            {
                webhook = new DiscordWebhookClient(this.Id, this.Token);
            }
            return webhook;
        }
    }
}
