using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using SheeToList.Model;

namespace SheeToList.Services
{
    internal class CategoryJsonTalker
    {
        //TODO: vérifier api talker et add products
        private const int CURRENT_VERSION = 2;
        private const string SAVE_FILENAME = "saveCategories.json";
        private const string DEFAULT_FILENAME = "DefaultCategories.json";

        public ObservableCollection<CategoryDefinition> Categories { get; set; } = new();
        private static readonly Lazy<CategoryJsonTalker> _instance = new(() => new CategoryJsonTalker());
        public static CategoryJsonTalker Instance => _instance.Value;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private CategoryJsonTalker()
        {
            Debug.WriteLine("CategoryJsonTalker initialized.");
            var _ = LoadAsync();
        }

        public static Task InitializeAsync() => LoadAsync();

        private static string GetFilePath(string fileName)
        {
#if ANDROID
            return System.IO.Path.Combine(FileSystem.AppDataDirectory, fileName);
#else
            return Path.Combine(AppContext.BaseDirectory, fileName);
#endif
        }

        public static async Task SaveAsync(ObservableCollection<CategoryDefinition> categories)
        {
            MainThread.BeginInvokeOnMainThread(() => Instance.Categories = categories);

            var filePath = GetFilePath(SAVE_FILENAME);
            if (File.Exists(filePath))
                File.Delete(filePath);

            using FileStream createStream = File.Create(filePath);
            Debug.WriteLine($"Saving {categories.Count} categories.");

            var wrapper = new CategoriesFileWrapper
            {
                Version = CURRENT_VERSION,
                Categories = categories.ToList()
            };
            await JsonSerializer.SerializeAsync(createStream, wrapper, _jsonOptions);
        }

        public static async Task LoadAsync()
        {
            var filePath = GetFilePath(SAVE_FILENAME);
            var categories = new ObservableCollection<CategoryDefinition>();

            if (File.Exists(filePath))
            {
                try
                {
                    using FileStream openStream = File.OpenRead(filePath);
                    var wrapper = await JsonSerializer.DeserializeAsync<CategoriesFileWrapper>(openStream, _jsonOptions);

                    int detectedVersion = 1; // Par défaut, supposer version 1 si non explicite

                    if (wrapper != null && wrapper.Categories != null && wrapper.Categories.Count > 0)
                    {
                        categories = new ObservableCollection<CategoryDefinition>(wrapper.Categories);
                        detectedVersion = wrapper.Version;
                        Debug.WriteLine($"Loaded categories. Version detected: {detectedVersion}");

                        // Vérifier la version et migrer si nécessaire
                        if (detectedVersion < CURRENT_VERSION)
                        {
                            Debug.WriteLine($"Migrating from version {detectedVersion} to {CURRENT_VERSION}");
                            categories = await MigrateAsync(categories, detectedVersion);
                            await SaveAsync(categories);
                        }
                    }
                    else
                    {
                        // Le wrapper est null ou vide → essayer de charger comme une liste simple (ancien format)
                        Debug.WriteLine("Wrapper is null or empty. Trying to load as legacy format (direct list)...");
                        openStream.Seek(0, SeekOrigin.Begin);
                        var legacyCategories = await JsonSerializer.DeserializeAsync<List<CategoryDefinition>>(openStream, _jsonOptions);

                        if (legacyCategories != null && legacyCategories.Count > 0)
                        {
                            categories = new ObservableCollection<CategoryDefinition>(legacyCategories);
                            Debug.WriteLine($"Loaded {categories.Count} categories from legacy format (v1).");
                            detectedVersion = 1; // Format legacy = version 1

                            // Migrer immédiatement
                            categories = await MigrateAsync(categories, 1);
                            await SaveAsync(categories);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading categories: {ex.Message}. Using defaults.");
                    categories = await LoadDefaultCategoriesAsync();
                    await SaveAsync(categories);
                }
            }
            else
            {
                // Première exécution → charger depuis DefaultCategories.json embarqué
                categories = await LoadDefaultCategoriesAsync();
                Debug.WriteLine("First run - loaded default categories from embedded resource.");
                await SaveAsync(categories);
            }

            Debug.WriteLine($"Final category count after load: {categories.Count}");
            MainThread.BeginInvokeOnMainThread(() => CategoryJsonTalker.Instance.Categories = categories);
        }

        private static async Task<ObservableCollection<CategoryDefinition>> LoadDefaultCategoriesAsync()
        {
            try
            {
                // Charger depuis le fichier JSON embarqué dans Resources/Raw/
                using var stream = await FileSystem.OpenAppPackageFileAsync(DEFAULT_FILENAME);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var wrapper = JsonSerializer.Deserialize<CategoriesFileWrapper>(json, _jsonOptions);
                return new ObservableCollection<CategoryDefinition>(wrapper?.Categories ?? []);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading default categories from embedded resource: {ex.Message}");
                // Fallback : utiliser les catégories codées en dur
                return AddDefaultCategory();
            }
        }

        private static async Task<ObservableCollection<CategoryDefinition>> MigrateAsync(
            ObservableCollection<CategoryDefinition> existingCategories,
            int fromVersion)
        {
            Debug.WriteLine($"Starting migration from version {fromVersion}...");

            if (fromVersion == 1 || fromVersion == 0)
            {
                var defaultCategories = await LoadDefaultCategoriesAsync();
                return MigrateFromV1ToV2(existingCategories, defaultCategories);
            }

            return existingCategories;
        }

        private static ObservableCollection<CategoryDefinition> MigrateFromV1ToV2(
            ObservableCollection<CategoryDefinition> oldCategories,
            ObservableCollection<CategoryDefinition> newDefaultCategories)
        {
            var migratedCategories = new ObservableCollection<CategoryDefinition>();

            // 1. Conserver les catégories existantes de l'utilisateur (possiblement modifiées)
            foreach (var oldCategory in oldCategories)
            {
                migratedCategories.Add(oldCategory);
                Debug.WriteLine($"Preserved user category: {oldCategory.Name}");
            }

            // 2. Ajouter les nouvelles catégories qui n'existaient pas
            foreach (var newCategory in newDefaultCategories)
            {
                var existsInOld = migratedCategories.Any(c =>
                    c.Name.Equals(newCategory.Name, StringComparison.OrdinalIgnoreCase));

                if (!existsInOld)
                {
                    migratedCategories.Add(newCategory);
                    Debug.WriteLine($"Added new category during migration: {newCategory.Name}");
                }
            }

            Debug.WriteLine($"Migration complete: {migratedCategories.Count} categories after migration");
            return migratedCategories;
        }

        public static async Task ResetToDefaultAsync()
        {
            var defaultCategories = await LoadDefaultCategoriesAsync();
            await SaveAsync(defaultCategories);
            Debug.WriteLine("Categories reset to defaults.");
        }

        public static ObservableCollection<CategoryDefinition> AddDefaultCategory()
        {
            var DefaultCategory = new ObservableCollection<CategoryDefinition>
            {
                new()
                {
                    Name = "Conserve",
                    Keywords = ["conserve", "bocal", "boite", "terrine", "rillette", "cornichon", "caperis"]
                },
                new(){
                    Name = "Fruit",
                    Keywords = ["pomme", "banane", "orange", "fraise", "raisin", "kiwi", "citron", "ananas", "poire", "clementine"]
                },
                new(){
                    Name = "Viande",
                    Keywords = ["poulet", "boeuf", "porc", "agneau", "steak", "saucisse", "dinde", "roti", "chippolata", "charcuterie", "jambon"]
                },
                new() {
                    Name = "Surgelé",
                    Keywords = ["surgeles", "congele", "glace", "pdt sautees", "pdt rissolees", "frite"]
                },
                new()
                {
                    Name = "Épicerie",
                    Keywords = ["épicerie", "epicerie", "conserve","plats préparés", "conserves", "sauce", "sauces", "pate",
                "cereale", "patate", "pdt", "legumineuse", "flageolets", "pois", "tarte", "flamekuche","pizza", "huile", "vinaigre",
                "vin blanc", "moutarde", "mayonnaise", "ketchup", "soupe", "bouillon","carbonara", "epinards creme", "lentille", "semoule"]
                },
                new()
                {
                    Name = "Legume",
                    Keywords = [ "tomate", "carotte", "salade", "laitue", "poivron", "oignon", "courgette", "concombre", "aubergine", "haricots",
                "leg provencal", "poireau", "endive", "choux", "chou ", "betterave"]
                },
                new()
                {
                    Name = "Poissons",
                    Keywords = ["poisson", "saumon", "thon", "crevette", "merlan", "truite", "morue", "cabillaud"]
                },
                new()
                {
                    Name = "ProduitLaitier",
                    Keywords = [ "lait", "yaourt", "fromage", "beurre", "crème", "yaourt", "maroilles", "caprice des dieux", "port salut",
                "chausse aux moines", "buche de chevre", "pave d'affinois","chevre nature", "rocamadour", "petit basque", "etorki", "st albray", "boursin",
                "mimolette", "cousteron", "bleu"]
                },
                new()
                {
                    Name = "Boulangerie",
                    Keywords = ["pain", "baguette", "croissant", "brioche", "pain de mie"]
                },
                new()
                {
                    Name = "Boisson",
                    Keywords = [  "eau", "jus", "soda", "coca", "vin", "biere", "café" ]
                },
                new()
                {
                    Name = "ProduitMenager",
                    Keywords = ["lessive", "savon", "shampoing", "détergent", "produit vaisselle", "eponges", "papier toilette"]
                }
            };

            return DefaultCategory;
        }
    }

    /// <summary>
    /// Wrapper pour le JSON avec versioning et support de migration
    /// </summary>
    internal class CategoriesFileWrapper
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("categories")]
        public List<CategoryDefinition> Categories { get; set; } = new();
    }
}
