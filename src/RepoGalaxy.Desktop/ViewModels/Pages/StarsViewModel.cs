using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using RepoGalaxy.Core.Models;
using RepoGalaxy.GitHub.Clients;

namespace RepoGalaxy.Desktop.ViewModels;

public sealed partial class StarsViewModel : ViewModelBase, ISearchablePage
{
    private readonly ILogger<StarsViewModel> _logger;
    private readonly GitHubApiClient _apiClient;
    private readonly RepositoryDetailsViewModel _details;
    private readonly List<RepositoryViewModel> _allRepositories = [];
    private string? _nextPageUrl;

    public ObservableCollection<RepositoryViewModel> Repositories { get; } = [];
    public ObservableCollection<string> Languages { get; } = ["全部语言"];
    public IReadOnlyList<string> SortOptions { get; } = ["最近 Star", "Stars 最多", "名称"];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isLoadingMore;
    [ObservableProperty] private bool _isAuthenticationRequired;
    [ObservableProperty] private string _statusMessage = "登录 GitHub 后即可读取你 Star 的仓库。";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedLanguage = "全部语言";
    [ObservableProperty] private string _selectedSort = "最近 Star";
    [ObservableProperty] private RepositoryViewModel? _selectedRepository;
    public bool HasMore => !string.IsNullOrEmpty(_nextPageUrl);
    public bool IsEmpty => !IsLoading && !IsAuthenticationRequired && Repositories.Count == 0;

    public StarsViewModel(ILogger<StarsViewModel> logger, GitHubApiClient apiClient, RepositoryDetailsViewModel details)
    {
        _logger = logger;
        _apiClient = apiClient;
        _details = details;
    }

    public void SetAuthenticationRequired()
    {
        IsAuthenticationRequired = true;
        StatusMessage = "登录 GitHub 后即可读取你 Star 的仓库。";
        Repositories.Clear();
        _allRepositories.Clear();
        _nextPageUrl = null;
        NotifyState();
    }

    public async Task LoadRepositoriesAsync()
    {
        if (IsLoading) return;
        try
        {
            IsAuthenticationRequired = false;
            IsLoading = true;
            StatusMessage = "正在加载 Star 仓库…";
            _allRepositories.Clear();
            _nextPageUrl = null;
            var page = await _apiClient.GetStarredRepositoriesPageAsync();
            AppendPage(page);
            StatusMessage = $"已加载 {_allRepositories.Count} 个 Star 仓库";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载 Star 仓库失败");
            StatusMessage = "加载失败，请检查网络或重新登录。";
            Repositories.Clear();
        }
        finally { IsLoading = false; NotifyState(); }
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsLoadingMore || !HasMore) return;
        try
        {
            IsLoadingMore = true;
            var page = await _apiClient.GetStarredRepositoriesPageAsync(_nextPageUrl);
            AppendPage(page);
            StatusMessage = $"已加载 {_allRepositories.Count} 个 Star 仓库";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载更多 Star 仓库失败");
            StatusMessage = "加载更多失败。";
        }
        finally { IsLoadingMore = false; NotifyState(); }
    }

    private void AppendPage(GitHubPage<Repository> page)
    {
        foreach (var repo in page.Items)
        {
            // 用 GitHubId 去重(Repository.Id=0 会让所有 repo 互相冲突)。
            if (_allRepositories.Any(x => x.Repository.GitHubId == repo.GitHubId)) continue;
            _allRepositories.Add(new RepositoryViewModel(repo));
        }
        _nextPageUrl = page.NextPageUrl;
        Languages.Clear();
        Languages.Add("全部语言");
        foreach (var language in _allRepositories.Select(x => x.PrimaryLanguage).Distinct(StringComparer.OrdinalIgnoreCase).Order())
            Languages.Add(language);
        ApplyFilter();
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadRepositoriesAsync();
    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedLanguageChanged(string value) => ApplyFilter();
    partial void OnSelectedSortChanged(string value) => ApplyFilter();
    partial void OnSelectedRepositoryChanged(RepositoryViewModel? value) { if (value is not null) _details.Show(value.Repository); }

    public void ClearDetailSelection(long? repositoryId)
    {
        if (repositoryId is null || SelectedRepository?.Repository.Id == repositoryId) SelectedRepository = null;
    }

    private void ApplyFilter()
    {
        IEnumerable<RepositoryViewModel> query = _allRepositories;
        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(x => x.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        if (SelectedLanguage != "全部语言")
            query = query.Where(x => x.PrimaryLanguage.Equals(SelectedLanguage, StringComparison.OrdinalIgnoreCase));
        query = SelectedSort switch
        {
            "Stars 最多" => query.OrderByDescending(x => x.Stars),
            "名称" => query.OrderBy(x => x.FullName),
            _ => query
        };
        Repositories.Clear();
        foreach (var item in query) Repositories.Add(item);
        NotifyState();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasMore));
    }
}