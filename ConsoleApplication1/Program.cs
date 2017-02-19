using System;
using MD.Framework.Utility;

namespace ConsoleApplication1
{
	class MyClass
	{
		public int Prop1 { get; set; }
		public int Prop2 { get; set; }
		public int Prop3 { get; set; }
		public int Prop4 { get; set; }
	}

	class Program
	{
		public string Prop1 { get; set; }

		static void Main(string[] args)
		{

			Console.ReadKey();
		}

		void Sort()
		{
			var test = SortCondition<MyClass>.OrderBy(q => q.Prop1).ThenBy(q => q.Prop2);
		}

	}
}
