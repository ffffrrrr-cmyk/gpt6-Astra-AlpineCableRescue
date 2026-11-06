# 霜岭索道救援

单人三维物理吊机救援游戏，使用 Unity 6000.6.7f1、URP 17.6.0、C#，目标 Windows x64。四个任务分别为初次接应、岩隙横风、中继转运和阵风双舱救援。核心源码由当前会话实际模型 gpt-6-astra 生成；采购来源核验缺项见 Documentation/开工核验.md。

## 启动与操作

构建成功后双击 `Builds/Windows/FrostlineRescue.exe`，或用 PowerShell 执行 `./Run.ps1`。完整构建目录必须保持在一起。无在线账号、游戏服务器或 API Key 依赖；开发与重新构建需要本机 Unity 许可。

| 操作 | 按键 |
| --- | --- |
| 左右回转 | A / D |
| 抬高或降低臂架 | W / S |
| 收缆或放缆 | Q / E |
| 锁接或安全解锁 | Space |
| 全景、跟随吊钩、俯视切换 | C |
| 暂停或恢复 | Esc |
| 重试当前任务 | R |
| 查看操作指南 | H |

进入首关后按 E 放缆，对准救援舱顶部青色吊环。吊钩底部与吊环距离小于 0.85 米、相对速度低于 1.1 m/s 时按 Space 锁接。按 Q 提升，确认舱底越过障碍后缓慢回转；W/S 改变吊臂水平投影半径。松开按键留出减速与消摆时间。将舱放在目标圈内，速度低、姿态直立且支承高度正确地停留 1.2 秒后，按 Space 解锁。

第三关必须先在中继平台安全解锁，再次锁接后送往高台。第四关第一舱送达后移至安全区，为第二舱腾出落区。自由练习不限时，不更新成绩和关卡解锁。正常模式逐关解锁。

## 工程复现

1. 使用已激活许可的 Unity 6000.6.7f1 打开本目录，并安装 Windows Build Support。项目包含所需 Packages 依赖与锁文件（由 Unity 解析生成）。
2. 等待导入完成。打开 `Assets/Scenes/Frostline.unity` 并进入 Play。
3. 若需从代码重建资源，使用菜单 `Frostline > Rebuild generated assets and scene`。这会更新本项目的 Generated 资产和入口场景。
4. 后台完整生成、规则验证和构建：`powershell -ExecutionPolicy Bypass -File ./Build.ps1`。编辑器路径不同则传 `-UnityEditor '完整路径/Unity.exe'`。
5. 仅运行规则与存档检查：`./Build.ps1 -VerifyOnly`。
6. 可运行包自动物理验证：`FrostlineRescue.exe --self-test -batchmode -logFile player-self-test.log`。验证会使用独立测试存档并在完成后退出，输出位于 EXE 同级 Logs。自动检查不代表用户手感与画面验收。

初始检查发现本机旧编辑器 2020.3.23f1c1 被许可保护阻塞，因此选用本机可启动且内置 URP 包的 6000.6.7f1。Unity 6.6 属于当前已安装版本，本项目不声称它是 LTS。

## 工程组织

- `MechanicalArt.cs`：倒角网格、桁架吊臂、分件驾驶室、卷筒、救援舱与材质。
- `AlpineWorld.cs`：山体雪线、松林、支承平台、索道和光照。
- `CraneController.cs`：回转、俯仰、限速绞盘、真实 Rigidbody/ConfigurableJoint 钢缆及锁接。
- `RescueCargo.cs`：碰撞损伤、风力、稳定落区与中继判定。
- `MissionDefinition.cs`：四关可编辑数据；生成后为 ScriptableObject 资产。
- `RescueRules.cs`、`RescueGame.cs`：玩法状态、计时、得分、进度与流程。
- `SaveStore.cs`：版本化原子存档、损坏备份与恢复。
- `RescueHud.cs`、`RescueCamera.cs`、`RescueAudio.cs`：中文界面、三种视角和程序化音效。
- `Assets/Editor/ProjectBuilder.cs`：生成场景/预制体、规则检查和 Windows 构建。
- `RuntimeVerification.cs`：实际运行物理操作的自动验证驾驶器，仅在 `--self-test` 时启用。

模型原始源码是本项目首次交付的源码；后续模型修复关联本会话与固定归档版本。当前没有人工核心代码修补。不要把 Library、Temp、引擎包、构建产物或二进制资产计入模型有效应用代码量。

## 存档与验证

正式存档位于 `%USERPROFILE%/AppData/LocalLow/FrostlineStudio/Frostline Rescue/progress-v1.json`。损坏存档保留 `.recovered`，更新保留 `.backup`。自动验证使用构建目录内的独立存档，不修改正式玩家进度。

实际验证结果、已知问题和启动交付状态以 Documentation/验证报告.md 为准。用户试玩步骤见 Documentation/试玩步骤.md。未经用户反馈，手感和画面状态均为待测。
