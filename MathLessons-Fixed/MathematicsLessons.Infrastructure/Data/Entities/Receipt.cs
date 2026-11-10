using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

public class Receipt
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Discount { get; set; }

    [MaxLength(20)]
    public string Commision { get; set; } = "0%";

    [Required, MaxLength(40)]
    public string Kind { get; set; } = "Session";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
