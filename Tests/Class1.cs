using System;

namespace Test
{
    public static class Program
    {
        public static void Main()
        {
            int temp = 100;
            string t2 = "lol {0}";
            Console.Write(t2, temp);
            Console.Write(temp.ToString(), t2);
        }
    }
}
