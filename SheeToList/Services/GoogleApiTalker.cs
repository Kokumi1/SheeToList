using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using SheeToList.Model;

namespace SheeToList.Services
{
    class GoogleApiTalker
    {
        private static GoogleCredential credential;

        public static void Initialize()
        {
        }
        private static SheetsService Service
        {
            get
            {
                credential = GoogleCredential.FromFile("credential.json")
                .CreateScoped(SheetsService.Scope.Spreadsheets);
                return new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "SheeToList",
                });
            }
        }

        //Get data from the Google Sheet
        public static async Task<IList<ProductToBuy>> GetData()
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
        private static IList<ProductToBuy> TuneData(IList<IList<Object>> importedData)
        {
            return [.. importedData
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
        }
    }
}
