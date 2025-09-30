功能 :登入登出註冊 文章CRUD 管理員分層
   * 瀏覽文章列表:
   * 發表新文章 (需要登入):
   * 查看文章詳情與回覆:
   * 註冊與登入流程:
   * 權限控制機制:
	* 
### 🚀 如何升級為管理員

若你使用的帳號名稱是 `iver`，可透過以下步驟將自己升級為管理員：

1. 前往註冊頁面，**使用「iver」作為使用者名稱**註冊新帳號
2. 註冊成功並登入後，進入以下網址：/Registers/MakeMeAdmin

## BUG日誌
ForumDbContext 最佳實作（避免多重 Cascade Path）
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

        // 拆分後的檢舉
        public DbSet<PostReport> PostReports { get; set; }
        public DbSet<ReplyReport> ReplyReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ------------------
            // Post 與 Register
            // ------------------
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Register)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.RegisterId)
                .OnDelete(DeleteBehavior.Cascade); // 刪會員 → 刪文章

            // ------------------
            // Reply 與 Post / Register
            // ------------------
            modelBuilder.Entity<Reply>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Replies)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade); // 刪文章 → 刪留言

            modelBuilder.Entity<Reply>()
                .HasOne(r => r.Register)
                .WithMany()
                .HasForeignKey(r => r.RegisterId)
                .OnDelete(DeleteBehavior.NoAction); // 避免多重 Cascade

            // ------------------
            // PostVote (推/噓)
            // ------------------
            modelBuilder.Entity<PostVote>()
                .HasOne(v => v.Post)
                .WithMany(p => p.PostVotes)
                .HasForeignKey(v => v.PostId)
                .OnDelete(DeleteBehavior.Cascade); // 刪文章 → 刪投票

            modelBuilder.Entity<PostVote>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.NoAction); // 避免多重 Cascade

            // ------------------
            // PostReport
            // ------------------
            modelBuilder.Entity<PostReport>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Reports)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostReport>()
                .HasOne(r => r.Reporter)
                .WithMany(u => u.PostReportsFiled)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.NoAction);

            // ------------------
            // ReplyReport
            // ------------------
            modelBuilder.Entity<ReplyReport>()
                .HasOne(r => r.Reply)
                .WithMany(rp => rp.Reports)
                .HasForeignKey(r => r.ReplyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReplyReport>()
                .HasOne(r => r.Reporter)
                .WithMany(u => u.ReplyReportsFiled)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

📝 備忘筆記：多重 Cascade Path Bug 解法

原則：一個 FK 可以 Cascade，但不能讓兩條路徑都 Cascade 到同一張表。

比如：刪除 Register → 刪除 Post → 刪除 Reply
又有 Register → Reply Cascade，就爆了。

解法套路：

保留「內容物的層級關係」Cascade
（Post 刪了 → Reply 跟 PostVote 自然刪）

對 Register 這種「人」的 FK → 一律用 NoAction

因為「人」不是內容物，刪掉帳號不代表內容一定要 cascade 刪除。

常見修正：

Reply.RegisterId → NoAction

PostVote.UserId → NoAction

Report.ReporterId → NoAction