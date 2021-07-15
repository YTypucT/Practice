namespace TeleBot2
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.IO;
    using System.Reflection;
    using Newtonsoft.Json;
    using Telegram.Bot;
    using Telegram.Bot.Args;
    using Telegram.Bot.Types.InputFiles;
    using Topshelf;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium;
    using System.Threading;

    class Service : ServiceControl
    {
        static readonly string FolderCurrent = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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
            Console.WriteLine(meName);
            Bot.SetWebhookAsync("").Wait();
            Bot.OnUpdate += (object su, UpdateEventArgs evu) =>
            {
                try
                {
                    var folder = Directory.CreateDirectory(Path.Combine(FolderCurrent, "!Chats", evu.Update.Message.Chat.Id.ToString(), DateTime.Now.ToString("yyyyMMddHH")
                      , (string.IsNullOrWhiteSpace(evu.Update.Message.From.Username) ? evu.Update.Message.From.Id.ToString() : evu.Update.Message.From.Username.ToString()))).FullName;
                    File.WriteAllText(Path.Combine(folder, evu.Update.Id + ".json"), JsonConvert.SerializeObject(evu.Update));
                    if (evu.Update.Message.Entities != null && evu.Update.Message.Entities.First().ToString() == "/s")
                    {
                        string fileId;
                        var fileResult = CreateStickerSquare(evu.Update.Message.Text, Path.Combine(FolderCurrent, "uniqlo.ttf"), Color.White, ColorTranslator.FromHtml("#ED1D24"), out fileId);
                        if (string.IsNullOrWhiteSpace(fileId))
                            using (var fs = File.OpenRead(fileResult))
                                Bot.SendStickerAsync(evu.Update.Message.Chat.Id, new InputOnlineFile(fs)).Wait();
                    }
                    else GetScreenshot(evu.Update.Message.Text);
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            };
            Bot.StartReceiving();
        }

        private void GetScreenshot(string text)
        {

            ChromeDriver browser;
            var options = new ChromeOptions();
            //if (!Environment.UserInteractive)
            //  options.AddArgument("headless");
            options.UnhandledPromptBehavior = UnhandledPromptBehavior.Dismiss;
            //options.AddArguments("proxy-server=socks5://127.0.0.1:" + portSocks);
            options.AddExcludedArgument("enable-automation");
            options.AddArgument("disable-blink-features=AutomationControlled");
            //options.BinaryLocation = ConfigBinary_location;
            options.AddArgument("no-default-browser-check");
            options.AddArgument("no-first-run");
            options.AddArgument("incognito");
            options.AddArgument("start-maximized");
            options.AddArgument("disable-infobars");
            options.AddArgument("silent");
            options.AddArgument("log-level=3");
            options.AddArgument("lang=ru");
            options.AddArgument("enable-precise-memory-info");
            options.AddArgument("disable-plugins");
            options.AddArgument("disable-default-apps");
            options.AddArgument("disable-extensions");
            options.AddArgument("disable-gpu");
            options.AddArgument("no-sandbox");
            options.AddArgument("window-size=1920,1080");
            options.AddUserProfilePreference("download.prompt_for_download", "false");
            options.AddUserProfilePreference("download.directory_upgrade", "true");
            //options.AddUserProfilePreference("download.default_directory", DownloadsFolder);
            options.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
            options.AddUserProfilePreference("profile.content_settings.exceptions.automatic_downloads.*.setting", 1);
            options.AddUserProfilePreference("profile.default_content_settings.popups", 0);
            browser = new ChromeDriver(FolderCurrent, options, TimeSpan.FromMinutes(10));
            browser.Manage().Window.Size = new Size(1920, 1080);
            browser.Navigate().GoToUrl(text);
            browser.Close();
            browser.Quit();
        }

        public bool Stop(HostControl hostControl)
        {
            Bot.StopReceiving();
            return true;
        }
        static string CreateStickerSquare(string messageText, string fileFont, Color textColor, Color backColor, out string fileId)
        {
            fileId = string.Empty;
            var foo = new PrivateFontCollection();
            foo.AddFontFile(fileFont);
            var fileResult = Path.Combine(FolderCurrent, DateTime.Now.Ticks.ToString() + ".png");

            var text = messageText.ToUpper();
            var cols = 3;
            var rows = text.Length / cols;
            float fontSize = 10f;
            var paddingSize = 40f;
            var chunkWidth = (float)(512 - paddingSize) / (float)cols;
            var chunkHeight = ((float)(512 - paddingSize) / (float)rows);// - (float)((rows - 1)* linesSpace);
            using (GraphicsPath GP = new GraphicsPath())
            using (var fontF = (FontFamily)foo.Families[0])
            {
                Rectangle br;
                do
                {
                    fontSize++;
                    GP.AddString(text[0].ToString(), fontF, (int)FontStyle.Bold, fontSize, Point.Empty, StringFormat.GenericTypographic);
                    br = Rectangle.Round(GP.GetBounds());
                }
                while (((float)br.Width < chunkWidth && (float)br.Height < chunkHeight));
                fontSize--;
            }
            using (var result = new Bitmap(512, 512, PixelFormat.Format24bppRgb))
            {
                result.MakeTransparent();
                result.SetResolution(96f, 96f);
                using (var g = Graphics.FromImage(result))
                {
                    var brushSolid = new SolidBrush(backColor);
                    var brushText = new SolidBrush(textColor);
                    g.Clear(Color.Transparent);
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    g.FillRectangle(brushSolid, 0, 0, result.Width, result.Height);
                    var lines = text.SplitInParts(cols).ToArray();
                    var firstLetterHeight = 0;
                    for (var r = 0; r < lines.Length; ++r)
                        for (int i = 0; i < lines[r].Length; i++)
                            using (var fontF = foo.Families[0])
                            using (var GP = new GraphicsPath())
                            {
                                var c = lines[r][i].ToString();
                                GP.AddString(c, fontF, (int)FontStyle.Bold, fontSize, Point.Empty, StringFormat.GenericTypographic);
                                Rectangle br = Rectangle.Round(GP.GetBounds());
                                if (i == 0)
                                    firstLetterHeight = br.Height;
                                var offsetLeft = paddingSize / 2 + (i * chunkWidth);
                                var offsetHeight = paddingSize / 2 + (r * chunkHeight) + (br.Height - firstLetterHeight) / 2;
                                g.TranslateTransform(offsetLeft + ((chunkWidth - br.Width) / 2 - br.X), offsetHeight + (chunkHeight - br.Height) / 2 - br.Y);
                                g.FillPath(Brushes.White, GP);
                                g.ResetTransform();
                            }
                }
                File.WriteAllBytes(fileResult, SaveAs(result, ImageFormat.Png, 100));
            }
            return fileResult;
        }

        public static byte[] SaveAs(Image image, ImageFormat imageFormat, Int64 quality)
        {
            ImageCodecInfo[] imageCodecs = ImageCodecInfo.GetImageDecoders();
            ImageCodecInfo formatEncoder = Array.Find<ImageCodecInfo>(imageCodecs, (c => c.FormatID == imageFormat.Guid));
            if (formatEncoder == null)
                formatEncoder = Array.Find<ImageCodecInfo>(imageCodecs, (c => c.FormatID == ImageFormat.Png.Guid));
            using (var qualityEncoderParameter = new EncoderParameter(Encoder.Quality, quality))
            using (var encoderParameters = new EncoderParameters(1))
            using (var ms = new MemoryStream())
            {
                encoderParameters.Param[0] = qualityEncoderParameter;
                image.Save(ms, formatEncoder, encoderParameters);
                return ms.ToArray();
            }
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
