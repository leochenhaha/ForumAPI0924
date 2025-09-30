using Microsoft.EntityFrameworkCore;

namespace ForumWebsite.Models
{
    public class ForumDbContext : DbContext
    {
        public ForumDbContext(DbContextOptions<ForumDbContext> options) : base(options) { }

        public DbSet<Register> Register { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PostVote> PostVotes { get; set; }

        // 拆分後的兩張檢舉表
        public DbSet<PostReport> PostReports { get; set; }
        public DbSet<ReplyReport> ReplyReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PostReport → Post（刪文時連帶刪此文的檢舉）
            modelBuilder.Entity<PostReport>()
                .HasOne(r => r.Post)
                .WithMany()                     // 不使用 Post 的反向導航屬性
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // PostReport → Reporter（保留檢舉人，避免多重級聯）
            modelBuilder.Entity<PostReport>()
                .HasOne(r => r.Reporter)
                .WithMany()                     // 不使用 Register 的反向導航屬性
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.NoAction);

            // ReplyReport → Reply（刪留言時連帶刪該留言的檢舉）
            modelBuilder.Entity<ReplyReport>()
                .HasOne(r => r.Reply)
                .WithMany()                     // 不使用 Reply 的反向導航屬性
                .HasForeignKey(r => r.ReplyId)
                .OnDelete(DeleteBehavior.Cascade);

            // ReplyReport → Reporter
            modelBuilder.Entity<ReplyReport>()
                .HasOne(r => r.Reporter)
                .WithMany()                     // 不使用 Register 的反向導航屬性
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.NoAction);

            // PostVote → Post (Cascade)
            modelBuilder.Entity<PostVote>()
                .HasOne(v => v.Post)
                .WithMany(p => p.PostVotes)
                .HasForeignKey(v => v.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // PostVote → User (NoAction)
            modelBuilder.Entity<PostVote>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Reply → Register (NoAction，避免多重 Cascade Path)
            modelBuilder.Entity<Reply>()
                .HasOne(r => r.Register)
                .WithMany()
                .HasForeignKey(r => r.RegisterId)
                .OnDelete(DeleteBehavior.NoAction);

        }
    }
}
