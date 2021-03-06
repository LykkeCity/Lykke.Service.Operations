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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class IsAliveResponse
    {
        /// <summary>
        /// Initializes a new instance of the IsAliveResponse class.
        /// </summary>
        public IsAliveResponse()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the IsAliveResponse class.
        /// </summary>
        public IsAliveResponse(string name, string version, string env, bool isDebug, IList<IssueIndicator> issueIndicators)
        {
            Name = name;
            Version = version;
            Env = env;
            IsDebug = isDebug;
            IssueIndicators = issueIndicators;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "env")]
        public string Env { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "isDebug")]
        public bool IsDebug { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "issueIndicators")]
        public IList<IssueIndicator> IssueIndicators { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Name == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Name");
            }
            if (Version == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Version");
            }
            if (Env == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Env");
            }
            if (IssueIndicators == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "IssueIndicators");
            }
            if (IssueIndicators != null)
            {
                foreach (var element in IssueIndicators)
                {
                    if (element != null)
                    {
                        element.Validate();
                    }
                }
            }
        }
    }
}
