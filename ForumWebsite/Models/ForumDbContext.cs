using Microsoft.EntityFrameworkCore;
using ForumWebsite.Models;

namespace ForumWebsite
{
    public class ForumDbContext : DbContext
    {
        public ForumDbContext(DbContextOptions<ForumDbContext> options)
            : base(options)
        {
        }

        public DbSet<Register> Register { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostVote> PostVotes { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reply>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Replies)
                .HasForeignKey(r => r.PostId);
        }



    }
}
