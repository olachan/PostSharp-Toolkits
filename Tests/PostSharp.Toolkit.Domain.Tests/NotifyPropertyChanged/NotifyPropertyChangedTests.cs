using NUnit.Framework;

namespace PostSharp.Toolkit.Domain.Tests.NotifyPropertyChanged
{
    [TestFixture]
    class NotifyPropertyChangedTests
    {
        [Test]
        public void SetFieldsViaSeparateMethodTest()
        {
            TestHelpers.DoInpcTest<MainWindowViewModel>(
                c =>
                {
                    c.User = new UserDto();
                    c.MoveCountry();
                },
                2,
                "UserCountry");
        }

        [NotifyPropertyChanged]
        public class MainWindowViewModel
        {
            public UserDto User { get; set; }

            [DependsOn("User.Address.Country")]
            public string UserCountry
            {
                get
                {
                    if (this.User == null)
                    {
                        return "Unknown";
                    }
                    return this.User.Address.Country;
                }
            }

            public void LoadUserData()
            {
                this.User = new UserDto();
            }

            public void MoveCountry()
            {
                this.User.Address.Country = "USA";
            }
        }



        [NotifyPropertyChanged]
        public class UserDto
        {
            private Address address = new Address("Hungary");

            public Address Address
            {
                get { return this.address; }
            }

        }

        [NotifyPropertyChanged]
        public class Address
        {
            public string Country { get; set; }

            public Address(string country)
            {
                this.Country = country;
            }
        }
    }
}
