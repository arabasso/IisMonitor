using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace IisMonitor
{
    public static class Extensions
    {
        public static string GetString(
            this HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            using(var reader = new StreamReader(stream, Encoding.GetEncoding(response.CharacterSet ?? "utf-8")))
            {
                return reader.ReadToEnd();
            }
        }

        public static IEnumerable<Url> GetUrls(
            this AppCmd appCmd)
        {
            foreach (var site in appCmd.GetLines("list sites /text:name"))
            {
                foreach (var binding in appCmd.GetLines($"list site \"{site}\" /text:bindings").SelectMany(s => s.Split(',')))
                {
                    foreach (var app in appCmd.GetLines($"list app /site.name:\"{site}\" /text:path"))
                    {
                        Url url;

                        try
                        {
                            url = new Url(site, binding, app);
                        }

                        catch
                        {
                            continue;
                        }

                        yield return url;
                    }
                }
            }
        }

        public static IEnumerable<string> GetHosts(
            this AppCmd appCmd)
        {
            foreach (var binding in appCmd.GetLines("list sites /text:bindings")
                .SelectMany(s => s.Split(',')))
            {
                var match = Regex.Match(binding, @"(https?)/(.*):(\d+):(?<host>.*)");

                yield return match.Groups["host"].Value;
            }
        }

        public static HttpWebResponse GetResponseNoException(
            this HttpWebRequest req)
        {
            try
            {
                return (HttpWebResponse)req.GetResponse();
            }

            catch (WebException we)
            {
                if (we.Response is HttpWebResponse resp)
                {
                    return resp;
                }

                throw;
            }
        }

        public static bool IsRedirect(
            this HttpWebResponse response)
        {
            var statusCode = (int) response.StatusCode;

            return statusCode >= 300 && statusCode <= 399;
        }

        public static SiteStatus ToSiteStatus(
            this Exception exception,
            Url url)
        {
            return new SiteStatus
            {
                Url = url,
                Code = -1,
                Description = exception.Message,
                Response = exception.InnerException?.Message ?? exception.Message
            };
        }
    }
}