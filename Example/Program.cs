using System;
using Newtonsoft;

namespace Example
{
  class P1
  {
    public static int Val1 = 1;
    public static int Val2 = 1;

    public static void RefTest()
    {
      P1.CallByValue(P1.Val1);
      P1.CallByRef(ref P1.Val2);
    }

    private static void CallByValue(int val)
    {
      Program.Log($"Val1: {P1.Val1} | val: {val}");
      val++;
      Program.Log($"Val1: {P1.Val1} | val: {val}");
      /**
       * Size: 1 | val: 1
       * Size: 1 | val: 2
       * > 기본적으로 Primitive는 CallByValue 같음
       */
    }

    private static void CallByRef(ref int val)
    {
      Program.Log($"Val2: {P1.Val2} | val: {val}");
      val++;
      Program.Log($"Val2: {P1.Val2} | val: {val}");
    }

    static int[] arr1 = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0 };
    static int[] arr2 = new int[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0 };

    public static void ArrSum()
    {
      // arr1 + arr2;
      int[] result = new int[arr1.Length];
      for (var i = 0; i < arr1.Length; i++)
      {
        result[i] = arr1[i] + arr2[i];
      }

      Program.Log(result);
    }
  }

  class Program
  {

    static void Main(string[] args)
    {
      Log("GoodBye World!");
      Log(new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0 });

      P1.RefTest();
      P1.ArrSum();
    }

    public static void Log<T>(T obj)
    {
      if (obj == null)
      {
        Console.WriteLine();
      }

      Type t = typeof(T);
      if (t.IsPrimitive || t == typeof(Decimal) || t == typeof(String))
      {
        Console.WriteLine($"{obj}");
      }
      else
      {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
      }
    }
  }
}
