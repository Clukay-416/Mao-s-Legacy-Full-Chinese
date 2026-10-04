using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
public class Inject {
 static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> ts) {foreach(var t in ts){yield return t;foreach(var n in Types(t.NestedTypes))yield return n;}}
 public static void Main(string[] args) {
  var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(Path.GetDirectoryName(args[0]));resolver.AddSearchDirectory(Path.GetDirectoryName(args[1]));
  var module=ModuleDefinition.ReadModule(args[0],new ReaderParameters{AssemblyResolver=resolver});
  var helper=ModuleDefinition.ReadModule(args[1]);var display=helper.Types.First(t=>t.FullName=="MaoChinese.Display");
  var qaPrefs=helper.Types.FirstOrDefault(t=>t.FullName=="MaoChinese.QAPrefs");
  Func<string,MethodReference> get=name=>module.ImportReference(display.Methods.First(m=>m.Name==name));
  var ensure=get("Ensure");var mesh=get("SetMesh");var ui=get("SetUI");var tmp=get("SetTMP");var language=get("GetLanguage");var langDefault=get("GetLanguageDefault");var wrap=get("Wrap");
  var wrapTwo=get("WrapTwo");
  int nMesh=0,nUi=0,nTmp=0,nLang=0,nBoot=0,nWrap=0;
  foreach(var t in Types(module.Types))foreach(var m in t.Methods)if(m.HasBody) {
   if(m.Name=="Text"&&m.ReturnType.FullName=="System.String"&&m.Parameters.Count>=2&&m.Parameters[0].ParameterType.FullName=="System.String"&&(m.Parameters[1].ParameterType.FullName=="System.Single"||m.Parameters[1].ParameterType.FullName=="System.Int32")) {
    m.Body.Instructions.Clear();m.Body.ExceptionHandlers.Clear();m.Body.Variables.Clear();var il=m.Body.GetILProcessor();il.Append(il.Create(m.IsStatic?OpCodes.Ldarg_0:OpCodes.Ldarg_1));il.Append(il.Create(m.IsStatic?OpCodes.Ldarg_1:OpCodes.Ldarg_2));if(m.Parameters[1].ParameterType.FullName=="System.Int32")il.Append(il.Create(OpCodes.Conv_R4));if(m.Parameters.Count==3)il.Append(il.Create(m.IsStatic?OpCodes.Ldarg_2:OpCodes.Ldarg_3));il.Append(il.Create(OpCodes.Call,m.Parameters.Count==3?wrap:wrapTwo));il.Append(il.Create(OpCodes.Ret));nWrap++;continue;
   }
   foreach(var ins in m.Body.Instructions) {
    var mr=ins.Operand as MethodReference;if(mr==null)continue;
    if(mr.Name=="set_text"&&mr.Parameters.Count==1) {
     if(mr.DeclaringType.FullName=="UnityEngine.TextMesh") {ins.OpCode=OpCodes.Call;ins.Operand=mesh;nMesh++;}
     else if(mr.DeclaringType.FullName=="UnityEngine.UI.Text") {ins.OpCode=OpCodes.Call;ins.Operand=ui;nUi++;}
     else if(mr.DeclaringType.FullName=="TMPro.TMP_Text"||mr.DeclaringType.FullName=="TMPro.TextMeshProUGUI"||mr.DeclaringType.FullName=="TMPro.TextMeshPro") {ins.OpCode=OpCodes.Call;ins.Operand=tmp;nTmp++;}
    }
    if(mr.DeclaringType.FullName=="UnityEngine.PlayerPrefs") {
     if(mr.Name=="GetInt") {ins.Operand=mr.Parameters.Count==1?language:langDefault;nLang++;}
     else if(qaPrefs!=null) {var replacement=qaPrefs.Methods.FirstOrDefault(x=>x.Name==mr.Name&&x.Parameters.Count==mr.Parameters.Count);if(replacement!=null)ins.Operand=module.ImportReference(replacement);}
    }
   }
   if((m.Name=="Awake"||m.Name=="Start")&&!m.IsStatic&&!t.FullName.StartsWith("TMPro.Examples")) {m.Body.GetILProcessor().InsertBefore(m.Body.Instructions[0],Instruction.Create(OpCodes.Call,ensure));nBoot++;}
  }
  module.Write(args[2]);
  Console.WriteLine("TextMesh="+nMesh+" UI="+nUi+" TMP="+nTmp+" language="+nLang+" bootstrap="+nBoot+" tooltip="+nWrap);
 }
}
