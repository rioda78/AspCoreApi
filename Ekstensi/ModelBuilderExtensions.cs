using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AspCoreApi.Ekstensi
{ 
        public static class ModelBuilderExtensions
        {
            public static void RegisterAllEntities<TBaseType>(this ModelBuilder modelBuilder, Assembly assembly)
            {
                var entityTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(TBaseType).IsAssignableFrom(t));

                foreach (var type in entityTypes)
                {
                    modelBuilder.Entity(type);
                }
            }

            public static void RegisterEntityTypeConfiguration(this ModelBuilder modelBuilder, Assembly assembly)
            {
                var applyGenericMethod = typeof(ModelBuilder)
                    .GetMethods()
                    .First(m => m.Name == nameof(ModelBuilder.ApplyConfiguration) && m.GetParameters().Length == 1);

                var typesToRegister = assembly
                    .GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                    .Select(t => new
                    {
                        Type = t,
                        Interface = t.GetInterfaces()
                            .FirstOrDefault(i =>
                                i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                    })
                    .Where(t => t.Interface != null);

                foreach (var config in typesToRegister)
                {
                    var entityType = config.Interface!.GenericTypeArguments[0];
                    var applyConcreteMethod = applyGenericMethod.MakeGenericMethod(entityType);
                    var configurationInstance = Activator.CreateInstance(config.Type)!;
                    applyConcreteMethod.Invoke(modelBuilder, new[] { configurationInstance });
                }
            }
        
    }

}
