using SheeToList.Model;

namespace SheeToList.View;

public partial class RecipePage : ContentPage
{
	public RecipePage(Recipe recipe)
	{
		InitializeComponent();
        BindingContext = new RecipeViewModel(recipe);
    }
}

public class RecipeViewModel
{
	private Recipe _recipe;
    public RecipeViewModel(Recipe recipe)
	{
		_recipe = recipe;

    }

	public string RecetteTitle
    {
			get => $"Liste des ingrédients pour : { _recipe.Name}";
		set
		{
			_recipe.Name = value;
        }
    }
}