# Swdz Highheels

一个用于 **Aicomi/Samabake/Honeycome(IL2CPP + BepInEx 6)** 的高跟鞋与姿势补正插件。  
支持按鞋子自动应用高度/脚踝/脚趾参数，并可在不同姿势和不同鞋子下保存独立姿势预设。

## 功能概览

- 自动识别鞋子并应用预设（高度、脚踝角度、脚趾角度）
- 编辑模式实时调参（GUI）
- 支持姿势补正（髋部偏移、大腿角、小腿角）
- 脱鞋（鞋模型不可见/禁用）时自动关闭高跟与姿势附加修正
- 支持 H 场景持续应用姿势修正

## 环境要求

- BepInEx 6（IL2CPP 版本）

## 安装

1. 将解压后的BepinEx文件夹直接复制到游戏根目录。你应该能在下面路径找到插件dll 
   `BepInEx/plugins/SwdzHighheels`
2. 启动游戏一次，插件会自动生成配置目录：  
   `BepInEx/plugins/SwdzHighheels/config/`

## 使用说明

### 打开界面

- 默认快捷键：`H`
- 可在配置中修改（`GUI Key`）

### 基础流程

1. 穿上鞋子，确认界面里检测到鞋子信息
2. 开启 `Edit Mode`
3. 调整：
   - `Height`
   - `Ankle`
   - `Toe`
4. 点击 `Save Config` 保存鞋子预设

### 姿势补正流程

1. 在编辑模式下开启 `Enable Pose Adjust`
2. 调整：
   - `Pose Hip Offset`
   - `Pose Thigh Angle`
   - `Pose Knee Angle`
3. 点击 `Save Pose Config` 保存姿势预设

> 姿势预设现在按“鞋子 + 顶点数 + 姿势”读取，  
> 同一姿势在不同鞋上可使用不同参数。

## 预设文件规则

### 鞋子预设（config 根目录）

- 文件名：`<鞋子显示名>_<顶点数>.json`
- 示例：`boots1_1196.json`

### 姿势预设（config/animation）

- 文件名：`pose_<联合键>.json`
- 联合键逻辑：`鞋名#顶点数@@姿势名`

## 界面中的“当前读取预设”

界面会显示：

- `Shoe Preset`：当前命中的鞋子预设名
- `Pose Preset`：当前命中的姿势预设键

若显示 `None`，表示当前状态未命中对应预设。
