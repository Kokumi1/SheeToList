using System.Text.Json;
using SheeToList.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SheeToList.Services
{
    internal class CategoryJsonTalker
    {
        public ObservableCollection<CategoryDefinition> Categories { get; set; } = new();
        private static readonly Lazy<CategoryJsonTalker> _instance = new(() => new CategoryJsonTalker());
        public static CategoryJsonTalker Instance => _instance.Value;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        private CategoryJsonTalker()
        {
            Debug.WriteLine("CategoryJsonTalker initialized.");
            var _ = LoadAsync();
        }

        private static string GetFilePath(string fileName)
        {
#if ANDROID
            return System.IO.Path.Combine(FileSystem.AppDataDirectory, fileName);
#else
            return Path.Combine(AppContext.BaseDirectory, fileName);
#endif
        }

        public static async Task SaveAsync(List<CategoryDefinition> categories)
        {
            var filePath = GetFilePath("saveCategories.json");
            if (File.Exists(filePath)) File.Delete(filePath);
            using FileStream createStream = File.Create(filePath);
            Debug.WriteLine($"Saving {categories.Count} categories.");
            await JsonSerializer.SerializeAsync(createStream, categories, _jsonOptions);
        }

        public static async Task LoadAsync()
        {
            var filePath = GetFilePath("saveCategories.json");
            var categories = new ObservableCollection<CategoryDefinition>();

            if (File.Exists(filePath))
            {
                using FileStream openStream = File.OpenRead(filePath);
                categories = await JsonSerializer.DeserializeAsync<ObservableCollection<CategoryDefinition>>(openStream, _jsonOptions)
                             ?? [];
            }
            else
            {
                // valeurs par défaut 
                categories = AddDefaultCategory();
                await SaveAsync([.. categories]);
            }

            CategoryJsonTalker.Instance.Categories = categories;
        }

        public static ObservableCollection<CategoryDefinition> AddDefaultCategory()
        {
            var DefaultCategory = new ObservableCollection<CategoryDefinition>
            {
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
                    Name = "Légumes",
                    Keywords = [ "tomate", "carotte", "salade", "laitue", "poivron", "oignon", "courgette", "concombre", "aubergine", "haricots",
                "leg provencal", "poireau", "endive", "choux", "chou "]
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
}