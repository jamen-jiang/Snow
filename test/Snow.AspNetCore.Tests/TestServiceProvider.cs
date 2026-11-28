using System;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 测试用的服务提供者包装: 给 <see cref="IServiceProvider"/> 补上 <see cref="IDisposable"/>,
/// 让测试可以继续用 <c>using</c> 释放底层容器。
/// </summary>
internal sealed class TestServiceProvider : IServiceProvider, IDisposable
{
    private readonly IServiceProvider _inner;

    public TestServiceProvider(IServiceProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public object? GetService(Type serviceType) => _inner.GetService(serviceType);

    public void Dispose()
    {
        if (_inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
