namespace Snow.Domain.Services;

/// <summary>
/// 领域服务基类(对应 ABP 的 <c>DomainService</c>): 只是个标记, 不含任何便利属性。
/// <para>与 ABP 的差别: ABP 的基类带 <c>LazyServiceProvider</c> 与 Clock/GuidGenerator/LoggerFactory 等属性,
/// 原因是它的子类散落在无数第三方仓库里, 构造器签名不能变。Snow 的子类都在本仓内,
/// 依赖直接写进子类构造器 —— 显式、编译期可查、不留 service locator。</para>
/// </summary>
public abstract class DomainService : IDomainService
{
}
