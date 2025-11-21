using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using SheeToList.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
            var filePath = GetFilePath("saveRecipes.json");
            if (!File.Exists(filePath))
            {
                // return new ObservableCollection<Recipe>();
                RecipeJsonTalker.Instance.Recipes = [];
            }
            using FileStream openStream = File.OpenRead(filePath);
            var recipes = await JsonSerializer.DeserializeAsync<ObservableCollection<Recipe>>(openStream, _jsonOptions);
            Debug.WriteLine($"Loaded {recipes?.Count ?? 0} recipes.");
            RecipeJsonTalker.Instance.Recipes = recipes ?? new ObservableCollection<Recipe>();
            //return recipes ?? new ObservableCollection<Recipe>();
        }
    }
}
