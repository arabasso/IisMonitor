using System.Configuration;

namespace IisMonitor
{
    public class MonitorSkip :
        ConfigurationElement
    {
        [ConfigurationProperty("uri", IsRequired = true)]
        public string Uri
        {
            get => (string)this["uri"];
            set => this["uri"] = value;
        }
    }
}