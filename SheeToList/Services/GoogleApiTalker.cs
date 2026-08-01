using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using SheeToList.Model;
using SheeToList.Resources.String;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

namespace SheeToList.Services
{
    class GoogleApiTalker
    {
        private static GoogleCredential credential;

        // Retrieve the Google service account JSON from Azure Key Vault.
        // The Key Vault URI must be provided via environment variable "KEY_VAULT_URI" and the secret
        // name is expected to be "GoogleServiceAccount".
        private static string GetCredentialJsonFromKeyVault()
        {
            var kvUri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
            if (string.IsNullOrEmpty(kvUri))
            {
                throw new InvalidOperationException("Environment variable KEY_VAULT_URI is not set.");
            }

            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
            var secret = client.GetSecret("GoogleServiceAccount");
            return secret.Value.Value;
        }

        private static SheetsService Service
        {
            get
            {
                var credentialJson = GetCredentialJsonFromKeyVault();
                credential = GoogleCredential.FromJson(credentialJson).CreateScoped(SheetsService.Scope.Spreadsheets);
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
            var spreadsheetId = Redacted.Sheet_ID;
            var range = $"{Redacted.Sheet_name} {Redacted.Sheet_select}";
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
            .Select(name => CreateProductFromData(name))];

             var  listProductsSorted =RecipeJsonTalker.RecipeCheckInList(listProducts).OrderBy(item => item.Name).ToList();
              listProducts = new ObservableCollection<ProductToBuy>(listProductsSorted);

            return listProducts;
        }

        public static ProductToBuy CreateProductFromData(string data)
        {
            // pomme :1:unit
            string?[] subData = new string[3];
            var splitData = data.Split(':');
            for (int i = 0; i < Math.Min(splitData.Length, 3); i++)
            {
                subData[i] = splitData[i];
            }

            int quantity = int.TryParse(subData[1] ?? string.Empty, out int  quantityValue) ? quantityValue : 1;
            QuantityUnit unit = Enum.TryParse(subData[2] ?? string.Empty, out QuantityUnit dataUnit) ? dataUnit : QuantityUnit.unit;
            ProductToBuy product = new() { Name = subData[0] ?? string.Empty, IsChecked = false, Quantity = quantity, QuantityUnit = unit};

            Debug.WriteLine($"product parsed: {product.Name} {product.Data}");
            return product;
        }
    }
}
