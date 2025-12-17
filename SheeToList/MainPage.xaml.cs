using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using SheeToList.Model;
using SheeToList.Services;
using SheeToList.View;

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

        public async Task<String?> ItemNameAskerAsync(string title, string message, string initialValue = "",string accept="Valider", string cancel="Annuler")
        {
            return  await DisplayPromptAsync(title, message, accept:accept, cancel:cancel, initialValue: initialValue);
        }
        public async Task<string?> ItemNameOrPickAskerAsync(string title,/* IEnumerable<string> choices, */string initialValue = "")
        {
            var popup = new PickOrTypePopup(/*choices,*/ initialValue);
            // Si vous voulez afficher un titre: vous pouvez envelopper popup avec un layout contenant un Label
            // Affiche le popup et attend le résultat
            this.ShowPopup(popup);
            return await popup.WaitForResultAsync();
        }

        private async void Recipe_Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RecipeList());
        }
        #endregion
    }


    // ViewModel for MainPage
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MainPage _page ;
        private bool _isBuyedProductVisible ;
        private ObservableCollection<ProductToBuy>? _filteredProducts;
        private bool _isLoading;
        // Debounce pour les sauvegardes
        private CancellationTokenSource? _saveCts;

        #region Properties
        public bool IsBuyedProductVisible
        {
            get => _isBuyedProductVisible;
            set
            {
                _isBuyedProductVisible = value;
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
                if (IsBuyedProductVisible)
                    return Products;
                _filteredProducts ??= new ObservableCollection<ProductToBuy>(Products.Where(item => !item.IsChecked));
                return _filteredProducts;
            }
        }
        public ObservableCollection<ProductToBuy> Products { get; set; }
        #endregion

        #region Constructor
        public MainViewModel(MainPage page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));

            IsBuyedProductVisible = false;
            Products = [];
            
            _page = page;

            //initialize commands
            AddItemCommand = new Command(AddProduct);
            ImportDataCommand = new Command(async () => await ImportData());
            EditItemCommand = new Command<ProductToBuy>(EditProduct);
            DeleteItemCommand = new Command<ProductToBuy>(DeleteProduct);
            FilterShowCommand = new Command(() => 
            {
                _filteredProducts = null;
                IsBuyedProductVisible = !IsBuyedProductVisible; 
            });

            //load saved productList and recipeList
            IsLoading = true;
            OnPropertyChanged(nameof(IsLoading));
            LoadData();
            //RecipeJsonTalker.LoadAsync();
            var recipe = RecipeJsonTalker.Instance.Recipes;

            CollectionChangedSetup();
        }
        #endregion

        #region Data Import
        public async Task ImportData()
        {
            if (IsLoading) return;
            IsLoading = true;
            OnPropertyChanged(nameof(IsLoading));
            _filteredProducts = null;

            GoogleApiTalker apiTalker = new();
            try
            {
                var unSortedProducts = await GoogleApiTalker.GetData();
                Products = new ObservableCollection<ProductToBuy>(unSortedProducts);
                await SaveData();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                var sorted = new List<ProductToBuy>();
                await _page.DisplayAlertAsync("Erreur", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
            OnPropertyChanged(nameof(ToBuyProducts));
        }
        #endregion

        #region Command Region
        public  ICommand AddItemCommand { get; }
        public ICommand FilterShowCommand { get; }
        public ICommand ImportDataCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public string FilterShowButtonText => IsBuyedProductVisible ? "Cachez" : "Révélez tout";
        #endregion

        #region Data Management
        public async void AddProduct()
        {
           // string? text = await _page.ItemNameAskerAsync("Entrer le nom", "Entrer le nom de l'objet à ajoutée");
            string? text = await _page.ItemNameOrPickAskerAsync("Entrer le nom");

            if (string.IsNullOrWhiteSpace(text)) return;
            if (Products.Any(p => p.Name.Equals(text, StringComparison.OrdinalIgnoreCase))) //Check for duplicates
            {
                await _page.DisplayAlertAsync("Doublon", "Ce produit est déjà dans la liste.", "OK");
                return;
            }
            ProductToBuy products = new ProductToBuy { Name =text, IsChecked = false};
            var recipeCheck = RecipeJsonTalker.RecipeCheckSingle(products);

            Products = new ObservableCollection<ProductToBuy>(Products.Concat(recipeCheck));    
            //Products.Add(new ProductToBuy { Name = text, IsChecked = false });
            _filteredProducts = null;

            sortProducts();
            OnPropertyChanged(nameof(ToBuyProducts));

            await SaveData();
        }

        private async void EditProduct(ProductToBuy Product)
        {
            string? text = await _page.ItemNameAskerAsync("Entrer le nom", "Entrer le nouveau nom",
                Product.Name);
            if (string.IsNullOrWhiteSpace(text) || Product == null) return;
            Product.Name = text;
            _filteredProducts = null;

            sortProducts();
            OnPropertyChanged(nameof(ToBuyProducts));

            await SaveData();
        }

        private async void DeleteProduct(ProductToBuy Product)
        {
            // Confirm deletion
            bool confirm = await  _page.DisplayAlertAsync("Confirmer", $"Supprimer {Product.Name} ?", "Oui", "Non");
            if (!confirm) return;

            Products.Remove(Product);
            _filteredProducts = null;
            OnPropertyChanged(nameof(ToBuyProducts));

            await SaveData();
            ;
        }

        private void sortProducts()
        {
            var sorted = Products.OrderBy(item => item.Name).ToList();
            Products = new ObservableCollection<ProductToBuy>(sorted);
            CollectionChangedSetup();
        }
        #endregion

        #region save/load data from json
        private async Task SaveData()
        {
            try {
            await SaveJsonTalker.SaveAsync(Products.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                var sorted = new List<ProductToBuy>();
                await _page.DisplayAlertAsync("Erreur", ex.Message, "OK");
            }
        }

        // Schedule/debounce la sauvegarde (utilisé pour changements fréquents comme IsChecked)
        private void ScheduleSave(int delayMs = 500)
        {
            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs, token);
                    if (!token.IsCancellationRequested)
                        await SaveData();
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }, token);
        }

        public async void LoadData()
        {
            var loadedProducts = await SaveJsonTalker.LoadAsync();
            Products = new ObservableCollection<ProductToBuy>(loadedProducts);

            _filteredProducts = null;

            sortProducts();
            CollectionChangedSetup();
            OnPropertyChanged(nameof(Products));
            OnPropertyChanged(nameof(ToBuyProducts));

            IsLoading = false;
        }

        #endregion

        #region Collection Changed Setup
        private void CollectionChangedSetup()
        {
            Products.CollectionChanged += Product_CollectionChanged;
            foreach (var item in Products)
                attachEventHandlers(item);
        }

        private void Product_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (ProductToBuy item in e.NewItems)
                    attachEventHandlers(item);
            if (e.OldItems != null)
                foreach (ProductToBuy item in e.OldItems)
                    detachEventHandlers(item);
            OnPropertyChanged(nameof(ToBuyProducts));
        }

        private void detachEventHandlers(ProductToBuy item)
        {
            item.PropertyChanged -= Item_PropertyChanged;
        }
        private void attachEventHandlers(ProductToBuy item)
        {
            item.PropertyChanged += Item_PropertyChanged;
        }
        #endregion

        #region Event Handlers
        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductToBuy.IsChecked))
            {
                _filteredProducts = null;
                OnPropertyChanged(nameof(ToBuyProducts));

                ScheduleSave();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    #endregion
}