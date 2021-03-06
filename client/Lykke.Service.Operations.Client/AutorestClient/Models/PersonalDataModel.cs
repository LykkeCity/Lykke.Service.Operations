// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Lykke.Service;
    using Lykke.Service.Operations;
    using Lykke.Service.Operations.Client;
    using Lykke.Service.Operations.Client.AutorestClient;
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Linq;

    public partial class PersonalDataModel
    {
        /// <summary>
        /// Initializes a new instance of the PersonalDataModel class.
        /// </summary>
        public PersonalDataModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the PersonalDataModel class.
        /// </summary>
        public PersonalDataModel(string country, string countryFromID, string countryFromPOA)
        {
            Country = country;
            CountryFromID = countryFromID;
            CountryFromPOA = countryFromPOA;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Country")]
        public string Country { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "CountryFromID")]
        public string CountryFromID { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "CountryFromPOA")]
        public string CountryFromPOA { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Country == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Country");
            }
            if (CountryFromID == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "CountryFromID");
            }
            if (CountryFromPOA == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "CountryFromPOA");
            }
        }
    }
}
