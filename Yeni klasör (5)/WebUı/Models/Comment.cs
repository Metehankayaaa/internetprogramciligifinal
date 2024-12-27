namespace WebUı.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Foreign Keys
        public int NewsId { get; set; }
        public string UserId { get; set; }

        // Navigation Properties
        public News News { get; set; }
        public AppUser User { get; set; }
    }
}
