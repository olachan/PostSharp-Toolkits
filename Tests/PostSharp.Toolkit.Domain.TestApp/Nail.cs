using System;

namespace PostSharp.Toolkit.Domain.TestApp
{
    public class Nail : EditableModelBase
    {
        public double Length { get; private set; }

        public double StickingLength { get; private set; }

        public string Color { get; set; }

        private static readonly Random _random = new Random();

        public Nail( double length)
        {
            this.Length = length;
            this.StickingLength = length;
            this.Color = "Black";
        }

        public double Hit(double hammerEfficiency)
        {
            return this.StickingLength = Math.Max(0, this.StickingLength - hammerEfficiency*.1);
        }
    }
}