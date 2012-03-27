namespace PostSharp.Toolkit.Tests
{
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        private static string GetFirstName(Person person)
        {
            return person.FirstName;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", GetFirstName(this), this.LastName);
        } 
    }
}