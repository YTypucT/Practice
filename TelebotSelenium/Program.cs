namespace TeleBot2
{
    using System;
    using System.Configuration;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using SeleniumScreener;

    static class Program
    {
        static void Main()
        {
            String linkURL;
            linkURL = Console.ReadLine();
            var screener = new Screener();
            screener.GetScreenshot(linkURL);  
        }
    }
}
