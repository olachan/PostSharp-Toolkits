using System;

namespace PostSharp.Toolkit.Tests
{
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public static string GetFirstName(Person person)
        {
            return person.FirstName;
        }

        public static string GetFullName(Person person)
        {
            return person.ToString();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Person.GetFirstName(this), this.LastName);
        }
    }
}
