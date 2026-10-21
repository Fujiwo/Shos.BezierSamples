# 計画書: 描画する曲線を3次スプライン曲線に変更する

- 作成日: 2026-10-22
- 対象: リポジトリ内の全サンプル (BezierWin32 / BezierWinForm ×2 / BezierWpf ×2 / WpfCad2Lib)
- 状態: 計画 (未着手)。[8. 決めてほしいこと](#8-決めてほしいこと) への回答を受けてから着手する。

---

## 1. 目的

通過点を結ぶ曲線を、いまの **Catmull-Rom スプライン**から、**3次スプライン曲線** (すべての点を通り、つなぎ目で2階微分まで連続する曲線) に変える。

| | いま (Catmull-Rom) | 変更後 (3次スプライン) |
|---|---|---|
| 点を通るか | 通る | 通る |
| 連続性 | C¹ (向きまで) | **C² (曲率まで)** |
| 最初と最後の区間 | 直線 | **曲線** (端の条件で決める) |
| 1点を動かしたときの影響 | 近くの4区間だけ | 全区間 (ただし1区間ごとに約 0.27 倍に減る) |
| 計算 | 区間ごとに個別に計算 | 三重対角の連立方程式を解く (O(n)) |

C² 連続になると、つなぎ目で曲率が急に変わらなくなり、見た目がより滑らかになる。最初と最後の区間も曲線になる。

## 2. 現状

すべてのプロジェクトが、同じ手順で曲線を描いている。

1. 通過点 P0〜P(n−1) から、内側の n − 3 区間のベジエ制御点を `P[i+1] ± (P[i+2] − P[i]) / 6` で求める。
2. 最初の区間 (P0 → P1) と最後の区間は直線で描く。

関係するコードは次のとおり。ベジエ変換のコードがプロジェクトごとに重複しているのは、教材として意図的なもの (CLAUDE.md)。今回も共通化はしない。

| プロジェクト | 変換 | 描画 | 曲線を使う図形・コマンド |
|---|---|---|---|
| `BezierWin32` | `Win32Graphics::ToBezierPolyline` | `Win32Graphics::DrawBezierLine` / `DrawBeziers` | `MainWindow::WindowProcedure` (WM_PAINT) |
| `BezierWinForm` / `.NETFramework` | `GeometryExtensions.ToBezierPolyline` / `ToBezierPolygon`、`Geometry.ToBezier` | `GraphicHelper.DrawBezierLine` / `DrawBezierPolygon` | `BezierLine` (`Add` / `AddLast` / `Draw`) |
| `BezierWpf` / `.NETFramework` | `GeometryExtensions.ToBezierPolyline` / `ToBezierPolygon`、`Geo.ToBezier` | `GraphicHelper.BezierPathGeometry` / `BezierPolygonPathGeometry` | `BezierCommand` (`Reshape` / `ReshapeLast`) |
| `WpfCad2Lib.Wpf` | `GeoExtensions.ToBezierPolyline` / `ToBezierPolygon`、`Geo.ToBezier` | `GeoHelper.BezierPathGeometry` / `BezierPolygonPathGeometry` | `CurveFigure` (`SetNext` / `SetLast` / `AppendSegment` / `Rebuild`) |

閉じた曲線の関数 (`ToBezierPolygon`、`DrawBezierPolygon`、`BezierPolygonPathGeometry`) は公開されているが、どこからも呼ばれていない (呼び出しはコメントアウトされている)。

## 3. アルゴリズム

### 3.1 ベジエへの変換 (いまと共通の形)

通過点 Pᵢ での接線を mᵢ とすると、区間 Pᵢ → Pᵢ₊₁ は次の制御点を持つ3次ベジエ曲線になる (3次エルミート曲線をベジエで表したもの)。

```text
Pᵢ,   Pᵢ + mᵢ / 3,   Pᵢ₊₁ − mᵢ₊₁ / 3,   Pᵢ₊₁
```

- いまのコードは mᵢ = (Pᵢ₊₁ − Pᵢ₋₁) / 2 なので、制御点が「± 両隣の差 / 6」になっている。
- 3次スプラインに変えるときは **mᵢ の求め方だけを変えればよい**。ベジエに変換して描く部分はそのまま使える。
- 区間は n − 1 個すべてがベジエ曲線になる。変換結果は `[P0, 制御点1, 制御点2, P1, 制御点1, 制御点2, P2, …]` の 1 + 3(n − 1) 点で、各 API のベジエ描画関数にそのまま渡せる。最初と最後の直線は要らなくなる。

### 3.2 接線を求める連立方程式 (一様パラメーター)

つなぎ目で2階微分が一致する条件から、内側の点 (i = 1 … n − 2) について次の式が成り立つ。

```text
mᵢ₋₁ + 4mᵢ + mᵢ₊₁ = 3(Pᵢ₊₁ − Pᵢ₋₁)
```

未知数 n 個に対して式が n − 2 個なので、両端に条件を足す。**自然スプライン** (端点で2階微分を 0 にする) の場合は次のとおり。

```text
2m₀     + m₁       = 3(P₁ − P₀)
mₙ₋₂   + 2mₙ₋₁     = 3(Pₙ₋₁ − Pₙ₋₂)
```

係数行列は三重対角で、対角成分が優位 (|4| > |1| + |1|、|2| > |1|)。そのため、ピボット選択なしの**トーマス法** (前進消去と後退代入) で、安定して O(n) で解ける。x 座標と y 座標は同じ係数行列なので、ベクトルのまま一度に解ける。

```csharp
// 自然3次スプラインの各通過点での接線 m[i] を求める (一様パラメーター、通過点は 2 個以上)。
static Vector[] NaturalSplineTangents(IList<Point> p)
{
    var n  = p.Count;
    var cp = new double[n];   // 前進消去後の上側の係数
    var dp = new Vector[n];   // 前進消去後の右辺
    for (var i = 0; i < n; i++) {
        var a = i == 0     ? 0.0 : 1.0;                    // 下側の係数
        var b = i == 0 || i == n - 1 ? 2.0 : 4.0;          // 対角 (両端は自然スプラインの条件)
        var c = i == n - 1 ? 0.0 : 1.0;                    // 上側の係数
        var d = 3.0 * (p[Math.Min(i + 1, n - 1)] - p[Math.Max(i - 1, 0)]);
        var denominator = b - a * (i == 0 ? 0.0 : cp[i - 1]);
        cp[i] = c / denominator;
        dp[i] = (d - a * (i == 0 ? new Vector() : dp[i - 1])) / denominator;
    }
    var m = new Vector[n];
    m[n - 1] = dp[n - 1];
    for (var i = n - 2; i >= 0; i--)                       // 後退代入
        m[i] = dp[i] - cp[i] * m[i + 1];
    return m;
}
```

上のコードは計画用のスケッチで、まだビルドしていない。実装するときに、各プロジェクトの書き方 (C# 14 / C# 7.3 / C++20) に合わせて書き直す。

### 3.3 点の数が少ない場合

| 通過点の数 | いま | 変更後 |
|---|---|---|
| 0〜1 | 何も描かない | 何も描かない |
| 2 | 直線 | 直線 (m₀ = m₁ = P₁ − P₀ になり、ベジエでも直線になる) |
| 3 | 直線 2 本 | 曲線 2 区間 |
| 4 以上 | 直線 + 曲線 (n − 3 区間) + 直線 | 曲線 (n − 1 区間) |

### 3.4 閉じた曲線 (周期スプライン)

閉じた曲線では、両端の条件の代わりに、最後の点と最初の点をつなぐ。すべての i について `mᵢ₋₁ + 4mᵢ + mᵢ₊₁ = 3(Pᵢ₊₁ − Pᵢ₋₁)` (添字は n を法とする) が成り立ち、係数行列は**巡回三重対角** (左下と右上の角に 1 が入る) になる。

- 解き方の候補は2つ。
  - Sherman-Morrison の公式で、トーマス法2回に分けて解く。
  - 点列を前後に数点ずつ延ばして通常の三重対角として解き、中央部分だけを使う (影響は約 0.27 倍ずつ減るので、数点で十分な精度になる)。
- 正確さと説明のしやすさから、**Sherman-Morrison を推奨**する。

### 3.5 ドラッグ中の描画 (WpfCad2Lib の `CurveFigure`)

いまの `CurveFigure` は、点を1つ足すと末尾の区間だけを差し替えている (O(1))。3次スプラインでは、点を1つ足すと全区間の接線が変わるので、この方法はそのままでは使えない。

| 方式 | 点を足すたびの計算 | 正確さ |
|---|---|---|
| A. 毎回全体を作り直す | O(n) (長いドラッグ全体では O(n²)) | 正確 |
| B. 末尾の K 区間だけ作り直す | O(K) | 近似。K = 6 で、それより前の接線への影響は約 0.27⁶ ≈ 0.04% |

**推奨は B (K = 6)** で、ドラッグを終えたとき (`SetLast`) だけ A で全体を正確に作り直す。

- ドラッグ中に画面に出る誤差は、ピクセル未満になる。
- ドラッグを終えた後の形は、A と完全に同じになる。
- レビュー 3.16 (曲線全体を毎回作り直すと O(n²) になる件) で直した性能も保てる。

BezierWpf の `BezierCommand` と WinForms の `BezierLine` は、いまも毎回全体を作り直しているので、A のままでよい。

## 4. 各プロジェクトの変更内容

### 4.1 WpfCad2Lib.Wpf (最初に実装する)

- **`Utility/Geometry.cs`**
  - `GeoExtensions` の `extension(IList<Point> points)` ブロックに、次の2つを足す。
    - `ToSplineTangents()`: 3.2 の連立方程式を解いて、各通過点の接線を返す。
    - `ToSplineBezier()`: 3.1 の形で、ベジエ描画用の点列を返す。
  - 閉じた曲線用に、`ToClosedSplineTangents()` / `ToClosedSplineBezier()` を足す (3.4)。
  - `GeoHelper.BezierPathGeometry` / `BezierPolygonPathGeometry` を、3次スプラインで描くように変える。シグネチャは変えない。
  - いまの `ToBezierPolyline` / `ToBezierPolygon` / `Geo.ToBezier` の扱いは、[8. 決めてほしいこと](#8-決めてほしいこと) の 1 による。
- **`Figures/Figures.cs`**
  - `CurveFigure` の `AppendSegment` を、3.5 の方式 B に変える。
  - `Rebuild` と `SetLast` は全体を作り直す (方式 A)。
  - `IsValid` (点が3個以上) と、点の間引き (`sensitivity = 8`) は変えない。
- コメント (XML ドキュメントと、アルゴリズムの説明) を、新しいアルゴリズムに合わせて書き直す。

### 4.2 BezierWpf / BezierWpf.NETFramework

- `Geometry.cs` に、4.1 と同じ関数を足す。.NETFramework 版は C# 7.3 で書く (拡張ブロック・コレクション式などは使わない)。
- `GraphicHelper.BezierPathGeometry` / `BezierPolygonPathGeometry` を3次スプラインに変える。
- `Command.cs` の `BezierCommand` は、呼び出す関数が同じなので、変更は要らない見込み。3点以上たまってから描く条件 (`positions.Count > 2`) は残す。

### 4.3 BezierWinForm / BezierWinForm.NETFramework

- `Geometry.cs` に、`PointF` / `SizeF` 版の同じ関数を足す。`float` で計算するが、連立方程式の途中計算は `double` にする (誤差を減らすため)。
- `GraphicHelper.DrawBezierLine` は、`Graphics.DrawBeziers` を1回呼ぶだけになる。最初と最後の `DrawLine` は要らなくなる。点が2個のときは `DrawLine` のままにする。
- `GraphicHelper.DrawBezierPolygon` を閉じた3次スプラインに変える。
- `Figure.cs` の `BezierLine` は、変更は要らない見込み。

### 4.4 BezierWin32

- `Win32Graphics` に、`double` の座標で接線を求める関数を足す。`POINT` は整数なので、制御点は最後に `Win32Geometry::Round` で丸める。
  - 実数のベクトル型 (`struct Vector2 { double x, y; }` など) を新しく作る。
  - C++20 の書き方 (`std::span`、`constexpr`、指示付き初期化子など) に合わせる。
- `DrawBezierLine` は、`DrawBeziers` (MoveToEx + PolyBezierTo) を1回呼ぶだけになる。最初と最後の `Polyline` は要らなくなる。点が2個のときは直線を描く。
- 点はクリックで1つずつ増え、数が少ないので、毎回全体を計算してよい。

### 4.5 ドキュメント

- **README.md**
  - 「通過点からベジエ曲線を作るしくみ」を3次スプライン版に書き直す。連立方程式、端の条件、トーマス法を説明する。
  - Catmull-Rom との関係 (Catmull-Rom は、連立方程式を両隣の点だけで近似したもの) を説明する。
  - クラス図に、新しく足す公開メンバーを書き足す。
  - 既知の制限の「両端の区間は直線」を削除する。
- **docs/images/**
  - 3次スプラインの説明図を新しく作る。
  - いまの2枚 (Catmull-Rom の説明図) を、比較用として残すか差し替えるかは、8 の 1 に合わせる。
  - 同じ点列に「Catmull-Rom」と「3次スプライン」を重ねた比較図も作る。
  - 座標はアルゴリズムと同じ式で計算して描く。Edge のヘッドレスモードで表示を確かめる。
- **CLAUDE.md**: 概要にある「制御点は Catmull-Rom 風に `P[i+1] ± (P[i+2] − P[i]) / 6` で求める」を書き直す。

## 5. 公開 API への影響 (WpfCad2Lib)

| 対象 | 影響 |
|---|---|
| `GeoHelper.BezierPathGeometry` / `BezierPolygonPathGeometry` / `BezierPath` / `BezierPolygonPath` | シグネチャは変わらない。**描かれる形は変わる** |
| `GeoExtensions.ToSplineTangents` などの新しいメソッド | 追加 |
| `GeoExtensions.ToBezierPolyline` / `ToBezierPolygon`、`Geo.ToBezier` | 8 の 1 による (残す / `[Obsolete]` を付ける / 削除) |

## 6. 作業の順序

| 段階 | 内容 | 確認方法 |
|---|---|---|
| 1 | WpfCad2Lib.Wpf: 変換関数、`GeoHelper`、`CurveFigure` | ビルド、数値の検証 (下記)、TestApp を Windows で動かして確認 |
| 2 | BezierWpf (.NET)、続いて .NETFramework | ビルド (.NETFramework は検証用の net48 プロジェクト)、Windows で動かして確認 |
| 3 | BezierWinForm (.NET)、続いて .NETFramework | 同上 |
| 4 | BezierWin32 | Visual Studio 2026 の MSVC (`/std:c++20 /permissive- /W4`) でビルド、Windows で動かして確認 |
| 5 | README・説明図・CLAUDE.md・コメント | Mermaid と SVG を Edge で表示して確認 |

各段階ごとにコミットする。

**数値の検証** (どのプロジェクトにも、テスト用のプロジェクトが無いので用意する)
- 方法は2つの案がある。
  - スクラッチ領域に検証用のコンソールアプリを作り、ソースを取り込んで確かめる。
  - xUnit のテストプロジェクトをリポジトリに追加する。レビュー 4.2 の推奨にも沿う。ただし WPF の `Point` を使うので、`net10.0-windows` のテストになる。
- 確かめる内容は次のとおり。
  - すべての通過点を通ること。
  - つなぎ目で、1階微分と2階微分が一致すること (C²)。
  - 両端で2階微分が 0 になること (自然スプライン)。
  - 閉じた曲線では、1周つながって C² になること。
  - 方式 B の近似と、全体を作り直した結果との差が、許容範囲に収まること。
- C++ は、同じ点列で C# 版と同じ制御点 (丸めの誤差の範囲で) になることを確かめる。

## 7. リスクと対策

| リスク | 対策 |
|---|---|
| 点の間隔がばらつくと、行き過ぎ (オーバーシュート) が Catmull-Rom より遠くまで波及する | 点の間引き (8〜16) は残す。必要なら、弦長パラメーター (8 の 3) を後から入れる |
| 長いドラッグで描画が重くなる | WpfCad2Lib は方式 B で O(K) に抑える。ほかのサンプルは点が少なく、毎回作り直してよい |
| 曲線の形が変わり、README の図やコメントと食い違う | 段階 5 で一括して更新する。コメントは各段階のコードと一緒に直す |
| .NET Framework 版に、C# 14 の書き方が混ざる | .NETFramework 版は C# 7.3 で書き、検証用の net48 プロジェクトでコンパイルを確かめる |
| 実機でしか分からない問題 (描画の見た目、操作感) | Windows での確認項目を段階ごとに書き出し、利用者に確かめてもらう |

## 8. 決めてほしいこと

| # | 内容 | 選択肢 | 推奨 |
|---|---|---|---|
| 1 | いまの Catmull-Rom の処理をどうするか | (a) 置き換えて削除する / (b) 残して、描画だけ3次スプラインにする / (c) 両方を選べるようにする (メニューや引数で切り替え) | **(b)**。教材として比べられるよう、関数とその説明を残す。(c) は UI の変更が増える |
| 2 | 端の条件 | 自然スプライン / 端の接線を指定 (クランプ) / not-a-knot | **自然スプライン**。外から与える情報が要らず、説明もしやすい |
| 3 | パラメーターの取り方 | 一様 / 弦長 (区間の長さ) / 求心 (区間の長さの平方根) | **一様** から始める。いまのコードと式の形をそろえられる。弦長は後から追加する |
| 4 | 閉じた曲線も3次スプラインにするか | する / Catmull-Rom のまま | **する** (3.4)。いまは使われていないが公開 API なので、閉じていない曲線と動作をそろえる |
| 5 | `CurveFigure` のドラッグ中の描画 | 方式 A (毎回全体) / 方式 B (末尾 K 区間) | **方式 B (K = 6)** で、ドラッグ終了時に方式 A |
| 6 | 数値の検証の方法 | スクラッチ領域の検証用アプリ / テストプロジェクトをリポジトリに追加 | **テストプロジェクトを追加**。今後の変更でも使える |
| 7 | 対象のプロジェクト | 全6実装 / .NET 10 版だけ / WpfCad2Lib だけ | **全6実装**。同じアルゴリズムを書き分ける、という教材の方針を保つ |
