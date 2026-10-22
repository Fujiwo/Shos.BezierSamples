# Shos.BezierSamples

マウスで指定した**すべての通過点を滑らかに通る曲線 (3 次スプライン曲線)** を、3 次ベジエ曲線の列として描くサンプル集です。
同じアルゴリズムを Win32 (C++)・Windows Forms・WPF で書き分けているので、API ごとの違いを見比べられます。
WPF 版を発展させた簡易 CAD ライブラリ **WpfCad2Lib** も含まれています。

> 教材として、プロジェクトごとに独立して読めるようにしています。そのため、曲線の変換のコードや .NET / .NET Framework 版の実装は、意図的にプロジェクトごとに重複させています。

## 目次

- [Shos.BezierSamples](#shosbeziersamples)
  - [目次](#目次)
  - [プロジェクト構成](#プロジェクト構成)
  - [動作環境とビルド](#動作環境とビルド)
  - [サンプルの操作方法](#サンプルの操作方法)
  - [通過点を通る曲線を作るしくみ](#通過点を通る曲線を作るしくみ)
    - [1 区間の制御点](#1-区間の制御点)
    - [接線を求める連立方程式](#接線を求める連立方程式)
    - [点列全体](#点列全体)
    - [閉じた曲線](#閉じた曲線)
    - [点の間引きとドラッグ中の描画](#点の間引きとドラッグ中の描画)
    - [Catmull-Rom スプラインとの比較](#catmull-rom-スプラインとの比較)
    - [プロジェクトごとの関数](#プロジェクトごとの関数)
  - [WpfCad2Lib の使い方](#wpfcad2lib-の使い方)
  - [WpfCad2Lib のクラス図](#wpfcad2lib-のクラス図)
    - [MVC の構成](#mvc-の構成)
    - [作図コマンド](#作図コマンド)
    - [図形](#図形)
    - [ユーティリティ (幾何計算・ベジエ変換)](#ユーティリティ-幾何計算ベジエ変換)
    - [ユーティリティ (描画・共通)](#ユーティリティ-描画共通)
  - [既知の制限](#既知の制限)
  - [ライセンス](#ライセンス)

## プロジェクト構成

| プロジェクト | 種類 | ターゲット | 内容 |
|---|---|---|---|
| `BezierWin32` | C++ / Win32 | PlatformToolset v145, C++20 | クリックした点を通る曲線を描く最小のサンプル |
| `BezierWinForm` | C# / Windows Forms | `net10.0-windows10.0.26100.0` | ドラッグした軌跡を通る曲線を描く。Model / View / Controller をデザイナーで配置するコンポーネントとして実装 |
| `BezierWinForm.NETFramework` | C# / Windows Forms | .NET Framework 4.8 | `BezierWinForm` の .NET Framework 版 |
| `BezierWpf` | C# / WPF | `net10.0-windows10.0.26100.0` | 線分・矩形・曲線をメニューで切り替えて描く、コマンド方式のサンプル |
| `BezierWpf.NETFramework` | C# / WPF | .NET Framework 4.8 | `BezierWpf` の .NET Framework 版 |
| `WpfCad2Lib.Core` | C# クラスライブラリ | `net10.0` | UI に依存しない共通部品 (`Subject` など) |
| `WpfCad2Lib.Wpf` | C# / WPF クラスライブラリ | `net10.0-windows10.0.26100.0` | 簡易 CAD ライブラリ。図形・選択・ズーム / パン・袋文字 |
| `WpfCad2Lib.TestApp` | C# / WPF アプリ | `net10.0-windows10.0.26100.0` | WpfCad2Lib の動作確認アプリ |
| `BezierSamples.Tests` | C# / xUnit | `net10.0-windows10.0.26100.0` | 曲線の変換の単体テスト (WpfCad2Lib・BezierWpf・BezierWinForm) |

```text
Shos.BezierSamples.sln
├─ BezierWin32/                  … Win32 (C++)
├─ BezierWinForm/                … Windows Forms (.NET 10)
├─ BezierWinForm.NETFramework/   … Windows Forms (.NET Framework 4.8)
├─ BezierWpf/                    … WPF (.NET 10)
├─ BezierWpf.NETFramework/       … WPF (.NET Framework 4.8)
├─ WpfCad2Lib.Core/              … 共通ライブラリ
├─ WpfCad2Lib.Wpf/               … 簡易 CAD ライブラリ
│   ├─ Core/                     … MVC・コマンド・図形の基底・ズーム / パン
│   ├─ Commands/                 … 作図コマンド
│   ├─ Figures/                  … 図形
│   └─ Utility/                  … 曲線の変換・幾何計算・袋文字など
├─ WpfCad2Lib.TestApp/           … 動作確認アプリ
├─ BezierSamples.Tests/          … 単体テスト
├─ docs/images/                  … README の図
├─ Plans/                        … 変更の計画書
└─ Reviews/                      … リポジトリのレビュー結果
```

## 動作環境とビルド

すべてのプロジェクトが Windows 専用です。

- Windows 10 / 11
- Visual Studio 2026 (PlatformToolset v145)
  - ワークロード「.NET デスクトップ開発」
  - ワークロード「C++ によるデスクトップ開発」 (`BezierWin32` をビルドする場合)
- .NET 10 SDK
- Windows SDK 10.0.26100
- .NET Framework 4.8 Developer Pack (`*.NETFramework` をビルドする場合)

Visual Studio で `Shos.BezierSamples.sln` を開き、スタートアップ プロジェクトを選んで実行します。
コマンドラインの場合は次のとおりです。

```powershell
# ソリューション全体 (C++ と .NET Framework を含む)。Developer PowerShell で実行する
msbuild Shos.BezierSamples.sln -restore

# .NET 10 版は、プロジェクトごとに dotnet コマンドでビルド・実行できる
dotnet run --project BezierWpf/BezierWpf.csproj
dotnet run --project WpfCad2Lib.TestApp/WpfCad2Lib.TestApp.csproj

# 単体テスト
dotnet test BezierSamples.Tests/BezierSamples.Tests.csproj
```

## サンプルの操作方法

| サンプル | 操作 |
|---|---|
| `BezierWin32` | 左クリックで通過点を追加します。右クリックで全消去します。 |
| `BezierWinForm` | 左ボタンでドラッグすると、その軌跡を通る曲線になります。 |
| `BezierWpf` | 「図形」メニューで線分・矩形・曲線を選んでからドラッグします。 |
| `WpfCad2Lib.TestApp` | 下の表を参照してください。 |

`WpfCad2Lib.TestApp` の操作は次のとおりです。

| 操作 | 内容 |
|---|---|
| 「図形」→「選択」 | クリックした位置の近くにある図形を選びます。何もない所をクリックすると選択を解除します。 |
| 「図形」→「線分」「矩形」「楕円」「曲線」「矢印」 | ドラッグで作図します。 |
| 「図形」→「文字列」 | クリックした位置に袋文字を置きます。 |
| 「設定」→「サイズ」「色」 | これから描く図形の線の太さ・文字サイズ・色を変えます。 |
| 「画面」→「ホーム」 | ズームとパンを初期状態に戻します。 |
| マウスホイール | 奥に回すと拡大、手前に回すと縮小します (等倍より小さくはなりません)。 |
| 矢印キー | 表示をパンします。先に作図領域をクリックしてフォーカスを移してください。 |

## 通過点を通る曲線を作るしくみ

どのプロジェクトも、通過点の列から**自然 3 次スプライン曲線** (すべての点を通り、つなぎ目で曲率まで連続する曲線) を求め、3 次ベジエ曲線の列として描いています。

### 1 区間の制御点

各通過点 P(i) での曲線の接線 (向きと速さ) を m(i) とします。
すると、隣り合う通過点の区間 P(i) → P(i+1) は、次の制御点を持つ 3 次ベジエ曲線になります。

![通過点での接線 m から、区間 P(i)→P(i+1) の制御点 C1, C2 を求める図](docs/images/spline-control-points.svg)

```text
C1 = P(i)   + m(i)   / 3
C2 = P(i+1) − m(i+1) / 3
```

3 次ベジエ曲線は、端点での接線が「端点から隣の制御点へのベクトルの 3 倍」になります。
そのため、通過点から接線の 1/3 だけ離れた位置に制御点を置けば、その通過点で接線 m を持つ曲線になります (3 次エルミート曲線をベジエで表したもの)。
隣の区間とは同じ接線を共有するので、通過点で向きがそろいます。

### 接線を求める連立方程式

接線 m は、つなぎ目で**曲がり具合 (2 階微分) もそろう**ように決めます。
各区間をパラメーター t (0〜1、どの区間も同じ幅) の 3 次式とすると、この条件から、内側の通過点ごとに次の式が成り立ちます。

```text
m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1))      (i = 1 … n−2)
```

未知数 m が n 個に対して式が n − 2 個なので、両端に条件を足します。
**自然スプライン**では、端点で曲がり具合を 0 にします。

```text
2·m(0)   + m(1)     = 3·(P(1)   − P(0))
m(n−2)   + 2·m(n−1) = 3·(P(n−1) − P(n−2))
```

まとめると、係数が対角とその左右だけにある**三重対角**の連立方程式になります。

```text
| 2 1           |   | m(0)   |   | 3·(P(1) − P(0))     |
| 1 4 1         |   | m(1)   |   | 3·(P(2) − P(0))     |
|   1 4 1       | · | m(2)   | = | 3·(P(3) − P(1))     |
|     …  …  …   |   |  …     |   |  …                  |
|         1 2   |   | m(n−1) |   | 3·(P(n−1) − P(n−2)) |
```

- どの行も対角の係数 (4 や 2) が左右の係数の和より大きい (対角優位) ので、**トーマス法** (前進消去と後退代入) で、ピボット選択なしに O(n) で安定して解けます。
- x 座標と y 座標は同じ係数行列なので、右辺だけを替えて 2 回解きます。

WpfCad2Lib の実装 (`GeoExtensions` の `SolveTridiagonal`) は次のとおりです。

```csharp
// 三重対角の連立方程式 lower[i]·x(i−1) + diagonal[i]·x(i) + upper[i]·x(i+1) = right[i] を、
// トーマス法 (前進消去と後退代入) で解く。lower[0] と upper[n−1] は使わない
static double[] SolveTridiagonal(double[] lower, double[] diagonal, double[] upper, double[] right)
{
    var count        = diagonal.Length;
    var reducedUpper = new double[count];   // 前進消去後の upper (対角を 1 に正規化したもの)
    var reducedRight = new double[count];   // 前進消去後の right
    // 前進消去: 各行から 1 つ上の行の lower 倍を引き、左側の係数を消していく
    for (var index = 0; index < count; index++) {
        var previousUpper = index == 0 ? 0.0 : reducedUpper[index - 1];
        var previousRight = index == 0 ? 0.0 : reducedRight[index - 1];
        var denominator   = diagonal[index] - lower[index] * previousUpper;
        reducedUpper[index] = upper[index] / denominator;
        reducedRight[index] = (right[index] - lower[index] * previousRight) / denominator;
    }
    // 後退代入: 最後の行から順に、1 つ下の行の解を使って求める
    var x = new double[count];
    x[count - 1] = reducedRight[count - 1];
    for (var index = count - 2; index >= 0; index--)
        x[index] = reducedRight[index] - reducedUpper[index] * x[index + 1];
    return x;
}
```

### 点列全体

n 個の通過点 P0〜P(n−1) から、最初と最後の区間も含めて n − 1 個のベジエ区間ができます。
変換処理 (`ToSplineBezier`) は、`[P0, C1, C2, P1, C1, C2, P2, …, P(n−1)]` の 1 + 3(n − 1) 点を返します。
これは、各 API のベジエ描画関数が受け取る形式そのものです。

![通過点の列全体を 3 次スプライン曲線でつなぐ図](docs/images/spline-polyline.svg)

| 通過点の数 | 描かれるもの |
|---|---|
| 0〜1 | 何も描かない |
| 2 | 直線 (m(0) = m(1) = P(1) − P(0) になり、ベジエ曲線も直線になる) |
| 3 以上 | n − 1 区間の曲線 |

### 閉じた曲線

`ToClosedSplineBezier` (WinForms の `DrawBezierPolygon`、WPF の `BezierPolygonPathGeometry`) は、最後の点から最初の点へ戻って 1 周する曲線を作ります。

- 最後の点の次を最初の点とみなし、**すべての**通過点で `m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1))` (添字は n を法とする) を解きます。
- 両端の条件は要りませんが、係数行列の左下と右上の角にも 1 が入ります (巡回三重対角)。
- これを **Sherman-Morrison の公式**で、角を除いた三重対角の連立方程式 2 つに分けて、トーマス法で解きます。
- 最初の点でも曲率まで連続した、継ぎ目の無い曲線になります。

### 点の間引きとドラッグ中の描画

- **点の間引き:** ドラッグで点を集めるサンプルでは、マウスの細かい揺れで曲線が波打たないよう、直前の点から一定距離以上動いたときだけ点を追加します。距離は `BezierWinForm` が 10、`BezierWpf` が 16、WpfCad2Lib の `CurveFigure` が 8 です。ドラッグを終えた位置は、距離にかかわらず必ず曲線の終点になります。
- **ドラッグ中の描画 (`CurveFigure`):**
  - 3 次スプラインでは、点を 1 つ足すと全区間の接線が少しずつ変わります。ドラッグ中に毎回全体を作り直すと、長い曲線では全体で O(n²) の計算になります。
  - ただし、1 点の変化の影響は 1 区間離れるごとに約 0.27 倍 (2 − √3) に減ります。
  - そこでドラッグ中は、末尾の 6 区間の接線だけを解き直して差し替え、それより前の接線は固定します (影響は 0.27⁶ ≈ 0.04%)。
  - ドラッグを終えたときに、全体を正確に作り直します。
  - ほかのサンプルは点が少ないか、描くたびに全体を計算しても十分に速いので、毎回全体を作り直しています。

### Catmull-Rom スプラインとの比較

**Catmull-Rom スプライン**による曲線描画と3 次スプラインを比較します。
**Catmull-Rom スプライン**による変換関数 (`ToBezierPolyline` / `ToBezierPolygon` など) は、各プロジェクトにあります。

違うのは**接線 m の求め方だけ**です。Catmull-Rom は、両隣の点だけで `m(i) = (P(i+1) − P(i−1)) / 2` と決めます。
制御点は `P(i+1) ± (P(i+2) − P(i)) / 6` になります (1/6 = 接線の 1/2 × 制御点の位置 1/3)。

![Catmull-Rom スプラインで区間 P1→P2 の制御点を求める図](docs/images/bezier-control-points.svg)

3 次スプラインの連立方程式で、両隣の接線が自分と同じ (m(i−1) = m(i+1) = m(i)) と仮定すると、`6·m(i) = 3·(P(i+1) − P(i−1))` となります。
これは Catmull-Rom の接線そのものです。つまり Catmull-Rom は、3 次スプラインの連立方程式を、その場の情報だけで近似したものといえます。

![同じ通過点での 3 次スプライン曲線と Catmull-Rom スプラインの比較](docs/images/spline-vs-catmull-rom.svg)

| | 3 次スプライン | Catmull-Rom |
|---|---|---|
| 点を通るか | 通る | 通る |
| 連続性 | C² (曲率まで) | C¹ (向きまで) |
| 最初と最後の区間 | 曲線 (自然スプラインの条件) | 直線 (片側の点が無く向きが決まらない) |
| 1 点を動かしたときの影響 | 全区間 (1 区間ごとに約 0.27 倍に減る) | 近くの 4 区間だけ |
| 計算 | 三重対角の連立方程式 (O(n)) | 区間ごとに個別に計算 |
| 最適性 | 曲がりの総量 ∫‖C″‖² が最小 | 特に無い |

Catmull-Rom では、最初の区間 (P0 → P1) と最後の区間は直線になっています。

![Catmull-Rom スプラインで通過点の列全体をベジエ曲線と直線でつなぐ図](docs/images/bezier-polyline.svg)

### プロジェクトごとの関数

| プロジェクト | 接線・変換 | 描画 |
|---|---|---|
| `BezierWin32` | `Win32Graphics::ToSplineTangents` / `ToSplineBezier` | `::MoveToEx` + `::PolyBezierTo` (2 点のときは `::Polyline`) |
| `BezierWinForm` | `GeometryExtensions.ToSplineTangents` / `ToSplineBezier` / `ToClosedSplineBezier` | `Graphics.DrawBeziers` (2 点のときは `Graphics.DrawLine`) |
| `BezierWpf` | `GeometryExtensions.ToSplineTangents` / `ToSplineBezier` / `ToClosedSplineBezier` | `PathGeometry` + `BezierSegment` |
| `WpfCad2Lib.Wpf` | `GeoExtensions.ToSplineTangents` / `ToSplineBezier` / `ToClosedSplineBezier` | `GeoHelper.BezierPathGeometry` (`PathGeometry`)、`CurveFigure` (差分更新) |

- **座標の型:** Win32 版は整数の `POINT` なので、接線は実数のベクトル (`Vector2`) で求め、制御点を最後に整数へ丸めます。WinForms 版は `float` の `PointF` なので、連立方程式の途中の計算を `double` で行います。
- **単体テスト:** `BezierSamples.Tests` は、.NET 10 版の 3 つの実装 (WpfCad2Lib・BezierWpf・BezierWinForm) について次の点を確かめています。
  - すべての点を通ること
  - つなぎ目で 1 階微分と 2 階微分が一致すること
  - 両端で 2 階微分が 0 になること
  - 実装どうしで同じ制御点になること

## WpfCad2Lib の使い方

`View` (UserControl) をウィンドウに置き、`MVC` で Model・View・Controller を結び付けます。
描画モードは `mvc.Command` に作図コマンドを設定して切り替えます。

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:w="clr-namespace:WpfCad2Lib.Wpf;assembly=WpfCad2Lib.Wpf">
    <w:View x:Name="view" />
</Window>
```

```csharp
using System.Windows;
using System.Windows.Media;
using WpfCad2Lib.Wpf;

public partial class MainWindow : Window
{
    readonly MVC mvc;

    public MainWindow()
    {
        InitializeComponent();
        mvc = new MVC(view, new EditCommand());                  // 最初は選択モード
        mvc.Model.Area = new Rect(new Size(1000.0, 1000.0));     // 作図領域 (モデル座標)
    }

    void OnCurve(object sender, RoutedEventArgs e) => mvc.Command = new CurveCommand();
    void OnText (object sender, RoutedEventArgs e) => mvc.Command = new TextCommand { Text = "ラベル" };
    void OnBlue (object sender, RoutedEventArgs e) => mvc.Model.FigureAttribute.SetColor(Colors.Blue);
    void OnHome (object sender, RoutedEventArgs e) => view.Home();
}
```

- `Model.FigureAttribute` (色・線の太さ・文字サイズ) は、**これから描く図形**に複製して使われます。描き終えた図形には影響しません。
- 背景に画像を表示するには、`view.Image.Source` に画像を設定します。`WpfCad2Lib.TestApp` の `MainWindow.xaml.cs` を参照してください。
- 独自の図形を追加するには、次の 2 つを作ります。
  - `Figure` の派生クラス。protected の `Bounds`・`GetInitialElements`・`SetFirst` を実装し、必要なら `SetNext` / `SetLast` もオーバーライドします。
  - その図形を返す `AddCommand` の派生クラス。`CreateFigure` をオーバーライドします。

## WpfCad2Lib のクラス図

利用者向けに、**public な型とメンバーだけ**を載せています (継承用の protected メンバーと private メンバーは省略)。
表記は次のとおりです。

- `«set»`: 書き込み専用のプロパティ
- `$`: static メンバー
- `*`: 抽象メンバー

型はすべて名前空間 `WpfCad2Lib.Wpf` にあります。ただし `Subject`・`MathHelper`・`EnumerableExtension`・`ObjectExtensions` は `WpfCad2Lib.Core` です。

### MVC の構成

```mermaid
classDiagram
    direction LR

    class MVC {
        +Model Model
        +Command Command «set»
        +MVC(View view, Command defaultCommand)
    }
    class View {
        +Model Model «set»
        +Image Image
        +Controller Controller «set»
        +View()
        +Home() void
        +AddModelAreaRect() void
    }
    class Model {
        +FigureAttribute FigureAttribute
        +Rect Area
        +Add(Figure item) void
        +Select(UIElementCollection elementCollection, Point position) void
    }
    class IMouseAcceptor {
        <<interface>>
        +Model Model «set»
        +UIElementCollection ElementCollection «set»
        +OnClick(Point position) void
        +OnDragStart(Point position) void
        +OnDragging(Point position) void
        +OnDragEnd(Point position) void
    }
    class Controller {
        +Model? Model «set»
        +UIElementCollection? ElementCollection «set»
        +Command Command «set»
        +OnClick(Point position) void
        +OnDragStart(Point position) void
        +OnDragging(Point position) void
        +OnDragEnd(Point position) void
    }
    class Device {
        +Rect ModelArea
        +Rect ViewArea
        +bool IsHome
        +Matrix ModelToViewMatrix
        +Matrix ViewToModelMatrix
        +Home() void
        +Zoom(Point zoomCenterModelPoint, double rate) void
        +Pan(Direction direction, double rate) void
    }
    class CadData {
        +FigureAttribute FigureAttribute
        +bool CanUndo
        +bool CanRedo
        +bool IsEmpty
        +int Count
        +GetEnumerator() IEnumerator~Figure~
        +Add(Figure figure) void
        +Remove(Figure figure) void
        +Update(IList~Figure~ figures, Figure olderFigure, Figure newerFigure) bool
        +RemoveAll() void
        +Select(UIElementCollection elementCollection, Point position, double nearDistance) void
        +GetSelected(bool isSelected) IEnumerable~Figure~
        +GetSelectedOne(bool isSelected) Figure?
        +Find(Point point, double nearDistance) Figure?
        +Undo() void
        +Redo() void
        +Clear() void
        +StartCommand() void
    }
    class Subject {
        +PropertyChangedEventHandler? PropertyChanged «event»
    }
    class FigureAttribute {
        +Color StrokeColor
        +Color FillColor
        +double StrokeThickness
        +double FontSize
        +SetColor(Color color) void
        +SetAttribute(IEnumerable~FrameworkElement~ elements, bool isFilled) void
        +Clone() object
    }

    MVC --> View
    MVC --> Model
    MVC --> Controller
    View --> Model
    View --> Controller
    View ..> Device : ズーム / パン
    Controller ..|> IMouseAcceptor
    Controller --> Command : 現在のコマンドに委譲
    Command ..|> IMouseAcceptor
    Command --> Model
    Model ..> Figure : Add
    Model --> FigureAttribute : これから描く図形の属性
    CadData o-- Figure
    CadData --> FigureAttribute
    Figure --> FigureAttribute
    FigureAttribute --|> Subject
```

### 作図コマンド

```mermaid
classDiagram
    direction TB

    class IMouseAcceptor {
        <<interface>>
    }
    class Command {
        <<abstract>>
        +Model? Model «set»
        +UIElementCollection? ElementCollection «set»
        +bool CanClick
        +bool CanDrag
        +OnStart() bool
        +OnClick(Point position) void
        +OnDragStart(Point position) void
        +OnDragging(Point position) void
        +OnDragEnd(Point position) void
    }
    class AddCommand {
        <<abstract>>
        +OnClick(Point position) void
        +OnDragStart(Point position) void
        +OnDragging(Point position) void
        +OnDragEnd(Point position) void
    }
    class EditCommand {
        +bool CanClick
        +bool CanDrag
        +OnClick(Point position) void
    }
    class TextCommand {
        +string Text
        +bool CanClick
        +bool CanDrag
    }
    class LineCommand
    class RectangleCommand
    class EllipseCommand
    class CurveCommand
    class ArrowCommand

    Command ..|> IMouseAcceptor
    Command <|-- EditCommand
    Command <|-- AddCommand
    AddCommand <|-- LineCommand
    AddCommand <|-- RectangleCommand
    AddCommand <|-- EllipseCommand
    AddCommand <|-- CurveCommand
    AddCommand <|-- ArrowCommand
    AddCommand <|-- TextCommand
```

`AddCommand` の派生クラスは、それぞれ同じ名前の図形 (`LineCommand` → `LineFigure` など) を作ります。

### 図形

```mermaid
classDiagram
    direction TB

    class Figure {
        <<abstract>>
        +FigureAttribute Attribute
        +bool IsSelected
        +bool IsValid
        +bool IsFilled
        +AddElements(UIElementCollection elementCollection, Point firstPoint) void
        +RemoveElements(UIElementCollection elementCollection) void
        +Select(UIElementCollection elementCollection, bool isSelected) void
        +SetNext(Point point) void
        +SetLast(Point point) void
        +IsNear(Point point, double nearDistance, ref double minimumDistance) bool
    }
    class RectFigure {
        <<abstract>>
    }
    class TextFigure {
        +string DefaultText$
        +string Text
    }
    class LineFigure
    class RectangleFigure
    class EllipseFigure
    class CurveFigure
    class ArrowFigure
    class OutlineText {
        +bool EnableFill
        +bool OverFill
        +PenLineJoin OutlineJoin
        +double OutlineMiterLimit
        +bool Bold
        +bool Italic
        +bool Highlight
        +Brush Fill
        +Brush Stroke
        +double StrokeThickness
        +FontFamily Font
        +double FontSize
        +string Text
        +OutlineText()
        +CreateText() void
    }

    Figure <|-- LineFigure
    Figure <|-- RectFigure
    RectFigure <|-- RectangleFigure
    RectFigure <|-- EllipseFigure
    Figure <|-- CurveFigure
    Figure <|-- ArrowFigure
    Figure <|-- TextFigure
    TextFigure ..> OutlineText : 袋文字で表示
```

- 各図形は `IsValid` と `IsFilled` を必要に応じてオーバーライドします。
- `OutlineText` (`FrameworkElement` の派生) の各プロパティには、対応する依存関係プロパティ (`TextProperty` などの static フィールド) があります。

### ユーティリティ (幾何計算・ベジエ変換)

```mermaid
classDiagram
    direction LR

    class GeoExtensions {
        <<static>>
        +Distance(this Point point1, Point point2)$ double
        +Add(this Point point1, Point point2)$ Point
        +Multiply(this Point point, double rate)$ Point
        +Center(this Rect rect)$ Point
        +Transform(this Matrix matrix, Rect rect)$ Rect
        +IsValidForBezier(this Point point, IList~Point~ points, double MinimumDistance)$ bool
        +ToBezierPolyline(this IList~Point~ points)$ IEnumerable~Point~
        +ToBezierPolygon(this IList~Point~ points)$ IEnumerable~Point~
        +ToSplineTangents(this IList~Point~ points)$ Vector[]
        +ToSplineBezier(this IList~Point~ points)$ IEnumerable~Point~
        +ToClosedSplineTangents(this IList~Point~ points)$ Vector[]
        +ToClosedSplineBezier(this IList~Point~ points)$ IEnumerable~Point~
    }
    class GeoHelper {
        <<static>>
        +BezierPath(IList~Point~ points, Color color, double strokeThickness)$ Path
        +BezierPolygonPath(IList~Point~ points, Color color, double strokeThickness)$ Path
        +BezierPathGeometry(IList~Point~ points)$ PathGeometry
        +BezierPolygonPathGeometry(IList~Point~ points)$ PathGeometry
    }
    class Geo {
        <<static>>
        +ToBezier(Point point1, Point point2, Point point3, Point point4)$ IEnumerable~Point~
        +Average(double value1, double value2)$ double
        +Center(Point point1, Point point2)$ Point
        +PointByOffset(Point point1, Point point2, double offset)$ Point
        +PointByDistance(Point point1, Point point2, double offset)$ Point
        +ToRect(this IEnumerable~Point~ points)$ Rect?
        +ToPoints(this Rect rect)$ IEnumerable~Point~
        +Distance(Point point, LineGeometry line)$ double
        +Distance(Point point, Rect rect, bool isFilled)$ double
        +Distance(Point point, IEnumerable~Point~ points)$ double
    }
    class Direction {
        <<enumeration>>
        Left
        Right
        Up
        Down
    }
    GeoHelper ..> GeoExtensions : ToSplineBezier / ToClosedSplineBezier
    GeoExtensions ..> Geo
```

### ユーティリティ (描画・共通)

```mermaid
classDiagram
    direction LR

    class Helper {
        <<static>>
        +CreateStrokeBrush(Color strokeColor)$ Brush
        +CreateFillBrush(Color fillColor)$ Brush
    }
    class HelperExtensions {
        <<static>>
        +IsDark(this Color color)$ bool
        +Multiply(this Color color, float coefficient)$ Color
        +MultiplyRGB(this Color color, float coefficient)$ Color
    }
    class Common {
        <<static>>
        +double DefaultStrokeThickness$
        +double SelectorStrokeThickness$
        +double SelectorRadius$
        +double DefaultFontSize$
        +Color DefaultLightColor$
        +Color DefaultDarkColor$
        +Color DefaultStrokeColor$
        +Color DefaultFillColor$
        +Color SelectorStrokeColor$
        +Color SelectorFillColor$
        +Color DefaultFontStrokeColor$
        +string DefaultFontFamilyName$
        +double DefaultShadowDepth$
        +double DefaultShadowDirection$
        +Color DefaultShadowColor$
        +double DefaultShadowOpacity$
        +double DefaultShadowBlurRadius$
        +double NearDistanceRate$
        +double ZoomMinimumRate$
        +double ZoomMaximumRate$
        +double MouseWheelZoomRate$
        +double DefaultPanRate$
        +DropShadowEffect DefaultShadowEffect$
    }
    class MathHelper {
        <<static>>
        +Average(double value1, double value2)$ double
    }
    class EnumerableExtension {
        <<static>>
        +ForEach~T~(this IEnumerable~T~ collection, Action~T~ action)$ void
    }
    class ObjectExtensions {
        <<static>>
        +GetMemberName~MemberType~(Expression~Func~MemberType~~ expression)$ string
    }

    Helper ..> HelperExtensions
```

## 既知の制限

- **Undo / Redo:** `CadData` に実装はありますが、UI にはつないでいません。つなぐには、操作ごとに `StartCommand()` を呼ぶことと、画面上の要素 (`UIElementCollection`) を同期することが必要です。
- **スクロールバー:** WpfCad2Lib の `View` は `RenderTransform` で拡大するため、拡大しても `ScrollViewer` のスクロールバーは動きません。表示の移動はパンで行います。

ほかの指摘事項と対応状況は、[Reviews/2026-10-21-repository-review.md](Reviews/2026-10-21-repository-review.md) にまとめています。

## ライセンス

[MIT License](LICENSE.txt) © 2023-2026 Fujio Kojima
