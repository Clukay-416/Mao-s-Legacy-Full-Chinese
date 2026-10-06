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
   // Share a line for the two effects, retaining the heading and every value.
   if(v.StartsWith("储备影响："))v=v.Replace("\n同盟稳定度","；同盟稳定度");
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
  public static void SetTMP(TMPro.TMP_Text text,string s) {
   if(text==null)return;Ensure();if(text.GetComponentInParent<TMPro.TMP_InputField>()!=null){text.text=s;return;}
   s=Localize(s);text.text=s;
   if(HasChinese(s)){var bridge=text.GetComponent<TMPCompatibility>();if(bridge==null)bridge=text.gameObject.AddComponent<TMPCompatibility>();bridge.Bind(text);}
  }
 }
 public class Layout : MonoBehaviour {
  static Dictionary<int,MeshState> states=new Dictionary<int,MeshState>();
  class MeshState {public TextMesh mesh;public float size;public string last;public Bounds? box;public string source;}
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
   Vector3 p=text.transform.position;Bounds? best=null;
   Transform t=text.transform;
   for(int depth=0;t!=null&&depth<4;depth++,t=t.parent) {
    var sprite=t.GetComponent<SpriteRenderer>();
    if(sprite!=null&&sprite.sprite!=null) {var b=sprite.bounds;if(b.size.x>.1f&&b.size.y>.1f&&p.x>=b.min.x&&p.x<=b.max.x&&p.y>=b.min.y&&p.y<=b.max.y)if(!best.HasValue||b.size.x*b.size.y<best.Value.size.x*best.Value.size.y)best=b;}
    var col=t.GetComponent<BoxCollider2D>();if(col!=null) {var b=col.bounds;if(b.size.x>.1f&&b.size.y>.1f&&p.x>=b.min.x&&p.x<=b.max.x&&p.y>=b.min.y&&p.y<=b.max.y)if(!best.HasValue||b.size.x*b.size.y<best.Value.size.x*best.Value.size.y)best=b;}
   }
   return best;
  }
  public static void Apply(TextMesh mesh,string raw) {
   if(mesh==null||!Display.HasChinese(raw)||mesh.GetComponentInParent<TMPCompatibility>()!=null||mesh.name=="ChinesePopupDisplay")return;
   if(mesh.transform.parent!=null&&mesh.transform.parent.name.StartsWith("Okoshko")) {var popup=mesh.GetComponent<PopupCompatibility>();if(popup==null)popup=mesh.gameObject.AddComponent<PopupCompatibility>();popup.Bind(mesh);return;}
   int id=mesh.GetInstanceID();MeshState st;
   var metrics=mesh.GetComponent<ChineseMeshMetrics>();if(metrics==null)metrics=mesh.gameObject.AddComponent<ChineseMeshMetrics>();
   if(!metrics.initialized){metrics.baseSize=mesh.characterSize;metrics.lastAppliedSize=mesh.characterSize;metrics.initialized=true;}
   else if(!Mathf.Approximately(mesh.characterSize,metrics.lastAppliedSize))metrics.baseSize=mesh.characterSize;
   if(!states.TryGetValue(id,out st)) {st=new MeshState{mesh=mesh,size=metrics.baseSize,box=Box(mesh),source=raw};states[id]=st;}
   bool sizeChanged=!Mathf.Approximately(st.size,metrics.baseSize);st.size=metrics.baseSize;
   if(st.last==raw&&!sizeChanged)return;
   st.box=Box(mesh);
   mesh.font=Display.ChineseFont;mesh.fontSize=Math.Max(24,mesh.fontSize);mesh.GetComponent<Renderer>().sharedMaterial=mesh.font.material;
   mesh.characterSize=st.size;mesh.richText=true;
   // The Latin font's tightly packed lines overlap CJK glyphs. Keep all content.
   mesh.lineSpacing=Math.Max(1.12f,mesh.lineSpacing);
   var rendered=mesh.GetComponent<Renderer>();string output=raw;
   if(st.box.HasValue) {
    Bounds b=st.box.Value;Vector3 anchor=mesh.transform.position;
    int horizontal=(int)mesh.anchor%3;float usableWidth=horizontal==1?2*Math.Min(anchor.x-b.min.x,b.max.x-anchor.x):(horizontal==0?b.max.x-anchor.x:anchor.x-b.min.x);
    if(usableWidth>.15f&&Math.Abs(mesh.transform.eulerAngles.z)<.1f)b.size=new Vector3(Math.Min(b.size.x,usableWidth),b.size.y,b.size.z);
    int vertical=(int)mesh.anchor/3;float usableHeight=vertical==1?2*Math.Min(anchor.y-b.min.y,b.max.y-anchor.y):(vertical==0?anchor.y-b.min.y:b.max.y-anchor.y);
    if(usableHeight>.08f&&Math.Abs(mesh.transform.eulerAngles.z)<.1f)b.size=new Vector3(b.size.x,Math.Min(b.size.y,usableHeight),b.size.z);
    var stripped=Regex.Replace(raw,"<[^>]*>","");
    if(stripped.Length>24) {
     float estimated=rendered.bounds.size.x/Math.Max(1,LongestWidth(stripped));
     if(estimated>0.0001f) {int n;output=Display.Wrap(raw,Math.Max(8,(b.size.x*.90f)/estimated),out n);mesh.text=output;}
    }
    var extent=rendered.bounds;float factor=1;
    if(extent.size.x>b.size.x*.92f)factor=Math.Min(factor,b.size.x*.92f/extent.size.x);
    if(extent.size.y>b.size.y*.90f)factor=Math.Min(factor,b.size.y*.90f/extent.size.y);
    if(factor<1)mesh.characterSize=st.size*Math.Max(.05f,factor);
   }
   metrics.lastAppliedSize=mesh.characterSize;st.last=output;
  }
  static float LongestWidth(string s) {float max=0,w=0;foreach(char c in s) {if(c=='\n'){max=Math.Max(w,max);w=0;}else w+=c>=0x2e80?2:1;}return Math.Max(max,w);}
  void Update() {
   if(Time.unscaledTime<next)return;next=Time.unscaledTime+.3f;
   int scene=SceneManager.GetActiveScene().buildIndex;
   if(scene!=previousScene){previousScene=scene;var gone=new List<int>();foreach(var item in states)if(item.Value.mesh==null)gone.Add(item.Key);foreach(int id in gone)states.Remove(id);}
   LocalizeArt();
   TutorialArt.Refresh();
   foreach(var mesh in UnityEngine.Object.FindObjectsOfType<TextMesh>()) {string s=Display.Localize(mesh.text);if(scene==5&&mesh.transform.parent!=null&&mesh.transform.parent.name.StartsWith("Aud"))s=s.Replace('\n',' ').Replace('|',' ');if(s!=mesh.text)mesh.text=s;Apply(mesh,s);}
   foreach(var text in UnityEngine.Object.FindObjectsOfType<Text>()) {var input=text.GetComponentInParent<InputField>();string s=(input!=null&&input.textComponent==text)?text.text:Display.Localize(text.text);if(s!=text.text)text.text=s;if(Display.HasChinese(s)&&text.font!=Display.ChineseFont){text.font=Display.ChineseFont;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.resizeTextForBestFit=true;text.resizeTextMinSize=10;}}
   foreach(var text in UnityEngine.Object.FindObjectsOfType<TMPro.TMP_Text>()) {if(text.GetComponentInParent<TMPro.TMP_InputField>()!=null)continue;string s=Display.Localize(text.text);if(s!=text.text)text.text=s;if(Display.HasChinese(s)){var bridge=text.GetComponent<TMPCompatibility>();if(bridge==null)bridge=text.gameObject.AddComponent<TMPCompatibility>();bridge.Bind(text);}}
  }
 }
 // Serialized on runtime clones, so cloned labels retain their original scale.
 public class ChineseMeshMetrics : MonoBehaviour {
  public bool initialized;public float baseSize;public float lastAppliedSize;
 }
 // Render popup glyphs in a fresh native renderer; preserve the game's text reference.
 public class PopupCompatibility : MonoBehaviour {
  TextMesh original,display;string last;Vector3 lastBox;
  public void Bind(TextMesh text){original=text;Refresh();}
  void LateUpdate(){Refresh();}
  void Refresh(){
   if(original==null)return;
   if(display==null){var go=new GameObject("ChinesePopupDisplay");go.layer=original.gameObject.layer;go.transform.SetParent(original.transform,false);display=go.AddComponent<TextMesh>();display.font=Display.ChineseFont;display.fontSize=32;display.richText=true;display.characterSize=original.characterSize;display.anchor=original.anchor;display.alignment=original.alignment;display.color=Color.white;var r=display.GetComponent<MeshRenderer>();r.sharedMaterial=display.font.material;var background=original.transform.parent.GetComponent<SpriteRenderer>();if(background!=null){r.sortingLayerID=background.sortingLayerID;r.sortingOrder=background.sortingOrder+1;}}
   original.GetComponent<MeshRenderer>().enabled=false;
   string value=Display.Localize(original.text);var backing=original.transform.parent.GetComponent<SpriteRenderer>();Vector3 box=backing==null?Vector3.zero:backing.bounds.size;
   if(last!=value||lastBox!=box){display.characterSize=original.characterSize;display.text=value;var bounds=display.GetComponent<Renderer>().bounds;float fit=1;if(box.x>.1f&&bounds.size.x>box.x*.9f)fit=Math.Min(fit,box.x*.9f/bounds.size.x);if(box.y>.1f&&bounds.size.y>box.y*.9f)fit=Math.Min(fit,box.y*.9f/bounds.size.y);display.characterSize*=fit;last=value;lastBox=box;}
  }
 }
 // System-created fonts have no embedded font data for this game's TMP FontEngine.
 // Keep the original TMP component and its text; render CJK through native Unity text.
 public class TMPCompatibility : MonoBehaviour {
  TMPro.TMP_Text original;TextMesh mesh;Text ui;string last;Vector2 lastRect;Color lastColor;float lastSize;
  bool originallyEnabled;bool bound;
  public void Bind(TMPro.TMP_Text text) {if(!bound){original=text;originallyEnabled=text.enabled;bound=true;}Refresh();}
  void LateUpdate(){Refresh();}
  void Refresh() {
   if(original==null)return;
   string value=Display.Localize(original.text);
   if(!Display.HasChinese(value)) {if(mesh!=null)mesh.gameObject.SetActive(false);if(ui!=null)ui.gameObject.SetActive(false);original.enabled=originallyEnabled;last=null;return;}
   original.enabled=false;
   var rt=original.rectTransform;Rect rect=rt.rect;Vector4 margin=original.margin;
   int alignment=(int)original.alignment;
   int column=(alignment&4)!=0?2:(alignment&2)!=0?1:0;
   int row=(alignment&1024)!=0?2:(alignment&512)!=0?1:0;
   TextAnchor anchor=(TextAnchor)(row*3+column);
   if(original is TMPro.TextMeshProUGUI) {
    if(ui==null){var child=new GameObject("ChineseTMPDisplay",typeof(RectTransform));child.transform.SetParent(rt,false);ui=child.AddComponent<Text>();ui.raycastTarget=false;}
    ui.gameObject.SetActive(true);var urt=ui.rectTransform;urt.anchorMin=Vector2.zero;urt.anchorMax=Vector2.one;urt.offsetMin=new Vector2(margin.x,margin.w);urt.offsetMax=new Vector2(-margin.z,-margin.y);
    ui.font=Display.ChineseFont;ui.text=value;ui.color=original.color;ui.alignment=anchor;ui.fontSize=Math.Max(10,(int)Math.Ceiling(original.fontSize));ui.supportRichText=true;ui.horizontalOverflow=HorizontalWrapMode.Wrap;ui.verticalOverflow=VerticalWrapMode.Truncate;ui.resizeTextForBestFit=true;ui.resizeTextMaxSize=ui.fontSize;ui.resizeTextMinSize=Math.Max(8,(int)(ui.fontSize*.5f));
    return;
   }
   if(mesh==null){var child=new GameObject("ChineseTMPDisplay");child.transform.SetParent(rt,false);mesh=child.AddComponent<TextMesh>();mesh.font=Display.ChineseFont;mesh.fontSize=32;mesh.richText=true;var mr=mesh.GetComponent<MeshRenderer>();var source=original.GetComponent<MeshRenderer>();mr.sharedMaterial=mesh.font.material;if(source!=null){mr.sortingLayerID=source.sortingLayerID;mr.sortingOrder=source.sortingOrder;}}
   mesh.gameObject.SetActive(true);
   if(last==value&&lastRect==rect.size&&lastSize==original.fontSize&&lastColor==original.color)return;
   float left=rect.xMin+margin.x,right=rect.xMax-margin.z,top=rect.yMax-margin.y,bottom=rect.yMin+margin.w;
   float width=Math.Max(.01f,right-left),height=Math.Max(.01f,top-bottom);
   mesh.transform.localPosition=new Vector3(column==0?left:column==1?(left+right)*.5f:right,row==0?top:row==1?(top+bottom)*.5f:bottom,-.001f);
   mesh.anchor=anchor;mesh.alignment=column==0?TextAlignment.Left:column==1?TextAlignment.Center:TextAlignment.Right;mesh.color=original.color;
   mesh.fontStyle=(original.fontStyle&TMPro.FontStyles.Bold)!=0?FontStyle.Bold:FontStyle.Normal;
   var renderer=mesh.GetComponent<MeshRenderer>();renderer.sharedMaterial=mesh.font.material;
   mesh.characterSize=1;mesh.text="国";
   float unitHeight=renderer.bounds.size.y/Math.Max(.0001f,Math.Abs(mesh.transform.lossyScale.y));
   float desired=Math.Min(height,Math.Max(.001f,original.fontSize*.1f));
   mesh.characterSize=desired/Math.Max(.0001f,unitHeight);
   mesh.text=value;float actualWidth=renderer.bounds.size.x/Math.Max(.0001f,Math.Abs(mesh.transform.lossyScale.x));
   if(actualWidth>width&&value.Length>12){int lines;mesh.text=Display.Wrap(value,Math.Max(4,width/actualWidth*VisibleWidth(value)),out lines);}
   float actualHeight=renderer.bounds.size.y/Math.Max(.0001f,Math.Abs(mesh.transform.lossyScale.y));actualWidth=renderer.bounds.size.x/Math.Max(.0001f,Math.Abs(mesh.transform.lossyScale.x));
   float factor=Math.Min(1,Math.Min(width/Math.Max(.0001f,actualWidth),height/Math.Max(.0001f,actualHeight)));
   mesh.characterSize*=Math.Max(.05f,factor)*.97f;
   last=value;lastRect=rect.size;lastSize=original.fontSize;lastColor=original.color;
  }
  static float VisibleWidth(string value){float width=0;foreach(char c in Regex.Replace(value,"<[^>]*>",""))if(c!='\n'&&c!='\r')width+=c>=0x2e80?2:1;return Math.Max(1,width);}
 }
}
