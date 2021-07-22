namespace TeleBot2
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Reflection;
    using Telegram.Bot;
    using Telegram.Bot.Args;
    using Topshelf;
    using SeleniumScreener;
    using System.Drawing;

    class Service : ServiceControl
    {
        //static readonly string FolderCurrent = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        static readonly string ConfigBotKey = ConfigurationManager.AppSettings["bot_key"];
        static readonly string ConfigBinary_location = ConfigurationManager.AppSettings["binary_location"];
        static TelegramBotClient Bot = new TelegramBotClient(ConfigBotKey);
        public bool Start(HostControl hostControl)
        {
            ProcessMessages();
            return true;
        }
        private void ProcessMessages()
        {
            var meName = "@" + Bot.GetMeAsync().Result.Username;
            var rkm = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup();
            Console.WriteLine(meName);
            Bot.SetWebhookAsync("").Wait();
            Bot.OnUpdate += (object su, UpdateEventArgs evu) =>
            {
                try
                {
                    if (evu.Update.Message.Entities != null)
                    {
                        var ent = evu.Update.Message.Entities.First();
                        var cmd = evu.Update.Message.Text.Substring(ent.Offset, ent.Length);
                        if (cmd == "/picture")
                        {
                            Bot.SendTextMessageAsync(evu.Update.Message.From.Id, "Send a link");
                        }

                    }
                    if (evu.Update.Message.Text.Contains("http"))
                    {
                        var linkURL = evu.Update.Message.Text;
                        var screener = new Screener(ConfigBinary_location);
                        byte[] bytes = screener.GetScreenshot(linkURL);
                        Stream S = new MemoryStream(bytes);
                        Bot.SendPhotoAsync(evu.Update.Message.From.Id, S);
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            };
            Bot.StartReceiving();
        }

        public bool Stop(HostControl hostControl)
        {
            Bot.StopReceiving();
            return true;
        }

    }

    static class StringExtensions
    {

        public static IEnumerable<String> SplitInParts(this String s, Int32 partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }
    }
    static class Program
    {
        static int Main()
        {
            return (int)HostFactory.Run(x =>
            {
                x.SetServiceName("TESTBOT");
                x.SetDisplayName("TESTBOT");
                x.SetDescription("Бот test");
                x.RunAsLocalSystem();
                x.StartAutomatically();
                x.Service<Service>();
                x.EnableServiceRecovery(r => r.RestartService(1));
            });
        }
    }
}
