using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
public class Verify {
 static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> ts){foreach(var t in ts){yield return t;foreach(var n in Types(t.NestedTypes))yield return n;}}
 public static void Main(string[] args){
  var a=ModuleDefinition.ReadModule(args[0]);var b=ModuleDefinition.ReadModule(args[1]);
  var original=Types(a.Types).ToDictionary(t=>t.FullName);var patched=Types(b.Types).ToDictionary(t=>t.FullName);
  if(!original.Keys.SequenceEqual(patched.Keys))throw new Exception("Game type definitions changed");
  int checkedMethods=0;
  foreach(var name in original.Keys){var t=original[name];var u=patched[name];
   if(!t.Fields.Select(f=>f.FullName).SequenceEqual(u.Fields.Select(f=>f.FullName)))throw new Exception("Game fields changed: "+name);
   if(!t.Methods.Select(m=>m.FullName).SequenceEqual(u.Methods.Select(m=>m.FullName)))throw new Exception("Method signatures changed: "+name);
   for(int i=0;i<t.Methods.Count;i++){var m=t.Methods[i];var n=u.Methods[i];if(!m.HasBody)continue;
    if(m.Name=="Text"&&m.ReturnType.FullName=="System.String"&&m.Parameters.Count>=2&&m.Parameters[0].ParameterType.FullName=="System.String")continue;
    var x=m.Body.Instructions.Where(z=>z.OpCode==OpCodes.Ldstr).Select(z=>(string)z.Operand);
    var y=n.Body.Instructions.Where(z=>z.OpCode==OpCodes.Ldstr).Select(z=>(string)z.Operand);
    if(!x.SequenceEqual(y))throw new Exception("Source literals changed: "+m.FullName);checkedMethods++;
   }
  }
  var helper=ModuleDefinition.ReadModule(args[2]);if(Types(helper.Types).Any(t=>t.Name=="QARunner"||t.Name=="QAPrefs"))throw new Exception("QA code found in shipping helper");
  Console.WriteLine("Verified unchanged game types, fields, signatures, and source literals in "+checkedMethods+" methods; QA code absent.");
 }
}
