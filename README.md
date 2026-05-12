# RepoCoyoteStim v0.5.8

适用于在 R.E.P.O. 游戏中连接 DG-LAB 郊狼 3.0 主机的 Socket 模组，支持受击、死亡惩罚、左右脚步、奔跑、跳跃、落地、滑行等事件触发连续波形和强度变化。

作者 / Author: **AngelcoMilk**  
GitHub: https://github.com/AngelcoMilk/RepoCoyoteStim

RepoCoyoteStim 会在游戏内启动一个 DG-LAB App Socket 控制端，让手机 DG-LAB 3.0 App 扫码连接，再由手机通过蓝牙连接郊狼 3.0 主机。电脑不需要蓝牙。

```text
R.E.P.O. Mod -> WebSocket -> 手机 DG-LAB 3.0 App -> 手机蓝牙 -> 郊狼 3.0 主机
```

## 重要软件版本

- 请使用 Google Play 下载的 `DG-LAB 3.0+` App，建议使用 3.x 系列。
- `DG-LAB 4.0` 及以上版本目前不兼容本 Mod 使用的 Socket 接入方式。
- 手机负责蓝牙连接郊狼 3.0 主机；电脑只需要和手机网络互通。

## 主要功能

- **受击惩罚**：本地玩家受到伤害时触发 A/B 通道惩罚波形，强度可按伤害变化。
- **死亡惩罚**：本地玩家死亡时可先清空旧队列，再输出更长的死亡波形。
- **连续波形**：走路、奔跑、滑行可进入持续状态，不只是单次点按。
- **左右脚通道**：优先使用游戏动画里的 `LeftFootDown / RightFootDown`，默认左脚 A、右脚 B。
- **动作事件**：支持跳跃、落地、滑行、敌人近距离脚步提示等事件。
- **游戏内面板**：显示二维码、绑定状态、A/B 强度、当前队列、事件计数和诊断信息。
- **波形预设**：内置舒适、标准、强惩罚、调试同步等预设。
- **导入/导出**：支持从 `BepInEx/config/RepoCoyoteStim/profiles/` 导入/导出波形配置。

## 连接方法

1. 在 r2modman/Thunderstore 导入 Mod 包并启动 R.E.P.O.
2. 进入游戏后按 `P` 打开“郊狼 3.0 控制面板”。
3. 打开手机 `DG-LAB 3.0+` App，进入 `SOCKET` 功能。
4. 扫描游戏面板中的二维码，不要使用普通扫码入口。
5. App 绑定成功后，在面板中确认 A/B 当前强度，并启用触发。
6. 任意时候按 `I` 可紧急停止，清空 A/B 队列并把强度设为 0。

## 多 IP 地址说明

面板会自动检测电脑上的 IPv4 地址，并显示多个“使用 xxx.xxx.xxx.xxx”按钮。多网卡、虚拟网卡、加速器、Tailscale、VMware、Hyper-V、WSL、Radmin 等环境下，电脑可能同时出现多个 IP。

选择规则：

1. 优先选择和手机处在同一个 Wi-Fi/局域网的 IPv4。
2. 常见可用地址通常长这样：`192.168.x.x`、`10.x.x.x`、`172.16.x.x - 172.31.x.x`。
3. 不要选 `127.0.0.1`，手机无法通过这个地址访问电脑。
4. 如果扫码后 App 提示连接失败，回到面板换另一个 IPv4，保存并刷新二维码后重试。
5. 确认 Windows 防火墙允许 R.E.P.O. 或当前端口通信。
6. 如果手机和电脑连的是访客 Wi-Fi、校园网、公司网或开启 AP 隔离的网络，局域网连接可能不可用。

默认本地端口为 `9999`。如果端口被占用，可以在面板或配置里修改端口，然后重启 Socket 服务并重新扫码。

## 远程连接说明

本 Mod 主线使用 DG-LAB 官方 App Socket 协议。DG-LAB App 原生“远程口令/远程码”不是公开 Socket v2 API，本 Mod 不把它作为稳定接入方式。

可用的远程方式：

- `PublicSocket`：二维码使用公网 `ws://` / `wss://` 地址。
- `RemoteServer`：Mod 连接到公网 Socket v2 后端。
- `RelayCode`：使用自建 relay code。注意这不是 DG-LAB App 原生远程口令，需要兼容的自建 relay 服务。

## 快捷键

- `P`：打开/关闭游戏内控制面板。
- `I`：紧急停止，清空 A/B 队列并将强度设为 0。

两个按键都可以在配置中修改。

## 配置说明

配置文件：

```text
BepInEx/config/cn.codex.repo.coyotestim.cfg
```

常用配置：

- `Connection.Port`：本地 Socket 端口，默认 `9999`。
- `Connection.AdvertiseHost`：二维码里展示给手机连接的电脑 IP/域名。
- `Safety.Enabled`：是否启用游戏事件触发。
- `Safety.AutoArmOnBind`：绑定 App 后是否自动启用触发。
- `Safety.AutoStrengthMode`：自动调强模式。
- `Safety.MaxWaveIntensity`：本 Mod 使用的波形强度上限。
- `Hurt.*`：受击惩罚强度、持续时间、频率和通道倍率。
- `Death.*`：死亡惩罚强度、持续时间、频率和是否清队列。
- `Footstep.*`：左右脚通道、脚步强度、频率、最小间隔和兜底设置。
- `Continuous.*`：连续波形补包间隔、lookahead 和淡出时间。

## 安装（r2modman）

1. 导入 zip。
2. 确认 DLL 路径：
   `BepInEx/plugins/RepoCoyoteStim/RepoCoyoteStim.dll`
3. 确认依赖 `BepInExPack` 已安装。

## 已知限制

- 只支持 DG-LAB 郊狼 3.0 的 App Socket 接入，不直接控制电脑蓝牙。
- `DG-LAB 4.0` 及以上 App 当前不兼容此 Socket 方案。
- 手机和电脑不在同一局域网时，局域网二维码不能直接使用，需要公网 Socket 或自建 relay。
- 事件只在本地客户端触发，不修改游戏网络状态，也不强制同步给其他玩家。
- 不建议在公开房间测试高强度或未调好的波形配置。

---

# RepoCoyoteStim v0.5.8

A R.E.P.O. DG-LAB Coyote 3.0 App Socket mod for in-game Coyote connection, hit/death punishment, footstep/action triggers, continuous waveforms, and configurable strength profiles.

Author: **AngelcoMilk**  
GitHub: https://github.com/AngelcoMilk/RepoCoyoteStim

Use the `DG-LAB 3.0+` Android app from Google Play, preferably the 3.x app line. `DG-LAB 4.0` and later are currently not compatible with this Socket workflow.
