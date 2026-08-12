using Domain.Common;
using Domain.ContentManagement.Entities;

namespace IceBot.UnitTests.ContentManagement;

public sealed class ContentPageTests
{
    [Fact]
    public void Published_Revision_Is_Immutable_When_Draft_Changes()
    {
        var actor = Guid.NewGuid();
        var page = ContentPage.Create("privacy-policy", "privacy-policy", "Privacy v1", "<p>v1</p>", actor, DateTimeOffset.UtcNow);
        var published = page.Publish(actor, page.Revision, DateTimeOffset.UtcNow);

        page.UpdateDraft("Privacy v2", "<p>v2</p>", page.Revision, actor, DateTimeOffset.UtcNow);

        Assert.Equal("Privacy v1", published.Title);
        Assert.Equal("<p>v1</p>", published.BodyHtml);
        Assert.Equal("Privacy v2", page.DraftTitle);
        Assert.Throws<DomainRuleException>(() => page.Publish(actor, 1, DateTimeOffset.UtcNow));
    }
}
