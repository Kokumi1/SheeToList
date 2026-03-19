using System.Diagnostics;
using System.Globalization;
using System.Text;
using SheeToList.Model;
using SheeToList.Services;

namespace SheeToList.Utils
{
    public class KeywordFlattener
    {
        // Mots-clés par défaut pour chaque catégorie, utilisés si le chargement depuis le store échoue ou est vide.
        private static readonly Dictionary<Category, string[]> DefaultKeywords = new()
        {
            { Category.Surgelé, new[] { "surgeles", "congele", "glace", "pdt sautees", "pdt rissolees", "frite", "frites", "cordon bleu"} },
            {Category.Épicerie, new[] { "épicerie", "epicerie", "conserve","plats préparés", "conserves", "sauce", "sauces", "pate", "pates",
                "cereale", "patate", "pdt", "legumineuse", "flageolets", "pois", "tarte", "flamekuche","pizza", "huile", "vinaigre",
                "vin blanc", "moutarde", "mayonnaise", "ketchup", "soupe", "bouillon","carbonara", "epinards creme", "lentille", "lentilles", "semoule"}  },
            { Category.Fruit, new[] { "pomme", "banane", "orange", "fraise", "raisin", "kiwi", "citron", "ananas", "poire", "clementine" } },
            { Category.Legume, new[] { "tomate", "carotte", "salade", "laitue", "poivron", "oignon", "courgette", "concombre", "aubergine", "haricots",
                "leg provencal", "poireau", "endive", "choux", "chou ", "leg" } },
            { Category.Viande, new[] { "poulet", "boeuf", "porc", "agneau", "steak", "saucisse", "dinde", "roti", "chippolata", "charcuterie", "jambon", "boudin" } },
            { Category.Poissons, new[] {"poisson", "saumon", "thon", "crevette", "merlan", "truite", "morue", "cabillaud" } },
            { Category.ProduitLaitier, new[] { "lait", "yaourt", "fromage", "beurre", "crème", "yaourt", "maroilles", "caprice des dieux", "port salut",
                "chausse aux moines", "buche de chevre", "pave d'affinois","chevre nature", "rocamadour", "petit basque", "etorki", "st albray", "boursin",
                "mimolette", "cousteron", "bleu"} },
            { Category.Boulangerie, new[] { "pain", "baguette", "croissant", "brioche", "pain de mie" } },
            { Category.Boisson, new[] { "eau", "jus", "soda", "coca", "vin", "biere", "café" } },
            { Category.ProduitMenager, new[] { "lessive", "savon", "shampoing", "détergent", "produit vaisselle", "eponges", "papier toilette" } },
            { Category.Conserve, new[] { "conserve", "bocal", "boite", "terrine", "rillette", "pate", "cornichon", "olive", "caperis" } }
        };

        /// <summary>
        /// Aplatit les mots-clés de toutes les catégories en une liste de paires (clé normalisée, catégorie).
        /// </summary>
        public static (string keyNormalized, Category cat)[] KeywordFlattening()
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
            return [.. pairs
                .GroupBy(p => p.key)
                .Select(g => g.First())];
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
