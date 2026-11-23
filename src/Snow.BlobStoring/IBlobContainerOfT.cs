namespace Snow.BlobStoring;

/// <summary>
/// 强类型容器: 用类型来区分容器, 名字见 BlobContainerNameAttribute
/// </summary>
/// <typeparam name="T">容器标记类型</typeparam>
public interface IBlobContainer<T> : IBlobContainer;
