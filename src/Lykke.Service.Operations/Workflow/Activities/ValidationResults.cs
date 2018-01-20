using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Workflow.Activities
{
    public class ValidationResults
    {
        public string ErrorMessage { get; set; }
        public JArray ValidationErrors { get; set; }
    }
}
