using System.Text.Json;
using SheeToList.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SheeToList.Services
{
    internal class RecipeJsonTalker
    {
        public ObservableCollection<Recipe> Recipes { get; set; }
        private static readonly Lazy<RecipeJsonTalker> _instance = new(() => new RecipeJsonTalker());
        public static RecipeJsonTalker Instance => _instance.Value;
        // Mise en cache de l'instance JsonSerializerOptions pour éviter la recréation
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        RecipeJsonTalker()
        {
            Debug.WriteLine("RecipeJsonTalker initialized.");
            LoadAsync();
        }

        private static string GetFilePath(string fileName)
        {
#if ANDROID
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return System.IO.Path.Combine(FileSystem.AppDataDirectory, fileName);
#else
            return Path.Combine(AppContext.BaseDirectory, fileName);
#endif
        }

        // save the json file
        public static async Task SaveAsync(List<Recipe> recipes)
        {
            var filePath = GetFilePath("saveRecipes.json");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            using FileStream createStream = File.Create(filePath);

            Debug.WriteLine($"Saving {recipes.Count} recipes.");
            await JsonSerializer.SerializeAsync(createStream, recipes, _jsonOptions);
        }

        // load the json file
        public static async void LoadAsync()
        {
            Debug.WriteLine("Loading recipes from JSON.");
            var filePath = GetFilePath("saveRecipes.json");
            var recipes = new ObservableCollection<Recipe>();

            if (File.Exists(filePath))
            {
                Debug.WriteLine("Saved recipes found, loading...");
                using FileStream openStream = File.OpenRead(filePath);
                recipes = await JsonSerializer.DeserializeAsync<ObservableCollection<Recipe>>(openStream, _jsonOptions);
            }
            else
            {
                   Debug.WriteLine("No saved recipes found");
                    recipes.Add(new Recipe
                {
                    Name = "Hamburger",
                    Ingredients = ["Steak haché", "Pain hamburger","fromage"]
                });
                SaveAsync(recipes.ToList()).Wait();
            }

            Debug.WriteLine($"recipe size: {recipes.Count}");
            RecipeJsonTalker.Instance.Recipes = recipes ?? [];
        }

        public static ObservableCollection<ProductToBuy> RecipeCheckSingle(ProductToBuy productToBuy)
        {
            var recipes = RecipeJsonTalker.Instance.Recipes;
            var recipeDictionnary = recipes.ToDictionary(r => r.Name.ToLower(), r => r.Ingredients);

            if (recipeDictionnary.Keys.FirstOrDefault(key => productToBuy.Name.ToLower().Contains(key, StringComparison.OrdinalIgnoreCase)) 
                is string matchedKey)
            {
                return new ObservableCollection<ProductToBuy>(
                    recipeDictionnary[matchedKey].Select(ingredient => new ProductToBuy { Name = $"{ingredient} ({productToBuy.Name})", IsChecked = false })
                );
            }
            return [productToBuy];
        }

        //Check for saved recipes in the products list and replace them with their ingredients
        public static ObservableCollection<ProductToBuy> RecipeCheckInList(ObservableCollection<ProductToBuy> importedList)
        {
            if (importedList == null) return [];
            var recipes = RecipeJsonTalker.Instance.Recipes;
            var recipeDictionnary = recipes.ToDictionary(r => r.Name.ToLower(), r => r.Ingredients);


            foreach (ProductToBuy product in importedList.ToList())
            {
                var productNameLower = product.Name.ToLower();
                // Check if any recipe name is contained in the product name
                if (recipeDictionnary.Keys.FirstOrDefault(key => productNameLower.Contains(key, StringComparison.OrdinalIgnoreCase)) is string matchedKey)
                {
                    productNameLower = matchedKey;

                    // If a match is found, replace the product with its ingredients
                    if(recipeDictionnary[productNameLower] == null || recipeDictionnary[productNameLower].Count == 0) continue;
                    else { 
                        foreach (var ingredient in recipeDictionnary[productNameLower])
                            {
                                importedList.Add(new ProductToBuy { Name = $"{ingredient} ({product.Name})", IsChecked = false });
                            }
                        importedList.Remove(product);
                    }
                }
            }

            return importedList;
        }
    }
}
