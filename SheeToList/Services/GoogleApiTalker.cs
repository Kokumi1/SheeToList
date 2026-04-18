using System.Collections.ObjectModel;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using SheeToList.Model;
using SheeToList.Resources.String;

namespace SheeToList.Services
{
    class GoogleApiTalker
    {
        private static GoogleCredential credential;

        //Get data from the Google Sheet
        public static async Task<ObservableCollection<ProductToBuy>> GetData()
        {
            //You have the apiless version of the application.
            // So we return an empty list of products to buy instead.

            return await Task.Run(static () =>
            {
               return TuneData(new List<IList<Object>>());
            });
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
