using Drangit.Web.Repositories;

namespace Drangit.Tests.Builders;

/// <summary>
/// Construit un <see cref="Repository"/> valide par défaut ; chaque test ne déclare que ce
/// qui compte pour lui.
/// </summary>
internal sealed class RepositoryBuilder
{
    private string _name = "un-depot";
    private long _id = 1;
    private LocalizedText _description = LocalizedText.Empty;
    private LocalizedText _longDescription = LocalizedText.Empty;
    private string? _language = "C#";
    private IReadOnlyList<string> _topics = [];
    private int _stars;
    private int _forks;
    private DateTimeOffset? _pushedAt = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private DateTimeOffset? _createdAt = new(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private string? _license;
    private Uri? _homepageUrl;
    private IReadOnlyList<Screenshot> _screenshots = [];
    private bool _isArchived;
    private bool _isFork;
    private bool _isFeatured;

    public RepositoryBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RepositoryBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public RepositoryBuilder WithDescription(LocalizedText description)
    {
        _description = description;
        return this;
    }

    public RepositoryBuilder WithLongDescription(LocalizedText longDescription)
    {
        _longDescription = longDescription;
        return this;
    }

    public RepositoryBuilder WithLanguage(string? language)
    {
        _language = language;
        return this;
    }

    public RepositoryBuilder WithTopics(params string[] topics)
    {
        _topics = topics;
        return this;
    }

    public RepositoryBuilder WithStars(int stars)
    {
        _stars = stars;
        return this;
    }

    public RepositoryBuilder WithForks(int forks)
    {
        _forks = forks;
        return this;
    }

    public RepositoryBuilder WithPushedAt(DateTimeOffset? pushedAt)
    {
        _pushedAt = pushedAt;
        return this;
    }

    public RepositoryBuilder WithCreatedAt(DateTimeOffset? createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public RepositoryBuilder WithLicense(string? license)
    {
        _license = license;
        return this;
    }

    public RepositoryBuilder WithDemo(string homepageUrl = "https://demo.example/app")
    {
        _homepageUrl = new Uri(homepageUrl);
        return this;
    }

    public RepositoryBuilder WithScreenshots(params string[] urls)
    {
        _screenshots = [.. urls.Select(url => new Screenshot(new Uri(url), LocalizedText.Empty))];
        return this;
    }

    public RepositoryBuilder Archived()
    {
        _isArchived = true;
        return this;
    }

    public RepositoryBuilder Forked()
    {
        _isFork = true;
        return this;
    }

    public RepositoryBuilder Featured()
    {
        _isFeatured = true;
        return this;
    }

    public Repository Build() => new()
    {
        Id = _id,
        Slug = RepositorySlug.FromName(_name, _id),
        Name = _name,
        FullName = $"drangoht/{_name}",
        HtmlUrl = new Uri($"https://github.com/drangoht/{_name}"),
        Description = _description,
        LongDescription = _longDescription,
        Language = _language,
        Topics = _topics,
        Stars = _stars,
        Forks = _forks,
        PushedAt = _pushedAt,
        CreatedAt = _createdAt,
        License = _license,
        HomepageUrl = _homepageUrl,
        IsArchived = _isArchived,
        IsFork = _isFork,
        IsFeatured = _isFeatured,
        Screenshots = _screenshots,
    };
}
