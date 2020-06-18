using System.Configuration;

namespace IisMonitor
{
    [ConfigurationCollection(typeof(MonitorSkip))]
    public class MonitorSkipCollection :
        ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MonitorSkip();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MonitorSkip)element).Uri;
        }

        public MonitorSkip this[int index] => (MonitorSkip)BaseGet(index);
    }
}