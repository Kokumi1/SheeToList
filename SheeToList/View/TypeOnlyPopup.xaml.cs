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

public partial class TypeOnlyPopup : Popup, INotifyPropertyChanged
{
    readonly TaskCompletionSource<ProductSelection?> _tcs = new();
    public IReadOnlyList<string> Items { get; set; }
    public IReadOnlyList<SuggestionItem> CategoriesProducts { get; set; }
    public string TitleText { get; set; } = "Choisis ou taper le produit à ajouter";
    private ObservableCollection<SuggestionItem> Suggestion = [];
    public ObservableCollection<SuggestionItem> Suggestions
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


    public TypeOnlyPopup(string initialValue = "")
	{
        InitializeComponent();
        BindingContext = this;
        EntryName.TextChanged += EntryName_TextChanged;
        MessageText.Text = TitleText;

        if (!string.IsNullOrWhiteSpace(initialValue))
            EntryName.Text = initialValue;

        PopulateCategoriesProducts();
    }

    private async void PopulateCategoriesProducts()
    {
        var products = KeywordFlattener.KeywordFlattening();
        CategoriesProducts = products
            .Select(p => new SuggestionItem(p.keyNormalized, p.cat.ToString()))
            .ToList();
    }

    //----------------------------------
    #region Suggestions logic
    private void UpdateSuggestions(string searchText)
    {
        var source = CategoriesProducts ?? [];
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Suggestions = new ObservableCollection<SuggestionItem>(source.Take(50));
            try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = Suggestions.Count > 0); } catch { }
            return;
        }
        var normalized = searchText.Trim();
        var matches = source
            .Where(s => s.Name.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .Concat(source.Where(s => !s.Name.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)
                                      && s.Name.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0))
            .Distinct()
            .Take(50);
        Suggestions = new ObservableCollection<SuggestionItem>(matches);
        try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = Suggestions.Count > 0); } catch { }
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
        if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
        {
            var chosen = e.CurrentSelection[0] as SuggestionItem;
            if (!string.IsNullOrWhiteSpace(chosen?.Name))
            {
                EntryName.Text = chosen.Name;
                SearchText = chosen.Name;
            }
        }
        // hide suggestions after selection
        try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = false); } catch { }
    }

    void SuggestionTapped(object? sender, EventArgs e)
    {
        // sender is the HorizontalStackLayout; its BindingContext is the SuggestionItem
        if (sender is Microsoft.Maui.Controls.BindableObject bo && bo.BindingContext is SuggestionItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                EntryName.Text = item.Name;
                SearchText = item.Name;
            }
        }
        try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = false); } catch { }
    }
    #endregion

    //-------------------------------
    #region UI
    async void OnAcceptClicked(object? sender, EventArgs e)
    {
        ProductSelection? result = null;

            var productName = SearchText;
            // Chercher la catégorie du produit dans les suggestions
            var suggestion = CategoriesProducts?.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));
            var category = suggestion?.Category;

            if (!string.IsNullOrWhiteSpace(productName))
                result = new ProductSelection(productName, category);

        Debug.WriteLine($"PickOrTypePopup result: Name={result?.Name}, Category={result?.Category}");

        _tcs.TrySetResult(result);
        _ = CloseAsync();
    }

    void OnCancelClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        CloseAsync();
    }
    #endregion

    public Task<ProductSelection?> WaitForResultAsync() => _tcs.Task;
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    //---------------------------------
    // SuggestionItem class
    #region SuggestionItem class
    public class SuggestionItem(string name, string category)
    {
        public string Name { get; set; } = name;
        public string Category { get; set; } = $"[{category}]";
    }
    #endregion
}