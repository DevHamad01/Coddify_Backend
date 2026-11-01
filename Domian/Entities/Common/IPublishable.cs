namespace Domian.Entities.Common
{
    /// <summary>Admin-authored content: drafts stay hidden until published; archived items vanish for learners but keep their progress.</summary>
    public interface IPublishable
    {
        bool IsPublished { get; set; }
        bool IsArchived { get; set; }
    }
}
