using Autofac;

using Microsoft.EntityFrameworkCore;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.Common;
namespace NB.API.Modules
{ 
    public class EFModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType(typeof(NutriBarnTestContext)).As(typeof(DbContext)).InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(Repository<>))
               .As(typeof(IRepository<>))
               .InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(Service<>))
               .As(typeof(IService<>))
               .InstancePerLifetimeScope();
        }
    }
}
