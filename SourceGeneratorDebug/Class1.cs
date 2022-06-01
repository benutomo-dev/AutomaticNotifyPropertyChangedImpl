using Benutomo;
using System.ComponentModel;
using static Benutomo.NotificationAccessibility;

using CAttribute = Benutomo.EnableNotificationSupportAttribute;

namespace SourceGeneratorDebug
{
    public partial class Class1 : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangingEventHandler? PropertyChanging;

        public event PropertyChangedEventHandler? PropertyChanged;

        class Class2
        {

        }

        [EnableNotificationSupport(EventArgsOnly = false)]
        [ChangedEvent(Public)]
        [ChangingEvent(Public)]
        public bool? IsEnabled
        {
            get => _IsEnabled();
            set => _IsEnabled(value, EqualityComparer<bool?>.Default);
        }

#nullable enable
        [EnableNotificationSupport]
        [ChangedEvent]
        [ChangingEvent]
        public string Text
        {
            get => _Text();
            set => _Text(value);
        }
#nullable restore

#nullable disable
        [EnableNotificationSupport]
        public int Number
        {
            get => _Number();
            set => _Number(value);
        }
#nullable restore

        [EnableNotificationSupport]
        public List<Dictionary<(int, string?), long>> X
        {
            get => _X();
            set => _X(value);
        }

        [EnableNotificationSupport]
        Class2 Inner
        {
            get => _Inner();
            set => _Inner(value);
        }
    }
}