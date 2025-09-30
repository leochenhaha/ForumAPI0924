namespace ForumWebsite.Models
{
    public enum ModerationAction
    {
        None = 0,
        DeletePost = 1,
        DeleteReply = 2,
        WarnUser = 3,
        SuspendUser = 4,
        BanUser = 5
    }
}