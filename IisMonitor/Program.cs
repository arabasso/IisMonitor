using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Threading.Tasks;

namespace IisMonitor
{
    class Program
    {
        static void Main()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            var appCmd = new AppCmd(ConfigurationManager.AppSettings["AppCmd"]);

            var skip = ((MonitorConfigurationSection)ConfigurationManager.GetSection("monitor")).Skip
                .OfType<MonitorSkip>()
                .Select(s => new Uri(s.Uri))
                .ToList();

            var errors = File.Exists("Url.err")
                ? File.ReadAllLines("Url.err").Select(s => new Uri(s)).ToList()
                : new List<Uri>();

            var errorWriter = new StreamWriter("Url.err", false);
            var outWriter = new StreamWriter("Out.log", true);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = int.Parse(ConfigurationManager.AppSettings["MaxDegreeOfParallelism"])
            };

            var hosts = appCmd.GetHosts().ToList();

            Parallel.ForEach(appCmd.GetUrls().Where(w => !skip.Contains(w.RemoteUri)), parallelOptions, url =>
            {
                try
                {
                    var status = url.GetStatus(hosts);

                    Console.WriteLine("{0}\t{1}\t{2}\t{3}({4})", DateTime.Now, url.Name, url.RemoteUri, status.Description, status.Code);

                    if (!status.Success)
                    {
                        lock (outWriter)
                        {
                            outWriter.WriteLine("{0}\t{1}\t{2}\t{3}({4})", DateTime.Now, url.Name, url.RemoteUri, status.Description, status.Code);
                            outWriter.Flush();
                        }

                        lock (errorWriter)
                        {
                            errorWriter.WriteLine(url.RemoteUri);
                            errorWriter.Flush();
                        }

                        if (!errors.Contains(url.RemoteUri))
                        {
                            SendNotification(status);
                        }
                    }
                }

                catch (Exception e)
                {
                    var status = e.ToSiteStatus(url);

                    Console.WriteLine("{0}\t{1}\t{2}\t{3}({4})", DateTime.Now, url.Name, url.RemoteUri, status.Description, status.Code);

                    lock (outWriter)
                    {
                        outWriter.WriteLine("{0}\t{1}\t{2}\t{3}({4})", DateTime.Now, url.Name, url.RemoteUri, status.Description, status.Code);
                        outWriter.Flush();
                    }

                    lock (errorWriter)
                    {
                        errorWriter.WriteLine(url.RemoteUri);
                        errorWriter.Flush();
                    }

                    if (!errors.Contains(url.RemoteUri))
                    {
                        SendNotification(status);
                    }
                }
            });

            outWriter.Close();
            errorWriter.Close();
        }

        private static void SendNotification(
            SiteStatus status)
        {
            using (var smtpClient = new SmtpClient())
            {
                var smtpSection = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");

                var from = new MailAddress(smtpSection.From, ConfigurationManager.AppSettings["Name"]);
                var to = new MailAddress(ConfigurationManager.AppSettings["To"]);

                var mailMessage = new MailMessage(from, to)
                {
                    Subject = $"{DateTime.Now} - {status.Description} - {status.Url.RemoteUri}",
                    IsBodyHtml = true,
                    Body = status.Response
                };

                smtpClient.Send(mailMessage);
            }
        }
    }
}