using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Domain.Base;
using RecruitIQ.Persistence.DbContext;

namespace RecruitIQ.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyGlobalQueryFilters(this ModelBuilder modelBuilder, RecruitIQDbContext context)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var type = entityType.ClrType;

            if (typeof(BaseEntity).IsAssignableFrom(type))
            {
                var parameter = Expression.Parameter(type, "e");

                // !e.IsDeleted
                var isDeletedProperty = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var isNotDeleted = Expression.Not(isDeletedProperty);

                Expression filter = isNotDeleted;

                if (typeof(IMultiTenant).IsAssignableFrom(type))
                {
                    // e.CompanyId
                    var companyIdProperty = Expression.Property(parameter, nameof(IMultiTenant.CompanyId));

                    // context.CurrentCompanyId
                    var contextExpr = Expression.Constant(context);
                    var currentCompanyIdProperty = Expression.Property(contextExpr, nameof(RecruitIQDbContext.CurrentCompanyId));

                    // e.CompanyId == context.CurrentCompanyId
                    var tenantFilter = Expression.Equal(companyIdProperty, currentCompanyIdProperty);

                    filter = Expression.AndAlso(filter, tenantFilter);
                }

                var lambda = Expression.Lambda(filter, parameter);
                modelBuilder.Entity(type).HasQueryFilter(lambda);
            }
        }
    }

    public static void ConfigureBaseProperties(this ModelBuilder modelBuilder, Microsoft.EntityFrameworkCore.DbContext context)
    {
        var isSqlite = context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var type = entityType.ClrType;

            if (typeof(BaseEntity).IsAssignableFrom(type))
            {
                var rowVersionProperty = modelBuilder.Entity(type).Property(nameof(BaseEntity.RowVersion));

                if (isSqlite)
                {
                    // SQLite does not generate rowversion values on write, so we mark it as concurrency token only.
                    rowVersionProperty.IsConcurrencyToken();
                }
                else
                {
                    // SQL Server uses rowversion type
                    rowVersionProperty.IsRowVersion().IsConcurrencyToken();
                }
            }
        }
    }
}
