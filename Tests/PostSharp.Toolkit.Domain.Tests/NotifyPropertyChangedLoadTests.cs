using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace PostSharp.Toolkit.Domain.Tests
{
    [TestFixture]
    class NotifyPropertyChangedLoadTests
    {
        [Test]
        public void SimplePerformanceCounterTest()
        {
            Stopwatch stopwatch = new Stopwatch();
            ViewModelNoAutoInpc vm = new ViewModelNoAutoInpc();

            stopwatch.Start();
            vm = new ViewModelNoAutoInpc();


            foreach ( var i in Enumerable.Repeat( 0, 20 ) )
            {
                vm.ChangeName();
                vm.ChangeSalary();
            }

            foreach (var i in Enumerable.Repeat(0, 20))
            {
                vm.Model.FirstName = "skdalfj";
                vm.OnPropertyChanged( "NameAndSalary" );
                vm.Model.LastName = "sklgjkls";
                vm.OnPropertyChanged( "NameAndSalary" );
            }
            vm.Model.Salary = 21;
            vm.OnPropertyChanged("NameAndSalary");
            long fib = vm.Model.Fibonacci10;
            double fibAvg = vm.Model.Fibonacci30Average;


            stopwatch.Stop();
            long noAutoElapsedTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Reset();

            ViewModelAutoInpc autovm = new ViewModelAutoInpc();

            stopwatch.Start();
            autovm = new ViewModelAutoInpc();


            foreach (var i in Enumerable.Repeat(0, 20))
            {
                autovm.ChangeName();
                autovm.ChangeSalary();
            }
            
            foreach (var i in Enumerable.Repeat(0, 20))
            {
                autovm.Model.FirstName = DateTime.Now.Ticks.ToString();
                autovm.Model.LastName = DateTime.Now.Ticks.ToString();
            }
            autovm.Model.Salary = 21;
            fib = autovm.Model.Fibonacci10;
            fibAvg = vm.Model.Fibonacci30Average;

            stopwatch.Stop();
            long autoElapsedTime = stopwatch.ElapsedMilliseconds;

            Debug.WriteLine("Manual INPC time: {0} \n Automatic INPC time: {1}", noAutoElapsedTime, autoElapsedTime);

            Assert.Less( autoElapsedTime, 4*noAutoElapsedTime );
        }
    }


    
    class ViewModelNoAutoInpc : INotifyPropertyChanged
    {
        public ViewModelNoAutoInpc()
        {
            this.Model = new ModelNoAutoInpc();
        }

        private ModelNoAutoInpc model;

        public ModelNoAutoInpc Model
        {
            get
            {
                return this.model;
            }
            set
            {
                this.model = value;
                this.OnPropertyChanged( "Model" );
            }
        }

        public string NameAndSalary 
        { 
            get
            {
                return string.Format( "{0}: {1}", this.Model.FullName, this.Model.Salary );
            } 
        }

        public void ChangeSalary()
        {
            this.Model.Salary = DateTime.Now.Ticks;
            this.OnPropertyChanged("NameAndSalary");
        }

        public void ChangeName()
        {
            this.Model.FirstName = DateTime.Now.Ticks.ToString();
            this.OnPropertyChanged("NameAndSalary");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged( string propertyName )
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if ( handler != null )
            {
                handler( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }
    }

    class ModelNoAutoInpc : INotifyPropertyChanged
    {
        private string firstName;

        private string lastName;

        private decimal salary;

        public string FirstName
        {
            get
            {
                return this.firstName;
            }
            set
            {
                this.firstName = value;
                this.OnPropertyChanged( "FirstName" );
                this.OnPropertyChanged( "FullName" );
            }
        }

        public string LastName
        {
            get
            {
                return this.lastName;
            }
            set
            {
                this.lastName = value;
                this.OnPropertyChanged( "LastName" );
                this.OnPropertyChanged( "FullName" );
            }
        }

        public string FullName 
        {
            get
            {
                return string.Format( "{0} {1}", this.FirstName, this.LastName );
            }
        }

        public decimal Salary
        {
            get
            {
                return this.salary;
            }
            set
            {
                this.salary = value;
                this.OnPropertyChanged( "Salary" );
            }
        }

        public long Fibonacci10
        {
            get
            {
                return this.Fibonacci().Skip( 9 ).First();
            }
        }

        public double Fibonacci30Average
        {
            get
            {
                return this.Fibonacci().Take(30).Average();
            }
        }

        IEnumerable<long> Fibonacci()
        {
            long n = 0, m = 1;

            yield return 0;
            yield return 1;
            while ( true )
            {
                long tmp = n + m;
                n = m;
                m = tmp;
                yield return m;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged( string propertyName )
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if ( handler != null )
            {
                handler( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }
    }

    [NotifyPropertyChanged]
    class ViewModelAutoInpc
    {
        public ViewModelAutoInpc()
        {
            this.Model = new ModelAutoInpc();
        }

        public ModelAutoInpc Model { get; set; }

        [DependsOn("Model.FullName", "Model.Salary")]
        public string NameAndSalary
        {
            get
            {
                return string.Format("{0}: {1}", this.Model.FullName, this.Model.Salary);
            }
        }

        public void ChangeSalary()
        {
            this.Model.Salary = DateTime.Now.Ticks;
        }

        public void ChangeName()
        {
            this.Model.FirstName = DateTime.Now.Ticks.ToString();
        }
    }

    [NotifyPropertyChanged]
    class ModelAutoInpc
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName
        {
            get
            {
                return string.Format("{0} {1}", this.FirstName, this.LastName);
            }
        }

        public decimal Salary { get; set; }

        public long Fibonacci10
        {
            get
            {
                return this.Fibonacci().Skip(9).First();
            }
        }

        public double Fibonacci30Average
        {
            get
            {
                return this.Fibonacci().Take(30).Average();
            }
        }

        [IdempotentMethod]
        IEnumerable<long> Fibonacci()
        {
            long n = 0, m = 1;

            yield return 0;
            yield return 1;
            while (true)
            {
                long tmp = n + m;
                n = m;
                m = tmp;
                yield return m;
            }
        }
    }
}
