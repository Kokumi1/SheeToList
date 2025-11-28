using System.Collections.ObjectModel;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using SheeToList.Model;

namespace SheeToList.Services
{
    class GoogleApiTalker
    {
        private static GoogleCredential credential;


        private static string GetCredentialPath()
        {
            var filename = "credential.json";

#if ANDROID
            var filepath = Path.Combine(FileSystem.AppDataDirectory, filename);
            if (!File.Exists(filepath))
            {
                using var stream = FileSystem.OpenAppPackageFileAsync(filename);
                using var fileStream = File.Create(filepath);
                Task.Run(async () => await (await stream).CopyToAsync(fileStream)).Wait();
            }
            var file = File.ReadAllText(filepath);
            return filepath;
#else
            return Path.Combine(AppContext.BaseDirectory, filename);
#endif
        }

        private static SheetsService Service
        {
            get
            {
                credential = GoogleCredential.FromFile(GetCredentialPath())
               // credential = GoogleCredential.FromFile("credential.json")
                .CreateScoped(SheetsService.Scope.Spreadsheets);
                return new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "SheeToList",
                });
            }
        }

        //Get data from the Google Sheet
        public static async Task<ObservableCollection<ProductToBuy>> GetData()
        {
            var spreadsheetId = "1ChvD0OKtSh_LGO_F7zq2225cklbK4br0WFkkcirF7RM";
            var range = "menu semaine !C4:D25";
            var request = Service.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await Task.Run(() => request.Execute());
            var values = response.Values;

            if (values == null || values.Count == 0)
            {
                Console.WriteLine("No data found.");
                return [];
            }
            else
            {
                return GoogleApiTalker.TuneData(values);
            }
        }

        //Tune data to split items separated by commas and remove empty entries
        private static ObservableCollection<ProductToBuy> TuneData(IList<IList<Object>> importedData)
        {
            ObservableCollection<ProductToBuy> listProducts = [.. importedData
                 .SelectMany(row => row
                 .OfType<string>()
                 .Where(itemName => !string.IsNullOrWhiteSpace(itemName))
                 .SelectMany(itemName =>
                    itemName.Contains(',')
                        ? itemName.Split(',')
                            .Select(subItem => subItem.Trim())
                            .Where(trimmed => !string.IsNullOrWhiteSpace(trimmed))
                        : [itemName]
                )
            )
            .Select(name => new ProductToBuy { Name = name, IsChecked = false })];

             var  listProductsSorted =RecipeJsonTalker.RecipeCheckInList(listProducts).OrderBy(item => item.Name).ToList();
              listProducts = new ObservableCollection<ProductToBuy>(listProductsSorted);

            return listProducts;
        }
    }
}
