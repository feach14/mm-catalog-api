using System.Data;
using Catalog.Database;
using Npgsql;

namespace Catalog.Api.Infrastructure.Ordering;

internal static class CatalogOrderTransaction
{
    internal static async Task<TResult> Run<TResult>(CatalogDbContext dbContext, Func<Task<TResult>> operation, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var result = await operation();
                await transaction.CommitAsync(ct);
                return result;
            }
            // These database conflicts abort the transaction; a fresh read can safely retry the complete operation.
            catch (PostgresException error) when (attempt < 2 && IsOrderConflict(error))
            {
            }
            catch (DbUpdateException error) when (attempt < 2 && error.InnerException is PostgresException postgres && IsOrderConflict(postgres))
            {
            }
            dbContext.ChangeTracker.Clear();
            await Task.Delay(TimeSpan.FromMilliseconds(25 * (attempt + 1)), ct);
        }
    }

    private static bool IsOrderConflict(PostgresException error) =>
        error.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
        || (error.SqlState == PostgresErrorCodes.UniqueViolation && error.ConstraintName == "ix_materials_order_by_col");
}