"use strict";

/**
 * MouseEventConverter が変換した「クリック」「ドラッグ」を受け取る側の窓口 (BezierWpf の IMouseAcceptor に対応)。
 * JavaScript にはインターフェイスが無いので、Controller と各 Command が同じ名前のメソッドを持つこと (ダックタイピング) で表す。
 * 座標はいずれも描画領域の左上を原点とする CSS ピクセルの位置 {x, y}。
 * @typedef {object} MouseAcceptor
 * @property {Model} model                         図形を追加する先 (Model)
 * @property {(position) => void} onClick          ボタンを押した位置からほとんど動かさずに離したときに呼ばれる
 * @property {(position) => void} onDragStart      ドラッグが始まったときに、ボタンを押した位置で呼ばれる
 * @property {(position) => void} onDragging       ドラッグ中にマウスが動くたびに呼ばれる
 * @property {(position) => void} onDragEnd        ドラッグ中にボタンを離した (またはポインターのキャプチャーを失った) ときに呼ばれる
 */

// ポインターの生のイベント (左ボタンの押下・移動・解放、キャプチャーの喪失) を、
// 「クリック」と「ドラッグ (開始・途中・終了)」のイベントに変換する。
//
// 状態は isStarted / isDragging の 2 つのフラグで表す。
//   待機中     (isStarted = false, isDragging = false)
//     │ 左ボタンを押す
//     ▼
//   押下中     (isStarted = true , isDragging = false) … 動いた位置を draggingPositions にためておく
//     │ 押した位置から dragStartLength 以上動く → dragStart と、ためた位置の dragging をまとめて発生させる
//     ▼
//   ドラッグ中 (isStarted = true , isDragging = true ) … 動くたびに dragging
//     │ ボタンを離す → dragEnd
//     ▼
//   待機中
// 押下中のまま (ほとんど動かさずに) ボタンを離すと click になり、ためた位置は捨てる。
// 少しの手ぶれでクリックがドラッグと見なされないよう、一定距離動くまではドラッグにしない。
//
// 入力には Pointer Events を使う。setPointerCapture は WPF の CaptureMouse に、
// lostpointercapture は LostMouseCapture に当たる。
// 対象にするのは主ポインター (isPrimary: マウス・ペン・最初に触れた指) の左ボタン (タッチとペンの接触も左ボタンとして届く) だけで、
// 操作中はほかのポインターを無視する。
class MouseEventConverter {
    constructor() {
        // 変換後のイベントの通知先 (WPF 版の event Action<Point> Click など 4 つに当たる)
        this.listeners         = { click: [], dragStart: [], dragging: [], dragEnd: [] };

        // ボタンが押されていて、クリックかドラッグかを判定中または判定済み
        this.isStarted         = false;
        // ドラッグ中と判定済み
        this.isDragging        = false;
        // ボタンを押した位置 (dragStart の位置)
        this.startPosition     = null;
        // 最後に分かったポインターの位置 (キャプチャーを失ったときの dragEnd の位置)
        this.lastPosition      = null;
        // ドラッグと判定されるまでに動いた位置。判定されたら dragging として順に流し、軌跡が途切れないようにする
        this.draggingPositions = [];
        // 操作中のポインターの pointerId。待機中は null
        this.activePointerId   = null;
    }

    /**
     * 変換後のイベントの通知先を登録する。type は "click" / "dragStart" / "dragging" / "dragEnd"。
     */
    addListener(type, listener) {
        this.listeners[type].push(listener);
    }

    /**
     * addListener で登録した通知先を外す。
     */
    removeListener(type, listener) {
        const index = this.listeners[type].indexOf(listener);
        if (index >= 0)
            this.listeners[type].splice(index, 1);
    }

    // 以下の on〜(event) は、描画領域の要素に登録する Pointer Events のハンドラー。
    // 座標を取り出してキャプチャーを操作し、状態遷移の本体 (onButtonDown など) を呼ぶだけ

    onPointerDown(event) {
        // 主ポインターの左ボタンだけを対象にする。操作中に届いたほかのポインター (2 本目の指など) は無視する
        if (this.activePointerId === null && event.isPrimary && event.button === 0)
            this.startPointer(event);
    }

    onPointerMove(event) {
        if (this.activePointerId === null) {
            // ほかのボタンを押したまま左ボタンを押すと、pointerdown ではなく pointermove (button が 0) として届く (コード化されたボタン操作)
            if (event.isPrimary && event.button === 0 && (event.buttons & 1) !== 0)
                this.startPointer(event);
            // それ以外 (ボタンを押さずに動かしたときなど) の移動は無視する
            return;
        }
        if (event.pointerId !== this.activePointerId)
            return;
        // 左ボタンが押されていない: ほかのボタンを押したまま左ボタンを離した (コード化されたボタン操作) か、
        // ブラウザの外で離して pointerup を取りこぼした。どちらも、この位置で離したものとして扱う
        if ((event.buttons & 1) === 0)
            this.endPointer(event);
        else
            this.onMove(this.toPosition(event));
    }

    onPointerUp(event) {
        // 右ボタンなど、左ボタン以外を離したときは何もしない
        if (event.pointerId === this.activePointerId && event.button === 0)
            this.endPointer(event);
    }

    onPointerCancel(event) {
        // ブラウザがポインターの操作を打ち切った (タッチがスクロールに使われたなど)。pointercancel の座標は使わず、最後に分かった位置で終える
        this.losePointer(event);
    }

    onLostPointerCapture(event) {
        // 通常の解放の後にも届くが、そのときは endPointer で activePointerId を null にしているので何も起きない
        this.losePointer(event);
    }

    // 待機中 → 押下中
    startPointer(event) {
        // テキストの選択や、ドラッグ & ドロップの開始を防ぐ
        event.preventDefault();
        try {
            // 描画領域の外 (ブラウザの外も含む) でボタンを離しても pointerup を受け取れるようにする
            event.currentTarget.setPointerCapture(event.pointerId);
        } catch (error) {
            // ポインターがすでに無効になっている。押下として扱わず、待機中のままにする
            return;
        }
        this.activePointerId = event.pointerId;
        this.onButtonDown(this.toPosition(event));
    }

    // 押下中 / ドラッグ中 → 待機中 (ボタンを離した)
    endPointer(event) {
        const pointerId = this.activePointerId;
        this.onButtonUp(this.toPosition(event));
        this.activePointerId = null;
        // releasePointerCapture() でも lostpointercapture が発生するが、
        // 先に onButtonUp で待機中に戻し、activePointerId も null にしているので、onLostPointerCapture では何も起きない
        try {
            event.currentTarget.releasePointerCapture(pointerId);
        } catch (error) {
            // ポインターがすでに無効になっている (キャプチャーも外れている) ので、何もしなくてよい
        }
    }

    // キャプチャーの喪失・操作の打ち切り
    losePointer(event) {
        if (event.pointerId !== this.activePointerId)
            return;
        this.activePointerId = null;
        this.onLostCapture();
    }

    // ポインターの位置を、描画領域 (イベントを登録した要素。枠線・余白なし) の左上を原点とする CSS ピクセルの座標で返す。
    // 要素の位置はスクロールやリサイズで変わるので、イベントのたびに求める
    toPosition(event) {
        const rect = event.currentTarget.getBoundingClientRect();
        return { x: event.clientX - rect.left, y: event.clientY - rect.top };
    }

    // 以下の位置 {x, y} を受け取るメソッドが状態遷移の本体 (WPF 版の Point を受け取るオーバーロードに当たる)。DOM には依存しない

    // 待機中 → 押下中
    onButtonDown(position) {
        this.isStarted         = true ;
        this.isDragging        = false;
        this.startPosition     = position;
        this.lastPosition      = position;
        this.draggingPositions = [];
    }

    canDragStart(position) {
        return Math.hypot(position.x - this.startPosition.x, position.y - this.startPosition.y) >= MouseEventConverter.dragStartLength;
    }

    onMove(position) {
        this.lastPosition = position;
        if (this.isDragging) {
            this.raise("dragging", position);
        } else if (this.isStarted) {
            // まだクリックかドラッグか分からないので、位置をためておく
            this.draggingPositions.push(position);
            // 押下中 → ドラッグ中: ためた位置をまとめて流して、ボタンを押した位置からの軌跡を受け手に渡す
            if (this.canDragStart(position)) {
                this.raiseDraggingWithDraggingPositions();
                this.isDragging = true;
            }
        }
    }

    onButtonUp(position) {
        // 移動通知が少なくても、解放位置で閾値を超えたら開始・移動を通知してから終える
        if (this.isStarted && !this.isDragging && this.canDragStart(position))
            this.onMove(position);
        // ドラッグ中 → 待機中
        if (this.isDragging) {
            this.raise("dragEnd", position);
            this.isDragging = this.isStarted = false;
        // 押下中 → 待機中: ドラッグと見なすほど動いていないのでクリック
        } else if (this.isStarted) {
            this.raise("click", position);
            this.isStarted = false;
            this.draggingPositions = [];
        }
    }

    // ボタンを離す前にキャプチャーを失った (Alt+Tab など) 場合は、最後の位置でドラッグを終える
    onLostCapture() {
        if (this.isDragging)
            this.raise("dragEnd", this.lastPosition);
        this.isDragging = this.isStarted = false;
        this.draggingPositions = [];
    }

    raise(type, position) {
        // 通知中に登録が変わっても影響を受けないよう、写しに対して呼ぶ
        for (const listener of this.listeners[type].slice())
            listener(position);
    }

    // ドラッグと判定した時点で、dragStart (ボタンを押した位置) と、それまでにためた位置の dragging を順に発生させる
    raiseDraggingWithDraggingPositions() {
        this.raise("dragStart", this.startPosition);
        for (const draggingPosition of this.draggingPositions)
            this.raise("dragging", draggingPosition);
        this.draggingPositions = [];
    }
}

// ドラッグと見なす移動距離 (ボタンを押した位置からの距離、CSS ピクセル)
MouseEventConverter.dragStartLength = 10.0;

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { MouseEventConverter };
