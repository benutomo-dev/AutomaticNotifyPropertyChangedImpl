using Benutomo;
using System.ComponentModel;

namespace SourceGeneratorDebug
{
    [AutomaticNotifyPropertyChangedImpl]
    public partial class Class1 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [EnableAutomaticNotify]
        public bool IsEnabled
        {
            get => _IsEnabled();
            set => _IsEnabled(value);
        }

        [EnableAutomaticNotify]
        public int Number
        {
            get => _Number();
            set => _Number(value);
        }
    }
}