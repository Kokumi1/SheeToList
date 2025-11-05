using SheeToList.Model;

namespace SheeToList.View;

public partial class RecipePage : ContentPage
{
	public RecipePage(Recipe recipe)
	{
		InitializeComponent();
        BindingContext = new RecipeViewModel();
    }
}

public class RecipeViewModel
{
	public RecipeViewModel()
	{


    }


}