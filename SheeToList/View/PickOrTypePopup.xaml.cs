using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using SheeToList.Model;
using SheeToList.Services;
using SheeToList.Utils;

namespace SheeToList.View;

public partial class PickOrTypePopup : Popup, INotifyPropertyChanged
{
    readonly TaskCompletionSource<string?> _tcs = new();
    public IReadOnlyList<string> Items { get; set; }
    public IReadOnlyList<string> CategoriesProducts { get; set; }
    public string TitleText { get; set; } = "Choisis ou taper le produit à ajouter";
    private ObservableCollection<String> Suggestion = [];
    public ObservableCollection<string> Suggestions
    {
        get => Suggestion;
        set
        {
            Suggestion = value;
            OnPropertyChanged();
        }
    }
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            UpdateSuggestions(_searchText);
        }
    }

    //----------------------------------
    #region Setup
    public PickOrTypePopup(/*IEnumerable<string> items,*/ string initialValue="")
	{
        InitializeComponent();
        BindingContext = this;
        EntryName.TextChanged += EntryName_TextChanged;
        MessageText.Text = TitleText;

        if (!string.IsNullOrWhiteSpace(initialValue))
            EntryName.Text = initialValue;

        PopulatePicker();
        PopulateCategoriesProducts();
        UpdateTabVisual();
    }

    private async void PopulateCategoriesProducts()
    {
        var products = KeywordFlattener.KeywordFlattening();
        CategoriesProducts = products.Select(p =>$"{p.keyNormalized} ({p.cat})").ToList();
    }

    private async void PopulatePicker()
    {
        if(RecipeJsonTalker.LoadAsync() != null)
            await RecipeJsonTalker.LoadAsync();

        var recipes = RecipeJsonTalker.Instance.Recipes ?? [];
        Items = recipes.Select(r => r.Name).ToList();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            foreach (var it in recipes)
            {
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
    #endregion


    //----------------------------------
    #region Suggestions logic
    private void UpdateSuggestions(string searchText)
    {
        var source = CategoriesProducts ?? [];
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Suggestions= new ObservableCollection<string>(source.Take(50));
            try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = Suggestions.Count > 0); } catch {}
            return;
        }
        var normalized = searchText.Trim();
        var matches = source
            .Where(s => s.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .Concat(source.Where(s => !s.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)
                                      && s.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0))
            .Distinct()
            .Take(50);
        Suggestions = new ObservableCollection<string>(matches);
        try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = Suggestions.Count > 0); } catch {}
        SuggestionsList.ItemsSource = Suggestions;
    }

    void EntryName_TextChanged(object? sender, Microsoft.Maui.Controls.TextChangedEventArgs e)
    {
        Debug.WriteLine("Entry text changed: " + e.NewTextValue);
        // keep SearchText in sync with Entry text
        SearchText = e.NewTextValue ?? string.Empty;
    }

    void SuggestionsList_SelectionChanged(object? sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        Debug.WriteLine("Suggestion selected: " + (e.CurrentSelection != null && e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] : "null"));
        if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
        {
            var chosen = e.CurrentSelection[0] as string;
            if (!string.IsNullOrWhiteSpace(chosen))
            {
                EntryName.Text = chosen;
                SearchText = chosen;
            }
        }
        // hide suggestions after selection
        try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = false); } catch {}
    }

    void SuggestionTapped(object? sender, EventArgs e)
    {
        // sender is the TapGestureRecognizer; its BindingContext is the item string
        if (sender is Microsoft.Maui.Controls.TapGestureRecognizer tap)
        {
            var text = tap.BindingContext as string;
            if (!string.IsNullOrWhiteSpace(text))
            {
                EntryName.Text = text;
                SearchText = text;
            }
        }
        try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = false); } catch {}
    }
    #endregion


    //-----------------------------------
    #region picker logic
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
    #endregion


    //-------------------------------
    #region UI
    void UpdateTabVisual()
    {
        var selectedColor = Colors.BlueViolet; // couleur onglet actif
        var normalColor = Color.FromArgb("#202020"); // couleur onglet inactif

        BtnType.BackgroundColor = TypePanel.IsVisible ? selectedColor : normalColor;
        BtnPick.BackgroundColor = PickPanel.IsVisible ? selectedColor : normalColor;
    }

    async void OnAcceptClicked(object? sender, EventArgs e)
    {
        string? result = null;
        if (TypePanel.IsVisible)
        {
            //result = EntryName.Text;
                result = SearchText;
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
    #endregion


    public Task<string?> WaitForResultAsync() => _tcs.Task;
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}