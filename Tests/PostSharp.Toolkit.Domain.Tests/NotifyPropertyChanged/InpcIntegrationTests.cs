#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.ComponentModel;
using System.Text;

using NUnit.Framework;

namespace PostSharp.Toolkit.Domain.Tests.NotifyPropertyChanged
{
    [TestFixture]
    public class InpcIntegrationTests
    {
        [Test]
        public void FirstNameChange_GeneratesEvents()
        {
            var cust = new Customer();
            var viewModel = new CustomerModelView( cust );
            TestHelpers.DoInpcTest( viewModel, vm =>
                {
                    vm.FirstName = "Test";
                }, 1, "FirstName");
        }

        [Test]
        public void FirstNameChange_GeneratesBusinessCardChangeNotification()
        {
            var cust = new Customer();
            var viewModel = new CustomerModelView(cust);
            TestHelpers.DoInpcTest(viewModel, vm =>
            {
                vm.FirstName = "Test";
            }, 1, "BusinessCard");
        }

        [Test]
        public void AccessingGetter_InBetweenEvents_Works()
        {
            var cust = new Customer();
            var viewModel = new CustomerModelView(cust);

            ((INotifyPropertyChanged) viewModel).PropertyChanged += ( s, e ) =>
                {
                    Assert.IsNotEmpty( viewModel.FirstName );
                    Assert.IsNull( viewModel.Address );
                };

            viewModel.FirstName = "Test";
        }
    }



    [NotifyPropertyChanged]
    class CustomerModelView
    {
        private Customer customer;

        public CustomerModelView(Customer customer)
        {
            this.customer = customer;
        }

        public string FirstName
        {
            get { return this.customer.FirstName; }
            set { this.customer.FirstName = value; }
        }

        public string LastName
        {
            get { return this.customer.LastName; }
            set { this.customer.LastName = value; }
        }

        public Address Address { get { return this.customer.Address; } }

        public string FullName { get { return this.customer.FullName; } }

        public string BusinessCard
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(this.FirstName);
                stringBuilder.Append(" ");
                stringBuilder.Append(this.LastName);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(this.customer.Address.Line1);
                string line2 = this.customer.Address.Line2;
                if (!string.IsNullOrWhiteSpace(line2))
                {
                    stringBuilder.AppendLine(line2);
                }
                stringBuilder.Append(this.customer.Address.PostalCode);
                stringBuilder.Append(' ');
                stringBuilder.Append(this.customer.Address.Town);

                return stringBuilder.ToString();
            }
        }


    }


    [NotifyPropertyChanged]
    class Entity
    {

    }

    class Customer : Entity
    {
        private string _lastName;
        private Address _address;

        public string FirstName { get; set; }

        public string LastName
        {
            get { return this._lastName; }
            set { this._lastName = value; }
        }

        public Address Address
        {
            get { return this._address; }
            set { this._address = value; }
        }

        [NotifyPropertyChangedIgnore]
        public string FullName { get { return this.GetFullName(); } }

        public string GetFullName()
        {
            return string.Join(" ", this.FirstName, this.LastName);
        }

        public void Reset()
        {
            this.FirstName = null;
            this._lastName = null;
            this._address = null;
        }


    }

    class Address : Entity
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Town { get; set; }
        public string PostalCode { get; set; }
    }
}