using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPort.Models;

public class PortfolioPosition
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? ISIN { get; set; }
    public double Quantity { get; set; }
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.00}")]
    public double LastPrice { get; set; }
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.00}")]
    public double PurchasePrice { get; set; }
    [NotMapped]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.00}")]
    public double PurchaseValue => Quantity * PurchasePrice;
    [NotMapped]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.00}")]
    public double Value => Quantity * LastPrice;
    [NotMapped]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.00}")]
    public double Change => (LastPrice - PurchasePrice) / PurchasePrice * 100;
    public DateTime LastPriceDate { get; set; }
    public string? PortfolioId { get; set; }
    [ForeignKey(nameof(PortfolioId))]
    public Portfolio? Portfolio { get; set; }
}