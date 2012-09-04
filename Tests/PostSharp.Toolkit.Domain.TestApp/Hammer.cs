namespace PostSharp.Toolkit.Domain.TestApp
{
    public class Hammer : ModelBase
    {
        public double Weight { get; set; }
        public double Length { get; set; }

        public double Efficiency
        {
            get { return this.Weight*this.Length; }
        }
    }
}