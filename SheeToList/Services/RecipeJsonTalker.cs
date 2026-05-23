using System.Text.Json;
using SheeToList.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;
using SheeToList.Utils;

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
            WriteIndented = true,
            Converters = { new ProductToBuyConverter() }
        };

        private RecipeJsonTalker()
        {
            Debug.WriteLine("RecipeJsonTalker initialized.");
            var task = LoadAsync();
           //task.Start();
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
        public static async Task LoadAsync()
        {
            Debug.WriteLine("Loading recipes from JSON.");
            var filePath = GetFilePath("saveRecipes.json");
            Debug.WriteLine($"Looking for file at: {filePath}");
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
                    Ingredients =
                    [
                        new() { Name = "Steak haché", IsChecked = false, Quantity = 100, QuantityUnit = QuantityUnit.g },
                        new () { Name = "Pain hamburger", IsChecked = false, Quantity = 1, QuantityUnit = QuantityUnit.unit },
                        new () { Name = "fromage", IsChecked = false, Quantity = 1, QuantityUnit = QuantityUnit.unit }
                    ]
                });
                SaveAsync(recipes.ToList()).Wait();
            }

            Debug.WriteLine($"recipe size: {recipes.Count}");
            RecipeJsonTalker.Instance.Recipes = recipes ?? [];
        }


        public static ObservableCollection<ProductToBuy> RecipeCheckSingle(ProductToBuy productToBuy)
        {
            var recipes = RecipeJsonTalker.Instance.Recipes;
            var recipeDictionary = recipes.ToDictionary(r => r.Name.ToLower(), r => r.Ingredients);

            if (recipeDictionary.Keys.FirstOrDefault(key => productToBuy.Name.ToLower().Contains(key, StringComparison.OrdinalIgnoreCase)) 
                is string matchedKey)
            {
                return new ObservableCollection<ProductToBuy>(
                    recipeDictionary[matchedKey].Select(ingredient => 
                        new ProductToBuy 
                        { 
                            Name = $"{ingredient.Name} ({productToBuy.Name})", 
                            IsChecked = false,
                            Categorie = ingredient.Categorie,
                            Quantity = ingredient.Quantity,
                            QuantityUnit = ingredient.QuantityUnit
                        })
                );
            }
            return [productToBuy];
        }

        //Check for saved recipes in the products list and replace them with their ingredients
        public static ObservableCollection<ProductToBuy> RecipeCheckInList(ObservableCollection<ProductToBuy> importedList)
        {
            if (importedList == null) return [];
            var recipes = RecipeJsonTalker.Instance.Recipes;
            var recipeDictionary = recipes.ToDictionary(r => r.Name.ToLower(), r => r.Ingredients);


            foreach (ProductToBuy product in importedList.ToList())
            {
                var productNameLower = product.Name.ToLower();
                // Check if any recipe name is contained in the product name
                if (recipeDictionary.Keys.FirstOrDefault(key => productNameLower.Contains(key, StringComparison.OrdinalIgnoreCase)) is string matchedKey)
                {
                    productNameLower = matchedKey;

                    // If a match is found, replace the product with its ingredients
                    if(recipeDictionary[productNameLower] == null || recipeDictionary[productNameLower].Count == 0) continue;
                    else { 
                        foreach (var ingredient in recipeDictionary[productNameLower])
                        {
                            //if the ingredient already exists in the list, add the quantity, otherwise add it to the list
                            if (importedList.Any(p => p.Name.Equals($"{ingredient.Name} ({product.Name})", StringComparison.OrdinalIgnoreCase)))
                            {
                                var existingProduct = importedList.First(p => p.Name.Equals($"{ingredient.Name} ({product.Name})", StringComparison.OrdinalIgnoreCase));
                                existingProduct.Quantity += ingredient.Quantity * product.Quantity;
                            }
                            else
                                importedList.Add(new ProductToBuy
                                {
                                    Name = $"{ingredient.Name} ({product.Name})",
                                    IsChecked = false,
                                    Categorie = ingredient.Categorie,
                                    Quantity = ingredient.Quantity * product.Quantity,
                                    QuantityUnit = ingredient.QuantityUnit
                                });
                        }
                        importedList.Remove(product);
                    }
                }
            }
            return importedList;
        }
    }
}
