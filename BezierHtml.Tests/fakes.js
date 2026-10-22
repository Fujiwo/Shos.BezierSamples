"use strict";

// テスト用の偽の要素・イベント・document・Canvas のコンテキストと、ブラウザ向けのスクリプトを読み込む補助。
// アプリのスクリプトは classic script として書かれていて、ほかのファイルの名前 (Geometry など) を呼び出し時にグローバルから参照する。
// そのため、テストでは依存するファイルを先に require して globalThis に置いてから使う

const fs   = require("node:fs");
const path = require("node:path");
const vm   = require("node:vm");

// folder (BezierCanvas など) の names のスクリプトを順に require し、公開された名前をすべて globalThis に置く
function loadScripts(folder, names) {
    const exports = {};
    for (const name of names)
        Object.assign(exports, require(path.join(__dirname, "..", folder, `${name}.js`)));
    Object.assign(globalThis, exports);
    return exports;
}

// MouseEventConverter の 4 つのイベントを [種類, x, y] の配列として記録する
function recordInputEvents(converter) {
    const records = [];
    for (const type of ["click", "dragStart", "dragging", "dragEnd"])
        converter.addListener(type, position => records.push([type, position.x, position.y]));
    return records;
}

// 偽の要素 (ポインターのキャプチャーの操作と、getBoundingClientRect を持つ)
class FakeElement {
    constructor(rect = { left: 0, top: 0 }) {
        this.rect            = rect;
        this.capturedIds     = [];
        this.calls           = [];
        this.listeners       = {};
        // true なら setPointerCapture が例外を投げる (ポインターがすでに無効な場合)
        this.throwsOnCapture = false;
    }

    getBoundingClientRect() { return this.rect; }

    setPointerCapture(pointerId) {
        this.calls.push(["setPointerCapture", pointerId]);
        if (this.throwsOnCapture)
            throw new Error("NotFoundError");
        this.capturedIds.push(pointerId);
    }

    releasePointerCapture(pointerId) {
        this.calls.push(["releasePointerCapture", pointerId]);
        this.capturedIds = this.capturedIds.filter(id => id !== pointerId);
    }

    addEventListener(type, listener) {
        (this.listeners[type] = this.listeners[type] || []).push(listener);
    }

    // 登録されたリスナーを呼ぶ (イベントの発生の代わり)
    dispatch(type, event) {
        for (const listener of this.listeners[type] || [])
            listener(event);
    }
}

// 偽の PointerEvent。既定はマウス (pointerId 1、主ポインター) の左ボタンを押した状態の pointermove
function pointerEvent(element, properties) {
    return Object.assign({
        currentTarget   : element,
        pointerId       : 1,
        isPrimary       : true,
        button          : -1,
        buttons         : 1,
        clientX         : 0,
        clientY         : 0,
        defaultPrevented: false,
        preventDefault() { this.defaultPrevented = true; }
    }, properties);
}

// 偽の Canvas 2D のコンテキスト。呼ばれたメソッドを [名前, 引数…] として calls に記録する。
// stroke の記録には、そのときの strokeStyle と lineWidth も入れる
class FakeContext {
    constructor(canvas) {
        this.canvas      = canvas;
        this.calls       = [];
        this.strokeStyle = "#000000";
        this.lineWidth   = 1;
        this.states      = [];
        for (const name of ["beginPath", "moveTo", "lineTo", "bezierCurveTo", "rect", "closePath", "setTransform", "clearRect"])
            this[name] = (...parameters) => this.calls.push([name, ...parameters]);
    }

    save() {
        this.states.push({ strokeStyle: this.strokeStyle, lineWidth: this.lineWidth });
        this.calls.push(["save"]);
    }

    restore() {
        Object.assign(this, this.states.pop());
        this.calls.push(["restore"]);
    }

    stroke() {
        this.calls.push(["stroke", this.strokeStyle, this.lineWidth]);
    }

    // 記録のうち、名前が names に含まれるものだけを返す
    callsOf(...names) {
        return this.calls.filter(call => names.includes(call[0]));
    }
}

// 偽の <canvas>。width / height に値を設定した回数を数える (設定すると、実際のブラウザでは内容が消える)
class FakeCanvas extends FakeElement {
    constructor() {
        super();
        this.context        = new FakeContext(this);
        this.sizeSetCount   = 0;
        this.internalWidth  = 300;
        this.internalHeight = 150;
    }

    get width() { return this.internalWidth; }
    set width(value) { this.internalWidth = value; this.sizeSetCount++; }
    get height() { return this.internalHeight; }
    set height(value) { this.internalHeight = value; this.sizeSetCount++; }

    getContext(type) { return type === "2d" ? this.context : null; }
}

// View が使うブラウザのグローバル (requestAnimationFrame / ResizeObserver / matchMedia / devicePixelRatio) を偽物にする。
// 戻り値で、予約された描画の実行・大きさの変化・devicePixelRatio の変化を起こせる
function installFakeBrowser() {
    const browser = {
        animationFrames  : [],
        resizeObservers  : [],
        mediaQueries     : [],

        // 予約された requestAnimationFrame のコールバックをすべて実行する
        runAnimationFrames() {
            const callbacks = browser.animationFrames.splice(0);
            for (const callback of callbacks)
                callback(0);
        },

        // 監視している要素の表示サイズが変わったことを通知する
        resize(width, height) {
            for (const observer of browser.resizeObservers)
                observer.callback([{ contentRect: { width, height } }]);
        },

        // devicePixelRatio を変え、登録されたメディアクエリーの change を発生させる
        changeDevicePixelRatio(value) {
            globalThis.devicePixelRatio = value;
            const queries = browser.mediaQueries.splice(0);
            for (const query of queries)
                query.listener();
        }
    };
    globalThis.devicePixelRatio      = 1;
    globalThis.requestAnimationFrame = callback => browser.animationFrames.push(callback);
    globalThis.ResizeObserver        = class {
        constructor(callback) { this.callback = callback; }
        observe(element) { browser.resizeObservers.push(this); }
    };
    globalThis.matchMedia            = media => ({
        media,
        addEventListener(type, listener) { browser.mediaQueries.push({ media, type, listener }); }
    });
    return browser;
}

// 偽の SVG 要素。属性を attributes に記録し、子要素を children に持つ
class FakeSvgElement extends FakeElement {
    constructor(namespace, name) {
        super();
        this.namespaceURI = namespace;
        this.tagName      = name;
        this.attributes   = {};
        this.children     = [];
    }

    setAttribute(name, value) { this.attributes[name] = String(value); }
    getAttribute(name) { return Object.prototype.hasOwnProperty.call(this.attributes, name) ? this.attributes[name] : null; }
    hasAttribute(name) { return this.getAttribute(name) !== null; }
    appendChild(child) { this.children.push(child); return child; }
}

// 偽の document (createElementNS だけを持つ) を globalThis に置く
function installFakeDocument() {
    const document = {
        createElementNS(namespace, name) { return new FakeSvgElement(namespace, name); }
    };
    globalThis.document = document;
    return document;
}

// ツールバーの偽のボタン (data-command 属性と aria-pressed 属性を持つ)
class FakeButton extends FakeSvgElement {
    constructor(command) {
        super(null, "button");
        this.dataset = { command };
        this.setAttribute("data-command", command);
        this.setAttribute("aria-pressed", "false");
    }

    click() { this.dispatch("click", { currentTarget: this }); }
}

// ページ (folder/index.html) を、ブラウザと同じく 1 つのグローバルなスコープで読み込む。
// index.html の <script src> を順に node:vm で実行するので、読み込みの順序の誤りや、トップレベルの名前の重複 (SyntaxError) を検出できる。
// drawingArea は document.getElementById("drawing-area") が返す要素。戻り値の page.evaluate("model") などで、トップレベルの名前を参照できる
function loadPage(folder, drawingArea, globals) {
    const html    = fs.readFileSync(path.join(__dirname, "..", folder, "index.html"), "utf8");
    const sources = [...html.matchAll(/<script src="([^"]+)"><\/script>/g)].map(match => match[1]);
    const buttons = [...html.matchAll(/data-command="([^"]+)"/g)].map(match => new FakeButton(match[1]));
    const document = {
        getElementById(id) { return id === "drawing-area" ? drawingArea : null; },
        querySelectorAll(selector) { return selector === "[data-command]" ? buttons : []; },
        createElementNS(namespace, name) { return new FakeSvgElement(namespace, name); }
    };
    const context = vm.createContext(Object.assign({ document, console }, globals));
    for (const source of sources) {
        const file = path.join(__dirname, "..", folder, source);
        vm.runInContext(fs.readFileSync(file, "utf8"), context, { filename: file });
    }
    return {
        sources,
        buttons,
        evaluate: expression => vm.runInContext(expression, context)
    };
}

module.exports = {
    loadScripts,
    recordInputEvents,
    FakeElement,
    pointerEvent,
    FakeContext,
    FakeCanvas,
    installFakeBrowser,
    FakeSvgElement,
    installFakeDocument,
    FakeButton,
    loadPage
};
