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
            BindingContext = new MainViewModel(this);

        }

        public async Task<String?> ItemNameAskerAsync(string title, string message, string initialValue = "")
        {
            return  await DisplayPromptAsync(title, message, initialValue: initialValue);
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MainPage _page ;
        private bool _isFilterVisible ;
        public bool IsFilterVisible
        {
            get => _isFilterVisible;
            set
            {
                _isFilterVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilterShowButtonText));
                OnPropertyChanged(nameof(FilteredItems));
            }
        }

        public ObservableCollection<ProductToBuy> FilteredItems
        {
            get
            {
                if (IsFilterVisible)
                    return Items;
                return new ObservableCollection<ProductToBuy>(Items.Where(item => !item.IsChecked));
            }
        }

        public ObservableCollection<ProductToBuy> Items { get; set; }

        public MainViewModel(MainPage page)
        {
            FilterShowCommand = new Command(() => IsFilterVisible = !IsFilterVisible);
            AddItemCommand = new Command(AddItem);
            ImportDataCommand = new Command(() => ImportData());
            EditItemCommand = new Command<ProductToBuy>(EditItem);
            DeleteItemCommand = new Command<ProductToBuy>(DeleteItem);

            Items = [];
            _page = page;

            Items.CollectionChanged += (s, e) =>
            {
                if(e.NewItems != null)
                    foreach (ProductToBuy item in e.NewItems)
                        item.PropertyChanged += Item_PropertyChanged;
                    if(e.OldItems != null)
                        foreach (ProductToBuy item in e.OldItems)
                            item.PropertyChanged -= Item_PropertyChanged;
                OnPropertyChanged(nameof(FilteredItems));
            };
        }

        public async void AddItem()
        {
            string? text = await _page.ItemNameAskerAsync("Entrer le nom", "Entrer le nom de l'objet à ajoutée");
            if (string.IsNullOrWhiteSpace(text)) return;

            Items.Add(new ProductToBuy { Name = text, IsChecked = false });
            OnPropertyChanged(nameof(Items));
        }

        public ICommand AddItemCommand { get; }
        public ICommand FilterShowCommand { get; }
        public ICommand ImportDataCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public string FilterShowButtonText => IsFilterVisible ? "Cachez" : "Révélez tout";

        public void ImportData()
        {
            GoogleApiTalker apiTalker = new();
           
            var sorted = GoogleApiTalker.GetData().OrderBy(item => item.Name).ToList();
            foreach (var item in sorted)
                Items.Add(item);

           
            OnPropertyChanged(nameof(Items));
        }

        private async void EditItem(ProductToBuy item)
        {
            if (item == null) return;
            // Logique d'édition (ex: ouvrir une popup de modification)
            string? text = await _page.ItemNameAskerAsync("Entrer le nom", "Entrer le nouveau nom",
                item.Name);
            if (string.IsNullOrWhiteSpace(text)) return;
            item.Name = text;
        }

        private void DeleteItem(ProductToBuy item)
        {
            if (item == null) return;
            Items.Remove(item);
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductToBuy.IsChecked))
            {
                OnPropertyChanged(nameof(FilteredItems));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}