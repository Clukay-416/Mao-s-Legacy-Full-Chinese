using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MaoChinese {
 public static class Display {
  static Dictionary<string,string> dict;
  static Dictionary<char,List<KeyValuePair<string,string>>> starts;
  static Dictionary<string,string> cache=new Dictionary<string,string>();
  static Font font;
  static bool started;
  static bool inSetter;
  static TMPro.TMP_FontAsset tmpFont;
  public static Font ChineseFont { get { if(font==null) font=Font.CreateDynamicFontFromOSFont(new string[]{"Microsoft YaHei","SimHei","SimSun"},32);return font;} }
  public static void Ensure() {
   if(started)return;started=true;
   var go=new GameObject("MaoChineseDisplay");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<Layout>();
#if QA
   go.AddComponent<QARunner>();
#endif
  }
  public static int GetLanguage(string key) { return key=="language"?0:PlayerPrefs.GetInt(key); }
  public static int GetLanguageDefault(string key,int fallback) { return key=="language"?0:PlayerPrefs.GetInt(key,fallback); }
  static void Load() {
   if(dict!=null)return;
   dict=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);starts=new Dictionary<char,List<KeyValuePair<string,string>>>();
   using(var r=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("dictionary.tsv"),Encoding.UTF8)) {
    string s;while((s=r.ReadLine())!=null) {var p=s.Split('\t');if(p.Length!=2)continue;
     string k=Encoding.UTF8.GetString(Convert.FromBase64String(p[0])),v=Encoding.UTF8.GetString(Convert.FromBase64String(p[1]));
     dict[k]=v;dict[k.Replace('|','\n')]=v.Replace('|','\n');
    }
   }
   foreach(var kv in dict) {if(kv.Key.Length<2 || !Regex.IsMatch(kv.Key,"[A-Za-zА-Яа-я]")||Regex.IsMatch(kv.Key,@"^(?:and|the|of|in|on|no|not|new|old|for|at|to|by|with|or|he|her|it|as|that|you|your|a|an|is)$",RegexOptions.IgnoreCase))continue;
    char ch=char.ToUpperInvariant(kv.Key[0]);List<KeyValuePair<string,string>> a;
    if(!starts.TryGetValue(ch,out a))starts[ch]=a=new List<KeyValuePair<string,string>>();a.Add(kv);
   }
   foreach(var a in starts.Values)a.Sort((x,y)=>y.Key.Length.CompareTo(x.Key.Length));
  }
  static bool Letter(char c) {return (c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='А'&&c<='я');}
  public static string Localize(string s) {
   if(string.IsNullOrEmpty(s))return s;Load();string v;if(cache.TryGetValue(s,out v))return v;
   if(dict.TryGetValue(s,out v))return v;
   var b=new StringBuilder();int i=0;
   while(i<s.Length) {List<KeyValuePair<string,string>> a;bool found=false;
    if(starts.TryGetValue(char.ToUpperInvariant(s[i]),out a))foreach(var kv in a) {
     int n=kv.Key.Length;if(i+n>s.Length)continue;
     if(i>0&&Letter(kv.Key[0])&&Letter(s[i-1]))continue;
     if(i+n<s.Length&&Letter(kv.Key[n-1])&&Letter(s[i+n]))continue;
     if(string.Compare(s,i,kv.Key,0,n,n<5?StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase)==0) {b.Append(kv.Value);i+=n;found=true;break;}
    }
    if(!found) {if(s[i]=='<') {int end=s.IndexOf('>',i);if(end>=i) {b.Append(s.Substring(i,end-i+1));i=end+1;continue;}} b.Append(s[i++]);}
   }
   v=b.ToString();v=Regex.Replace(v,@"(?<![A-Za-z0-9])[-+]?\d+\.\d{4,}",m=>{double number;return double.TryParse(m.Value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out number)?(m.Value.StartsWith("+")?"+":"")+number.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture):m.Value;});
   if(v.StartsWith("储备影响：")||v.StartsWith("服务/工业/民生"))v=v.Replace("储备影响：\n","").Replace("服务/工业/民生：","服务/工业/民生 ").Replace("\n同盟稳定性","　稳定");
   if(cache.Count>6000)cache.Clear();cache[s]=v;return v;
  }
  public static bool HasChinese(string s) {if(s==null)return false;foreach(char c in s)if(c>=0x3400&&c<=0x9fff)return true;return false;}
  public static string Wrap(string s,float columns,out int lines) {
   s=Localize(s??"").Replace('|','\n');columns=Math.Max(4,columns);
   var b=new StringBuilder();float w=0;lines=1;
   for(int i=0;i<s.Length;i++) {char c=s[i];
    if(c=='<') {int end=s.IndexOf('>',i);if(end>=i) {b.Append(s.Substring(i,end-i+1));i=end;continue;}}
    if(c=='\n') {b.Append(c);w=0;lines++;continue;}
    float cw=c>=0x2e80?2f:1f;
    bool closing="，。！？；：、）】》’”.,!?;:)".IndexOf(c)>=0;
    if(w+cw>columns&&!closing&&w>0) {b.Append('\n');w=0;lines++;}
    b.Append(c);w+=cw;
   }
   return b.ToString();
  }
  public static string WrapTwo(string s,float columns) {int lines;return Wrap(s,columns,out lines);}
  public static void SetMesh(TextMesh mesh,string s) {
   if(mesh==null)return;if(inSetter){mesh.text=s;return;}
   inSetter=true;try {Ensure();s=Localize(s);if(SceneManager.GetActiveScene().name=="Settings"&&mesh.transform.parent!=null&&mesh.transform.parent.name.StartsWith("Aud"))s=s.Replace('\n',' ').Replace('|',' ');mesh.text=s;Layout.Apply(mesh,s);}finally{inSetter=false;}
  }
  public static void SetUI(Text text,string s) {if(text==null)return;Ensure();var input=text.GetComponentInParent<InputField>();if(input==null||input.textComponent!=text)s=Localize(s);text.text=s;if(HasChinese(s)) {text.font=ChineseFont;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.resizeTextForBestFit=true;text.resizeTextMinSize=10;text.resizeTextMaxSize=Math.Max(10,text.fontSize);} }
  public static void SetTMP(TMPro.TMP_Text text,string s) {if(text==null)return;Ensure();if(text.GetComponentInParent<TMPro.TMP_InputField>()!=null){text.text=s;return;}s=Localize(s);text.text=s;if(HasChinese(s)){if(tmpFont==null)tmpFont=TMPro.TMP_FontAsset.CreateFontAsset(ChineseFont);text.font=tmpFont;text.enableWordWrapping=true;text.enableAutoSizing=true;text.fontSizeMin=10;text.fontSizeMax=Math.Max(10,text.fontSize);}}
 }
 public class Layout : MonoBehaviour {
  static Dictionary<int,MeshState> states=new Dictionary<int,MeshState>();
  class MeshState {public float size;public string last;public Bounds? box;public string source;}
  float next;int previousScene=-1;
  static Sprite solid;
  static HashSet<int> captions=new HashSet<int>();
  static void LocalizeArt() {
   foreach(var r in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>()) {
    if(r.sprite==null||captions.Contains(r.GetInstanceID()))continue;
    string n=r.sprite.name,caption=null;Vector2 region=Vector2.one,offset=Vector2.zero;
    if(n.StartsWith("DLC04")) {caption="毛泽东的遗产\n生活方式";region=new Vector2(.63f,.64f);}
    else if(n.StartsWith("DLC05")) {caption="战争与金钱\n的军阀";region=new Vector2(.50f,.40f);offset=new Vector2(.22f,-.24f);}
    else if(n.StartsWith("DLC08")||n.StartsWith("DLC8-")) {caption="毛泽东的遗产\n雷锋的遗产";region=new Vector2(.9f,.28f);offset=new Vector2(0,-.32f);}
    if(caption==null)continue;captions.Add(r.GetInstanceID());
    if(solid==null){var pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();solid=Sprite.Create(pixel,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);}
    Vector2 size=r.sprite.bounds.size;var cover=new GameObject("ChineseArtCaption");cover.transform.SetParent(r.transform,false);cover.transform.localPosition=new Vector3(size.x*offset.x,size.y*offset.y,-.01f);var bg=cover.AddComponent<SpriteRenderer>();bg.sprite=solid;bg.color=new Color(.62f,.02f,.04f,1);bg.sortingLayerID=r.sortingLayerID;bg.sortingOrder=r.sortingOrder+1;cover.transform.localScale=new Vector3(size.x*region.x,size.y*region.y,1);
    var label=new GameObject("Caption");label.transform.SetParent(cover.transform,false);label.transform.localPosition=new Vector3(0,0,-.01f);label.transform.localScale=new Vector3(1/(size.x*region.x),1/(size.y*region.y),1);var mesh=label.AddComponent<TextMesh>();mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.fontSize=32;mesh.characterSize=.08f;mesh.color=new Color(1,.87f,.33f,1);mesh.text=caption;mesh.font=Display.ChineseFont;var mr=label.GetComponent<MeshRenderer>();mr.sharedMaterial=mesh.font.material;mr.sortingLayerID=r.sortingLayerID;mr.sortingOrder=r.sortingOrder+2;Apply(mesh,caption);
   }
  }
  static Bounds? Box(TextMesh text) {
   var r=text.GetComponent<Renderer>();if(r==null)return null;
   Vector3 p=r.bounds.center;Bounds? best=null;
   Transform t=text.transform;
   for(int depth=0;t!=null&&depth<4;depth++,t=t.parent) {
    var sprite=t.GetComponent<SpriteRenderer>();
    if(sprite!=null&&sprite.sprite!=null) {var b=sprite.bounds;if(b.size.x>.1f&&b.size.y>.1f&&p.x>=b.min.x&&p.x<=b.max.x&&p.y>=b.min.y&&p.y<=b.max.y)if(!best.HasValue||b.size.x*b.size.y<best.Value.size.x*best.Value.size.y)best=b;}
    var col=t.GetComponent<BoxCollider2D>();if(col!=null) {var b=col.bounds;if(b.size.x>.1f&&b.size.y>.1f&&p.x>=b.min.x&&p.x<=b.max.x&&p.y>=b.min.y&&p.y<=b.max.y)if(!best.HasValue||b.size.x*b.size.y<best.Value.size.x*best.Value.size.y)best=b;}
   }
   return best;
  }
  public static void Apply(TextMesh mesh,string raw) {
   if(mesh==null||!Display.HasChinese(raw))return;
   int id=mesh.GetInstanceID();MeshState st;
   if(!states.TryGetValue(id,out st)) {st=new MeshState{size=mesh.characterSize,box=Box(mesh),source=raw};states[id]=st;}
   if(st.last==raw)return;
   mesh.font=Display.ChineseFont;mesh.fontSize=Math.Max(24,mesh.fontSize);mesh.GetComponent<Renderer>().sharedMaterial=mesh.font.material;
   mesh.characterSize=st.size;mesh.richText=true;
   var rendered=mesh.GetComponent<Renderer>();string output=raw;
   if(st.box.HasValue) {
    Bounds b=st.box.Value;Vector3 anchor=mesh.transform.position;
    int horizontal=(int)mesh.anchor%3;float usableWidth=horizontal==1?2*Math.Min(anchor.x-b.min.x,b.max.x-anchor.x):(horizontal==0?b.max.x-anchor.x:anchor.x-b.min.x);
    if(usableWidth>.15f&&Math.Abs(mesh.transform.eulerAngles.z)<.1f)b.size=new Vector3(Math.Min(b.size.x,usableWidth),b.size.y,b.size.z);
    var stripped=Regex.Replace(raw,"<[^>]*>","");
    if(stripped.Length>24) {
     float estimated=rendered.bounds.size.x/Math.Max(1,LongestWidth(stripped));
     if(estimated>0.0001f) {int n;output=Display.Wrap(raw,Math.Max(8,(b.size.x*.90f)/estimated),out n);mesh.text=output;}
    }
    var extent=rendered.bounds;float factor=1;
    if(extent.size.x>b.size.x*.92f)factor=Math.Min(factor,b.size.x*.92f/extent.size.x);
    if(extent.size.y>b.size.y*.90f)factor=Math.Min(factor,b.size.y*.90f/extent.size.y);
    if(factor<1)mesh.characterSize=st.size*Math.Max(.18f,factor);
   }
   st.last=output;
  }
  static float LongestWidth(string s) {float max=0,w=0;foreach(char c in s) {if(c=='\n'){max=Math.Max(w,max);w=0;}else w+=c>=0x2e80?2:1;}return Math.Max(max,w);}
  void Update() {
   if(Time.unscaledTime<next)return;next=Time.unscaledTime+.3f;
   int scene=SceneManager.GetActiveScene().buildIndex;
   if(scene!=previousScene){previousScene=scene;states.Clear();}
   LocalizeArt();
   TutorialArt.Refresh();
   foreach(var mesh in UnityEngine.Object.FindObjectsOfType<TextMesh>()) {string s=Display.Localize(mesh.text);if(scene==5&&mesh.transform.parent!=null&&mesh.transform.parent.name.StartsWith("Aud"))s=s.Replace('\n',' ').Replace('|',' ');if(s!=mesh.text)mesh.text=s;Apply(mesh,s);}
   foreach(var text in UnityEngine.Object.FindObjectsOfType<Text>()) {var input=text.GetComponentInParent<InputField>();string s=(input!=null&&input.textComponent==text)?text.text:Display.Localize(text.text);if(s!=text.text)text.text=s;if(Display.HasChinese(s)&&text.font!=Display.ChineseFont){text.font=Display.ChineseFont;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.resizeTextForBestFit=true;text.resizeTextMinSize=10;}}
  }
 }
}
