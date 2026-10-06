using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MaoChinese {
 public static class TutorialArt {
  static int lastId=-1;static Sprite paper;
  static Color ink=new Color(.23f,.10f,.14f),background=new Color(.89f,.84f,.74f);
  static Transform root;static SpriteRenderer source;
  public static void Refresh() {
   if(SceneManager.GetActiveScene().name!="Tutorial"){lastId=-1;return;}
   var obj=GameObject.Find("Back/Sp");if(obj==null)return;source=obj.GetComponent<SpriteRenderer>();if(source==null||source.sprite==null)return;
   int id=source.sprite.GetInstanceID();if(id==lastId)return;lastId=id;
   if(root!=null){root.gameObject.SetActive(false);UnityEngine.Object.Destroy(root.gameObject);}root=new GameObject("ChineseTutorialLabels").transform;root.SetParent(source.transform,false);
   int n;if(!int.TryParse(source.sprite.name,out n))return;
   if(paper==null){var texture=new Texture2D(1,1);texture.SetPixel(0,0,Color.white);texture.Apply();paper=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);}
   if(n<=8||n==10) {
    Bar(new string[]{"意识形态","经济","科技","视图","政治","战争"},120,88,280,50);
    string[] names={"政府","影响力","军事","经济"};for(int i=0;i<4;i++)Label(names[i],1670,725+93*i,215,48);
    if(n==2||n==5||n==10){Label(n==5?"中华人民共和国":"国家信息",10,962,225,40);Label("领袖　政体　阵营　关系",225,976,860,36);Label("外交信息与影响力",225,1018,860,36);}
   }else if(n>=11&&n<=13){
    Bar(new string[]{"世界地图","经济","科技","视图","政治","战争"},146,90,275,48);
    string[] left={"经济类型","国家垄断资本主义","政党","人民民主制","公民权利","镇压异议","行政体制","单一制","传统与宗教","反对传统主义","军事力量","建设"};
    int[] ys={302,346,423,477,550,603,678,731,803,850,934,988};int[] heights={31,59,37,56,36,56,37,56,37,61,36,52};for(int i=0;i<left.Length;i++)Label(left[i],50,ys[i],355,heights[i]);
    Label("中国特色社会主义",522,248,365,78);Label("社会主义威权政体",1050,237,339,77);Label("中共派系",470,389,470,40);Label("选举",1112,393,205,39);
    string[] factions={"左派激进分子","保守派","温和派","改革派","自由派"};for(int i=0;i<5;i++)Label(factions[i],595,494+107*i,265,69);
    if(n==13)Label("军事实力：+0.6",305,302,310,40);
   }else if(n>=14&&n<=16){
    Bar(new string[]{"世界地图","意识形态","科技","视图","政治","战争"},115,112,282,47);
    string[] top={"工业","农业","服务业","腐败程度"};for(int i=0;i<4;i++)Label(top[i],196+455*i,242,300,59);
    string[] sectors={"军队","国家安全部","科技","国家机关","党员津贴","宣传活动","农业","工业","服务业","福利","国际援助","外交使团"};
    for(int i=0;i<12;i++)Label(sectors[i],238+(i%3)*482,413+(i/3)*178,233,42);
    Label("黄金储备",1674,946,175,37);Label("债务损失：",1500,484,370,29);Label("预算 -0.3",1500,520,370,28);Label("债务上限：",1500,578,370,28);
    Label("腐败造成的损失",1500,670,370,24);Label("预算 -1.9",1500,692,370,23);Label("生活水平：-0.3",1500,716,370,20);Label("投资上限：",1500,766,370,27);
   }else if(n==17){Label("科技点数",157,362,250,55);}
   else if(n==18){
    Bar(new string[]{"世界地图","经济","科技","意识形态","政治","视图"},146,90,275,48);
    string[] labels={"贸易","影响力","领土","局势","社会"};for(int i=0;i<5;i++)Label(labels[i],129,366+110*i,242,50);
    Label("周恩来的遗产\n侧重改革派与自由派",1100,962,590,78);
   }else if(n>=19&&n<=21){
    string[] cabinet={"华国锋","毛泽东","乔冠华","吴德","陈锡联","陈云","赵紫阳","张春桥"};int[] cy={17,69,121,181,231,281,331,381};for(int i=0;i<cabinet.Length;i++)Label(cabinet[i],423,cy[i],224,42);
    Card("毛泽东","左派激进\n坚定　病弱",746,252,225,149);Card("江青","左派激进\n坚定　残酷",1047,252,230,149);Card("王洪文","左派激进\n坚定　善谋",1350,252,225,149);
    string[] second={"张春桥","汪东兴","李先念","姚文元"};string[] third={"叶剑英","纪登奎","陈锡联","吴德","黄华"};string[] fourth={"赵紫阳","胡耀邦","王震","邓小平","陈云","乔冠华"};
    string[] secondTraits={"左派激进\n坚定\n善谋","左派激进\n坚定\n和平","温和派\n坚定\n节俭","左派激进\n坚定\n傲慢"};string[] thirdTraits={"改革派\n学者\n善谋","左派激进\n务实\n节俭","左派激进\n务实\n善谋","左派激进\n坚定\n残酷","温和派\n务实\n亲华"};string[] fourthTraits={"自由派\n温和\n亲西方","自由派\n温和\n偶像","温和派\n坚定\n亲华","改革派\n务实\n节俭","改革派\n务实\n节俭","温和派\n坚定\n亲西方"};
    for(int i=0;i<4;i++)Card(second[i],secondTraits[i],573+303*i,467,225,146);
    for(int i=0;i<5;i++)Card(third[i],thirdTraits[i],443+303*i,680,225,145);
    for(int i=0;i<6;i++)Card(fourth[i],fourthTraits[i],420+247*i,898,220,145);
    if(n==20||n==21){Label("总理\n军委主席\n外交部长",664,118,157,116);Label("毛泽东（83岁）",11,22,359,31);Label("左派激进",11,68,359,31);Label("坚定",11,115,359,31);Label("病弱",11,162,359,31);Label("<color=red>职务：</color>军委主席\n<color=green>主席：</color>\n未被调查\n未被监视\n<color=red>影响力很高</color>",10,210,364,135);
     string[] buttons={"尝试暗杀","派往南方","派往西方","派往首都","派往北方","任命外交部长","派往东方","任命总理","任命军委主席","支持","打压","展开调查","监视"};int[] bx={129,26,228,26,228,26,228,26,228,26,228,26,228};int[] by={483,560,560,642,642,723,723,803,803,885,885,965,965};for(int i=0;i<buttons.Length;i++)Label(buttons[i],bx[i],by[i],143,38);
    }
   }else if(n==22){
    Bar(new string[]{"世界地图","经济","科技","意识形态","政治","视图"},122,91,282,49);
    War("柬越冲突",0,193);War("泰国内战",1,193);Label("干预点数",58,611,233,66);Label("国际援助与影响力\n每两周增加 +0.6",315,580,262,93);
   }else if(n==9){
    Label("五个不",130,79,1353,67);
    Label("华国锋同志，祝贺您就任中华人民共和国国务院总理。您的前任周恩来以正直和行政才能，赢得了国内外人民的爱戴与尊敬。他也积极推动经济改革，并在党内提拔邓小平等改革派人士。因此，1976年1月8日周恩来的逝世引发了民众的深切悲痛，而毛泽东和中共领导层对此十分克制，并对民众的反应感到不满。按照毛泽东的指示，开展了“五个不”运动：不戴孝带、不献花圈、不设纪念、不举行追悼仪式、不悬挂周恩来的照片。目前，这些措施引起的只有民众的不满。作为新任总理，您可以影响这项运动的执行。",73,156,1401,514);
   }
  }
  static void Bar(string[] names,float x,float y,float width,float height){for(int i=0;i<names.Length;i++)Label(names[i],x+i*width,y,width-15,height);}
  static void War(string name,int row,int top){float y=top+row*175;Label(name,668,y,821,44);string[] labels={"人道","外交","顾问","武器"};for(int j=0;j<4;j++){int col=j%2,r=j/2;Label(labels[j],387+col*121,y+8+r*59,93,39);Label(labels[j],1550+col*121,y+8+r*59,93,39);}Label(row==0?"柬埔寨":"共产党",826,y+65,239,38);Label(row==0?"越南":"保皇派",1089,y+65,237,38);}
  static void Card(string name,string traits,float x,float y,float w,float h){
   Label(name,x,y,w,38);string[] rows=traits.Replace("　","\n").Split('\n');float rowHeight=(h-44)/3;
   for(int i=0;i<rows.Length;i++)Label(rows[i],x+w*.50f,y+46+i*rowHeight,w*.50f+10,rowHeight-3);
  }
  static void Label(string caption,float x,float y,float w,float h){
   caption=caption.Replace("左派激进分子","极左派").Replace("左派激进","极左派").Replace("中华派","亲华").Replace("亲西方派","亲西方").Replace("科技点数","科研点数").Replace("善谋","谋略家").Replace("残酷","强硬");
   Vector2 size=source.sprite.bounds.size;float sx=size.x/1920,sy=size.y/1080;
   var go=new GameObject("Label");go.transform.SetParent(root,false);go.transform.localPosition=new Vector3((x+w/2-960)*sx,(540-y-h/2)*sy,-.02f);var bg=go.AddComponent<SpriteRenderer>();bg.sprite=paper;bg.color=background;bg.sortingLayerID=source.sortingLayerID;bg.sortingOrder=source.sortingOrder;go.transform.localScale=new Vector3(w*sx,h*sy,1);
   var t=new GameObject("Chinese");t.transform.SetParent(go.transform,false);t.transform.localPosition=new Vector3(0,0,-.01f);t.transform.localScale=new Vector3(1/(w*sx),1/(h*sy),1);var mesh=t.AddComponent<TextMesh>();mesh.font=Display.ChineseFont;mesh.fontSize=32;mesh.characterSize=.11f;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.color=ink;mesh.text=caption;var mr=t.GetComponent<MeshRenderer>();mr.sharedMaterial=mesh.font.material;mr.sortingLayerID=source.sortingLayerID;mr.sortingOrder=source.sortingOrder;Layout.Apply(mesh,caption);
  }
 }
}


