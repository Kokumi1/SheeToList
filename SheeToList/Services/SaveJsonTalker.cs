using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SheeToList.Model;

namespace SheeToList.Services
{
    internal class SaveJsonTalker
    {
        // Mise en cache de l'instance JsonSerializerOptions pour éviter la recréation
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        private static string GetFilePath(string fileName)
        {
#if ANDROID
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return System.IO.Path.Combine(folderPath, fileName);
#else
            return Path.Combine(AppContext.BaseDirectory, fileName);
#endif
        }

        // save the json file
        public static async Task SaveAsync(List<ProductToBuy> products)
        {
            var filePath = GetFilePath("saveProducts.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            using FileStream createStream = File.Create(filePath);

            await JsonSerializer.SerializeAsync(createStream, products, _jsonOptions);
        }

        // load the json file
        public static async Task<List<ProductToBuy>> LoadAsync()
        {
            var filePath = GetFilePath("saveProducts.json");
            if (!File.Exists(filePath))
            {
                return new List<ProductToBuy>();
            }
            using FileStream openStream = File.OpenRead(filePath);
            var products = await JsonSerializer.DeserializeAsync<List<ProductToBuy>>(openStream, _jsonOptions);
            return products ?? new List<ProductToBuy>();
        }
    }
}
