using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Domain.Domain.Entity
{
    public class TodayNewsArticle
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Full processed article JSON including the injected imageUrl property.
        /// </summary>
        [Required]
        public string JsonData { get; set; } = string.Empty;

        /// <summary>
        /// Total number of articles stored in the JsonData array.
        /// </summary>
        public int ArticleCount { get; set; }

        /// <summary>
        /// UTC timestamp of when this record was inserted.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
