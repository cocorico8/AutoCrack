using System.Text.Json.Serialization;

namespace AutoCrack.Core.Models
{
    /// <summary>
    /// Represents the game information returned by the API.
    /// </summary>
    public class GameMetadata
    {
        [JsonPropertyName("found")]
        public bool Found { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        // Byte array to store the downloaded image in memory
        [JsonIgnore]
        public byte[]? CoverImageBytes { get; set; }
    }
}