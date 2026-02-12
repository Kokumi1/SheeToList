using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SheeToList.Model
{
    public class CategoryDefinition : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private ObservableCollection<string> _keywords = new();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Keywords
        {
            get => _keywords;
            set { _keywords = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}