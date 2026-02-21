using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;
using SheeToList.Services;

namespace SheeToList.View;

public partial class CategoryListPage : ContentPage
{
    public CategoryListPage()
    {
        InitializeComponent();
        BindingContext = new CategoryListViewModel(this);
    }
}

public class CategoryListViewModel : INotifyPropertyChanged
{
    private readonly Page _page;
    private CategoryDefinition? _selectedCategory;
    private bool _isLoading;

    public ObservableCollection<CategoryDefinition> Categories { get; private set; } = new();
    public CategoryDefinition? SelectedCategory
    {
        get => _selectedCategory;
        set { _selectedCategory = value; OnPropertyChanged(); }
    }

    public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

    // Commands
    public ICommand AddCategoryCommand { get; }
    public ICommand EditCategoryCommand { get; }
    public ICommand DeleteCategoryCommand { get; }
    public ICommand OpenDetailCommand { get; }

    public CategoryListViewModel(Page page)
    {
        _page = page;
        AddCategoryCommand = new Command(AddCategory);
        EditCategoryCommand = new Command(EditSelectedCategory, () => SelectedCategory != null);
        DeleteCategoryCommand = new Command(DeleteSelectedCategory, () => SelectedCategory != null);
        OpenDetailCommand = new Command<CategoryDefinition>(OpenDetail);

        // load categories
        _ = Load();
    }

    private async Task Load()
    {
        IsLoading = true;
        Categories = CategoryJsonTalker.Instance.Categories;
        Debug.WriteLine("Category instance" + Categories.Count);
        OnPropertyChanged(nameof(Categories));
        IsLoading = false;
    }

    private async void AddCategory()
    {
        string? name = await _page.DisplayPromptAsync("Nouvelle catégorie", "Nom de la catégorie :", accept: "Créer", cancel: "Annuler");
        if (string.IsNullOrWhiteSpace(name)) return;

        if (Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await _page.DisplayAlertAsync("Doublon", "Cette catégorie existe déjà.", "OK");
            return;
        }

        var cd = new CategoryDefinition { Name = name.Trim(), Keywords = new ObservableCollection<string>() };
        Categories.Add(cd);
        SelectedCategory = cd;
        await Save();
        // ouvrir l'écran détail
        await OpenDetailInternal(cd);
    }

    private async void EditSelectedCategory()
    {
        if (SelectedCategory == null) return;
        await OpenDetailInternal(SelectedCategory);
    }

    private async void DeleteSelectedCategory()
    {
        if (SelectedCategory == null) return;
        bool confirm = await _page.DisplayAlertAsync("Confirmer", $"Supprimer la catégorie {SelectedCategory.Name} ?", "Oui", "Non");
        if (!confirm) return;
        Categories.Remove(SelectedCategory);
        SelectedCategory = null;
        await Save();
    }

    private void OpenDetail(CategoryDefinition category)
    {
        _ = OpenDetailInternal(category);
    }

    private async Task OpenDetailInternal(CategoryDefinition category)
    {
        var page = new CategoryDetailPage(category);
        await _page.Navigation.PushAsync(page);
    }

    private async Task Save()
    {
        try
        {
            await CategoryJsonTalker.SaveAsync(Categories.ToList());
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