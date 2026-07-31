using RepoGalaxy.Core.Models;

namespace RepoGalaxy.Core.Interfaces;

/// <summary>
/// 仓库服务接口
/// </summary>
public interface IRepositoryService
{
    // 查询
    Task<Repository?> GetByIdAsync(long id);
    Task<Repository?> GetByFullNameAsync(string owner, string name);
    Task<IEnumerable<Repository>> GetAllAsync();
    Task<IEnumerable<Repository>> GetBookmarksAsync();
    Task<IEnumerable<Repository>> SearchAsync(string keyword);
    
    // 发现流
    Task<IEnumerable<Repository>> GetDiscoveryFeedAsync(int page = 1, int pageSize = 50);
    Task<IEnumerable<Repository>> GetTrendingAsync(string? language = null, string since = "daily");
    
    // 本地操作
    Task<bool> BookmarkAsync(long repositoryId, string collection = "默认收藏");
    Task<bool> UnbookmarkAsync(long repositoryId);
    Task<bool> IgnoreAsync(long repositoryId);
    Task RecordViewAsync(long repositoryId, ViewSource source, TimeSpan? duration = null);

    // 解析:给定一个 Repository model(可能来自 API、Id 还未持久化),
    // 找到或插入对应的 Repositories 行,返回 db 主键。
    // 用于修复 GitHub DTO 创建的 Repository(Id=0)与 db 持久化 Repository(Id=N)不一致的问题。
    Task<long> ResolveRepositoryIdAsync(Repository model, CancellationToken cancellationToken = default);
    
    // 缓存
    Task<IEnumerable<Repository>> GetCachedAsync(TimeSpan? maxAge = null);
    Task RefreshCacheAsync();
    Task ClearCacheAsync();
}
