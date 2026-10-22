"use strict";

/**
 * MVC の Model。描画領域の <svg> 要素を包み、その子要素 (<line> / <rect> / <path>) を図形として持つ。
 * WPF 版で Canvas の子要素 (UIElementCollection) をそのまま Model にしているのと同じく、DOM がそのまま Model になる。
 * SVG は保持モード (ブラウザが図形を覚えている) なので、要素を追加すれば表示され、属性を書き換えればブラウザが描き直す。
 * そのため、BezierCanvas の Model と違って変更の通知は要らない。
 */
class Model {
    /**
     * @param svg 図形を入れる <svg> 要素
     */
    constructor(svg) {
        this.svg = svg;
    }

    /**
     * 図形 (SVG の要素) を追加する。追加すると、その場で画面に表示される。
     */
    add(shape) {
        this.svg.appendChild(shape);
    }

    /**
     * 描いた順の図形 (作図中の図形も含む)。
     */
    get shapes() {
        return this.svg.children;
    }
}

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { Model };
