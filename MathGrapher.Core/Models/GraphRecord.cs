namespace MathGrapher.Core.Models
{
    public class GraphRecord
    {
        public int Id { get; set; }
        public required string Expression { get; set; }
        public double XMin { get; set; }
        public double XMax { get; set; }
        public double Step { get; set; }
        public double? Integral { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}