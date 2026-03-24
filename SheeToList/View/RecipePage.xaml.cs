using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Extensions;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;
using SheeToList.Services;

namespace SheeToList.View;

public partial class RecipePage : ContentPage
{
	public RecipePage(Recipe recipe)
	{
		InitializeComponent();
        BindingContext = new RecipeViewModel(recipe, this);
    }
	public async Task<String?> ItemNameAskerAsync(string title, string message, string initialValue = "", string accept = "Valider", string cancel = "Annuler")
	{
		return await DisplayPromptAsync(title, message, accept: accept, cancel: cancel, initialValue: initialValue);
	}

	public async Task<(string? name, string? category)> ItemNameOrPickAskerAsync(string title, string initialValue = "")
	{
		var popup = new PickOrTypePopup(initialValue);
		this.ShowPopup(popup);
		var result = await popup.WaitForResultAsync();
		return (result?.Name, result?.Category);
	}
}

public class RecipeViewModel : INotifyPropertyChanged
{
	private Recipe _recipe;
    private int recipeIndex;
    private RecipePage _page;
    public RecipeViewModel(Recipe recipe, RecipePage page)
    {
        recipeIndex = RecipeJsonTalker.Instance.Recipes.IndexOf(recipe);
        _recipe = RecipeJsonTalker.Instance.Recipes[recipeIndex];
        _page = page;

        //initialize commands
        AddItemCommand = new Command(AddIngredient);
        EditItemCommand = new Command<ProductToBuy>(EditIngredient);
        DeleteItemCommand = new Command<ProductToBuy>(DeleteIngredient);
        EditRecipeNameCommand = new Command(EditRecipeName);
    }

	public string RecetteTitle
	{
		get => $"ingrédients pour : { _recipe.Name}";
		set
		{
			_recipe.Name = value;
		}
	}
	public ObservableCollection<ProductToBuy>? RecipeIngredientList => _recipe.Ingredients;

	public ICommand AddItemCommand { get; }
	public ICommand EditItemCommand { get; }
	public ICommand EditRecipeNameCommand { get; }
	public ICommand DeleteItemCommand { get; }

	private async void AddIngredient()
	{
		var (name, category) = await _page.ItemNameOrPickAskerAsync("Ajouter un ingrédient");

		if (string.IsNullOrWhiteSpace(name)) return;
		if (RecipeIngredientList.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))      //Check for duplicates
		{
			await _page.DisplayAlertAsync("Doublon", "Cette ingredient est déjà dans la liste.", "OK");
			return;
		}

		var ingredient = new ProductToBuy { Name = name.Trim(), IsChecked = false };
		// Assigner la catégorie si elle a été détectée
		if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<Category>(category, ignoreCase: true, out var parsedCategory))
		{
			ingredient.Categorie = parsedCategory;
		}

		RecipeIngredientList?.Add(ingredient);
		SaveRecipeChanges();
		OnPropertyChanged(nameof(RecipeIngredientList));
	}

	private async void EditIngredient(ProductToBuy ingredient)
	{
		var (newName, newCategory) = await _page.ItemNameOrPickAskerAsync("Renommer un ingrédient", ingredient.Name);
		if (string.IsNullOrWhiteSpace(newName)) return;
		if (RecipeIngredientList.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && p != ingredient))      //Check for duplicates
		{
			await _page.DisplayAlertAsync("Doublon", "Ce produit est déjà dans la liste.", "OK");
			return;
		}

		ingredient.Name = newName.Trim();
		if (!string.IsNullOrWhiteSpace(newCategory) && Enum.TryParse<Category>(newCategory, ignoreCase: true, out var parsedCategory))
		{
			ingredient.Categorie = parsedCategory;
		}

		SaveRecipeChanges();
		OnPropertyChanged(nameof(RecipeIngredientList));
	}

	private async void DeleteIngredient(ProductToBuy ingredient)
	{
		// Confirm deletion
		bool confirm = await _page.DisplayAlertAsync("Confirmer", $"Supprimer {ingredient.Name} ?", "Oui", "Non");
		if (!confirm) return;

		RecipeIngredientList?.Remove(ingredient);
		SaveRecipeChanges();
		OnPropertyChanged(nameof(RecipeIngredientList));
	}

    private async void EditRecipeName()
    {
        Debug.WriteLine("Editing recipe name...");
        string? newName = await _page.ItemNameAskerAsync("Changer le nom de la recette", "Nouveau nom:");
        if (string.IsNullOrWhiteSpace(newName)) return;
        _recipe.Name = newName.Trim();
        SaveRecipeChanges();
        OnPropertyChanged(nameof(RecetteTitle));
    }

    private void SaveRecipeChanges()
    {
        RecipeJsonTalker.Instance.Recipes[recipeIndex] = _recipe;
        RecipeJsonTalker.SaveAsync(RecipeJsonTalker.Instance.Recipes.ToList());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}