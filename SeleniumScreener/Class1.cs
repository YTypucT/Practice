using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumScreener
{
    public class Screener
    {
        static readonly string FolderCurrent = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static Bitmap GetEntireScreenshot(ChromeDriver browser, Rectangle cropArea)
        {
            var browserJSE = (IJavaScriptExecutor)browser;
            int totalWidth = (int)(long)browserJSE.ExecuteScript("return document.body.offsetWidth");
            int totalHeight = (int)(long)browserJSE.ExecuteScript("return  document.body.parentNode.scrollHeight");
            int viewportWidth = (int)(long)browserJSE.ExecuteScript("return document.body.clientWidth");
            int viewportHeight = (int)(long)browserJSE.ExecuteScript("return window.innerHeight");
            List<Rectangle> rectangles = new List<Rectangle>();
            for (int i = 0; i < Math.Min(totalHeight, viewportHeight * 2); i += viewportHeight)
            {
                if (i > 0)
                {
                    try { browser.ExecuteScript("arguments[0].style.display='none'", browser.FindElement(By.XPath("//*[@id='page_bottom_banners_root']/div"))); } catch { }
                    try
                    {
                        browser.ExecuteScript(@"
            var elems = window.document.getElementsByTagName('*');
            for(i = 0; i < elems.length; i++) 
            { 
                if (window.getComputedStyle) 
                {
                   var elemStyle = window.getComputedStyle(elems[i], null); 
                   if (elemStyle.getPropertyValue('position') == 'fixed' && elems[i].innerHTML.length != 0 )
                     elems[i].parentNode.removeChild(elems[i]);
                }
                else 
                {
                   var elemStyle = elems[i].currentStyle; 
                   if (elemStyle.position == 'fixed' && elems[i].childNodes.length != 0)
                     elems[i].parentNode.removeChild(elems[i]); 
                }   
            }");
                    }
                    catch { }
                }
                int newHeight = viewportHeight;
                if (i + viewportHeight > totalHeight)
                    newHeight = totalHeight - i;
                for (int ii = 0; ii < totalWidth; ii += viewportWidth)
                {
                    int newWidth = viewportWidth;
                    if (ii + viewportWidth > totalWidth)
                        newWidth = totalWidth - ii;
                    rectangles.Add(new Rectangle(ii, i, newWidth, newHeight));
                }
            }
            using (var result = new Bitmap(totalWidth, Math.Min(totalHeight, viewportHeight * 2), PixelFormat.Format24bppRgb))
            {
                result.SetResolution(96f, 96f);
                using (Graphics graphics = Graphics.FromImage(result))
                {
                    graphics.Clear(Color.Transparent);
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    Rectangle previous = Rectangle.Empty;
                    foreach (var rectangle in rectangles)
                    {
                        if (previous != Rectangle.Empty)
                        {
                            int xDiff = rectangle.Right - previous.Right;
                            int yDiff = rectangle.Bottom - previous.Bottom;
                            browserJSE.ExecuteScript(String.Format("window.scrollBy({0}, {1})", xDiff, yDiff));
                            Thread.Sleep(200);
                        }
                        Image screenshotImage;
                        using (MemoryStream memStream = new MemoryStream(((ITakesScreenshot)browser).GetScreenshot().AsByteArray))
                            screenshotImage = Image.FromStream(memStream);
                        Rectangle sourceRectangle = new Rectangle(viewportWidth - rectangle.Width, viewportHeight - rectangle.Height, rectangle.Width, rectangle.Height);
                        graphics.DrawImage(screenshotImage, rectangle, sourceRectangle, GraphicsUnit.Pixel);
                        previous = rectangle;
                    }
                }
                return (Bitmap)result.Clone(cropArea, result.PixelFormat);
            }
        }
        public static byte[] SaveAs(Image image, ImageFormat imageFormat, Int64 quality)
        {
            ImageCodecInfo[] imageCodecs = ImageCodecInfo.GetImageDecoders();
            ImageCodecInfo formatEncoder = Array.Find<ImageCodecInfo>(imageCodecs, (c => c.FormatID == imageFormat.Guid));
            if (formatEncoder == null)
                formatEncoder = Array.Find<ImageCodecInfo>(imageCodecs, (c => c.FormatID == ImageFormat.Jpeg.Guid));
            using (var qualityEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality))
            using (var encoderParameters = new EncoderParameters(1))
            using (var ms = new MemoryStream())
            {
                encoderParameters.Param[0] = qualityEncoderParameter;
                image.Save(ms, formatEncoder, encoderParameters);
                return ms.ToArray();
            }
        }
        public void GetScreenshot (String linkURL)
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
            browser.Navigate().GoToUrl(linkURL);
            Thread.Sleep(5000);

            if (linkURL.Contains("twitter"))
            {
                var article = browser.FindElementByTagName("article");
                Screenshot screen = browser.GetScreenshot();
                var bmpScreen = new Bitmap(new MemoryStream(screen.AsByteArray));
                var cropArea = new Rectangle(article.Location, article.Size);
                bmpScreen.Clone(cropArea, bmpScreen.PixelFormat).Save("C:\\Users\\Viktor\\Source\\Repos\\TeleBot\\TeleBot2\\ScreenshotTwitter.png");
            }
            //else if (linkURL.Contains("facebook"))
            //{
            //    var article = browser.FindElementByClassName("f");
            //    Screenshot screen = browser.GetScreenshot();
            //    var bmpScreen = new Bitmap(new MemoryStream(screen.AsByteArray));
            //    var cropArea = new Rectangle(article.Location, article.Size);
            //    screen.SaveAsFile(@"C:\Users\Viktor\Source\Repos\TeleBot\TeleBot2\ScreenshotFacebook.png");
            //}
            else if (linkURL.Contains("vk"))
            {
                var post = browser.FindElementByXPath("//*[contains (@class, '_post_content') and not contains (@class, 'replies')]");
                var cropArea = new Rectangle(post.Location, post.Size);
                GetEntireScreenshot(browser, cropArea).Save("C:\\Users\\Viktor\\Source\\Repos\\TeleBot\\TeleBot2\\ScreenshotVk.png");
            }
            else
                Console.WriteLine("The site is incorrect");
            browser.Close();
            browser.Quit();
        }
    }
}
