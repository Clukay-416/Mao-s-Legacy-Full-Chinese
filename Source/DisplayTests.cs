using System;
using System.Text.RegularExpressions;
using MaoChinese;
public class DisplayTests {
 static void Check(bool pass,string name){if(!pass)throw new Exception(name);Console.WriteLine("PASS "+name);}
 public static void Main(){
  Check(Display.Localize("Gold\nReserve")=="黄金储备","multiline exact translation");
  Check(!Display.Localize("This island has sand and sea.").Contains("民族民主"),"acronym does not replace a common English word");
  Check(Display.Localize("Delta +0.123456").Contains("+0.12"),"positive numeric sign preserved");
  Check(Display.Localize("Delta -0.123456").Contains("-0.12"),"negative numeric sign preserved");
  int lines;string input="<color=red>"+new string('汉',30)+"，继续。</color>";string wrapped=Display.Wrap(input,20,out lines);
  Check(lines>1&&Regex.Matches(wrapped,"<color=red>").Count==1&&Regex.Matches(wrapped,"</color>").Count==1,"Chinese wrapping preserves rich text tags");
  Check(!wrapped.Contains("\n，")&&!wrapped.Contains("\n。"),"closing punctuation stays with the preceding line");
  Check(!Display.Localize("Influence of the Reserve:\nServices, Ind. and SoL: +0.5").Contains("。5"),"dynamic decimal fragment remains a decimal");
 }
}
