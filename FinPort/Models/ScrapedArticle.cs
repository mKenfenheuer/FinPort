using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPort.Models;

public class ScrapedArticle
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; set; }
    public string? PositionId { get; set; }
    [ForeignKey(nameof(PositionId))]
    public PortfolioPosition? Position { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Summary { get; set; }
    public string? Source { get; set; }
    public DateTime ScrapedAt { get; set; }
}
