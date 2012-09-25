using System;

namespace PostSharp.Toolkit.Domain.TestApp
{
    public class Nail : EditableModelBase
    {
        public double Length { get; private set; }

        public double StickingLength { get; private set; }

        public string Color { get; set; }

        private static readonly Random _random = new Random();

        public Nail()
        {
            this.Length = _random.Next(200);
            this.StickingLength = this.Length;
            this.Color = "Black";
        }

        public double Hit(double hammerEfficiency)
        {
            return this.StickingLength = Math.Max(0, this.StickingLength - hammerEfficiency*.1);
        }
    }
}