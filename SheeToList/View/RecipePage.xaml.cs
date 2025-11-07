using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SheeToList.Model;
using static System.Net.Mime.MediaTypeNames;

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
    private RecipePage _page;
    public RecipeViewModel(Recipe recipe, RecipePage page)
    {
        _recipe = recipe;
        _page = page;
        RecipeIngredientList = new ObservableCollection<string>(recipe.Ingredients);

        //initialize commands
        AddItemCommand = new Command(AddIngredient);
        EditItemCommand = new Command<string>(EditIngredient);
        DeleteItemCommand = new Command<string>(DeleteIngredient);
    }

    public string RecetteTitle
    {
			get => $"ingrédients pour : { _recipe.Name}";
		set
		{
			_recipe.Name = value;
        }
    }
    public ObservableCollection<string>? RecipeIngredientList { get; set; }

    public ICommand AddItemCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand DeleteItemCommand { get; }

    private async void AddIngredient()
    {
        string? text = await _page.ItemNameAskerAsync("Ajouter un ingrédient", "Nom de l'ingrédient :");
       
        if (string.IsNullOrWhiteSpace(text)) return;
        if (RecipeIngredientList.Any(p => p.Equals(text, StringComparison.OrdinalIgnoreCase)))      //Check for duplicates
        {
            await _page.DisplayAlert("Doublon", "Ce produit est déjà dans la liste.", "OK");
            return;
        }

        RecipeIngredientList?.Add(text);
        OnPropertyChanged(nameof(RecipeIngredientList));
    }
    private async void EditIngredient(string ingredient)
    {
    }
    private async void DeleteIngredient(string ingredient)
    {
        // Confirm deletion
        bool confirm = await _page.DisplayAlert("Confirmer", $"Supprimer {ingredient} ?", "Oui", "Non");
        if (!confirm) return;

        RecipeIngredientList?.Remove(ingredient);
        OnPropertyChanged(nameof(RecipeIngredientList));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}