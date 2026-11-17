namespace Rongia.Mediator;

/// <summary>
/// Marker interface to represent a command with a void response
/// </summary>
public interface ICommand : IBaseRequest
{
}

/// <summary>
/// Marker interface to represent a command with a response
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface ICommand<out TResponse> : IBaseRequest
{
}
