using Benutomo;
using System.ComponentModel;

namespace SourceGeneratorDebug
{
    [AutomaticNotifyPropertyChangedImpl]
    public partial class Class1 : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangingEventHandler? PropertyChanging;

        public event PropertyChangedEventHandler? PropertyChanged;

        class Class2
        {

        }

        [EnableAutomaticNotify]
        public bool? IsEnabled
        {
            get => _IsEnabled();
            set => _IsEnabled(value);
        }

#nullable enable
        [EnableAutomaticNotify]
        public string Text
        {
            get => _Text();
            set => _Text(value);
        }
#nullable restore

#nullable disable
        [EnableAutomaticNotify]
        public int Number
        {
            get => _Number();
            set => _Number(value);
        }
#nullable restore

        [EnableAutomaticNotify]
        public List<Dictionary<(int, string?), long>> X
        {
            get => _X();
            set => _X(value);
        }

        [EnableAutomaticNotify]
        Class2 Inner
        {
            get => _Inner();
            set => _Inner(value);
        }
    }
}