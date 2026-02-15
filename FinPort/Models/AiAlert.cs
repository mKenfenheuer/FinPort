using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPort.Models;

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public class AiAlert
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; set; }
    public string? PortfolioId { get; set; }
    [ForeignKey(nameof(PortfolioId))]
    public Portfolio? Portfolio { get; set; }
    public string? PositionId { get; set; }
    [ForeignKey(nameof(PositionId))]
    public PortfolioPosition? Position { get; set; }
    public AlertSeverity Severity { get; set; }
    public string? Title { get; set; }
    public string? Analysis { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsNotified { get; set; }
}
