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
                             ?? new ObservableCollection<CategoryDefinition>();
            }
            else
            {
                // valeurs par défaut
                categories.Add(new CategoryDefinition { Name = "Fruit", Keywords = new ObservableCollection<string> { "pomme", "banane", "orange" } });
                categories.Add(new CategoryDefinition { Name = "Viande", Keywords = new ObservableCollection<string> { "poulet", "boeuf" } });
                await SaveAsync(categories.ToList());
            }

            CategoryJsonTalker.Instance.Categories = categories;
        }
    }
}