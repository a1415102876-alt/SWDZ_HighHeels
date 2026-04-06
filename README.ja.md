# Swdz Highheels

[English](./Readme.md) | [简体中文](./Readme.zh-CN.md) | 日本語

**Aicomi / Samabake / Honeycome（IL2CPP + BepInEx 6）** 向けの、ハイヒール調整＆ポーズ補正プラグインです。  
靴ごとに「身長（高さ）/ 足首角度 / つま先角度」を自動適用でき、さらに **ポーズ（アニメ）や靴ごと** に独立したポーズプリセットを保存できます。

## 機能概要

- 靴を自動検出してプリセットを適用（Height / Ankle / Toe）
- Edit Mode（GUI）でリアルタイム調整
- ポーズ補正に対応（Hip Offset / Thigh Angle / Knee Angle）
- 脱鞋（靴モデルが非表示/無効）時に、ハイヒール調整＆ポーズ補正を自動で無効化
- H シーンでもポーズ補正を継続適用

## 必要環境

- BepInEx 6（IL2CPP 版）

## インストール

1. 解凍した `BepInEx` フォルダを、そのままゲームのルートフォルダにコピーします。  
   プラグイン DLL は次の場所にあります：  
   `BepInEx/plugins/SwdzHighheels`
2. ゲームを 1 回起動すると、設定フォルダが自動生成されます：  
   `BepInEx/plugins/SwdzHighheels/config/`

## 使い方

### UI を開く

- デフォルトのホットキー：`H`
- 設定（`GUI Key`）で変更可能

### 基本手順

1. 靴を装備し、UI に靴情報が表示されることを確認
2. `Edit Mode` をオン
3. 次を調整：
   - `Height`
   - `Ankle`
   - `Toe`
4. `Save Config` を押して靴プリセットを保存

### ポーズ補正の手順

1. Edit Mode で `Enable Pose Adjust` をオン
2. 次を調整：
   - `Pose Hip Offset`
   - `Pose Thigh Angle`
   - `Pose Knee Angle`
3. `Save Pose Config` を押してポーズプリセットを保存

> ポーズプリセットは **「靴 + 頂点数 + ポーズ」** で読み込みます。  
> 同じポーズでも靴が違えば、別々の補正値を使えます。

## プリセットのファイル規則

### 靴プリセット（config 直下）

- ファイル名：`<靴の表示名>_<頂点数>.json`
- 例：`boots1_1196.json`

### ポーズプリセット（`config/animation`）

- ファイル名：`pose_<複合キー>.json`
- 複合キー形式：`靴名#頂点数@@ポーズ名`

## UI の「現在読み込み中のプリセット」

UI には次が表示されます：

- `Shoe Preset`：現在ヒットしている靴プリセット名
- `Pose Preset`：現在ヒットしているポーズプリセットキー

`None` と表示される場合、現在の状態ではプリセットがヒットしていません。

