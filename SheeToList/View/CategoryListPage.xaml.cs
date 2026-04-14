using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;
using SheeToList.Resources.String;
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
    public string PageTitle => AppString.Main_category;
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
        string? name = await _page.DisplayPromptAsync(AppString.popup_category_title, AppString.popup_category_text,
            accept: AppString.popup_category_create, cancel: AppString.popup_category_cancel);
        if (string.IsNullOrWhiteSpace(name)) return;

        if (Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await _page.DisplayAlertAsync(AppString.popup_warn_title, AppString.popup_category_warn_title, AppString.General_ok);
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
        bool confirm = await _page.DisplayAlertAsync(AppString.popup_confirm, 
            $"{AppString.popup_del_confirm_1} {SelectedCategory.Name} {AppString.popup_del_confirm_2}",
            AppString.popup_yes, AppString.popup_no);
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
            await CategoryJsonTalker.SaveAsync(Categories);
        }
        catch (Exception ex)
        {
            await _page.DisplayAlertAsync(AppString.popup_error , ex.Message, AppString.General_ok);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}