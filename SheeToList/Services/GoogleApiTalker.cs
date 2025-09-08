using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

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

        public string GetDataString()
        {
            var spreadsheetId = "1ChvD0OKtSh_LGO_F7zq2225cklbK4br0WFkkcirF7RM";
            var range = "menu semaine !C4:D25";
            var request = Service.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;
            var returnString = "";

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    returnString += string.Join(" / ", row) + "\n";
                }
            }
            else
            {
                returnString = "No data found.";
            }
            return returnString;
        }
    }
}
