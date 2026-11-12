using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// Subscription plan — pay monthly or annually (annual = discount).
public class Sub
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Type { get; set; } = "";

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 10000)]
    public decimal PricePerMonth { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 100000)]
    public decimal PricePerYear { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = "";

    public bool IncludesPrivateSessions { get; set; }

    [Range(0, 999)]
    public int MaxSessionsPerMonth { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
