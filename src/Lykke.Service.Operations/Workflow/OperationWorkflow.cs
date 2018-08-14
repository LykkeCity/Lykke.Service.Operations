using System;
using System.Linq.Expressions;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Activities;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Workflow;

namespace Lykke.Service.Operations.Workflow
{
    public class OperationWorkflow : Workflow<Operation>
    {
        private readonly Operation _activityExecutor;
        protected ILog Log { get; }

        public OperationWorkflow(Operation operation, ILog log, IActivityFactory activityFactory) 
            : base(operation, activityFactory, operation)
        {
            Log = log;
            _activityExecutor = operation;
        }

        public override Execution<Operation> Run(Operation operation)
        {
            Log.WriteInfo(GetType().Name, null, $"Operation [{operation.Id}] Run - running operation of type '{operation.Type}'");

            var state = operation.WorkflowState;
            var result = base.Run(operation);
            operation.ApplyValuesChanges();

            Log.WriteInfo(GetType().Name, null, $"Operation [{operation.Id}] of type '{operation.Type}' Run - state changed from {state} to {operation.WorkflowState}");
            
            return result;
        }

        public override Execution<Operation> Resume<TClosure>(Operation operation, Guid activityExecutionId, TClosure closure)
        {
            if (operation.WorkflowState == WorkflowState.Corrupted
                || operation.WorkflowState == WorkflowState.Complete)
            {
                Log.Debug(string.Format("Operation [{0}] Resume - can not resume operation of type '{1}' with state '{3}' with closure {2}", operation.Id, operation.Type, closure.ToString(), operation.WorkflowState));
                return null; //new Execution<Operation> { State = operation.State, ActiveNode = operation.ActiveNode };
            }
            
            var result = base.Resume(operation, activityExecutionId, closure);
            operation.ApplyValuesChanges();

            return result;
        }
        
        internal ISlotCreationHelper<Operation, GenericActivity<TInput, TOutput, TFailOutput>> Node<TInput, TOutput, TFailOutput>(string name, Expression<Func<IActivityReference, Tuple<TInput, TOutput, TFailOutput>>> method)
            where TInput : class
            where TOutput : class
            where TFailOutput : class
        {
            var methodCallExpression = method.Body as MethodCallExpression;
            if (methodCallExpression == null)
            {
                throw new InvalidOperationException(typeof(IActivityReference).Name + " method or extension method should be called");
            }

            var activityType = ((MethodCallExpression)method.Body).Method.Name;

            return Node<GenericActivity<TInput, TOutput, TFailOutput>>(name, new { activityType, nodeName = name, executor = _activityExecutor }, activityType);
        }

        protected ISlotCreationHelper<Operation, ValidationActivity<TInput>> ValidationNode<TInput>(string name)
            where TInput : class
        {
            return Node<ValidationActivity<TInput>>(name, null, string.Format("{0} validation", typeof(TInput).Name));
        }
    }

    public interface IActivityReference
    {
        Tuple<dynamic, dynamic, dynamic> WaitForResultsFromMe();
        Tuple<dynamic, dynamic, dynamic> RequestConfirmation();
        Tuple<BlockchainCashoutInput, dynamic, dynamic> SettleOnBlockchain();
    }
}
