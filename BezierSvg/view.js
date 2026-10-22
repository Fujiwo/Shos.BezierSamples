"use strict";

/**
 * MVC の View。描画領域の <svg> を持ち、そのポインターのイベントを MouseEventConverter でクリック/ドラッグに変換して Controller に渡す。
 * 図形は <svg> の子要素として追加されるとブラウザが自動的に描くので、View 自身は描画の処理を持たない (WPF 版の View と同じ)。
 *
 * <svg> には viewBox を付けないので、SVG の座標 (ユーザー単位) は描画領域の左上を原点とする CSS ピクセルと一致する。
 * そのため、MouseEventConverter が求めた座標を、そのまま x1 などの属性に使える
 * (viewBox などで拡大縮小する場合は、svg.getScreenCTM().inverse() でポインターの位置を SVG の座標に変換する)。
 * SVG はベクターなので、devicePixelRatio が変わっても大きさが変わっても、ブラウザがそのまま描き直す。特別な処理は要らない。
 */
class View {
    /**
     * @param svg   描画領域の <svg>。表示サイズは CSS で決め、枠線・余白・transform を付けない
     * @param model 図形を持つ Model (svg を包んだもの)
     */
    constructor(svg, model) {
        this.svg                 = svg;
        this.model               = model;
        this.mouseEventConverter = new MouseEventConverter();
        this.currentController   = null;
        // Controller に通知する関数 (removeListener で外すために覚えておく)
        this.inputListeners      = null;

        this.initializeMouseEventHandlers();
    }

    /**
     * マウス操作を受け取る Controller。設定すると、Model を渡し、
     * 変換後のクリック/ドラッグのイベントの通知先を新しい Controller に付け替える。
     */
    set controller(value) {
        if (this.currentController !== null)
            this.removeInputEventHandlers();
        this.currentController       = value;
        this.currentController.model = this.model;
        this.initializeInputEventHandlers();
    }

    // <svg> のポインターのイベント → MouseEventConverter (Controller が変わっても付け替えないので、コンストラクターで 1 度だけつなぐ)
    initializeMouseEventHandlers() {
        const converter = this.mouseEventConverter;
        this.svg.addEventListener("pointerdown"       , event => converter.onPointerDown       (event));
        this.svg.addEventListener("pointermove"       , event => converter.onPointerMove       (event));
        this.svg.addEventListener("pointerup"         , event => converter.onPointerUp         (event));
        this.svg.addEventListener("pointercancel"     , event => converter.onPointerCancel     (event));
        this.svg.addEventListener("lostpointercapture", event => converter.onLostPointerCapture(event));
    }

    // MouseEventConverter が変換したクリック/ドラッグ → Controller
    initializeInputEventHandlers() {
        const controller = this.currentController;
        this.inputListeners = {
            click    : position => controller.onClick    (position),
            dragStart: position => controller.onDragStart(position),
            dragging : position => controller.onDragging (position),
            dragEnd  : position => controller.onDragEnd  (position)
        };
        for (const type of Object.keys(this.inputListeners))
            this.mouseEventConverter.addListener(type, this.inputListeners[type]);
    }

    // 古い Controller の登録を外す (外さないと、Controller を差し替えたときに古い方にもイベントが届き続ける)
    removeInputEventHandlers() {
        for (const type of Object.keys(this.inputListeners))
            this.mouseEventConverter.removeListener(type, this.inputListeners[type]);
        this.inputListeners = null;
    }
}

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { View };
