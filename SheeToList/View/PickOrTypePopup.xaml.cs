using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using SheeToList.Model;
using SheeToList.Resources.String;
using SheeToList.Services;
using SheeToList.Utils;

namespace SheeToList.View;

/// <summary>
/// Représente un produit sélectionné avec son nom et sa catégorie
/// </summary>
public record ProductSelection(string Name, string? Category);


public partial class PickOrTypePopup : Popup, INotifyPropertyChanged
{
    public string PopupTitle => AppString.popup_type_select_placeholder;
    public string TitleText => AppString.popup_type_title;
    public string typeLabel => AppString.popup_addproduct_title;
    readonly TaskCompletionSource<ProductSelection?> _tcs = new();
    public IReadOnlyList<string> Items { get; set; }
    public IReadOnlyList<SuggestionItem> CategoriesProducts { get; set; }
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

    //----------------------------------
    #region Setup
    public PickOrTypePopup(string initialValue="")
	{
        InitializeComponent();
        BindingContext = this;
        EntryName.TextChanged += EntryName_TextChanged;
        MessageText.Text = TitleText;

        if (!string.IsNullOrWhiteSpace(initialValue))
            EntryName.Text = initialValue;

        PopulatePicker();
        PopulateCategoriesProducts();
        PopulateCategoryPicker();
        UpdateTabVisual();
    }

    private async void PopulateCategoriesProducts()
    {
        var products = KeywordFlattener.KeywordFlattening();
        CategoriesProducts = products
            .Select( p => new SuggestionItem(p.keyNormalized, p.cat.ToString()))
            .ToList();
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
    private void PopulateCategoryPicker()
    {
        var categories = Enum.GetNames<Category>()
            .OrderBy(c => c)
            .ToList();

        foreach (var category in categories)
        {
            PickerCategoryList.Items.Add(category);
        }
    }
    #endregion


    //----------------------------------
    #region Suggestions logic
    private void UpdateSuggestions(string searchText)
    {
        var source = CategoriesProducts ?? [];
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Suggestions= new ObservableCollection<SuggestionItem>(source.Take(50));
            try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = Suggestions.Count > 0); } catch {}
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
        if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
        {
            var chosen = e.CurrentSelection[0] as SuggestionItem;
            if (!string.IsNullOrWhiteSpace(chosen?.Name))
            {
                EntryName.Text = chosen.Name;
                SearchText = chosen.Name;
                if (!string.IsNullOrWhiteSpace(chosen?.Category))
                {
                    var categoryName = chosen.Category.Trim('[', ']');
                    int categoryIndex = PickerCategoryList.Items.IndexOf(categoryName);
                    if (categoryIndex >= 0)
                        PickerCategoryList.SelectedIndex = categoryIndex;
                }
            }
        }
        // hide suggestions after selection
        try { MainThread.BeginInvokeOnMainThread(() => SuggestionsList.IsVisible = false); } catch {}
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
            PickerList.Title = $"{AppString.popup_type_select_title} {chosen}";
        }
        else
        {
            PickerList.Title = AppString.popup_type_select_placeholder;
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
        ProductSelection? result = null;
        
        if (TypePanel.IsVisible)
        {
            var productName = SearchText;
            // Chercher la catégorie du produit dans les suggestions
            var suggestion = CategoriesProducts?.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));
            var category = suggestion?.Category.Trim('[', ']');
            category = PickerCategoryList.SelectedIndex >= 0 ? PickerCategoryList.Items[PickerCategoryList.SelectedIndex] : category?.Trim('[', ']');

            if (!string.IsNullOrWhiteSpace(productName))
                result = new ProductSelection(productName, category);
        }
        else if (PickPanel.IsVisible && PickerList.SelectedIndex >= 0)
        {
            var productName = PickerList.Items[PickerList.SelectedIndex];
            result = new ProductSelection(productName, null); // Pas de catégorie pour les recettes
        }
        
        Debug.WriteLine($"PickOrTypePopup result: Name={result?.Name}, Category={result?.Category}");

        _tcs.TrySetResult(result);
        _ = CloseAsync();
    }
    
    void OnCancelClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        CloseAsync();
    }

    void PickerCategoryList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (PickerCategoryList.SelectedIndex >= 0 && PickerCategoryList.SelectedIndex < PickerCategoryList.Items.Count)
        {
            var chosen = PickerCategoryList.Items[PickerCategoryList.SelectedIndex];
            // titre dynamique : affiche le choix de l'utilisateur
            PickerCategoryList.Title = $"{AppString.popup_type_select_title} {chosen}";
        }
        else
        {
            PickerCategoryList.Title = AppString.popup_type_select_placeholder;
        }
    }
    #endregion


    public Task<ProductSelection?> WaitForResultAsync() => _tcs.Task;
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

//---------------------------------
// SuggestionItem class
#region SuggestionItem class
public class SuggestionItem(string name, string category)
{
    public string Name { get; set; } = name;
    public string Category { get; set; } = $"[{category}]";
}
#endregion

