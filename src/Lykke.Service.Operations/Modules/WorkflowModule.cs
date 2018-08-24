using System;
using System.Collections.Generic;
using System.ComponentModel;
using Autofac;
using Autofac.Core;
using FluentValidation;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow;
using Lykke.Service.Operations.Workflow.Activities;
using Lykke.Workflow;

namespace Lykke.Service.Operations.Modules
{
    public class WorkflowModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {            
            builder.RegisterAssemblyTypes(typeof(OperationWorkflow).Assembly)
                .Where(t => t.IsSubclassOf(typeof(OperationWorkflow))).Named(type => type.Name, typeof(OperationWorkflow));

            builder.RegisterAssemblyTypes(typeof(OperationWorkflow).Assembly)
                .Where(t => t.IsClosedTypeOf(typeof(IValidator<>)))
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterGeneric(typeof(ValidationActivity<>));
            builder.RegisterGeneric(typeof(DelegateActivity<,>));
            builder.RegisterGeneric(typeof(GenericActivity<,,>));

            builder.RegisterType<ActivityFactory>().As<IActivityFactory>();

            builder.Register<Func<string, Operation, OperationWorkflow>>(context =>
            {
                var ctx = context.Resolve<IComponentContext>();

                return (workflow, operation) =>
                    ctx.ResolveNamed<OperationWorkflow>(workflow, new TypedParameter(typeof(Operation), operation));
            });
        }
    }

    internal class ActivityFactory : IActivityFactory
    {
        private readonly ILifetimeScope _lifetimeScope;

        public ActivityFactory(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public TActivity Create<TActivity>(object activityCreationParams) where TActivity : IActivityWithOutput<object, object, object>
        {
            List<Parameter> parameters = new List<Parameter>();

            if (activityCreationParams != null)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(activityCreationParams))
                {
                    var value = descriptor.GetValue(activityCreationParams);
                    parameters.Add(new NamedParameter(descriptor.Name, value));
                }
            }

            return _lifetimeScope.Resolve<TActivity>(parameters);
        }

        public void Release<TActivity>(TActivity activity) where TActivity : IActivityWithOutput<object, object, object>
        {
            
        }
    }
}
