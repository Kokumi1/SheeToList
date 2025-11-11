using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;
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
        string? text = await _page.ItemNameAskerAsync("Entrer le nom", "Entrer le nom de l'objet à ajoutée");

		if (string.IsNullOrWhiteSpace(text)) return;
        if (Recipes.Any(p => p.Name.Equals(text, StringComparison.OrdinalIgnoreCase)))      //Check for duplicates
        {
            await _page.DisplayAlert("Doublon", "Cette recette est déjà dans la liste.", "OK");
            return;
        }

        Recipes?.Add(new Recipe { Name = text });
        OnPropertyChanged(nameof(Recipes));
        // Implementation for adding a recipe
    }

	private async void DeleteRecipe(Recipe recipe)
	{
        // Confirm deletion
        bool confirm = await _page.DisplayAlert("Confirmer", $"Supprimer {recipe.Name} ?", "Oui", "Non");
        if (!confirm) return;

		Recipes?.Remove(recipe);
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