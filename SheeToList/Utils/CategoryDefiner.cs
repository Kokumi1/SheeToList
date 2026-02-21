using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using SheeToList.Model;
using SheeToList.Services;

namespace SheeToList.Utils
{
    public static class CategoryDefiner
    {
        //TODO: Déplacer les mots clés par défault et l'applatisseur dans une classe dédié.
        // Dictionnaire de mots-clés => catégorie
        private static readonly Dictionary<Category, string[]> DefaultKeywords = new()
        {
            { Category.Surgelé, new[] { "surgeles", "congele", "glace", "pdt sautees", "pdt rissolees", "frite"} },
            {Category.Épicerie, new[] { "épicerie", "epicerie", "conserve","plats préparés", "conserves", "sauce", "sauces", "pate",
                "cereale", "patate", "pdt", "legumineuse", "flageolets", "pois", "tarte", "flamekuche","pizza", "huile", "vinaigre",
                "vin blanc", "moutarde", "mayonnaise", "ketchup", "soupe", "bouillon","carbonara", "epinards creme", "lentille", "semoule"}  },
            { Category.Fruit, new[] { "pomme", "banane", "orange", "fraise", "raisin", "kiwi", "citron", "ananas", "poire", "clementine" } },
            { Category.Legume, new[] { "tomate", "carotte", "salade", "laitue", "poivron", "oignon", "courgette", "concombre", "aubergine", "haricots",
                "leg provencal", "poireau", "endive", "choux", "chou " } },
            { Category.Viande, new[] { "poulet", "boeuf", "porc", "agneau", "steak", "saucisse", "dinde", "roti", "chippolata", "charcuterie", "jambon" } },
            { Category.Poissons, new[] {"poisson", "saumon", "thon", "crevette", "merlan", "truite", "morue", "cabillaud" } },
            { Category.ProduitLaitier, new[] { "lait", "yaourt", "fromage", "beurre", "crème", "yaourt", "maroilles", "caprice des dieux", "port salut",
                "chausse aux moines", "buche de chevre", "pave d'affinois","chevre nature", "rocamadour", "petit basque", "etorki", "st albray", "boursin",
                "mimolette", "cousteron", "bleu"} },
            { Category.Boulangerie, new[] { "pain", "baguette", "croissant", "brioche", "pain de mie" } },
            { Category.Boisson, new[] { "eau", "jus", "soda", "coca", "vin", "biere", "café" } },
            { Category.ProduitMenager, new[] { "lessive", "savon", "shampoing", "détergent", "produit vaisselle", "eponges", "papier toilette" } }
        };

        private static (string keyNormalized, Category cat)[]? FlatKeywords;

        static void KeywordFlattener()
        {
            //FlatKeywords = [.. Keywords.SelectMany(kvp => kvp.Value.Select(k => (keyNormalized: RemoveDiacritics(k).ToLowerInvariant(), cat: kvp.Key)))];
            var pairs = new List<(string key, Category cat)>(capacity: 256);

            try
            {
                var store = CategoryJsonTalker.Instance;
                var storeCategories = store?.Categories;

                if (storeCategories != null && storeCategories.Count > 0)
                {
                    foreach (var categoryDefinition in storeCategories)
                    {
                        if (categoryDefinition == null || categoryDefinition.Keywords == null) continue;

                        // Tenter de mapper le nom (string) vers l'enum Category.
                        // Si échoue, on utilise Category.Autre.
                        if (!Enum.TryParse<Category>(categoryDefinition.Name, ignoreCase: true, out var mappedCategory))
                            mappedCategory = Category.Autre;

                        foreach (var key in categoryDefinition.Keywords)
                        {
                            
                            if (string.IsNullOrWhiteSpace(key)) continue;
                            var keyNormalized = RemoveDiacritics(key).ToLowerInvariant();
                            pairs.Add((keyNormalized, mappedCategory));
                        }
                    }
                }
            }
            catch
            {
                // ignore et fallback vers DefaultKeywords
                Debug.WriteLine("Erreur lors du chargement des catégories personnalisées. Utilisation des catégories par défaut.");
            }

            // Si aucune paire issue du store, utilise les keywords par défaut
            if (pairs.Count == 0)
            {
                foreach (var kvp in DefaultKeywords)
                {
                    foreach (var key in kvp.Value)
                    {
                        if (string.IsNullOrWhiteSpace(key)) continue;
                        var keyNormalized = RemoveDiacritics(key).ToLowerInvariant();
                        pairs.Add((keyNormalized, kvp.Key));
                    }
                }
            }

            // Dédupliquer par clé normalisée en gardant la première occurrence
            FlatKeywords = [.. pairs
                .GroupBy(p => p.key)
                .Select(g => g.First())];
        }

        /// <summary>
        /// Assigne une catégorie à chaque produit de la collection.
        /// Si <paramref name="overwriteExisting"/> est false, la catégorie n'est modifiée
        /// que si elle est égale à <see cref="Category.Autre"/> ou à la valeur par défaut.
        /// </summary>
        public static void AssignCategories(IEnumerable<ProductToBuy> products, bool overwriteExisting = false)
        {
            if (products is null) throw new ArgumentNullException(nameof(products));

            if (FlatKeywords is null) KeywordFlattener();

            foreach (var product in products)
            {
                if (product is null) continue;

                // Détermine si on doit écraser
                bool shouldAssign = overwriteExisting
                                    || product.Categorie.Equals(Category.Autre)
                                    || EqualityComparer<Category>.Default.Equals(product.Categorie, default(Category));
                if (!shouldAssign) continue;

                product.Categorie = InferCategoryFromName(product.Name);
            }
        }

        /// <summary>
        /// Infère une Category à partir du nom du produit.
        /// Retourne Category.Autre si aucun mot-clé ne correspond.
        /// </summary>
        public static Category InferCategoryFromName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Category.Autre;

            var normalized = RemoveDiacritics(name).ToLowerInvariant();

            // Recherche la première catégorie dont un mot clé est contenu dans le nom
            foreach (var (keyNormalized, cat) in FlatKeywords!)
            {
                if (normalized.Contains(keyNormalized, StringComparison.Ordinal))
                    return cat;
            }
            return Category.Autre;
        }

        // Supprime les accents pour améliorer la détection de mots-clés
        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var ch in normalized)
            {
                var unicode = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (unicode != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(ch);
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
