using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPort.Models;

public class Setting
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string? Key { get; set; }
    public string? Value { get; set; }
}