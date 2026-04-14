using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;
using SheeToList.Resources.String;
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
    public  string PageTitle => AppString.category_detail_title;
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
        DeleteKeywordCommand = new Command<string>(DeleteKeyword);
    }

    private async void AddKeyword()
    {
        string? text = await _page.DisplayPromptAsync(AppString.Category_add_title, AppString.Category_Keyword);
        if (string.IsNullOrWhiteSpace(text)) return;
        if (Category.Keywords.Any(k => k.Equals(text, StringComparison.OrdinalIgnoreCase)))
        {
            await _page.DisplayAlertAsync(AppString.popup_warn_title, AppString.Category_popup_dublicate,
                AppString.General_ok);
            return;
        }
        Category.Keywords.Add(text.Trim());
        await SaveCategoryAsync();
    }

    private async void EditKeyword(string keyword)
    {
        if (keyword == null) return;
        string? newVal = await _page.DisplayPromptAsync(AppString.Category_edit_keyword, AppString.Category_edit_text,
            initialValue: keyword);
        if (string.IsNullOrWhiteSpace(newVal)) return;
        if (Category.Keywords.Any(k => k.Equals(newVal, StringComparison.OrdinalIgnoreCase))) { await _page.DisplayAlertAsync("Doublon", "Ce mot-clé existe déjà.", "OK"); return; }
        int idx = Category.Keywords.IndexOf(keyword);
        if (idx >= 0) 
        {
            Category.Keywords[idx] = newVal.Trim();
            await SaveCategoryAsync();
        }
    }

    private async void DeleteKeyword(string keyword)
    {
        if (keyword == null) return;
        bool confirm = await _page.DisplayAlertAsync(AppString.popup_confirm,
            AppString.popup_del_confirm_1 + keyword + AppString.popup_del_confirm_2,
            AppString.popup_yes, AppString.popup_no);
        if (!confirm) return;
        Category.Keywords.Remove(keyword);
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
            await _page.DisplayAlertAsync(AppString.popup_error, ex.Message, AppString.General_ok);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}