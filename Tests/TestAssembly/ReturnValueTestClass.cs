namespace TestAssembly
{
    public class ReturnValueTestClass
    {
        public void VoidMethod()
        {
        }

        public string ReturnsHelloString()
        {
            return "Hello";
        }

        public int ReturnsIntValue42()
        {
            return 42;
        }

        public Product ReturnsProduct()
        {
            return new Product
            {
                Id = 1,
                Name = "Test"
            };
        }

        public object ReturnsProductAsObject()
        {
            return this.ReturnsProduct();
        }

        public object ReturnsBoxedInt()
        {
            return this.ReturnsIntValue42();
        }

        public MyStruct ReturnsStruct()
        {
            return new MyStruct("MyValue");
        }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public struct MyStruct
    {
        public readonly string Value;

        public MyStruct(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}