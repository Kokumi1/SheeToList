using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using SheeToList.Model;
using System.Collections.ObjectModel;

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
            Recipes = [];
            Recipes.Add(new Recipe { Name = "Tarte aux pommes", Ingredients = ["pomme"] });
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

            await JsonSerializer.SerializeAsync(createStream, recipes, _jsonOptions);
        }

        // load the json file
        public static async Task<List<Recipe>> LoadAsync()
        {
            var filePath = GetFilePath("saveRecipes.json");
            if (!File.Exists(filePath))
            {
                return new List<Recipe>();
            }
            using FileStream openStream = File.OpenRead(filePath);
            var recipes = await JsonSerializer.DeserializeAsync<List<Recipe>>(openStream, _jsonOptions);
            return recipes ?? new List<Recipe>();
        }
    }
}
