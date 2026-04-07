using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Maui.Extensions;
using SheeToList.Model;
using SheeToList.Services;
using SheeToList.View;
using SheeToList.Utils;
using System.Threading.Tasks;

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
    
        public async Task<(string? name, string? category)> ItemNameOrPickAskerAsync(string title,/* IEnumerable<string> choices, */string initialValue = "")
        {
            var popup = new PickOrTypePopup(/*choices,*/ initialValue);
            // Si vous voulez afficher un titre: vous pouvez envelopper popup avec un layout contenant un Label
            // Affiche le popup et attend le résultat
            this.ShowPopup(popup);
            var result = await popup.WaitForResultAsync();
            Debug.WriteLine("result: " + result?.Name + ", category: " + result?.Category );
            return (result?.Name, result?.Category);
        }

        private async void Recipe_Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RecipeList());
        }
        private async void Category_Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CategoryListPage());
        }
        #endregion


    }



    // ViewModel for MainPage
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MainPage _page;
        private bool _isBuyedProductVisible;
        private ObservableCollection<ProductToBuy>? _filteredProducts;
        private bool _isLoading;
        // Debounce pour les sauvegardes
        private CancellationTokenSource? _saveCts;

        //----------------------
        //Properties
        #region Properties
        public bool IsBuyedProductVisible
        {
            get => _isBuyedProductVisible;
            set
            {
                _isBuyedProductVisible = value;
                Debug.WriteLine("IsBuyedProductVisible set to " + value);
                BuildGroups();
                //OnPropertyChanged();
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
                Debug.WriteLine("ToBuyProducts getter called");
                if (IsBuyedProductVisible) { 
                    Debug.WriteLine("Returning all products");
                    return Products;
                   //return _filteredProducts ??= new ObservableCollection<ProductToBuy>(Products.Where(item =>! item.IsChecked));
                }
                if (Products is null)
                    return [];
                Debug.WriteLine("Returning filtered products");
                return _filteredProducts ??= new ObservableCollection<ProductToBuy>(Products.Where(item => !item.IsChecked)); 
            }
        }
        public ObservableCollection<Grouping<string, ProductToBuy>> ToBuyProductsGrouped { get; private set; } = [];
        public ObservableCollection<ProductToBuy> Products { get; set; }
        #endregion





        //----------------------
        //Constructor
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
            ToggleCheckedCommand = new Command(ToggleChecked);
            DeleteAllItemsCommand = new Command(DeleteAllProducts);

            //load saved productList and recipeList
            IsLoading = true;
            OnPropertyChanged(nameof(IsLoading));
            LoadData();
            var _R = RecipeJsonTalker.Instance;
            var _C = CategoryJsonTalker.InitializeAsync();

            CollectionChangedSetup();
        }
        #endregion






        //----------------------
        //Data Import
        #region Data Import
        public async Task ImportData()
        {
            bool confirm = await _page.DisplayAlertAsync("Confirmer", $"Cela supprimeras toute la liste. Continuer?", "Oui", "Non");
            if (IsLoading || !confirm) return;
            IsLoading = true;
            OnPropertyChanged(nameof(IsLoading));
            _filteredProducts = null;

            try
            {
                var unSortedProducts = await GoogleApiTalker.GetData();
                Products = new ObservableCollection<ProductToBuy>(unSortedProducts);
                CategoryDefiner.AssignCategories(Products, overwriteExisting: true);
                BuildGroups();
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






        //----------------------
        //Commands
        #region Command Region
        public ICommand AddItemCommand { get; }
        public ICommand FilterShowCommand { get; }
        public ICommand ImportDataCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public string FilterShowButtonText => IsBuyedProductVisible ? "Cachez" : "Révélez tout";
        public ICommand DeleteAllItemsCommand { get; }
        public ICommand ToggleCheckedCommand { get; }
        #endregion






        //----------------------
        //Data manament
        #region Data Management
        public async void AddProduct()
        {
            var (name, category) = await _page.ItemNameOrPickAskerAsync("Entrer le nom");
            Debug.WriteLine($"AddProduct called with name: {name}, category: {category}");

            if (string.IsNullOrWhiteSpace(name)) return;
            if (Products.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) //Check for duplicates
            {
                await _page.DisplayAlertAsync("Doublon", "Ce produit est déjà dans la liste.", "OK");
                return;
            }
            ProductToBuy products = new() { Name = name, IsChecked = false };
            // Si une catégorie a été détectée via les suggestions, l'assigner directement
            if (!string.IsNullOrWhiteSpace(category) && 
                Enum.TryParse<Category>(category, ignoreCase: true, out var parsedCategory))
            {
                    products.Categorie = parsedCategory;
            }

            var recipeCheck = RecipeJsonTalker.RecipeCheckSingle(products);
            Debug.WriteLine($"Recipe check found {recipeCheck.Count} related products");
            Debug.WriteLine("Related products: " + string.Join(", ", recipeCheck.Select(p => p.Name+ " categorie "+ p.Categorie)));

            Products = new ObservableCollection<ProductToBuy>(Products.Concat(recipeCheck));
            _filteredProducts = null;

            CategoryDefiner.AssignCategories(Products, overwriteExisting: false);
            SortProducts();

            await SaveData();
        }

        private async void EditProduct(ProductToBuy Product)
        {
            string? text = await _page.ItemNameAskerAsync("Entrer le nom", "Entrer le nouveau nom",
                Product.Name);
            if (string.IsNullOrWhiteSpace(text) || Product == null) return;
            Product.Name = text;
            _filteredProducts = null;

            SortProducts();

            await SaveData();
        }

        private async void DeleteProduct(ProductToBuy Product)
        {
            // Confirm deletion
            bool confirm = await _page.DisplayAlertAsync("Confirmer", $"Supprimer {Product.Name} ?", "Oui", "Non");
            if (!confirm) return;

            Products.Remove(Product);
            _filteredProducts = null;
            BuildGroups();

            await SaveData();
            ;
        }

        private async void DeleteAllProducts()
        {
            // Confirm deletion
            bool confirm = await _page.DisplayAlertAsync("Confirmer", $"Supprimer tous les produits ?", "Oui", "Non");
            if (!confirm) return;
            Products.Clear();
            _filteredProducts = null;
            BuildGroups();
            await SaveData();
            ;
        }

        private async void SortProducts()
        {
             BuildGroups();
            var sorted = Products.OrderBy(item => item.Name).ToList();
            Products = new ObservableCollection<ProductToBuy>(sorted);
            CollectionChangedSetup();
        }

        private void BuildGroups()
        {
            var groups = ToBuyProducts
                .GroupBy(p => p.Categorie)
                .OrderBy(g => (int)g.Key)
                .Select(g => new Grouping<string, ProductToBuy>(
                    (g.Key == Category.Autre) ? "Autres" : g.Key.ToString(),
                    g.OrderBy(p => p.Name)));
            Debug.WriteLine("Building groups:");

            ToBuyProductsGrouped = new ObservableCollection<Grouping<string, ProductToBuy>>(groups);


            Debug.WriteLine($"Built {ToBuyProductsGrouped.Count} groups.");
            OnPropertyChanged(nameof(ToBuyProductsGrouped));
            OnPropertyChanged(nameof(ToBuyProducts));
        }

        // Command exécutée lorsque la checkbox change d'état
        private void ToggleChecked()
        {
            // _filteredProducts est réinitialisé pour forcer le rafraîchissement de la vue filtrée
            Debug.WriteLine("ToggleChecked called");
            _filteredProducts = null;
            OnPropertyChanged(nameof(ToBuyProducts));

            if (!IsBuyedProductVisible)
            try { BuildGroups(); } catch { /* safe-fail si BuildGroups non finalisé */ }

            // Debounce/sauvegarde
            ScheduleSave();
        }
        #endregion






        //----------------------
        //Save /Load Data
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
                catch (TaskCanceledException) {}
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

            SortProducts();
            CollectionChangedSetup();

            IsLoading = false;
        }
        #endregion





        //----------------------
        //Collection Changed Setup
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






        //----------------------
        // Event Handlers
        #region Event Handlers
        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductToBuy.IsChecked))
            {
                _filteredProducts = null;

                ScheduleSave();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    #endregion
}