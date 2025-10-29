namespace SheeToList.View;

public partial class RecipePage : ContentPage
{
	public RecipePage()
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