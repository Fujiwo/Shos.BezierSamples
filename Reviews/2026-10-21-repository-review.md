# Shos.BezierSamples リポジトリ全体レビュー

- レビュー日: 2026-10-21
- 対象コミット: `2df745f` (.NET 10とv145へ開発環境を更新)
- 対象: リポジトリ内の全ソース (8 プロジェクト、約 5,000 行)
- 方法: 静的レビュー (ソースの通読)。Linux 環境のため Windows 向けのビルド・実行はしていません。「要確認」と書いた項目は、実機での確認を推奨します。

---

## 1. 概要

| プロジェクト | 種別 | ターゲット | 内容 |
|---|---|---|---|
| BezierWin32 | C++ / Win32 | v145, C++20 | クリック点を通るベジエ曲線を描く最小サンプル |
| BezierWinForm | C# / WinForms | net10.0-windows | ドラッグ軌跡をベジエ曲線で描く (MVC 風) |
| BezierWinForm.NETFramework | C# / WinForms | .NET Framework 4.8 | 上と同じ内容の .NET Framework 版 |
| BezierWpf | C# / WPF | net10.0-windows | 線分・矩形・曲線を描くコマンド方式のサンプル |
| BezierWpf.NETFramework | C# / WPF | .NET Framework 4.8 | 上と同じ内容の .NET Framework 版 |
| WpfCad2Lib.Core | C# ライブラリ | net10.0 | 共通ユーティリティ (`Subject`, `Math` など) |
| WpfCad2Lib.Wpf | C# / WPF ライブラリ | net10.0-windows | 簡易 CAD (図形・選択・Undo・ズーム/パン・袋文字) |
| WpfCad2Lib.TestApp | C# / WPF アプリ | net10.0-windows | WpfCad2Lib の動作確認アプリ |

どのプロジェクトも、Catmull-Rom 風の「通過点からベジエ制御点を作る」同じアルゴリズムを実装しています (制御点 = `P[i+1] ± (P[i+2] − P[i]) / 6`)。

## 2. 総評

**良い点**

- 同じベジエ変換を Win32 / WinForms / WPF で書き分けており、API の違いを比べる教材としてわかりやすい。
- 曲線変換部分 (`ToBezierPolyline` など) は副作用のない小さな関数にまとまっていて読みやすい。
- .NET 版はすべて `Nullable` が有効で、`.github/copilot-instructions.md` にある「`is null` / `is not null` に統一する」ルールを守っている (C++ と .NET Framework は対象外。違反はコメントアウトされたコードの 1 か所だけ)。
- WpfCad2Lib は、`MouseEventConverter` でクリックとドラッグを分け、`Command` パターンで描画モードを切り替える構造になっていて拡張しやすい。

**主な課題**

1. **WpfCad2Lib の Undo/Redo と Home 機能に、実際に誤動作するバグがある** (3.1 / 3.2)。
2. 未使用、または配線されていない機能 (Undo、多角形ベジエ、属性変更の反映など) の中に、潜在バグが残っている。
3. コメントアウトされたコードが多い (38 ファイルに存在)。テストもないため、修正の影響を確認しにくい。
4. README がタイトルだけで、各プロジェクトの目的やビルド方法がわからない。

---

## 3. 指摘事項 (重大度順)

重大度: 🔴 高 (実際に誤動作する) / 🟠 中 (潜在バグ・特定の条件で誤動作する・保守上の大きな問題) / 🟡 低 (品質改善・スタイル)

### 3.1 🔴 `CadUndo.Undo` / `Redo` が遅延列挙を 2 回評価している

`WpfCad2Lib.Wpf/Core/CadUndo.cs:177-189`

```csharp
var popedFigures = undoBuffer.Undo();               // yield を使うイテレーター (遅延評価)
popedFigures.ForEach(popedFigure => Undo(figures, popedFigure)); // 1 回目の列挙: Pop して適用
return popedFigures.Count() > 0;                    // 2 回目の列挙: さらに Pop する (適用はしない)
```

`UndoBuffer.Undo()` / `Redo()` は列挙するたびに `Pop()` / `Forward()` を実行し、バッファー位置を動かします。このため `Count()` を呼ぶと**次の Undo グループが適用されないまま読み捨てられ**、履歴とデータの対応が崩れます。また、1 回目の列挙で全件取り出していた場合は `Count()` が 0 になり、戻り値が `false` (何も Undo しなかった) になります。

**修正案**: `var popedFigures = undoBuffer.Undo().ToList();` のように、先に確定させてから使う。

あわせて次の点も確認してください (🟠)。

- `CadData.StartCommand()` がどこからも呼ばれていない (`CadData.cs:124`)。このため `IsStart` が立つのは最初の 1 件だけで、**全操作が 1 つの Undo グループになります**。
- `CadData.Undo()` / `Redo()` は `figures` リストを書き換えるだけで、`UIElementCollection` (画面上の要素) を更新しません。Undo しても画面に図形が残ります。
- 現在 Undo/Redo は TestApp から呼ばれていないため表面化していませんが、機能として配線すると上記がすべて表に出ます。

### 3.2 🔴 `Device.Home()` がパン後に初期表示へ戻らない

`WpfCad2Lib.Wpf/Core/Device.cs:81-85, 97-112`

- `Pan()` は `modelToViewMatrix` を直接平行移動するだけで、`Offset` プロパティを更新しません。
- `Home()` は `Offset = new Vector()` と `Rate = 1.0` を設定しますが、どちらも値が変わらないと `SetModelToViewMatrix()` が呼ばれません。
- `ZoomMinimumRate = 1.0` (`Common.cs:32`) なので、拡大してから縮小で戻した場合も `Rate` は 1.0 のままです。

その結果、**パンだけした後や、拡大→縮小で倍率 1 に戻った後に「画面 → ホーム」を選んでも何も起きません**。

**修正案**: `Home()` の中で、変更の有無にかかわらず `SetModelToViewMatrix()` を呼ぶ。または、`Pan()` で `Offset` を更新し、`GetModelToViewMatrix()` に `offset` を反映させる (いまは該当箇所がコメントアウトされています: `Device.cs:129-139`)。

関連: `OnSizeChanged` で `ViewArea` が変わると行列が作り直されるため、ウィンドウサイズを変えるとパンとズームの平行移動成分も失われます (`View.xaml.cs:115-131`)。

### 3.3 🟠 矢印キーによるパンが動かない可能性が高い (要確認)

`WpfCad2Lib.Wpf/Core/View.xaml:8`, `View.xaml.cs:159-167`

`KeyUp="OnKeyUp"` は `UserControl` に登録されています。しかし `UserControl` は既定で `Focusable=false` で、子の `Canvas` もフォーカスを受けません。キーイベントはフォーカスのある要素から**上方向に**伝わるため、Window にフォーカスがある状態では View まで届きません。

**修正案**: View に `Focusable="True"` を付けてクリック時に `Focus()` する。または、Window 側で `PreviewKeyUp` を受けて View に渡す。

### 3.4 🟠 ドラッグ中にマウスキャプチャーをしていない

`WpfCad2Lib.Wpf/Core/MouseEventConverter.cs:31-82`、`BezierWpf/MouseEventConverter.cs` (`.NETFramework` 版も同じ)

ドラッグ中にポインターが Canvas の外 (メニューやウィンドウ外) に出てボタンを離すと、`MouseLeftButtonUp` が届きません。その場合 `DragEnd` が発生せず、作りかけの図形が確定も削除もされずに残ります。`IsValid` でない図形も残ります。

**修正案**: `MouseLeftButtonDown` で `((UIElement)sender).CaptureMouse()`、`Up` で `ReleaseMouseCapture()` する。`LostMouseCapture` を受けた場合は、ドラッグを中断する処理を入れる。

### 3.5 🟠 `BezierPolygonPathGeometry` の始点がずれている (潜在バグ)

`BezierWpf/Geometry.cs:43-67`、`BezierWpf.NETFramework/Geometry.cs` (同じ内容)、`WpfCad2Lib.Wpf/Utility/Geometry.cs:48-72`

`ToBezierPolygon()` が返す点列は `points[1]` から始まり、`points[0]` → `points[1]` で閉じます。ところが `PathFigure.StartPoint = points[0]` になっているため、次の問題が起きます。

- 最初の `BezierSegment` は `P1→P2` 用の制御点なのに、`P0` から描かれて形が歪む。
- 最後の `LineSegment(points[^1])` と `IsClosed = true` によって、余分な直線が 2 本引かれる。

WinForms 版 (`GraphicHelper.DrawBezierPolygon`) は `bezierPoints` をそのまま `DrawBeziers` に渡しているため正しく描けます。WPF 版は `StartPoint = bezierPoints[0]` とし、末尾の `LineSegment` を削除すべきです。いまは呼び出し元がコメントアウトされているだけなので、使い始めた時点で表面化します。

### 3.6 🟠 `FigureAttribute.Clone()` がイベント購読者まで複製する

`WpfCad2Lib.Wpf/Core/FigureAttribute.cs:111-114`、`Command.cs:84`

`MemberwiseClone()` は `PropertyChanged` の背後にあるデリゲートフィールドもコピーします。そのため、Model 側の `FigureAttribute` に購読者がいる場合、その購読者が複製先の図形にも引き継がれます。図形の属性を変えると、無関係なハンドラーが呼ばれることになります。

**修正案**: `Clone()` では値プロパティだけを持つ新しいインスタンスを返す (コメントアウトされている `Copy` メソッドの方式)。

### 3.7 🟠 `Figure` の属性変更の反映処理が不完全 (潜在バグ)

`WpfCad2Lib.Wpf/Core/Figure.cs:146-186`

- `SetElements()` は呼ばれるたびに `attribute.PropertyChanged += delegate {...}` を追加し、購読を解除しない (ハンドラーが累積する)。
- `Attribute` のセッター (`Figure.cs:17-21`) で属性オブジェクトを差し替えても、購読先は古いオブジェクトのまま。
- `OnAttributeChanged()` は `RegetElements()` の結果で `elements` を置き換えるが、`UIElementCollection` 上の古い要素の削除も新しい要素の追加もしない。
- 実際の描画経路 (`AddElements(collection, firstPoint)` → `InitializeElements()`) では購読自体をしていないため、**作成済み図形の属性を後から変えても画面に反映されません**。

`AddElements(UIElementCollection)` と `ToElements` / `RegetElements` はどこからも呼ばれていません。整理して、片方の経路にまとめることを推奨します。

### 3.8 🟠 `OutlineText.cs` だけが Shift_JIS で保存されている

`WpfCad2Lib.Wpf/Utility/OutlineText.cs`

他のファイルはすべて UTF-8 (BOM 付き) ですが、このファイルだけ Shift_JIS で保存されているため、日本語コメントが UTF-8 前提のツール (GitHub の表示、git diff、Linux 環境など) では文字化けします。コンパイルへの影響はコメントだけなのでありませんが、保守性が下がります。あわせて、インデントにタブとスペースが混在しています。

**修正案**: UTF-8 (BOM 付き) に変換し、インデントをスペースにそろえる。`.editorconfig` に `charset = utf-8-bom` を指定すると再発を防げます。

`OutlineText` については次の点も確認してください (🟡)。

- プロパティのメタデータが `AffectsRender` だけで、`AffectsMeasure` が付いていません。そのため `Text` や `FontSize` を変えても `MeasureOverride` が呼び直されず、レイアウトサイズが古いまま残る可能性があります。
- コンストラクターと `SetTextAttribute` の両方で `Effect` を設定しています。

### 3.9 🟠 TestApp のリソース構成

`WpfCad2Lib.TestApp/MainWindow.xaml.cs:40-51`、`WpfCad2Lib.TestApp.csproj`、`WpfCad2Lib.Wpf.csproj:15-19`

- TestApp が読み込む `Resources\sample.jpg` は、TestApp 自身の csproj では出力先へのコピーが設定されていません。**ライブラリ (WpfCad2Lib.Wpf) の `CopyToOutputDirectory` がプロジェクト参照経由で伝わることで、たまたま動いています**。アプリ固有のリソースはアプリ側に持たせるべきです。
- `WpfCad2Lib.TestApp/Resources/` の `sample.docx` / `sample.pdf` (約 2.2 MB) / `sample.xlsx` / `sample.jpg` はどこからも使われていません。
- 画像が見つからない場合、`new BitmapImage(...)` が例外を投げて起動時に落ちます。
- `App.config` (`supportedRuntime v4.0`) は SDK 形式の .NET 10 プロジェクトでは意味がないので削除できます。
- `Title="MainWindow"` のままになっています。

### 3.10 🟡 コメントに個人のローカルパスと外部 URL が残っている

`WpfCad2Lib.TestApp/MainWindow.xaml.cs:28-37`

コメントアウトされたコードに、ユーザー名を含むローカルパス (`C:/Users/g_kojima_fujio.FC/...`) と、第三者サイトの画像 URL が残っています。公開リポジトリなら削除を推奨します。

### 3.11 🟡 WinForms 版の操作・表示まわり

`BezierWinForm/Controller.cs`, `View.cs`, `MainForm.Designer.cs`, `Program.cs` (`.NETFramework` 版も同じ)

- `OnMouseDown(PointF point)` が引数の `point` を使っていません (`Controller.cs:25-26`)。そのため**曲線の始点がマウスを押した位置ではなく、最初の MouseMove の位置になります**。WPF 版は `StartPosition` から始まるので、動作が揃っていません。
- 左右どのボタンでも描画が始まります。
- ドラッグ中のプレビューは `CreateGraphics()` でアンチエイリアスなしで描かれ、確定後の `OnPaint` ではアンチエイリアスありで描かれるため、見た目が変わります (`View.cs:23-28`)。`DoubleBuffered = true` にして `Invalidate()` で描き直す方式が素直です。
- フォームのサイズが `ClientSize = 1849x1683` と、高 DPI 環境で作ったときの値のまま固定されています (`MainForm.Designer.cs:54`)。100% スケールの画面ではウィンドウが画面からはみ出します。
- .NET 版の `Program.cs` は、旧来の `EnableVisualStyles()` / `SetCompatibleTextRenderingDefault()` を使っています。.NET 6 以降は `ApplicationConfiguration.Initialize()` を使い、`ApplicationHighDpiMode` を設定することを推奨します。
- `Nullable` が有効なのに、デザイナーファイルの `IContainer components = null;` や `private View view;` が非 null 宣言のままです。ビルドすると CS8625 / CS8618 の警告が出るはずです (要確認)。
- `Figure.Draw` は呼ばれるたびに `new Pen(Color.Black)` を作っています。`Pens.Black` で足ります。

### 3.12 🟡 ドラッグ終了点が曲線に含まれないことがある

`BezierWpf/Command.cs:115-127`、`WpfCad2Lib.Wpf/Figures/Figures.cs:168-178`

`OnDragEnd` → `Reshape` / `SetNext` でも感度による間引き (`sensitivity`) が働くため、最後の移動量が小さいと**マウスを離した位置が曲線の終点になりません**。ドラッグ終了時は距離にかかわらず終点を追加することを推奨します。

また、曲線の最初と最後の区間は直線 (`LineSegment`) で描かれます。これは Catmull-Rom 方式で端点の接線が定まらないためですが、端点を複製した仮想点を足すと両端も滑らかな曲線にできます。

### 3.13 🟡 名前の衝突・精度

- `WpfCad2Lib.Core.Math` (`WpfCad2Lib.Core/Math.cs`) は `System.Math` と同じ名前です。`using WpfCad2Lib.Core;` と `using System;` を併記したファイルで `Math.` を使うと CS0104 (あいまいな参照) になります。実際 `Geometry.cs:148` ではそれを避けるために `System.Math.Abs` と書いています。`MathHelper` などへの改名を推奨します。
- `BezierWpf.Geometry` クラスは `System.Windows.Media.Geometry` と同じ名前で、読み手が混乱しやすいです。
- WPF / WpfCad2Lib 版は `double` で計算しているのに、係数が `const float a = 1.0f / 3.0f / 2.0f;` です。`double` に昇格すると `0.16666667163…` となり、わずかに誤差が出ます。`1.0 / 6.0` (double) にすべきです (`BezierWpf/Geometry.cs:107,154`、`WpfCad2Lib.Wpf/Utility/Geometry.cs:79,239`)。

### 3.14 🟡 BezierWin32

`BezierWin32/BezierWin32.cpp`

- 名前空間名が `BezeirWin32` とつづり誤りになっています (`:8, :211, :220`)。
- メンバー関数名 `RegisterClass` は Win32 のマクロ (`RegisterClassW`) と同じ名前のため、プリプロセッサーで置き換えられます (`:96, :113`)。動作はしますが、デバッグ時に混乱するので `RegisterWindowClass` などに改名を推奨します。
- `::floor` を使っているのに `<cmath>` をインクルードしておらず、他のヘッダー経由で取り込まれているだけです (`:29`)。
- `DrawBeziers` の `if (points.size() == 2)` は、直前の `assert(points.size() >= 4)` があるため到達しません (`:54-57`)。
- `wWinMain` の第 3 引数が `LPTSTR` ですが、`LPWSTR` にするのが正しいです (`:215`)。
- `using namespace std;` がグローバルスコープにあります。
- `RegisterClass` の戻り値を見ていません (失敗しても `CreateWindow` の失敗としてしか検出されません)。

### 3.15 🟡 BezierWpf (.NET / .NET Framework)

- `Controller.Model` を後から設定しても、すでに設定済みの `Command` には伝わりません。`View.Controller` → `Controller.Command` の順で設定しないと `Command.Model` が null のままになり、初期化順に依存しています (`BezierWpf/Controller.cs:12-26`、`MainWindow.xaml.cs:17-18`)。
- `.NETFramework` 版の `Controller.Command` セッターは null チェックをしていないため、`null` を設定すると NRE になります (`BezierWpf.NETFramework/Controller.cs:20-24`)。
- `View.Controller` のセッターを 2 回呼ぶと、イベントが二重に購読されます (`View.xaml.cs:17-25`。WpfCad2Lib の `View.xaml.cs:35-45` も同じ)。
- 矩形の位置を `Margin` で指定しています。`Canvas` 上では `Canvas.SetLeft` / `SetTop` を使うのが慣用的です (`Command.cs:98`、WpfCad2Lib の `Figures.cs:104`、`Figure.cs:172`)。
- `AddCommand.Stroke` は呼ばれるたびに新しい `SolidColorBrush` を作っています。`Brushes.Black` (Freeze 済み) で足ります。

### 3.16 🟡 WpfCad2Lib のその他

- `View.xaml` で `Canvas` に `RenderTransform` を使っているため、拡大してもレイアウトサイズが変わらず、`ScrollViewer` のスクロールバーが機能しません。スクロールさせたい場合は `LayoutTransform` を使うか、Canvas の `Width` / `Height` を設定する必要があります (要確認)。
- マウスホイールを上に回すと縮小、下に回すと拡大になります (`View.xaml.cs:156`)。一般的なアプリとは逆です。
- `View.Home()` の `var point = ...` は使われていません (`View.xaml.cs:77`)。`Figure.GetSelectors()` の `selectorPoints` も同様です (`Figure.cs:161`)。
- `Command.canClick` / `canDrag` はプロパティなので、.NET の命名規約では PascalCase (`CanClick`) です。同様に、`RaizeDraggingWithDraggingPositions` → `Raise...`、`popedFigures` → `poppedFigures`、`IMouseAccepter` → `IMouseAcceptor` (英語のつづり) の修正を推奨します。
- `TextCommand` は入力文字列を `"あいうえお"` に固定し、さらに `static` フィールドを書き換えています (`Commands.cs:57-74`)。`TextFigure` の既定値 `"テキスト"` とも食い違っています。
- `IsSelected` の実装が `selected = int.MaxValue / -1` という int 値の符号化になっています。`bool` で十分です (`Figure.cs:15, 23-27`)。
- `CadData.Find` は `Aggregate` の中でクロージャー変数 `result` を書き換えており、読みにくいです。素直な `foreach` を推奨します (`CadData.cs:92-104`)。
- `Geo.ToRect` / `Geo.Distance(Point, IEnumerable<Point>)` は `Count()` を呼んだ後にもう一度列挙しています。`IList` を受けるか、`Any()` を使うべきです (`Geometry.cs:114-128, 178-193`)。
- `ArrowFigure.Type` 列挙型と `MVC.view` フィールドは使われていません。
- 図形ごとに新しい `DropShadowEffect` とグラデーションブラシを作っていて Freeze もしていません (`Common.cs:37-48`、`Helper.cs`)。図形が増えるとレンダリングが重くなります。共有インスタンスを Freeze して使い回すことを推奨します。
- `Helper.CreateFillBrush` が使う `Color.Multiply` はアルファも乗算するため、塗りが半透明になります (0.65 倍など)。意図通りか確認してください (`Helper.cs:21-24, 36-39`)。
- `CurveFigure` はマウスが動くたびに `PathGeometry` を全体から作り直しているため、点数 n に対して O(n²) の処理になります。長い曲線では重くなる可能性があります。
- `TestApp` の `SettingSize` / `SettingColor` は `MenuItem.Header` の文字列 ("大", "赤" など) で分岐しているため、表示文字列を変えると壊れます。`Tag` や `CommandParameter` を使うことを推奨します。

---

## 4. 横断的な課題

### 4.1 コードの重複 (対象外)

次の重複は、各プロジェクトを独立したサンプルとして比べられるようにするための意図的なものなので、**指摘の対象外**とします。

- ベジエ変換 (`ToBezierPolyline` / `ToBezierPolygon` / `ToBezier`) が C++、WinForms ×2、WPF ×2、WpfCad2Lib の 6 か所にあること
- `.NETFramework` 版と .NET 版がほぼ同じ内容であること
- `MouseEventConverter`、`EnumerableExtension.ForEach`、`IMouseAccepter` などが BezierWpf と WpfCad2Lib にあること

### 4.2 テストがない

ベジエ変換、`Geo.Distance`、`CadUndo`、`Device` の行列計算は、どれも UI に依存しない純粋なロジックで、単体テストを書きやすい部分です。3.1 と 3.2 のバグも、簡単なテストで検出できます。xUnit / MSTest のテストプロジェクトを 1 つ追加することを推奨します。

### 4.3 コメントアウトされたコード

38 ファイルにコメントアウトされたコードがあり、大きな塊もあります (例: `BezierWpf/View.xaml.cs:32-102`、`WpfCad2Lib.Wpf/Core/Model.cs:54-124`、`CadData.cs`、`Geometry.cs`)。履歴は git に残っているので、削除して読みやすさを優先することを推奨します。

### 4.4 ドキュメント・ビルド環境

- `README.md` がタイトル 1 行だけです。最低限、次の内容を書くことを推奨します。
  - 各プロジェクトの目的とスクリーンショット
  - 必要な環境 (VS 2026 / v145 ツールセット、.NET 10 SDK、Windows SDK)
  - 操作方法 (Win32 は左クリックで点を追加・右クリックでクリア、WinForms はドラッグ、WPF はメニューでモード選択してからドラッグ、など)
- `TargetFramework` が `net10.0-windows10.0.26100.0` で、特定の Windows SDK バージョンが必要になっています。WinRT API を使っていなければ、`net10.0-windows` で足ります。
- `.editorconfig` がないため、文字コード (3.8)・インデント・命名規約が統一されていません。
- CI (GitHub Actions の `windows-latest` で `dotnet build` / `msbuild`) がないため、開発環境の更新 (直近 2 コミット) でビルドが壊れても検出できません。

---

## 5. 推奨対応 (優先順)

| 優先度 | 対応 | 該当 |
|---|---|---|
| 1 | `CadUndo.Undo/Redo` の列挙を `ToList()` で確定させる | 3.1 |
| 2 | `Device.Home()` で行列を確実にリセットする | 3.2 |
| 3 | マウスキャプチャーの追加、キー入力の受け口の修正 | 3.3, 3.4 |
| 4 | `BezierPolygonPathGeometry` の始点を修正する (3 か所) | 3.5 |
| 5 | `OutlineText.cs` を UTF-8 に変換し、`.editorconfig` を追加する | 3.8, 4.4 |
| 6 | 個人パス・外部 URL のコメントと、未使用リソース (約 2.4 MB) を削除する | 3.9, 3.10 |
| 7 | ベジエ変換と Undo の単体テストを追加する | 4.2 |
| 8 | Undo を UI に配線する場合は、`StartCommand` 呼び出しと UIElement の同期を実装する | 3.1 |
| 9 | コメントアウトされたコードを整理し、README を充実させる | 4.3, 4.4 |
| 10 | その他 🟡 項目 (命名・WinForms の始点・終点の扱いなど) | 3.11-3.16 |
