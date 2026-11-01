using PaymentsDemo.Api.Infrastructure.Exceptions;

namespace PaymentsDemo.Api.Infrastructure.Extensions;

public static class DomainExtensions
{
    extension<TEntity>(Task<TEntity> queryable)
    {
        public async Task<TEntity> ThrowsNotFoundIfNull()
        {
            TEntity? entity = await queryable;
            if (entity is null)
            {
                string entityType = typeof(TEntity).Name;
                throw new NotFoundException("No record found matching the criteria specified.");
            }

            return entity;
        }
    }
}