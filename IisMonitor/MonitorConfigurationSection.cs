using System.Configuration;

namespace IisMonitor
{
    public class MonitorConfigurationSection :
        ConfigurationSection
    {
        [ConfigurationProperty("skip")]
        public MonitorSkipCollection Skip => ((MonitorSkipCollection)base["skip"]);
    }
}