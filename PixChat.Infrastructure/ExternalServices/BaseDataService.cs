using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Interfaces;
using PixChat.Infrastructure.Database;

namespace PixChat.Infrastructure.ExternalServices;

public abstract class BaseDataService
{
    private readonly IDbContextWrapper<ApplicationDbContext> _dbContextWrapper;
    public readonly ILogger<BaseDataService> _logger;

    protected BaseDataService(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<BaseDataService> logger)
    {
        _dbContextWrapper = dbContextWrapper;
        _logger = logger;
    }
    
    protected ApplicationDbContext Context => _dbContextWrapper.DbContext;

    protected Task ExecuteSafeAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
        ExecuteSafeAsync(token => action(), cancellationToken);

    protected Task<TResult> ExecuteSafeAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default) =>
        ExecuteSafeAsync(token => action(), cancellationToken);

    private async Task ExecuteSafeAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContextWrapper.BeginTransactionAsync(cancellationToken);

        try
        {
            await action(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, $"transaction is rollbacked");
            throw;
        }
    }

    private async Task<TResult> ExecuteSafeAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContextWrapper.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, $"transaction is rollbacked");
            throw;
        }
    }
}