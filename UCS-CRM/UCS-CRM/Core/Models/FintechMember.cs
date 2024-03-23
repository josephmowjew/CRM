using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Globalization;

namespace UCS_CRM.Core.Models
{
    public partial class FintechMember
    {
        [JsonProperty("status")]
        public long Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public List<Datum> Data { get; set; }
    }

    public partial class Datum
    {
        [JsonProperty("fIdxno")]
        public long FIdxno { get; set; }

        [JsonProperty("account")]
        public string Account { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("idno")]
        public string Idno { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("mobile")]
        public string Mobile { get; set; }

        [JsonProperty("mtype")]
        public string Mtype { get; set; }

        [JsonProperty("employer")]
        public string Employer { get; set; }

        [JsonProperty("branch")]
        public string Branch { get; set; }

        [JsonProperty("dob")]
        public DateTimeOffset Dob { get; set; }

        [JsonProperty("createdOn")]
        public DateTimeOffset CreatedOn { get; set; }
    }

    public partial class FintechMember
    {
        public static FintechMember FromJson(string json) => JsonConvert.DeserializeObject<FintechMember>(json, UCS_CRM.Core.Models.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this FintechMember self) => JsonConvert.SerializeObject(self, UCS_CRM.Core.Models.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
