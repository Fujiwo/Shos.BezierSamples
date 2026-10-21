# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 概要

「すべての通過点を通る曲線を作る」同じアルゴリズムを Win32 (C++) / WinForms / WPF で書き分けた教材用サンプル集。曲線は自然3次スプライン: 各通過点の接線 m を三重対角の連立方程式 `m[i−1] + 4m[i] + m[i+1] = 3(P[i+1] − P[i−1])` (両端は自然スプラインの条件) をトーマス法で解いて求め、区間ごとに 3 次ベジエ `P[i], P[i] + m[i]/3, P[i+1] − m[i+1]/3, P[i+1]` で描く (`ToSplineTangents` / `ToSplineBezier`、閉じた曲線は Sherman-Morrison で `ToClosedSplineBezier`)。以前の Catmull-Rom 版 (`ToBezierPolyline` / `ToBezierPolygon`、制御点 `P[i+1] ± (P[i+2] − P[i]) / 6`) は比較用に残している。経緯は `Plans/2026-10-22-cubic-spline.md`。

| プロジェクト | 種別 / ターゲット |
|---|---|
| BezierWin32 | C++ Win32, PlatformToolset v145 |
| BezierWinForm / BezierWinForm.NETFramework | WinForms, `net10.0-windows10.0.26100.0` / .NET Framework 4.8 |
| BezierWpf / BezierWpf.NETFramework | WPF, `net10.0-windows10.0.26100.0` / .NET Framework 4.8 |
| WpfCad2Lib.Core | `net10.0` クラスライブラリ (`Subject` = `INotifyPropertyChanged` 基底など) |
| WpfCad2Lib.Wpf | 簡易 CAD ライブラリ (図形・選択・Undo・ズーム/パン・袋文字) |
| WpfCad2Lib.TestApp | WpfCad2Lib の動作確認アプリ |
| BezierSamples.Tests | xUnit。WpfCad2Lib / BezierWpf / BezierWinForm (.NET 10 版) の曲線の変換のテスト |

## ビルド

すべて `Shos.BezierSamples.sln` に含まれる。Windows 専用 (WinForms/WPF/Win32、.NET Framework 4.8) なので、通常は Windows の Visual Studio / MSBuild でビルドする。

```
dotnet build BezierWpf/BezierWpf.csproj          # .NET 10 版は個別に dotnet build 可
msbuild Shos.BezierSamples.sln -restore          # C++ / .NET Framework を含む全体
dotnet test BezierSamples.Tests/BezierSamples.Tests.csproj
```

- CI・lint 設定は無い。
- WSL の dotnet でも `-p:EnableWindowsTargeting=true` を付ければ .NET 10 版はビルドできる (リポジトリ内の obj/ を汚さないよう `-p:IntermediateOutputPath=<一時ディレクトリ>/` も付ける)。ただしテストと WPF / WinForms アプリの実行、.NET Framework 版と C++ のビルドはできない。
- この環境では WSL から Windows 側のツールを呼べる: `C:\Program Files\dotnet\dotnet.exe` (テスト実行)、Visual Studio 2026 の `VsDevCmd.bat` / `vcvars64.bat` 経由の MSBuild・cl (全体ビルド・C++)。リポジトリの Windows パスは `C:\dropboxfolder\dropbox\source\github\repos\Shos.BezierSamples.2013.2.19`。
- アプリを実際に操作しての確認はできないので、その旨を明記する。

## アーキテクチャ

- **BezierWinForm**: `Model` / `Controller` / `View` をデザイナーで配置する `Component` として実装した MVC。`Controller` はドラッグ中の `BezierLine` を保持し、`Update` イベントで `View` に再描画を要求する (`View` は `DoubleBuffered` で、確定済みの図形とドラッグ中の曲線をどちらも `OnPaint` で描く)。MouseUp で `Model.Add` → `Model.Update` → `View.Invalidate`。
- **BezierWpf**: `MouseEventConverter` がマウスイベントをクリック/ドラッグ (`IMouseAcceptor`: `OnClick` / `OnDragStart` / `OnDragging` / `OnDragEnd`) に変換し、`Controller` が現在の `Command` (Edit / 線分 / 矩形 / 曲線など) に委譲する。図形は WPF の `Shape` として `UIElementCollection` に直接追加する。
- **WpfCad2Lib.Wpf**: BezierWpf の構造を発展させたもの。`MVC` クラスが `View` (XAML UserControl) ・`Model`・`Controller` を結線し、`mvc.Command = new LineCommand()` のように描画モードを切り替える。`AddCommand` が `CreateFigure()` で `Figure` を生成し、`Model.FigureAttribute` を複製して適用する。Undo は `Core/CadUndo.cs`、図形の実装は `Figures/Figures.cs`。
- **WpfCad2Lib.TestApp**: `RoutedCommand` からツールバー操作を `mvc.Command` に割り当て、`Resources/sample.jpg` を背景に表示する。

## 規約・注意点

- C# 9.0 以降が使えるプロジェクトでは null 比較を `is null` / `is not null` に統一する (`== null` / `!= null` は使わない)。C++ と .NET Framework 版は対象外。.NET 版はすべて `<Nullable>enable</Nullable>`。
- ソースは UTF-8 (BOM 付き)・CRLF。既存ファイルの編集時はこれを保つ。
- 曲線の変換 (3次スプライン・Catmull-Rom)・`MouseEventConverter` などのプロジェクト間コード重複、および .NET / .NET Framework の二重実装は教材として意図的なもの。共通化を提案・実施しない。
- `Reviews/` にリポジトリレビュー結果 (既知のバグ一覧。例: `CadUndo.Undo/Redo` の遅延列挙の二重評価) がある。関連箇所を触る前に参照する。
