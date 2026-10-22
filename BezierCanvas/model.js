"use strict";

/**
 * MVC の Model。描いた図形 (figures.js の Figure) を順に持ち、変わったことを View に知らせる。
 * WPF 版では Canvas の子要素 (UIElementCollection) をそのまま Model にしていて、追加や変更は WPF が画面に反映する。
 * Canvas 2D (即時モード) には図形を覚えておく仕組みが無いので、図形の一覧はアプリが持ち、
 * 変わるたびに View が全体を描き直す (WinForms 版の Invalidate → OnPaint に近い)。
 */
class Model {
    constructor() {
        // 描いた順の図形 (作図中の図形も含む)
        this.figures          = [];
        // 変更の通知先
        this.changedListeners = [];
    }

    /**
     * 図形を追加し、変更を通知する。
     */
    add(figure) {
        this.figures.push(figure);
        this.update();
    }

    /**
     * 図形の形が変わったことを通知する。Model の中の図形を書き換えたら呼ぶ。
     */
    update() {
        for (const listener of this.changedListeners.slice())
            listener();
    }

    /**
     * 変更の通知先を登録する。
     */
    addChangedListener(listener) {
        this.changedListeners.push(listener);
    }
}

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { Model };
