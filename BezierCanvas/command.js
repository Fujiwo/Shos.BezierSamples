"use strict";

/**
 * 描画モード (編集・線分・矩形・曲線など) ごとのマウス操作の処理の基底クラス (Command パターン)。
 * Controller は受け取ったクリック/ドラッグを Command に委譲するので、
 * モードの違いはすべて派生クラスに閉じ込められる。何もしない既定の実装を持ち、派生クラスは必要なものだけをオーバーライドする。
 */
class Command {
    constructor() {
        // 図形を追加する先 (Model)。Controller が設定する
        this.model = null;
    }

    /** クリックされたときの処理。既定では何もしない。 */
    onClick    (position) {}
    /** ドラッグが始まったときの処理。既定では何もしない。 */
    onDragStart(position) {}
    /** ドラッグ中の処理。既定では何もしない。 */
    onDragging (position) {}
    /** ドラッグが終わったときの処理。既定では何もしない。 */
    onDragEnd  (position) {}
}

/**
 * 編集モード。このサンプルでは選択などの編集機能は未実装で、マウス操作に対して何もしない (起動直後のモード)。
 */
class EditCommand extends Command {}

/**
 * ドラッグで図形を 1 つ追加するモードの基底クラス (Template Method パターン)。
 * ドラッグの流れ (図形の生成・属性の設定・Model への追加・変更の通知) はここで共通に扱い、
 * どんな図形を作り、マウスの位置に合わせてどう形を変えるかを派生クラスが決める。
 */
class AddCommand extends Command {
    constructor() {
        super();
        // ドラッグを始めた位置 (マウスのボタンを押した位置)
        this.startPosition = null;
        // 現在ドラッグで描いている図形。ドラッグ中以外は null
        this.figure        = null;
    }

    /**
     * ドラッグの開始位置を覚える。図形はまだ作らない (最初の onDragging で作る)。
     */
    onDragStart(position) {
        this.startPosition = position;
        this.figure        = null;
        this.onStart();
    }

    /**
     * 最初の呼び出しで図形を作って Model に追加し、以降はマウスの位置に合わせて形を変える。
     */
    onDragging(position) {
        // 作図中の図形も、最初の 1 回だけ Model に追加する (WPF 版と同じタイミング)。
        // Model の中にあるので、確定済みの図形と一緒に毎回の再描画で描かれる
        if (this.figure === null) {
            this.figure = this.createFigure();
            AddCommand.setAttribute(this.figure);
            this.model?.add(this.figure);
        }
        this.reshape(position);
        // Canvas 2D は図形を書き換えても自動では描き直されないので、変わったことを Model から View に知らせる。
        // WPF 版にはこの呼び出しが無い (Shape のプロパティを変えると WPF が描き直す)
        this.model?.update();
    }

    /**
     * 最後の位置で形を確定する。図形は Model に残したまま、描いている図形の参照だけを手放す。
     * 追加は onDragging の 1 か所だけなので、ここで二重に追加されることはない。
     */
    onDragEnd(position) {
        this.reshapeLast(position);
        this.model?.update();
        this.figure = null;
    }

    /** ドラッグの開始時に呼ばれる。派生クラスが、ドラッグごとの状態を初期化するのに使う。 */
    onStart() {}
    /** 追加する図形を作る。最初の onDragging で 1 度だけ呼ばれる。派生クラスが実装する。 */
    createFigure() { throw new Error("createFigure is not implemented."); }
    /** ドラッグ中のマウスの位置に合わせて、figure の形を変える。派生クラスが実装する。 */
    reshape(position) {}
    /** ドラッグを終えた位置で形を確定する。既定では reshape と同じ。 */
    reshapeLast(position) { this.reshape(position); }

    static setAttribute(figure) {
        figure.stroke          = AddCommand.stroke;
        figure.strokeThickness = AddCommand.strokeThickness;
    }
}

AddCommand.stroke          = "black";
// 線の太さ (CSS ピクセル)
AddCommand.strokeThickness = 3.0;

/**
 * ドラッグの開始位置から現在位置まで線分を描くモード。
 */
class LineCommand extends AddCommand {
    /** 始点をドラッグの開始位置にした線分を作る。 */
    createFigure() { return new LineFigure(this.startPosition); }

    /** 線分の終点をマウスの位置に動かす。 */
    reshape(position) {
        if (this.figure === null)
            return;
        this.figure.end = position;
    }
}

/**
 * ドラッグの開始位置と現在位置を対角とする矩形を描くモード。
 */
class RectangleCommand extends AddCommand {
    /** 矩形を作る。位置と大きさは reshape で決める。 */
    createFigure() { return new RectangleFigure(); }

    /** 開始位置とマウスの位置を対角とするように、矩形の位置と大きさを変える。 */
    reshape(position) {
        if (this.figure === null)
            return;
        // 2 点から左上と、0 以上の幅・高さを求める (正規化)。
        // こうすると、左上以外の方向へドラッグしても正しく描ける (WPF 版の new Rect(StartPosition, position) と同じ)
        this.figure.x      = Math.min(this.startPosition.x, position.x);
        this.figure.y      = Math.min(this.startPosition.y, position.y);
        this.figure.width  = Math.abs(position.x - this.startPosition.x);
        this.figure.height = Math.abs(position.y - this.startPosition.y);
    }
}

/**
 * ドラッグの軌跡を通過点として、それらを滑らかに通るベジエ曲線を描くモード。
 */
class BezierCommand extends AddCommand {
    constructor() {
        super();
        // 今回のドラッグで集めた通過点
        this.positions = null;
    }

    /** 通過点の列を、ドラッグの開始位置だけで始める。 */
    onStart() {
        this.positions = [this.startPosition];
    }

    /** 曲線の図形を作る。通過点は reshape で設定する。 */
    createFigure() { return new BezierFigure(); }

    /** 通過点を間引きながら集め、3 個以上たまったら図形の通過点を設定し直す。 */
    reshape(position) {
        if (this.positions === null || this.figure === null)
            return;

        // マウスの移動イベントは 1 ピクセル単位でも来るので、すべてを通過点にすると、手の細かい揺れで曲線が波打つ。
        // 直前の通過点から sensitivity より遠くへ動いたときだけ追加して、点を間引く
        if (this.positions.length > 0) {
            const lastPosition = this.positions[this.positions.length - 1];
            if (Geometry.distance(position, lastPosition) > BezierCommand.sensitivity)
                this.positions.push(position);
        }
        // 1 点では線にならず、2 点では直線にしかならないので、3 点以上たまってから描く
        if (this.positions.length > 2)
            this.figure.positions = this.positions.slice();
    }

    /** ドラッグを終えた位置を最後の通過点にして、曲線を確定する。 */
    // ドラッグの終点は間隔に関係なく必ず通るようにする (直前の点に近すぎる場合はその点を置き換える)
    reshapeLast(position) {
        if (this.positions === null || this.figure === null)
            return;

        const lastPosition = this.positions[this.positions.length - 1];
        if (this.positions.length >= 2 && Geometry.distance(position, lastPosition) <= BezierCommand.sensitivity)
            this.positions[this.positions.length - 1] = position;
        // 点はオブジェクトなので、!== では参照の比較になる。座標の値で比べる
        else if (position.x !== lastPosition.x || position.y !== lastPosition.y)
            this.positions.push(position);
        if (this.positions.length > 2)
            this.figure.positions = this.positions.slice();
    }
}

// 点の間引きの距離 (CSS ピクセル)。直前の通過点からこれより遠くへ動いたときだけ、新しい通過点にする
BezierCommand.sensitivity = 16.0;

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { Command, EditCommand, AddCommand, LineCommand, RectangleCommand, BezierCommand };
