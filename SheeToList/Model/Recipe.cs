using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheeToList.Model
{
    public class Recipe
    {
        string name="";
        List<string> ingredients = [];

        public string Name { get => name; set => name = value; }    
        public List<string> Ingredients { get => ingredients; set => ingredients = value; }
    }
}
