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
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();

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

        public ObservableCollection<ProductToBuy> Items { get; }

        public MainViewModel()
        {
            FilterShowCommand = new Command(() => IsFilterVisible = !IsFilterVisible);
            AddItemCommand = new Command(() => AddItem("added thing"));
            ImportDataCommand = new Command(() => ImportData());

                Items = [];
        }

        public void AddItem(string text)
        {
            Items.Add(new ProductToBuy { Name = text, IsChecked = false });
            OnPropertyChanged(nameof(Items));
        }

        public ICommand AddItemCommand { get; }
        public ICommand FilterShowCommand { get; }
        public ICommand ImportDataCommand { get; }
        public string FilterShowButtonText => IsFilterVisible ? "Cachez" : "Révélez tout";

        public void ImportData()
        {
            GoogleApiTalker apiTalker = new();
            IList<IList<Object>> sheetData = apiTalker.GetData();
            foreach (var row in sheetData)
            {
                var itemName = string.Join(" / ", row);
                Items.Add(new ProductToBuy { Name = itemName, IsChecked = false });
            }
            OnPropertyChanged(nameof(Items));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}