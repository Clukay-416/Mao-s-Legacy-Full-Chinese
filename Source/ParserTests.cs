using System;
using System.IO;
using System.Reflection;
using System.Collections;
public class ParserTests {
 public static void Main(string[] args){
  var asm=Assembly.LoadFrom(args[0]);
  foreach(var spec in new string[][]{new[]{"LFKG.FocusReader","ReadFocuses","focuses",args[1]},new[]{"LEGK.EventReader","ReadEvents","events",args[2]}}){
   var type=asm.GetType(spec[0],true);type.GetMethod("CreateDictionary",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
   type.GetMethod(spec[1],BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{File.ReadAllText(spec[3])});
   var dict=(IDictionary)type.GetField(spec[2],BindingFlags.Public|BindingFlags.Static).GetValue(null);if(dict.Count<1)throw new Exception("Empty dictionary");
   foreach(var key in dict.Keys)if(string.IsNullOrWhiteSpace((string)key))throw new Exception("Invalid internal key");
   Console.WriteLine("PASS original "+spec[0]+" parsed translated resources, entries="+dict.Count);
  }
 }
}
