using System;
using System.Threading.Tasks;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Workflow;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Services
{
    public interface IWorkflowService
    {
        Task<WorkflowState> CompleteActivity(Operation operation, Guid? activityId, JObject activityOutput);
    }
}
