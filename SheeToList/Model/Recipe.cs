using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheeToList.Model
{
    public class Recipe
    {
        string name="";
        ObservableCollection<ProductToBuy> ingredients = [];

        public string Name { get => name; set => name = value; }    
        public ObservableCollection<ProductToBuy> Ingredients { get => ingredients; set => ingredients = value; }
    }
}
