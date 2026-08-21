using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace NetCore.Donation.Infrastructure.Database.Extensions;

public static class ModelBuilder
{
	public static void SetDefaultValueTableName(this Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
	{
		foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(x => x.ClrType.GetCustomAttribute(typeof(TableAttribute)) != null))
		{
			var entityClass = entity.ClrType;

			if (entityClass.GetCustomAttribute(typeof(TableAttribute)) is TableAttribute tableAttribute)
			{
				modelBuilder.Entity(entityClass).ToTable(tableAttribute.Name);
			}

			var properties = entityClass.GetProperties().Where(p =>
				(p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(Guid))
				&& p.CustomAttributes.Any(a => a.AttributeType == typeof(DatabaseGeneratedAttribute) && p.CustomAttributes.All(c => c.AttributeType != typeof(DefaultValueAttribute))));

			foreach (var property in properties)
			{
				var defaultValueSql = "GetDate()";
				if (property.PropertyType == typeof(Guid))
				{
					defaultValueSql = "newsequentialid()";
				}

				modelBuilder.Entity(entityClass).Property(property.Name).HasDefaultValueSql(defaultValueSql);
			}

			foreach (var property in entityClass.GetProperties().Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(DefaultValueAttribute))))
			{
				if (property.GetCustomAttribute(typeof(DefaultValueAttribute)) is DefaultValueAttribute { Value: not null } attribute)
				{
					modelBuilder.Entity(entityClass).Property(property.Name)
						.HasDefaultValueSql(attribute.Value.ToString());
				}
			}
		}
	}
}