using Rongia.Caching;
using Rongia.DependencyInjection;

namespace Rongia;

public interface ISmoke2Service
{
    Task<string> GetAsync(long id);
    Task<string> GetPlain(long id);
    Task Touch(long id);
}

public sealed class Smoke2Service : ISmoke2Service, ITransientDependency
{
    public static int Calls;

    [Cacheable("smoke2", Key = "'o:' + #id")]
    public async Task<string> GetAsync(long id)
    {
        Interlocked.Increment(ref Calls);
        await Task.Yield();
        return "v" + id + "#" + Calls;
    }

    [Cacheable("smoke2", Key = "'p:' + #id")]
    public Task<string> GetPlain(long id)
    {
        Interlocked.Increment(ref Calls);
        return Task.FromResult("p" + id + "#" + Calls);
    }

    [CacheEvict("smoke2", AllEntries = true)]
    public Task Touch(long id) => Task.CompletedTask;
}
