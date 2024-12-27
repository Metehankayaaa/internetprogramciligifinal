    namespace WebUı.Models
{
    public class News
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImagePath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public int ViewCount { get; set; } = 0;

        // Foreign Key
        public int CategoryId { get; set; }
        public string UserId { get; set; } // Haberi ekleyen kullanıcı

        // Navigation Properties
        public Category Category { get; set; }
        public AppUser User { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }
}
