using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SheeToList.Model
{
    public class ProductToBuy : INotifyPropertyChanged
    {
        string name ="";
         bool isChecked;

    public string Name
    {
        get => name;
        set { name = value; OnPropertyChanged(); }
    }

    public bool IsChecked
    {
        get => isChecked;
        set { isChecked = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
}
