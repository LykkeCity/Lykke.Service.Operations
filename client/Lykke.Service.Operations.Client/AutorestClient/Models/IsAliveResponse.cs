// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Collections.Generic;

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
        public IsAliveResponse(bool isDebug, string name = default(string), string version = default(string), string env = default(string), IList<IssueIndicator> issueIndicators = default(IList<IssueIndicator>))
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
        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Version")]
        public string Version { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Env")]
        public string Env { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IsDebug")]
        public bool IsDebug { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IssueIndicators")]
        public IList<IssueIndicator> IssueIndicators { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            
        }
    }
}
