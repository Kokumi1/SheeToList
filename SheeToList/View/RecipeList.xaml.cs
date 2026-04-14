using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;
using SheeToList.Resources.String;
using SheeToList.Services;

namespace SheeToList.View;

public partial class RecipeList : ContentPage
{
	public RecipeList()
	{
		InitializeComponent();
		BindingContext = new RecipeListViewModel(this);
    }

    public async Task<String?> ItemNameAskerAsync(string title, string message, string initialValue = "", string accept = "Valider", string cancel = "Annuler")
    {
        return await DisplayPromptAsync(title, message, accept: accept, cancel: cancel, initialValue: initialValue);
    }
    public async void GotoRecipeDetailPage(Recipe recipe)
    {
        await Navigation.PushAsync(new RecipePage(recipe));
    }
}

public class RecipeListViewModel: INotifyPropertyChanged
{
    public string PageTitle => AppString.Recipe_list_title;
    private bool _isLoading;
	private readonly RecipeList _page;

    public bool IsLoading
	{
		get => _isLoading;
		set
		{
			  _isLoading = value;
			OnPropertyChanged();
        } 
    }
	public ObservableCollection<Recipe>? Recipes { get; set; }

    public RecipeListViewModel(RecipeList recipeList)
	{
		Recipes = RecipeJsonTalker.Instance.Recipes;
        _page = recipeList;

		AddItemCommand = new Command(AddRecipe);
		DeleteItemCommand = new Command<Recipe>(DeleteRecipe);
		SelectItemCommand = new Command<Recipe>(SelectRecipe);
    }

    public ICommand AddItemCommand { get; }
    public ICommand DeleteItemCommand { get; }
	public ICommand SelectItemCommand { get; }

	private async void AddRecipe()
	{
        string? text = await _page.ItemNameAskerAsync(AppString.Popup_Recipe_Add_Title, AppString.Popup_Recipe_Add_Text);

		if (string.IsNullOrWhiteSpace(text)) return;
        if (Recipes.Any(p => p.Name.Equals(text, StringComparison.OrdinalIgnoreCase)))      //Check for duplicates
        {
            await _page.DisplayAlertAsync(AppString.popup_warn_title, AppString.Popup_Recipe_Warn, AppString.General_ok);
            return;
        }

        Recipes?.Add(new Recipe { Name = text.Trim() });
        await RecipeJsonTalker.SaveAsync([.. Recipes]);
        OnPropertyChanged(nameof(Recipes));

        _page.GotoRecipeDetailPage(Recipes.Last());
    }

	private async void DeleteRecipe(Recipe recipe)
	{
        // Confirm deletion
        bool confirm = await _page.DisplayAlertAsync(AppString.popup_confirm,
            $"{AppString.popup_del_confirm_1} {recipe.Name} {AppString.popup_del_confirm_2}",
            AppString.popup_yes, AppString.popup_no);
        if (!confirm) return;

		Recipes?.Remove(recipe);
        await RecipeJsonTalker.SaveAsync([.. Recipes]);
        OnPropertyChanged(nameof(Recipes));
    }
	private void SelectRecipe(Recipe recipe)
	{
        _page.GotoRecipeDetailPage(recipe);
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

}