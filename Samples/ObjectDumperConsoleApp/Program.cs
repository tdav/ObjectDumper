using System;
using System.Collections.Generic;
using ObjectDumperConsoleApp.Model;

namespace ObjectDumperConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var persons = new List<Person>
            {
                new Person { Name = "John", VDateTime=DateTime.Now, Age = 20, BModelDate=DateTime.Now },
                new Person { Name = "Thomas", Age = 30, BModelDate=new ModelDateTime()  },                
                new Person { Name = "Thomas", Age = 30 },                
            };

            //var personsDump = ObjectDumper.Dump(persons, DumpStyle.CSharp);

            var domp = new DumpOptions()
            {
                ForWeb=true,
                OnlyValues = false,
                NullValue = "",
                LineBreakChar = string.Empty,
                IgnoreDefaultValues = true,
                PropertyOrderBy = x=>x.Name
            };
            var personsDump = ObjectDumper.Dump(persons, domp);

            Console.WriteLine(personsDump);
            Console.ReadLine();
        }
    }
}
