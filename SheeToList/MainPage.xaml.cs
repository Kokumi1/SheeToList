using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using SheeToList.Model;
using SheeToList.Services;

namespace SheeToList
{
    public partial class MainPage : ContentPage
    {
        #region MainPage
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
    #endregion


    // ViewModel for MainPage
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MainPage _page ;
        private bool _isbuyedProductVisible ;
        private bool _isLoading;

        #region Properties
        public bool IsbuyedProductVisible
        {
            get => _isbuyedProductVisible;
            set
            {
                _isbuyedProductVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilterShowButtonText));
                OnPropertyChanged(nameof(ToBuyProducts));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ProductToBuy> ToBuyProducts
        {
            get
            {
                if (IsbuyedProductVisible)
                    return Products;
                return new ObservableCollection<ProductToBuy>(Products.Where(item => !item.IsChecked));
            }
        }
        public ObservableCollection<ProductToBuy> Products { get; set; }
        #endregion

        #region Constructor
        public MainViewModel(MainPage page)
        {
            FilterShowCommand = new Command(() => IsbuyedProductVisible = !IsbuyedProductVisible);
            AddItemCommand = new Command(AddProduct);
            ImportDataCommand = new Command(() => ImportData());
            EditItemCommand = new Command<ProductToBuy>(EditProduct);
            DeleteItemCommand = new Command<ProductToBuy>(DeleteProduct);

            Products = [];
            _page = page;
            CollectionChangedSetup();
        }
        #endregion

        #region Data Import
        public async Task ImportData()
        {
            IsLoading = true;
            OnPropertyChanged(nameof(IsLoading));

            //await _page.DisplayAlert("Debug", IsLoading.ToString(), "OK");
            GoogleApiTalker apiTalker = new();
            try
            {
                var sorted =(await  GoogleApiTalker.GetData()).OrderBy(item => item.Name).ToList();
                foreach (var item in sorted)
                    Products.Add(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                var sorted = new List<ProductToBuy>();
                await _page.DisplayAlert("Erreur", "Impossible de récupérer les données. Vérifiez votre connexion internet.", "OK");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(IsLoading));
            }
           // await _page.DisplayAlert("Debug", IsLoading.ToString(), "OK");
            OnPropertyChanged(nameof(Products));
        }
        #endregion

        #region Command Region
        public ICommand AddItemCommand { get; }
        public ICommand FilterShowCommand { get; }
        public ICommand ImportDataCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public string FilterShowButtonText => IsbuyedProductVisible ? "Cachez" : "Révélez tout";
        #endregion

        #region Products Management
        public async void AddProduct()
        {
            string? text = await _page.ItemNameAskerAsync("Entrer le nom", "Entrer le nom de l'objet à ajoutée");
            if (string.IsNullOrWhiteSpace(text)) return;

            Products.Add(new ProductToBuy { Name = text, IsChecked = false });
            OnPropertyChanged(nameof(Products));
        }
        private async void EditProduct(ProductToBuy Product)
        {
            if (Products == null) return;
            string? text = await _page.ItemNameAskerAsync("Entrer le nom", "Entrer le nouveau nom",
                Product.Name);
            if (string.IsNullOrWhiteSpace(text)) return;
            Product.Name = text;
        }

        private void DeleteProduct(ProductToBuy Product)
        {
            if (Products == null) return;
            Products.Remove(Product);
        }
        #endregion

        private void CollectionChangedSetup()
        {
            Products.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (ProductToBuy item in e.NewItems)
                        item.PropertyChanged += Item_PropertyChanged;
                if (e.OldItems != null)
                    foreach (ProductToBuy item in e.OldItems)
                        item.PropertyChanged -= Item_PropertyChanged;
                OnPropertyChanged(nameof(ToBuyProducts));
            };
        }

        #region Event Handlers
        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductToBuy.IsChecked))
            {
                OnPropertyChanged(nameof(ToBuyProducts));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    #endregion
}