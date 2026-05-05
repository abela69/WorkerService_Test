using System.Text.Json.Serialization;

namespace WorkerService_Test.Models
{
    public class DocumentMessage
    {
        [JsonPropertyName("debitCustomerId")]
        public int? DebitCustomerId { get; set; }

        [JsonPropertyName("creditCustomerId")]
        public int? CreditCustomerId { get; set; }

        [JsonPropertyName("channelId")]
        public int ChannelId { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
    }
}
