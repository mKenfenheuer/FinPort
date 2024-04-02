namespace FinPort.Models
{
    public class UpdateMessage
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Field { get; set; }
        public double Value { get; set; }
    }
}