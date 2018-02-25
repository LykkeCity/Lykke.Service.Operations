using Common.Log;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Activities;
using Lykke.Workflow;

namespace Lykke.Service.Operations.Workflow
{
    public class OperationWorkflow : Workflow<Operation>
    {
        protected ILog Log { get; }

        public OperationWorkflow(Operation operation, ILog log, IActivityFactory activityFactory) 
            : base(operation, activityFactory, operation)
        {
            Log = log;
        }

        public override Execution<Operation> Run(Operation operation)
        {
            Log.WriteInfo(nameof(CashoutWorkflow), null, $"Operation [{operation.Id}] Run - running operation of type '{operation.Type}'");

            var state = operation.WorkflowState;
            var result = base.Run(operation);
            operation.ApplyValuesChanges();

            Log.WriteInfo(nameof(CashoutWorkflow), null, $"Operation [{operation.Id}] of type '{operation.Type}' Run - state changed from {state} to {operation.WorkflowState}");
            
            return result;
        }

        protected ISlotCreationHelper<Operation, ValidationActivity<TInput>> ValidationNode<TInput>(string name)
            where TInput : class
        {
            return Node<ValidationActivity<TInput>>(name, null, string.Format("{0} validation", typeof(TInput).Name));
        }
    }
}