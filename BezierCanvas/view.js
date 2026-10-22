"use strict";

/**
 * MVC の View。<canvas> を持ち、そのポインターのイベントを MouseEventConverter でクリック/ドラッグに変換して Controller に渡す。
 * Canvas 2D は即時モード (描いた結果の画素だけが残り、図形は覚えていない) なので、
 * Model が変わるたび・大きさが変わるたびに、Model のすべての図形を描き直す (WinForms 版の OnPaint に近い)。
 *
 * 座標は描画領域の左上を原点とする CSS ピクセルで扱う。<canvas> の内部の画素数だけを devicePixelRatio に合わせ、
 * 描くときに context の変換で CSS ピクセルを内部の画素に合わせるので、高 DPI でも図形の座標・線の太さ・しきい値は変わらない。
 */
class View {
    /**
     * @param canvas 描画領域の <canvas>。表示サイズは CSS で決め、枠線・余白・transform を付けない
     * @param model  描く図形を持つ Model
     */
    constructor(canvas, model) {
        this.canvas              = canvas;
        this.context             = canvas.getContext("2d");
        this.model               = model;
        this.mouseEventConverter = new MouseEventConverter();
        this.currentController   = null;
        // Controller に通知する関数 (removeListener で外すために覚えておく)
        this.inputListeners      = null;
        // 表示サイズ (CSS ピクセル)
        this.cssWidth            = 0;
        this.cssHeight           = 0;
        // 予約済みの再描画の requestAnimationFrame の ID。予約していなければ null
        this.renderRequestId     = null;

        this.initializeMouseEventHandlers();
        // Model が変わったら描き直す
        model.addChangedListener(() => this.invalidate());
        this.initializeResizeHandlers();
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

    /**
     * 再描画を予約する。何回呼んでも、次の画面の更新の前に 1 回だけ描き直す (WinForms の Invalidate に当たる)。
     */
    invalidate() {
        if (this.renderRequestId !== null)
            return;
        this.renderRequestId = requestAnimationFrame(() => {
            this.renderRequestId = null;
            this.render();
        });
    }

    /**
     * Model のすべての図形を、いますぐ描き直す。
     */
    render() {
        View.render(this.context, this.model.figures, this.cssWidth, this.cssHeight);
    }

    /**
     * context をいったん消し、figures を順に描く。cssWidth / cssHeight は <canvas> の表示サイズ (CSS ピクセル)。
     */
    static render(context, figures, cssWidth, cssHeight) {
        if (cssWidth <= 0 || cssHeight <= 0)
            return;
        // CSS ピクセルの座標を内部の画素に合わせる。倍率は丸めた後の画素数から求めるので、表示サイズと内部の画素がずれない
        context.setTransform(context.canvas.width / cssWidth, 0, 0, context.canvas.height / cssHeight, 0, 0);
        context.clearRect(0, 0, cssWidth, cssHeight);
        for (const figure of figures)
            figure.draw(context);
    }

    /**
     * 表示サイズに合わせて <canvas> の内部の画素数を変え、すぐに描き直す。
     */
    resize(cssWidth, cssHeight) {
        this.cssWidth  = cssWidth;
        this.cssHeight = cssHeight;
        const width    = Math.round(cssWidth  * devicePixelRatio);
        const height   = Math.round(cssHeight * devicePixelRatio);
        // 内部の画素数を設定すると、同じ値でも内容が消えるので、変わったときだけ設定する
        if (this.canvas.width !== width)
            this.canvas.width = width;
        if (this.canvas.height !== height)
            this.canvas.height = height;
        // 内容が消えても、図形はすべて Model にあるので描き直せば元に戻る。
        // ResizeObserver の通知は画面の更新の前に来るので、ここで描けばちらつかない
        this.render();
    }

    // <canvas> のポインターのイベント → MouseEventConverter (Controller が変わっても付け替えないので、コンストラクターで 1 度だけつなぐ)
    initializeMouseEventHandlers() {
        const converter = this.mouseEventConverter;
        this.canvas.addEventListener("pointerdown"       , event => converter.onPointerDown       (event));
        this.canvas.addEventListener("pointermove"       , event => converter.onPointerMove       (event));
        this.canvas.addEventListener("pointerup"         , event => converter.onPointerUp         (event));
        this.canvas.addEventListener("pointercancel"     , event => converter.onPointerCancel     (event));
        this.canvas.addEventListener("lostpointercapture", event => converter.onLostPointerCapture(event));
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

    // 表示サイズと devicePixelRatio の変化を監視する
    initializeResizeHandlers() {
        // 表示サイズは CSS で決めているので、内部の画素数を変えても表示サイズは変わらず、通知が繰り返し起きることはない。
        // 監視を始めると、最初の大きさもすぐに通知される
        new ResizeObserver(entries => {
            const size = entries[entries.length - 1].contentRect;
            this.resize(size.width, size.height);
        }).observe(this.canvas);
        this.watchDevicePixelRatio();
    }

    // devicePixelRatio だけが変わった (表示倍率の違うモニターへの移動など) ときは、表示サイズが変わらないことがあるので別に監視する。
    // 「現在の値と一致する」というメディアクエリーなので、変わったら新しい値で登録し直す
    watchDevicePixelRatio() {
        matchMedia(`(resolution: ${devicePixelRatio}dppx)`).addEventListener("change", () => {
            this.resize(this.cssWidth, this.cssHeight);
            this.watchDevicePixelRatio();
        }, { once: true });
    }
}

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { View };
