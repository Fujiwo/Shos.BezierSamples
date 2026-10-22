"use strict";

/**
 * MVC の Controller。View から受け取ったクリック/ドラッグを、現在の Command (描画モード) に委譲する。
 * 自分ではモードごとの処理を持たず、Command を差し替えるだけで振る舞いが変わる (Command パターン)。
 */
class Controller {
    constructor() {
        this.currentModel   = null;
        this.currentCommand = null;
    }

    /**
     * 図形を追加する先 (Model)。View の controller の設定時に View が渡す。設定した値は現在の Command にも渡す。
     */
    get model() { return this.currentModel; }
    // Command を設定した後に Model を設定しても Command に伝わるようにする
    set model(value) {
        this.currentModel = value;
        if (this.currentCommand !== null)
            this.currentCommand.model = value;
    }

    /**
     * 現在の描画モード。ツールバーの選択に応じて main.js が差し替える。新しい Command には、描き込む先として現在の Model を渡す。
     */
    get command() { return this.currentCommand; }
    set command(value) {
        this.currentCommand = value;
        if (value !== null)
            value.model = this.currentModel;
    }

    // 以下の 4 つは、どれも現在の Command にそのまま委譲する (Command が未設定なら何もしない)

    /** クリックを現在の Command に委譲する。 */
    onClick    (position) { this.currentCommand?.onClick    (position); }
    /** ドラッグの開始を現在の Command に委譲する。 */
    onDragStart(position) { this.currentCommand?.onDragStart(position); }
    /** ドラッグ中の移動を現在の Command に委譲する。 */
    onDragging (position) { this.currentCommand?.onDragging (position); }
    /** ドラッグの終了を現在の Command に委譲する。 */
    onDragEnd  (position) { this.currentCommand?.onDragEnd  (position); }
}

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { Controller };
