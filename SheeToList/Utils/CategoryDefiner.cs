using System.Globalization;
using System.Text;
using SheeToList.Model;

namespace SheeToList.Utils
{
    public static class CategoryDefiner
    {
        private static (string keyNormalized, Category cat)[]? FlatKeywords;

        /// <summary>
        /// Assigne une catégorie à chaque produit de la collection.
        /// Si <paramref name="overwriteExisting"/> est false, la catégorie n'est modifiée
        /// que si elle est égale à <see cref="Category.Autre"/> ou à la valeur par défaut.
        /// </summary>
        public static void AssignCategories(IEnumerable<ProductToBuy> products, bool overwriteExisting = false)
        {
            ArgumentNullException.ThrowIfNull(products);

            FlatKeywords ??= KeywordFlattener.KeywordFlattening();

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

            var normalized = RemoveDiacritics(name).ToLowerInvariant() + " ";

            // Recherche la première catégorie dont un mot clé est contenu dans le nom
            foreach (var (keyNormalized, cat) in FlatKeywords!)
            {
                if (normalized.Contains(keyNormalized + " ", StringComparison.Ordinal) || 
                    normalized.Contains(keyNormalized + "s ",StringComparison.Ordinal))
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
