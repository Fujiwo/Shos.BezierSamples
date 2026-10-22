"use strict";

/**
 * MVC の Controller。クリックは現在の Command (描画モード) に、ドラッグは開始時の Command に終了まで委譲する。
 * 自分ではモードごとの処理を持たず、Command を差し替えるだけで振る舞いが変わる (Command パターン)。
 */
class Controller {
    constructor() {
        this.currentModel   = null;
        this.currentCommand = null;
        this.dragCommand    = null;
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
     * ドラッグ中に差し替えても、進行中の図形は開始時の Command で完成させ、新しい Command は次のドラッグから使う。
     */
    get command() { return this.currentCommand; }
    set command(value) {
        this.currentCommand = value;
        if (value !== null)
            value.model = this.currentModel;
    }

    /** クリックを現在の Command に委譲する。 */
    onClick(position) { this.currentCommand?.onClick(position); }

    /** ドラッグの通知先を開始時に固定する。 */
    onDragStart(position) {
        this.dragCommand = this.currentCommand;
        this.dragCommand?.onDragStart(position);
    }

    /** ドラッグ中の移動を開始時の Command に委譲する。 */
    onDragging(position) { this.dragCommand?.onDragging(position); }

    /** 開始時の Command に終了を通知し、ドラッグの参照を手放す。 */
    onDragEnd(position) {
        const command = this.dragCommand;
        this.dragCommand = null;
        command?.onDragEnd(position);
    }
}

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { Controller };
