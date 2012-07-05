using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PostSharp.Toolkit.INPC
{
    internal class ChangedPropertiesAccumulator : IEnumerable<WeakPropertyDescriptor>
    {
        private IList<WeakPropertyDescriptor> changedProperties = new List<WeakPropertyDescriptor>();
        

        public void AddProperty(object obj, string propertyName)
        {
            foreach ( WeakPropertyDescriptor weakPropertyDescriptor in changedProperties )
            {
                if (weakPropertyDescriptor.Instance.IsAlive && ReferenceEquals( weakPropertyDescriptor.Instance.Target, obj ) && weakPropertyDescriptor.PropertyName == propertyName)
                {
                    return;
                }
            }

            changedProperties.Add( new WeakPropertyDescriptor( obj, propertyName ) );
        }

        public void Remove(WeakPropertyDescriptor propertyDescriptor)
        {
            this.changedProperties.Remove( propertyDescriptor );
        }

        
        public void Compact()
        {
            var deadObjects = changedProperties.Where( w => !w.Instance.IsAlive ).ToList();
            foreach ( WeakPropertyDescriptor weakPropertyDescriptor in deadObjects )
            {
                changedProperties.Remove( weakPropertyDescriptor );
            }
        }

        public IEnumerator<WeakPropertyDescriptor> GetEnumerator()
        {
            return this.changedProperties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
