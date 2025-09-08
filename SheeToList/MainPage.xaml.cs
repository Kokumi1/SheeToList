using SheeToList.Services;

namespace SheeToList
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        string dataString = "";

        public MainPage()
        {
            InitializeComponent();

            //initialize Google Sheets API
            GoogleApiTalker apiTalker = new();
           dataString =   apiTalker.GetDataString();

            LabelTextFromSheet.Text = dataString;
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}
