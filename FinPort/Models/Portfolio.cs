using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPort.Models;

public class Portfolio
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    [NotMapped]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.00}")]
    public double? Value => Positions?.Sum(p => p.Value);
    [NotMapped]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.00}")]
    public double? Change => (Positions?.Sum(p => p.Value) - Positions?.Sum(p => p.PurchaseValue)) / Positions?.Sum(p => p.PurchaseValue) * 100;
    public List<PortfolioPosition>? Positions { get; set; }

}
