using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;
using SheeToList.Services;

namespace SheeToList.View;

public partial class CategoryDetailPage : ContentPage
{
    public CategoryDetailPage(CategoryDefinition category)
    {
        InitializeComponent();
        BindingContext = new CategoryDetailViewModel(category, this);
    }
}

public class CategoryDetailViewModel : INotifyPropertyChanged
{
    private readonly Page _page;
    public CategoryDefinition Category { get; private set; }
    private string? _selectedKeyword;

    public string? SelectedKeyword
    {
        get => _selectedKeyword;
        set { _selectedKeyword = value; OnPropertyChanged(); }
    }

    public ICommand AddKeywordCommand { get; }
    public ICommand EditKeywordCommand { get; }
    public ICommand DeleteKeywordCommand { get; }

    public CategoryDetailViewModel(CategoryDefinition category, Page page)
    {
        Category = category;
        _page = page;

        AddKeywordCommand = new Command(AddKeyword);
        EditKeywordCommand = new Command<string>(EditKeyword);
        DeleteKeywordCommand = new Command(DeleteKeyword, () => SelectedKeyword != null);
    }

    private async void AddKeyword()
    {
        string? text = await _page.DisplayPromptAsync("Ajouter mot-clé", "Mot-clé :");
        if (string.IsNullOrWhiteSpace(text)) return;
        if (Category.Keywords.Any(k => k.Equals(text, StringComparison.OrdinalIgnoreCase)))
        {
            await _page.DisplayAlertAsync("Doublon", "Ce mot-clé existe déjà.", "OK");
            return;
        }
        Category.Keywords.Add(text.Trim());
        await SaveCategoryAsync();
    }

    private async void EditKeyword(string keyword)
    {
        if (keyword == null) return;
        string? newVal = await _page.DisplayPromptAsync("Éditer mot-clé", "Nouveau mot-clé :", initialValue: keyword);
        if (string.IsNullOrWhiteSpace(newVal)) return;
        if (Category.Keywords.Any(k => k.Equals(newVal, StringComparison.OrdinalIgnoreCase))) { await _page.DisplayAlertAsync("Doublon", "Ce mot-clé existe déjà.", "OK"); return; }
        int idx = Category.Keywords.IndexOf(keyword);
        if (idx >= 0) 
        {
            Category.Keywords[idx] = newVal.Trim();
            await SaveCategoryAsync();
        }
    }

    private async void DeleteKeyword()
    {
        if (SelectedKeyword == null) return;
        bool confirm = await _page.DisplayAlertAsync("Confirmer", $"Supprimer '{SelectedKeyword}' ?", "Oui", "Non");
        if (!confirm) return;
        Category.Keywords.Remove(SelectedKeyword);
        SelectedKeyword = null;
        await SaveCategoryAsync();
    }

    private async Task SaveCategoryAsync()
    {
        try
        {
            // ensure category exists in store
            var store = CategoryJsonTalker.Instance;
            if (!store.Categories.Contains(Category))
            {
                store.Categories.Add(Category);
            }
            await CategoryJsonTalker.SaveAsync([.. store.Categories]);
        }
        catch (Exception ex)
        {
            await _page.DisplayAlertAsync("Erreur", ex.Message, "OK");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}