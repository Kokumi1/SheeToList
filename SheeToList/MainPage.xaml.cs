using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;
using SheeToList.Services;

namespace SheeToList
{
    public partial class MainPage : ContentPage
    {
        //MainViewModel viewModel;

        public MainPage()
        {
            InitializeComponent();
            //viewModel = new MainViewModel();
            BindingContext = new MainViewModel();
            //var Items = new MainViewModel().Items;

        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _isFilterVisible;
        public bool IsFilterVisible
        {
            get => _isFilterVisible;
            set
            {
                _isFilterVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilterShowButtonText));
            }
        }

        public ObservableCollection<Item> Items { get; }

        public MainViewModel()
        {
            FilterShowCommand = new Command(() => IsFilterVisible = !IsFilterVisible);
            AddItemCommand = new Command(() => AddItem("added thing"));
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

        public void AddItem(string text)
        {
            Items.Add(new Item { Text = text, IsChecked = false });
            OnPropertyChanged(nameof(Items));
        }

        public ICommand AddItemCommand { get; }
        public ICommand FilterShowCommand { get; }
        public string FilterShowButtonText => IsFilterVisible ? "Cachez" : "Révélez tout";

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
