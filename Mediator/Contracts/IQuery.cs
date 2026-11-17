namespace Mediator;

/// <summary>
/// Marker interface to represent a query with a response
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IQuery<out TResponse> : IBaseRequest
{
}
