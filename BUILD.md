# 重建说明

普通玩家直接下载 Releases 中的安装包即可。以下步骤用于修改翻译或继续维护源码。

需要 Windows PowerShell、.NET Framework 4.x 的 C# 编译器、Python 3.11 或更新版本，以及自己持有的对应 1.8.5 原版游戏。请先在测试副本中操作。

```powershell
python -m pip install -r .\Source\requirements.txt
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1 -GameRoot "你的游戏目录" -Python "python"
```

脚本从 NuGet 官方地址下载 Mono.Cecil 0.11.6，用游戏原有 Unity 程序集编译新增显示层，再对原版程序集注入显示调用。已有补丁时优先使用 `.MaoChineseBackup` 内的原文件；脚本不修改所选游戏目录，重建文件写入 `.build/rebuilt/`。

资源重建时，`Source/textassets.json` 的 `en` 行必须与所提供的原版资源完全对应，随后写入等行数的 `zh`。内部键、标签、分隔符与格式参数不可随意翻译。`Source/translations.json` 构成显示词典；修改补充译文时也应同步这里的对应条目。

`Source/make_delta.py` 将自有原版与修改版生成 MLD1 差分，`Source/Delta.cs` 在安装时重建文件。格式为 gzip 压缩的 `MLD1`、原始/目标长度，以及复制/字面量指令；每个原始、目标和差分文件均有 SHA-256 校验。重建包会生成新的 manifest；重新发布前需用独立副本检查安装、重复安装、卸载、文本格式和游戏显示。

`Source/Verify.cs`、`DisplayTests.cs` 与 `ParserTests.cs` 保留本次验证用的源码，供维护参考，不包含测试场景控制器或玩家存档。
