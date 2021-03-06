// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Lykke.Service;
    using Lykke.Service.Operations;
    using Lykke.Service.Operations.Client;
    using Lykke.Service.Operations.Client.AutorestClient;
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class ModelStateEntry
    {
        /// <summary>
        /// Initializes a new instance of the ModelStateEntry class.
        /// </summary>
        public ModelStateEntry()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ModelStateEntry class.
        /// </summary>
        /// <param name="validationState">Possible values include:
        /// 'Unvalidated', 'Invalid', 'Valid', 'Skipped'</param>
        public ModelStateEntry(ModelValidationState validationState, object rawValue = default(object), string attemptedValue = default(string), IList<ModelError> errors = default(IList<ModelError>), bool? isContainerNode = default(bool?), IList<ModelStateEntry> children = default(IList<ModelStateEntry>))
        {
            RawValue = rawValue;
            AttemptedValue = attemptedValue;
            Errors = errors;
            ValidationState = validationState;
            IsContainerNode = isContainerNode;
            Children = children;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "RawValue")]
        public object RawValue { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "AttemptedValue")]
        public string AttemptedValue { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Errors")]
        public IList<ModelError> Errors { get; private set; }

        /// <summary>
        /// Gets or sets possible values include: 'Unvalidated', 'Invalid',
        /// 'Valid', 'Skipped'
        /// </summary>
        [JsonProperty(PropertyName = "ValidationState")]
        public ModelValidationState ValidationState { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IsContainerNode")]
        public bool? IsContainerNode { get; private set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Children")]
        public IList<ModelStateEntry> Children { get; private set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Children != null)
            {
                foreach (var element in Children)
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
