# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 概要

「通過点からベジエ曲線を作る」同じアルゴリズムを Win32 (C++) / WinForms / WPF で書き分けた教材用サンプル集。制御点は Catmull-Rom 風に `P[i+1] ± (P[i+2] − P[i]) / 6` で求める (`ToBezierPolyline` / `ToBezierPolygon` など)。

| プロジェクト | 種別 / ターゲット |
|---|---|
| BezierWin32 | C++ Win32, PlatformToolset v145 |
| BezierWinForm / BezierWinForm.NETFramework | WinForms, `net10.0-windows10.0.26100.0` / .NET Framework 4.8 |
| BezierWpf / BezierWpf.NETFramework | WPF, `net10.0-windows10.0.26100.0` / .NET Framework 4.8 |
| WpfCad2Lib.Core | `net10.0` クラスライブラリ (`Subject` = `INotifyPropertyChanged` 基底など) |
| WpfCad2Lib.Wpf | 簡易 CAD ライブラリ (図形・選択・Undo・ズーム/パン・袋文字) |
| WpfCad2Lib.TestApp | WpfCad2Lib の動作確認アプリ |

## ビルド

すべて `Shos.BezierSamples.sln` に含まれる。Windows 専用 (WinForms/WPF/Win32、.NET Framework 4.8) なので、通常は Windows の Visual Studio / MSBuild でビルドする。

```
dotnet build BezierWpf/BezierWpf.csproj          # .NET 10 版は個別に dotnet build 可
msbuild Shos.BezierSamples.sln                   # C++ / .NET Framework を含む全体
```

- テストプロジェクト・CI・lint 設定は無い。
- Linux/WSL ではプラットフォーム非依存の `WpfCad2Lib.Core` 以外はビルド・実行できない。動作確認できなかった場合はその旨を明記する。

## アーキテクチャ

- **BezierWinForm**: `Model` / `Controller` / `View` をデザイナーで配置する `Component` として実装した MVC。`Controller` はドラッグ中の `BezierLine` を保持して `Update` イベントで `View` に直接描画させ、MouseUp で `Model.Add` → `Model.Update` → `View.Invalidate` で全体を再描画する。
- **BezierWpf**: `MouseEventConverter` がマウスイベントをクリック/ドラッグ (`IMouseAccepter`: `OnClick` / `OnDragStart` / `OnDragging` / `OnDragEnd`) に変換し、`Controller` が現在の `Command` (Edit / 線分 / 矩形 / 曲線など) に委譲する。図形は WPF の `Shape` として `UIElementCollection` に直接追加する。
- **WpfCad2Lib.Wpf**: BezierWpf の構造を発展させたもの。`MVC` クラスが `View` (XAML UserControl) ・`Model`・`Controller` を結線し、`mvc.Command = new LineCommand()` のように描画モードを切り替える。`AddCommand` が `CreateFigure()` で `Figure` を生成し、`Model.FigureAttribute` を複製して適用する。Undo は `Core/CadUndo.cs`、図形の実装は `Figures/Figures.cs`。
- **WpfCad2Lib.TestApp**: `RoutedCommand` からツールバー操作を `mvc.Command` に割り当て、`Resources/sample.jpg` を背景に表示する。

## 規約・注意点

- C# 9.0 以降が使えるプロジェクトでは null 比較を `is null` / `is not null` に統一する (`== null` / `!= null` は使わない)。C++ と .NET Framework 版は対象外。.NET 版はすべて `<Nullable>enable</Nullable>`。
- ソースは UTF-8 (BOM 付き)・CRLF。既存ファイルの編集時はこれを保つ。
- ベジエ変換・`MouseEventConverter` などのプロジェクト間コード重複、および .NET / .NET Framework の二重実装は教材として意図的なもの。共通化を提案・実施しない。
- `Reviews/` にリポジトリレビュー結果 (既知のバグ一覧。例: `CadUndo.Undo/Redo` の遅延列挙の二重評価) がある。関連箇所を触る前に参照する。
