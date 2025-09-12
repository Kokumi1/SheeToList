using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SheeToList.Model
{
    //TODO: rename: class name to Product
    public class Item : INotifyPropertyChanged
    {
        string text;
    bool isChecked;

    public string Text
    {
        get => text;
        set { text = value; OnPropertyChanged(); }
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
