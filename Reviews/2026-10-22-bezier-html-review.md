# BezierCanvas / BezierSvg レビュー

- レビュー日: 2026-10-22
- 対象コミット: `7f91187ea8cd0e1bb172469e0251ab58bef5049f`
- 対象: [BezierCanvas](../BezierCanvas/)、[BezierSvg](../BezierSvg/) の全 JavaScript・HTML・CSS、および [BezierHtml.Tests](../BezierHtml.Tests/) と [README](../README.md) の関連記述。
- 方法: 静的レビュー、Node.js v24.1.0 による既存テストの実行、既存の偽 DOM・PointerEvent を用いた追加の再現確認。
- 範囲外: アプリの修正、他のプラットフォームの実装の再レビュー。ブラウザの実操作・実描画は確認していない。

> 以下の 1〜4 節は上記コミットに対するレビュー記録。R1・R2 はその後修正し、修正内容と検証結果を [5 節](#5-修正対応-2026-10-22) に追記した。

## 1. 結論

**修正を推奨する不具合は 2 件。どちらも両実装に存在する。**

| ID | 優先度 | 対象 | 指摘 |
|---|---|---|---|
| R1 | P1 / 高 | Canvas・SVG | ドラッグ途中の Command 差し替えで、開始通知を受けていない Command に移動通知が届く。Canvas では再描画不能の図形が残る場合がある |
| R2 | P2 / 中 | Canvas・SVG | 解放位置で初めてドラッグ閾値を超える入力をクリックに誤分類し、線分・矩形を描けない |

既存テストは **108 件成功、失敗 0 件 (20 suites、終了コード 0)**。ただし、上記 2 条件は既存テストでカバーされておらず、成功結果だけでは入力処理の正しさを保証できない。

優先度は、P1 = 特定の操作で継続的な機能停止につながるため優先して修正、P2 = 入力条件に依存する機能不良として修正、とする。

## 2. 指摘事項

### R1. ドラッグ中の描画モード変更で Command のライフサイクルが途切れる

**優先度: P1 / 高。再現確認済み。**

**該当箇所**

- ボタンクリックによる Command の作り直し: [Canvas main.js](../BezierCanvas/main.js#L26)、[SVG main.js](../BezierSvg/main.js#L27)。
- Command の無条件差し替え: [Canvas controller.js](../BezierCanvas/controller.js#L28)、[SVG controller.js](../BezierSvg/controller.js#L28)。
- 開始位置の設定と図形生成: [Canvas command.js](../BezierCanvas/command.js#L46)、[SVG command.js](../BezierSvg/command.js#L46)。
- Canvas で残った不正図形を読み取る箇所: [figures.js](../BezierCanvas/figures.js#L48)。

**再現手順**

1. 「線分」を選択する。
2. 描画領域の `(0, 0)` で押下し、`(20, 0)` まで移動してドラッグを開始する。
3. ボタンを離す前にツールバーの「線分」を再選択する。または「矩形」に切り替える。
4. さらに移動する。

ポインターキャプチャーはポインターイベントの宛先を固定するだけで、キーボードによるボタンの作動は防がない。したがって、描画中のツールバー操作を禁止する仕組みとしては使えない。例えば、ツールバーボタンにフォーカスがある状態で、ドラッグ中に Enter / Space で作動させる経路を考慮する必要がある。

今回の確認では手順 3 を既存の偽ボタンの `click()` で与え、ページのイベント配線を通した不具合を再現した。キーボードによる実操作自体はブラウザ未確認である。

**実際の結果**

- 新しい Command は `startPosition === null` のまま、次の `onDragging()` を受け取る。
- 「矩形」への変更では、両版とも `RectangleCommand.reshape()` が `startPosition.x` を参照して `TypeError` になる。
- 同じ「線分」の再選択では、SVG 版は `LineCommand.createShape()` で `TypeError` になる。
- Canvas 版の「線分」再選択はさらに影響が大きい。`start === null` の `LineFigure` が Model に追加され、再描画時に `LineFigure.addPath()` が例外になる。解放後も不正図形が Model に残るため、再描画を繰り返しても同じ例外が発生する。不正図形以降の図形も描画されない。

**原因**

Controller はイベントごとに「現在の Command」を参照するが、Command の差し替え時に進行中のドラッグを終了・中断せず、MouseEventConverter のドラッグ状態も維持する。新しい Command に `onDragStart()` が届かないまま、`onDragging()` や `onDragEnd()` だけが届く。

同じボタンの再選択も毎回新しいインスタンスを作るため、別のモードへの変更に限らず発生する。

**修正方針**

ドラッグ開始から終了までの Command の対応関係を保証する。例えば、ドラッグ終了まで Command の変更を保留する、または差し替え前に旧 Command と入力変換器の操作を整合した手順で終了・中断する。

`reshape()` の null ガードだけでは、旧 Command の未完了状態や不正図形の追加を解決しない。同じボタンの再選択だけを無視する修正も、別モードへの変更を防げない。

**追加すべきテスト**

- ページを読み込み、ドラッグ中に同一ボタン・別モードのボタンを作動させる。
- 例外が出ず、不正図形が Model に残らず、終了通知が対応する Command に一度だけ届くこと。
- 続くドラッグが正常に動作すること。
- Canvas では変更直後・解放後・次の作図後の再描画まで確認すること。

### R2. 解放位置をドラッグ開始判定に使わず、離れた位置での解放をクリックとして扱う

**優先度: P2 / 中。イベント列による再現確認済み。実ブラウザでの発生頻度は未確認。**

**該当箇所**

- Pointer Events の解放処理: [Canvas mouse-event-converter.js](../BezierCanvas/mouse-event-converter.js#L130)、[SVG mouse-event-converter.js](../BezierSvg/mouse-event-converter.js#L130)。
- 移動時だけのドラッグ開始判定: [Canvas mouse-event-converter.js](../BezierCanvas/mouse-event-converter.js#L173)、[SVG mouse-event-converter.js](../BezierSvg/mouse-event-converter.js#L173)。
- 解放時のクリック判定: [Canvas mouse-event-converter.js](../BezierCanvas/mouse-event-converter.js#L188)、[SVG mouse-event-converter.js](../BezierSvg/mouse-event-converter.js#L188)。

**再現する入力**

```text
pointerdown: (0, 0)
pointermove: (9, 0)   // この時点では 10 CSS ピクセル未満
pointerup:   (30, 0)  // 解放時には閾値を超えている
```

**実際の結果**

両版とも通知は `click(30, 0)` の 1 回だけになる。線分・矩形の Command はクリックでは何もしないため、30 CSS ピクセル離れた位置まで動かして離しても図形が追加されない。

比較のため、中間の `pointermove` を `(30, 0)` にした入力では、同じ開始・終了位置でも `dragStart` → `dragging` → `dragEnd` が発生することを確認した。

**原因**

`canDragStart()` を呼ぶのは `onMove()` だけで、`onButtonUp()` は既存の `isDragging` / `isStarted` の状態だけで分岐する。解放位置が開始位置から十分離れていても、最後の移動通知までにドラッグ判定されていなければクリックになる。

`onPointerMove()` が左ボタン解放を検出して `endPointer()` を呼ぶ復旧経路も同じ解放処理を使うため、同じ判定漏れがある。

**修正方針**

まだ押下中の状態で解放された場合は、解放座標でも開始位置からの距離を判定する。閾値以上なら、開始通知と保留していた移動通知、解放位置での移動・終了通知を整合した順序で流す。

単に `dragEnd` だけを通知しても、AddCommand は最初の `onDragging()` で図形を生成する設計なので、図形が作られない。閾値未満のクリック、およびキャンセル・キャプチャー喪失時の既存仕様は維持する。

**追加すべきテスト**

- `(0, 0)` で押下し、移動通知なしで `(10, 0)` / `(30, 0)` で解放する。
- 最後の移動は `(9, 0)`、解放は `(30, 0)` とする。
- 解放座標が閾値未満のときは従来どおりクリックになること。
- 両版の線分・矩形について、変換後のイベントだけでなく、実際の Model 追加と終点・寸法まで確認すること。

## 3. 確認できた良い点と、指摘対象にしなかった仕様

- 自然 3 次スプラインと周期スプラインは、副作用のない幾何変換として分離されている。既存テストは通過点、C² 連続性、自然境界条件、少数点、入力の非変更、両版の一致を検証している。
- SVG 要素は SVG 名前空間で生成し、塗りつぶしなしと miter limit を明示している。
- Canvas は CSS ピクセルの座標と内部の画素数を分け、ResizeObserver と devicePixelRatio の変化に追従する。複数の更新通知を requestAnimationFrame にまとめている。
- 主ポインターの左ボタンに限定し、キャプチャー、キャンセル、キャプチャー喪失、複数ポインター、複数ボタンの押下・解放を扱っている。
- classic script の読み込み順とトップレベル宣言の衝突は、ページ単位の既存テストでも確認している。
- 編集モードが未実装であること、通過点が 2 個以下の曲線が非表示のまま残ることは明示された仕様であり、今回の不具合には数えない。
- プロジェクト間の重複は教材上の意図として維持し、共通化を改善要求に含めない。

## 4. 検証記録

### 既存テスト

リポジトリルートから実行:

```powershell
node --test "BezierHtml.Tests\*.test.js"
```

結果: **108 tests / 20 suites、pass 108、fail 0、cancelled 0、skipped 0、todo 0、終了コード 0**。

### 追加の再現確認

[既存のテスト補助](../BezierHtml.Tests/fakes.js) の `loadPage`、`FakeCanvas`、`FakeSvgElement`、`pointerEvent` などを使い、Node の一時的なインラインスクリプトで確認した。アプリや既存テストファイルは変更していない。

| 確認 | Canvas | SVG |
|---|---|---|
| ドラッグ中に「線分」→「矩形」へ変更 | `RectangleCommand.reshape` で null の `x` を読み取り例外 | 同左 |
| ドラッグ中に「線分」を再選択 | null 始点の図形が残り、解放後の `view.render()` でも例外。2 回の再描画で確認 | `LineCommand.createShape` で null の `x` を読み取り例外 |
| 押下 `(0, 0)` → 移動 `(9, 0)` → 解放 `(30, 0)` | `[["click", 30, 0]]` | 同左 |
| 比較: 押下 `(0, 0)` → 移動 `(30, 0)` → 解放 `(30, 0)` | `dragStart` → `dragging` → `dragEnd` | 同左 |

すべて実際の誤動作を assertion で確認した。これは不具合の存在の確認であり、修正後の回帰テスト成功を意味しない。

### 残る確認

- 実ブラウザでの Enter / Space とポインタードラッグの併用。
- 低頻度・間引かれた移動通知での解放時判定。
- タッチ・ペン・ブラウザ外での解放、モニター間移動、ページズーム時の実描画。

実ブラウザの確認を行っていないため、表示品質や上記操作を実機検証済みとはしない。

## 5. 修正対応 (2026-10-22)

| ID | 対応 | 回帰テスト |
|---|---|---|
| R1 | 両版の Controller がドラッグ開始時の Command を保持し、移動・終了を同じ Command に委譲する。選択モードは直ちに更新するが、新しい Command を使うのは次のドラッグからとする | 同じ「線分」の再選択と「矩形」への変更について、通常解放・キャンセル・キャプチャー喪失を検証。元の図形の終点、終了通知が 1 回だけ届くこと、旧 Command の図形参照の解放、次の作図のモードと形状を確認。Canvas は変更後・解放後・次の作図後の再描画も確認 |
| R2 | 両版の `onButtonUp()` が未判定の押下について解放位置でも閾値を調べ、必要なら既存の移動処理を通して開始・保留した移動・解放位置での移動・終了を順に通知する | 移動通知なしの距離 10 / 30、保留点がある解放、閾値未満のクリック、左ボタン解放の `pointermove`、後続操作、二重終了の防止を検証。両版の線分・矩形が Model に 1 個追加され、終点・寸法が解放座標と一致することを確認 |

既存のキャンセル・キャプチャー喪失時の仕様は維持し、曲線変換・点の間引き・図形の生成処理には変更を加えていない。

**検証結果**

- 修正前: 対象 3 ファイルのテスト 109 件中、81 件成功・28 件失敗。R1・R2 に対応した失敗であることを確認し、構文エラー・セットアップエラーはなかった。
- 修正後: 同じ対象 109 件がすべて成功。
- 全件: `node --test "BezierHtml.Tests\*.test.js"` により **138 tests / 20 suites、pass 138、fail 0、cancelled 0、skipped 0、todo 0、終了コード 0**。
- 関連する [README](../README.md)・ソースコメント・[説明図](../docs/images/canvas-vs-svg.svg) も変更後の仕様に合わせた。

ブラウザでの Enter / Space とポインタードラッグの併用、および実描画の確認は引き続き未実施である。
