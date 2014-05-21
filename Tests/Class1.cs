using System;

namespace Test
{
    public static class Program
    {
        public static void Main()
        {
            int temp = 100;
            string t2 = "lol {0}";
            string tmp;
            Console.Write(t2, temp);
            tmp = temp.ToString();
            tmp += " ";
            Console.Write(tmp, t2);
        }
    }
}
