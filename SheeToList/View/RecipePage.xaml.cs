using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        EditItemCommand = new Command<string>(EditIngredient);
        DeleteItemCommand = new Command<string>(DeleteIngredient);
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
    public ObservableCollection<string>? RecipeIngredientList => _recipe.Ingredients;

    public ICommand AddItemCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand EditRecipeNameCommand { get; }
    public ICommand DeleteItemCommand { get; }

    private async void AddIngredient()
    {
        string? text = await _page.ItemNameAskerAsync("Ajouter un ingrédient", "Nom de l'ingrédient :");
       
        if (string.IsNullOrWhiteSpace(text)) return;
        if (RecipeIngredientList.Any(p => p.Equals(text, StringComparison.OrdinalIgnoreCase)))      //Check for duplicates
        {
            await _page.DisplayAlertAsync("Doublon", "Cette ingredient est déjà dans la liste.", "OK");
            return;
        }

        RecipeIngredientList?.Add(text.Trim());
        SaveRecipeChanges();
        OnPropertyChanged(nameof(RecipeIngredientList));
    }
    private async void EditIngredient(string ingredient)
    {
        string? text = await _page.ItemNameAskerAsync("Renommer un ingrédient", "Nom de l'ingrédient :");
        if (string.IsNullOrWhiteSpace(text)) return;
        if (RecipeIngredientList.Any(p => p.Equals(text, StringComparison.OrdinalIgnoreCase)))      //Check for duplicates
        {
            await _page.DisplayAlertAsync("Doublon", "Ce produit est déjà dans la liste.", "OK");
            return;
        }

        RecipeIngredientList[RecipeIngredientList.IndexOf(ingredient)] = text;
        SaveRecipeChanges();
        OnPropertyChanged(nameof(RecipeIngredientList));
    }
    private async void DeleteIngredient(string ingredient)
    {
        // Confirm deletion
        bool confirm = await _page.DisplayAlertAsync("Confirmer", $"Supprimer {ingredient} ?", "Oui", "Non");
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