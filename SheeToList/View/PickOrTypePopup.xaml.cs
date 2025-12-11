using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using SheeToList.Model;
using SheeToList.Services;

namespace SheeToList.View;

public partial class PickOrTypePopup : Popup
{
    readonly TaskCompletionSource<string?> _tcs = new();
    public IReadOnlyList<string> Items { get; set; }
    public string TitleText { get; set; } = "Choisis ou taper le produit à ajouter";

    public PickOrTypePopup(/*IEnumerable<string> items,*/ string initialValue="")
	{
        InitializeComponent();
        MessageText.Text = TitleText;

        if (!string.IsNullOrWhiteSpace(initialValue))
            EntryName.Text = initialValue;

        PopulatePicker();
        UpdateTabVisual();
    }

    private async void PopulatePicker()
    {
        if(RecipeJsonTalker.LoadAsync() != null)
            await RecipeJsonTalker.LoadAsync();

        var recipes = RecipeJsonTalker.Instance.Recipes ?? new System.Collections.ObjectModel.ObservableCollection<Recipe>();
        Items = recipes.Select(r => r.Name).ToList();
        //Items = [.. items ?? []];
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            foreach (var it in recipes)
            {
                Debug.WriteLine("PickerList Add " + it.Name);
                PickerList.Items.Add(it.Name);
            }

            string initialValue = EntryName.Text;
            if (!string.IsNullOrWhiteSpace(initialValue))
            {
                if (PickerList.Items.Contains(initialValue))
                    PickerList.SelectedIndex = PickerList.Items.IndexOf(initialValue);
            }
        });
    }

    void PickerList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (PickerList.SelectedIndex >= 0 && PickerList.SelectedIndex < PickerList.Items.Count)
        {
            var chosen = PickerList.Items[PickerList.SelectedIndex];
            // titre dynamique : affiche le choix de l'utilisateur
            PickerList.Title = $"Sélectionné : {chosen}";
        }
        else
        {
            PickerList.Title = "Sélectionner...";
        }
    }

    void OnTabClicked(object? sender, EventArgs e)
    {
        if (sender == BtnType)
        {
            TypePanel.IsVisible = true;
            PickPanel.IsVisible = false;
        }
        else
        {
            TypePanel.IsVisible = false;
            PickPanel.IsVisible = true;
        }
        UpdateTabVisual();
    }

    void UpdateTabVisual()
    {
        BtnType.BackgroundColor = TypePanel.IsVisible ? Colors.LightGray : Colors.Transparent;
        BtnPick.BackgroundColor = PickPanel.IsVisible ? Colors.LightGray : Colors.Transparent;
    }

    async void OnAcceptClicked(object? sender, EventArgs e)
    {
        string? result = null;
        if (TypePanel.IsVisible)
        {
            result = EntryName.Text;
        }
        else if (PickPanel.IsVisible && PickerList.SelectedIndex >= 0)
        {
            result = PickerList.Items[PickerList.SelectedIndex];
        }
        Debug.WriteLine("PickOrTypePopup result: " + result);

        _tcs.TrySetResult(string.IsNullOrWhiteSpace(result) ? null : result); 
        CloseAsync();
    }
    void OnCancelClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        CloseAsync();
    }

    public Task<string?> WaitForResultAsync() => _tcs.Task;
}