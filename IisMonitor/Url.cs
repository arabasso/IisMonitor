using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace IisMonitor
{
    public class Url
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public Uri LocalUri { get; set; }
        public Uri RemoteUri { get; set; }

        public Url(
            string site,
            string binding,
            string app)
        {
            Name = site;

            var match = Regex.Match(binding, @"(?<scheme>https?)/(?<ip>.*):(?<port>\d+):(?<host>.*)");

            if (!match.Success) throw new Exception($"Binding Exception: {binding}");

            var scheme = match.Groups["scheme"].Value;
            var ip = match.Groups["ip"].Value == "*" ? ConfigurationManager.AppSettings["Ip"] : match.Groups["ip"].Value;
            var port = int.Parse(match.Groups["port"].Value);

            Host = match.Groups["host"].Value;

            LocalUri = new Uri($"{scheme}://{ip}:{port}{app}");
            RemoteUri = new Uri($"{scheme}://{(!string.IsNullOrEmpty(Host) ? Host : ip)}:{port}{app}");
        }

        private static HttpWebRequest GetRequest(
            Uri uri,
            string host)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);

            request.AllowAutoRedirect = false;
            request.Timeout = int.Parse(ConfigurationManager.AppSettings["Timeout"]);
            request.CookieContainer = new CookieContainer();

            if (!string.IsNullOrEmpty(host))
            {
                request.Host = host;
            }

            return request;
        }

        public UrlStatus GetStatus(
            List<string> hosts)
        {
            var request = GetRequest(LocalUri, Host);

            var response = request.GetResponseNoException();

            while (response.IsRedirect())
            {
                var location = response.Headers["Location"];

                try
                {
                    var locationUri = location.StartsWith("/")
                        ? new Uri(RemoteUri, location)
                        : new Uri(location);

                    if (locationUri.Host == request.Host || hosts.Contains(locationUri.Host))
                    {
                        var pathAndQuery = Uri.EscapeUriString(locationUri.PathAndQuery);

                        var uri = new Uri($"{locationUri.Scheme}://{ConfigurationManager.AppSettings["Ip"]}:{locationUri.Port}{pathAndQuery}");

                        request = GetRequest(uri, locationUri.Host);
                    }

                    else
                    {
                        request = GetRequest(locationUri, locationUri.Host);
                    }

                    request.CookieContainer.Add(response.Cookies);
                }

                catch (Exception e)
                {
                    throw new Exception($"Redirect Exception: {location}", e);
                }

                response = request.GetResponseNoException();
            }

            return new UrlStatus
            {
                Url = this,
                Code = (int)response.StatusCode,
                Description = response.StatusDescription,
                Response = response.GetString()
            };
        }
    }

    public class UrlStatus
    {
        public Url Url { get; set; }
        public int Code { get; set; }
        public string Description { get; set; }
        public string Response { get; set; }
        public bool Success => Code >= 200 && Code <= 299;
    }
}