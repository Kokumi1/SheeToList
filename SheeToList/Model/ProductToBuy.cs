using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SheeToList.Model
{
    public class ProductToBuy : INotifyPropertyChanged
    {
        string name ="";
        int quantity = 1;
        QuantityUnit quantityUnit = QuantityUnit.unit;
        Category categorie = Category.Autre;
        bool isChecked;

    public string Name
    {
        get => name;
        set { name = value; OnPropertyChanged(); }
    }
        public int Quantity { get => quantity;  set  { quantity = value; OnPropertyChanged(); } }
        public QuantityUnit QuantityUnit { get => quantityUnit; set { quantityUnit = value; OnPropertyChanged(); } }

        public string Data { get => $"{Name}    {Quantity} {quantityUnit}"; }

        public bool IsChecked
    {
        get => isChecked;
        set { isChecked = value; OnPropertyChanged(); }
    }

        public Category Categorie
    {
        get => categorie;
        set { categorie = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
}
