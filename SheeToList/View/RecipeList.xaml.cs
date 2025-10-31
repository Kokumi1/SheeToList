using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;

namespace SheeToList.View;

public partial class RecipeList : ContentPage
{
	public RecipeList()
	{
		InitializeComponent();
		BindingContext = new RecipeListViewModel(this);
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
		Recipes = [];
		Recipes.Add(new Recipe { Name = "Tarte aux pommes" });
        _page = recipeList;

		AddItemCommand = new Command(AddRecipe);
		EditItemCommand = new Command<Recipe>(EditRecipe);
		DeleteItemCommand = new Command<Recipe>(DeleteRecipe);
		SelectItemCommand = new Command<Recipe>(SelectRecipe);
    }

    public ICommand AddItemCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand DeleteItemCommand { get; }
	public ICommand SelectItemCommand { get; }

	private void AddRecipe()
	{
		// Implementation for adding a recipe
	}
	private void EditRecipe(Recipe recipe)
	{

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

	}
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

}