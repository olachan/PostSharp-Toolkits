using System;
using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.TestApp
{
    public class Toolbox : ModelBase
    {
        private static readonly Random _random = new Random();

        [NestedTrackedObject]
        public Hammer Hammer { get; set; }

        [NestedTrackedObject]
        public TrackedCollection<Nail> Nails { get; private set; }

        public Toolbox()
        {
            this.Nails = new TrackedCollection<Nail>();
            for (int i = 0; i < _random.Next(13); ++i )
            {
                this.Nails.Add(new Nail(_random.Next(200)));
            }
            this.CreateNewHammer();
        }

        public void CreateNewHammer()
        {
            var hammer = new Hammer() { Length = _random.Next(29), Weight = _random.Next(113) };
            
            this.Hammer = hammer;
        }
    }
}