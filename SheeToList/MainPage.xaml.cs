using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SheeToList.Services;

namespace SheeToList
{
    public partial class MainPage : ContentPage
    {
        
        string dataString = "";

        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
            var Items = new MainViewModel().Items;
         
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Item> Items { get; }

        public MainViewModel()
        {
            //initialize Google Sheets API
            /*    GoogleApiTalker apiTalker = new();
               dataString =   apiTalker.GetDataString();

                LabelTextFromSheet.Text = dataString;*/


            // Génère une liste "infinie" pour la démo (ici 10 éléments)
            Items = new ObservableCollection<Item>(
                Enumerable.Range(1, 10).Select(i => new Item { Text = $"Item {i}", IsChecked = false })
            );
            OnPropertyChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
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
