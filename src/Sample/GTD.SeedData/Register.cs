using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using Autofac;
using Autofac.Features.Variance;
using GTD.Common;
using GTD.ReadModel;
using NEvilES;
using NEvilES.DataStore;
using NEvilES.Pipeline;

namespace GTD.SeedData
{
    public static class Register<TContainer>
    {
        public static TContainer Build<TBuilder>(TBuilder builder) where TBuilder : ContainerBuilder
        {
            var version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            var domain = typeof(Domain.Client).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(domain).AsClosedTypesOf(typeof(IProcessCommand<>));
            builder.RegisterAssemblyTypes(domain).AsClosedTypesOf(typeof(IHandleStatelessEvent<>));
            builder.RegisterAssemblyTypes(domain).AsClosedTypesOf(typeof(IHandleAggregateCommandMarker<>));
            builder.RegisterAssemblyTypes(domain).AsClosedTypesOf(typeof(INeedExternalValidation<>));

            //builder.RegisterSource(new ContravariantRegistrationSource());
            var readModel = typeof(ReadModel.Client).GetTypeInfo().Assembly;

            builder.RegisterAssemblyTypes(readModel).AsClosedTypesOf(typeof(IProject<>));
            builder.RegisterAssemblyTypes(readModel).AsClosedTypesOf(typeof(IProjectWithResult<>));
            builder.RegisterType<DataAccess>().AsImplementedInterfaces().InstancePerLifetimeScope();

            var eventStore = new[]
            {
                typeof(DatabaseEventStore).GetTypeInfo().Assembly,
                typeof(PipelineProcessor).GetTypeInfo().Assembly,
            };
            builder.RegisterAssemblyTypes(eventStore).AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<Factory>().As<IFactory>().InstancePerLifetimeScope();
            builder.RegisterType<PipelineTransaction>().As<CommandContext.ITransaction>().AsSelf().InstancePerLifetimeScope();

            builder.Register(c =>
            {
                var transaction = c.Resolve<CommandContext.ITransaction>();
                var user = c.ResolveNamed<CommandContext.IUser>("user");
                var impersonatedBy = c.ResolveOptionalNamed<CommandContext.IUser>("impersonator");
                return new CommandContext(user, transaction, impersonatedBy, version);
            }).As<CommandContext>().InstancePerLifetimeScope();

            return (TContainer) builder.Build();
        }
    }
}