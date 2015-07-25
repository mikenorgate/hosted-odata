using System;
using Microsoft.Owin.Hosting;

namespace OESoftware.Hosted.OData.Api
{
    public class Program
    {
        static void Main(string[] args)
        {
            string baseUrl = "http://*:5000";
            using (WebApp.Start<Startup>(baseUrl))
            {
                Console.WriteLine("Press Enter to quit.");

                Console.ReadKey();

            }
        }
    }
}