using System;

namespace ObjectDumperConsoleApp.Model
{
    public class Person
    {
        public string Name { get; set; }

        public DateTime? VDateTime { get; set; }
        public int Age { get; set; }

        public Type PersonType { get; set; }

        public ModelDateTime BModelDate { get; set; }    
    }
}
