"use strict";

// MouseEventConverter (ポインターのイベント → クリック / ドラッグ) のテスト。
// 状態遷移の本体 (onButtonDown など) には座標の列を直接与え、Pointer Events の振り分けには偽の要素とイベントを使う

const { describe, test } = require("node:test");
const assert = require("node:assert/strict");
const { loadScripts, recordInputEvents, FakeElement, pointerEvent } = require("./fakes.js");

const versions = ["BezierCanvas", "BezierSvg"];

for (const version of versions) {
    describe(`${version}/mouse-event-converter.js`, () => {
        const { MouseEventConverter } = loadScripts(version, ["mouse-event-converter"]);

        function create() {
            const converter = new MouseEventConverter();
            return { converter, records: recordInputEvents(converter) };
        }

        describe("状態遷移", () => {
            test("押下位置からの距離が 10 未満で離すとクリック", () => {
                const { converter, records } = create();
                converter.onButtonDown({ x: 0, y: 0 });
                converter.onMove({ x: 9.99, y: 0 });
                converter.onButtonUp({ x: 9.99, y: 0 });
                assert.deepEqual(records, [["click", 9.99, 0]]);
            });

            test("距離がちょうど 10 でドラッグになる", () => {
                const { converter, records } = create();
                converter.onButtonDown({ x: 0, y: 0 });
                converter.onMove({ x: 6, y: 8 });
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 6, 8]]);
            });

            for (const distance of [10, 30]) {
                test(`移動通知が無くても解放位置の距離が ${distance} ならドラッグになる`, () => {
                    const { converter, records } = create();
                    converter.onButtonDown({ x: 0, y: 0 });
                    converter.onButtonUp({ x: distance, y: 0 });
                    assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", distance, 0], ["dragEnd", distance, 0]]);
                    assert.equal(converter.isStarted, false);
                    assert.equal(converter.isDragging, false);
                    assert.deepEqual(converter.draggingPositions, []);
                    converter.onLostCapture();
                    converter.onButtonUp({ x: distance, y: 0 });
                    assert.equal(records.length, 3);
                });
            }

            test("解放位置で初めて閾値を超えると、保留した移動・解放位置・終了を順に通知する", () => {
                const { converter, records } = create();
                converter.onButtonDown({ x: 0, y: 0 });
                converter.onMove({ x: 3, y: 0 });
                converter.onMove({ x: 9, y: 0 });
                converter.onButtonUp({ x: 30, y: 0 });
                assert.deepEqual(records, [
                    ["dragStart", 0, 0], ["dragging", 3, 0], ["dragging", 9, 0],
                    ["dragging", 30, 0], ["dragEnd", 30, 0]
                ]);
                converter.onButtonDown({ x: 50, y: 50 });
                converter.onButtonUp({ x: 50, y: 50 });
                assert.deepEqual(records.slice(5), [["click", 50, 50]]);
            });

            test("移動通知なしで閾値未満の位置で解放するとクリックになる", () => {
                const { converter, records } = create();
                converter.onButtonDown({ x: 0, y: 0 });
                converter.onButtonUp({ x: 9.99, y: 0 });
                assert.deepEqual(records, [["click", 9.99, 0]]);
            });

            test("ドラッグと判定した時点で、押下位置の dragStart とためた位置の dragging を順に通知する", () => {
                const { converter, records } = create();
                converter.onButtonDown({ x: 0, y: 0 });
                converter.onMove({ x: 3, y: 0 });
                converter.onMove({ x: 6, y: 0 });
                assert.deepEqual(records, []);
                converter.onMove({ x: 11, y: 0 });
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 3, 0], ["dragging", 6, 0], ["dragging", 11, 0]]);
                // 判定後の移動と解放
                converter.onMove({ x: 20, y: 0 });
                converter.onButtonUp({ x: 25, y: 0 });
                assert.deepEqual(records.slice(4), [["dragging", 20, 0], ["dragEnd", 25, 0]]);
            });

            test("ドラッグ中にキャプチャーを失うと、最後の位置で dragEnd して待機中に戻る", () => {
                const { converter, records } = create();
                converter.onButtonDown({ x: 0, y: 0 });
                converter.onMove({ x: 20, y: 0 });
                converter.onMove({ x: 30, y: 5 });
                converter.onLostCapture();
                assert.deepEqual(records.slice(-1), [["dragEnd", 30, 5]]);
                const count = records.length;
                converter.onMove({ x: 40, y: 5 });
                converter.onButtonUp({ x: 40, y: 5 });
                assert.equal(records.length, count);
            });

            test("押下中にキャプチャーを失うと、何も通知せず、ためた位置も捨てる", () => {
                const { converter, records } = create();
                converter.onButtonDown({ x: 0, y: 0 });
                converter.onMove({ x: 3, y: 0 });
                converter.onLostCapture();
                assert.deepEqual(records, []);
                converter.onButtonDown({ x: 50, y: 50 });
                converter.onButtonUp({ x: 50, y: 50 });
                assert.deepEqual(records, [["click", 50, 50]]);
            });

            test("解放に続くキャプチャーの喪失で、二重に dragEnd しない", () => {
                const { converter, records } = create();
                converter.onButtonDown({ x: 0, y: 0 });
                converter.onMove({ x: 20, y: 0 });
                converter.onButtonUp({ x: 20, y: 0 });
                converter.onLostCapture();
                assert.equal(records.filter(record => record[0] === "dragEnd").length, 1);
            });

            test("待機中の移動は無視する", () => {
                const { converter, records } = create();
                converter.onMove({ x: 100, y: 100 });
                converter.onButtonUp({ x: 100, y: 100 });
                assert.deepEqual(records, []);
            });

            test("removeListener で外した通知先には届かない", () => {
                const converter = new MouseEventConverter();
                const records   = [];
                const listener  = position => records.push(position);
                converter.addListener("click", listener);
                converter.removeListener("click", listener);
                converter.onButtonDown({ x: 0, y: 0 });
                converter.onButtonUp({ x: 0, y: 0 });
                assert.deepEqual(records, []);
            });
        });

        describe("Pointer Events の振り分け", () => {
            function createWithElement() {
                const element = new FakeElement({ left: 100, top: 40 });
                return Object.assign(create(), { element });
            }

            test("座標は要素の左上を原点とする", () => {
                const { converter, records, element } = createWithElement();
                converter.onPointerDown(pointerEvent(element, { button: 0, clientX: 110, clientY: 50 }));
                converter.onPointerUp  (pointerEvent(element, { button: 0, buttons: 0, clientX: 112, clientY: 53 }));
                assert.deepEqual(records, [["click", 12, 13]]);
            });

            test("左ボタンのドラッグでキャプチャーを設定・解除し、解除に続く lostpointercapture では何も起きない", () => {
                const { converter, records, element } = createWithElement();
                const down = pointerEvent(element, { button: 0, clientX: 100, clientY: 40 });
                converter.onPointerDown(down);
                assert.ok(down.defaultPrevented);
                converter.onPointerMove(pointerEvent(element, { clientX: 120, clientY: 40 }));
                converter.onPointerUp  (pointerEvent(element, { button: 0, buttons: 0, clientX: 130, clientY: 40 }));
                converter.onLostPointerCapture(pointerEvent(element, { buttons: 0 }));
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 20, 0], ["dragEnd", 30, 0]]);
                assert.deepEqual(element.calls, [["setPointerCapture", 1], ["releasePointerCapture", 1]]);
            });

            test("右ボタンだけの操作は無視する", () => {
                const { converter, records, element } = createWithElement();
                converter.onPointerDown(pointerEvent(element, { button: 2, buttons: 2, clientX: 100, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { buttons: 2, clientX: 150, clientY: 40 }));
                converter.onPointerUp  (pointerEvent(element, { button: 2, buttons: 0, clientX: 150, clientY: 40 }));
                assert.deepEqual(records, []);
                assert.deepEqual(element.calls, []);
            });

            test("左ボタンのドラッグ中に右ボタンを押して離しても、ドラッグは途切れない", () => {
                const { converter, records, element } = createWithElement();
                converter.onPointerDown(pointerEvent(element, { button: 0, clientX: 100, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { clientX: 120, clientY: 40 }));
                // 右ボタンの押下・解放は、コード化されたボタン操作として pointermove で届く
                converter.onPointerMove(pointerEvent(element, { button: 2, buttons: 3, clientX: 125, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { button: 2, buttons: 1, clientX: 130, clientY: 40 }));
                converter.onPointerUp  (pointerEvent(element, { button: 0, buttons: 0, clientX: 135, clientY: 40 }));
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 20, 0], ["dragging", 25, 0], ["dragging", 30, 0], ["dragEnd", 35, 0]]);
            });

            test("右ボタンを押したまま左ボタンを押す・離す (コード化されたボタン操作) も左ボタンの操作として扱う", () => {
                const { converter, records, element } = createWithElement();
                converter.onPointerDown(pointerEvent(element, { button: 2, buttons: 2, clientX: 100, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { button: 0, buttons: 3, clientX: 100, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { buttons: 3, clientX: 120, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { button: 0, buttons: 2, clientX: 125, clientY: 40 }));
                converter.onPointerUp  (pointerEvent(element, { button: 2, buttons: 0, clientX: 125, clientY: 40 }));
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 20, 0], ["dragEnd", 25, 0]]);
            });

            test("pointerup を取りこぼしても、左ボタンが離れた pointermove でドラッグを終える", () => {
                const { converter, records, element } = createWithElement();
                converter.onPointerDown(pointerEvent(element, { button: 0, clientX: 100, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { clientX: 120, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { buttons: 0, clientX: 140, clientY: 60 }));
                converter.onPointerMove(pointerEvent(element, { buttons: 0, clientX: 150, clientY: 60 }));
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 20, 0], ["dragEnd", 40, 20]]);
            });

            test("左ボタン解放の pointermove で初めて閾値を超えてもドラッグになる", () => {
                const { converter, records, element } = createWithElement();
                converter.onPointerDown(pointerEvent(element, { button: 0, clientX: 100, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { clientX: 109, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { button: 0, buttons: 2, clientX: 130, clientY: 40 }));
                converter.onPointerUp(pointerEvent(element, { button: 2, buttons: 0, clientX: 130, clientY: 40 }));
                converter.onLostPointerCapture(pointerEvent(element, { buttons: 0 }));
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 9, 0], ["dragging", 30, 0], ["dragEnd", 30, 0]]);
                assert.equal(converter.activePointerId, null);
                assert.deepEqual(element.calls, [["setPointerCapture", 1], ["releasePointerCapture", 1]]);
            });

            test("操作中のほかのポインター (2 本目の指) と、主ポインターでないポインターは無視する", () => {
                const { converter, records, element } = createWithElement();
                converter.onPointerDown(pointerEvent(element, { pointerId: 5, isPrimary: false, button: 0, clientX: 100, clientY: 40 }));
                assert.deepEqual(element.calls, []);
                converter.onPointerDown(pointerEvent(element, { pointerId: 1, button: 0, clientX: 100, clientY: 40 }));
                converter.onPointerDown(pointerEvent(element, { pointerId: 2, isPrimary: false, button: 0, clientX: 300, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { pointerId: 2, clientX: 400, clientY: 40 }));
                converter.onPointerUp  (pointerEvent(element, { pointerId: 2, button: 0, buttons: 0, clientX: 400, clientY: 40 }));
                converter.onPointerCancel(pointerEvent(element, { pointerId: 2 }));
                converter.onPointerMove(pointerEvent(element, { pointerId: 1, clientX: 120, clientY: 40 }));
                converter.onPointerUp  (pointerEvent(element, { pointerId: 1, button: 0, buttons: 0, clientX: 120, clientY: 40 }));
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 20, 0], ["dragEnd", 20, 0]]);
            });

            test("pointercancel では最後の位置で終え、続く lostpointercapture で二重に終えない", () => {
                const { converter, records, element } = createWithElement();
                converter.onPointerDown(pointerEvent(element, { button: 0, clientX: 100, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { clientX: 130, clientY: 45 }));
                converter.onPointerCancel(pointerEvent(element, { buttons: 0, clientX: 0, clientY: 0 }));
                converter.onLostPointerCapture(pointerEvent(element, { buttons: 0 }));
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 30, 5], ["dragEnd", 30, 5]]);
                // 次の操作は普通にできる
                converter.onPointerDown(pointerEvent(element, { button: 0, clientX: 200, clientY: 40 }));
                converter.onPointerUp  (pointerEvent(element, { button: 0, buttons: 0, clientX: 200, clientY: 40 }));
                assert.deepEqual(records.slice(-1), [["click", 100, 0]]);
            });

            test("キャプチャーを外されると、その時点の位置で終え、以降の移動と解放は無視する", () => {
                const { converter, records, element } = createWithElement();
                converter.onPointerDown(pointerEvent(element, { button: 0, clientX: 100, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { clientX: 130, clientY: 40 }));
                converter.onLostPointerCapture(pointerEvent(element, {}));
                converter.onPointerMove(pointerEvent(element, { clientX: 160, clientY: 40 }));
                converter.onPointerUp  (pointerEvent(element, { button: 0, buttons: 0, clientX: 160, clientY: 40 }));
                assert.deepEqual(records, [["dragStart", 0, 0], ["dragging", 30, 0], ["dragEnd", 30, 0]]);
            });

            test("setPointerCapture が例外を投げたら、押下として扱わない", () => {
                const { converter, records, element } = createWithElement();
                element.throwsOnCapture = true;
                converter.onPointerDown(pointerEvent(element, { button: 0, clientX: 100, clientY: 40 }));
                converter.onPointerMove(pointerEvent(element, { clientX: 130, clientY: 40 }));
                converter.onPointerUp  (pointerEvent(element, { button: 0, buttons: 0, clientX: 130, clientY: 40 }));
                assert.deepEqual(records, []);
                assert.equal(converter.activePointerId, null);
            });
        });
    });
}
