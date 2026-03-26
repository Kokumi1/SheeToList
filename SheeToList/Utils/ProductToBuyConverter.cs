using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SheeToList.Model;

namespace SheeToList.Utils
{
    internal class ProductToBuyConverter : JsonConverter<ProductToBuy>
    {
        public override ProductToBuy Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return new ProductToBuy { Name = reader.GetString() ?? "", IsChecked = false, Categorie = Category.Autre };
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                var element = doc.RootElement;
                return new ProductToBuy
                {
                    Name = element.GetProperty("Name").GetString() ?? "",
                    IsChecked = element.TryGetProperty("IsChecked", out var checkedProp) ? checkedProp.GetBoolean() : false,
                    Categorie = element.TryGetProperty("Categorie", out var catProp) ? (Category)catProp.GetInt32() : Category.Autre
                };
            }

            throw new JsonException($"Cannot convert {reader.TokenType} to ProductToBuy");
        }

        public override void Write(Utf8JsonWriter writer, ProductToBuy value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", value.Name);
            writer.WriteBoolean("IsChecked", value.IsChecked);
            writer.WriteNumber("Categorie", (int)value.Categorie);
            writer.WriteEndObject();
        }
    }
}
