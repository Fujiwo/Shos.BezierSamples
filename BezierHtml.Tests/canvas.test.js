"use strict";

// BezierCanvas のコマンド・モデル・図形・View のテスト。
// 描画は偽の Canvas 2D のコンテキストに対する命令の記録で確かめる (実際に表示されることは、ブラウザでの手動確認に残る)

const { describe, test, beforeEach } = require("node:test");
const assert = require("node:assert/strict");
const { loadScripts, FakeContext, FakeCanvas, installFakeBrowser, loadPage, pointerEvent } = require("./fakes.js");

const {
    Geometry, GraphicHelper, Model, LineFigure, RectangleFigure,
    EditCommand, LineCommand, RectangleCommand, BezierCommand, Controller, View
} = loadScripts("BezierCanvas", ["geometry", "graphic-helper", "mouse-event-converter", "model", "figures", "command", "controller", "view"]);

// command に、start から positions を順にたどるドラッグを与える (最後の位置で dragEnd)
function drag(command, start, positions) {
    command.onDragStart(start);
    for (const position of positions.slice(0, -1))
        command.onDragging(position);
    command.onDragEnd(positions[positions.length - 1]);
}

// Model の変更の通知の回数を数える
function countUpdates(model) {
    const counter = { count: 0 };
    model.addChangedListener(() => counter.count++);
    return counter;
}

function createCommand(CommandClass) {
    const model   = new Model();
    const command = new CommandClass();
    command.model = model;
    return { model, command };
}

describe("BezierCanvas のコマンド", () => {
    test("EditCommand ではクリックとドラッグでモデルが変わらない", () => {
        const { model, command } = createCommand(EditCommand);
        const updates = countUpdates(model);
        command.onClick({ x: 1, y: 1 });
        drag(command, { x: 0, y: 0 }, [{ x: 50, y: 0 }, { x: 100, y: 0 }]);
        assert.equal(model.figures.length, 0);
        assert.equal(updates.count, 0);
    });

    test("クリックだけでは図形を追加しない", () => {
        for (const CommandClass of [LineCommand, RectangleCommand, BezierCommand]) {
            const { model, command } = createCommand(CommandClass);
            command.onClick({ x: 1, y: 1 });
            assert.equal(model.figures.length, 0);
        }
    });

    test("線分は開始位置から現在位置まで。線の属性は黒・太さ 3", () => {
        const { model, command } = createCommand(LineCommand);
        command.onDragStart({ x: 10, y: 20 });
        command.onDragging({ x: 30, y: 40 });
        const figure = model.figures[0];
        assert.ok(figure instanceof LineFigure);
        assert.deepEqual([figure.start, figure.end], [{ x: 10, y: 20 }, { x: 30, y: 40 }]);
        assert.equal(figure.stroke, "black");
        assert.equal(figure.strokeThickness, 3);
        command.onDragEnd({ x: 50, y: 60 });
        assert.deepEqual(figure.end, { x: 50, y: 60 });
    });

    test("矩形はどの方向へのドラッグでも、2 点を対角とする正規化した矩形になる", () => {
        for (const end of [{ x: 40, y: 70 }, { x: -20, y: 70 }, { x: 40, y: -30 }, { x: -20, y: -30 }]) {
            const { model, command } = createCommand(RectangleCommand);
            drag(command, { x: 10, y: 20 }, [end, end]);
            const figure = model.figures[0];
            assert.ok(figure instanceof RectangleFigure);
            assert.deepEqual({ x: figure.x, y: figure.y, width: figure.width, height: figure.height },
                             { x: Math.min(10, end.x), y: Math.min(20, end.y), width: Math.abs(end.x - 10), height: Math.abs(end.y - 20) });
        }
    });

    test("ドラッグ 1 回でモデルの図形は 1 個だけ増え、作図中の図形も変更のたびに通知される", () => {
        const { model, command } = createCommand(LineCommand);
        const updates = countUpdates(model);
        command.onDragStart({ x: 0, y: 0 });
        assert.equal(model.figures.length, 0);
        command.onDragging({ x: 10, y: 0 });
        assert.equal(model.figures.length, 1);
        const countAfterFirst = updates.count;
        command.onDragging({ x: 20, y: 0 });
        assert.equal(updates.count, countAfterFirst + 1);
        command.onDragEnd({ x: 30, y: 0 });
        assert.equal(updates.count, countAfterFirst + 2);
        assert.equal(model.figures.length, 1);
        assert.equal(command.figure, null);
        // 次のドラッグでは別の図形が追加される
        drag(command, { x: 0, y: 10 }, [{ x: 20, y: 10 }, { x: 30, y: 10 }]);
        assert.equal(model.figures.length, 2);
        assert.notEqual(model.figures[0], model.figures[1]);
    });

    describe("曲線", () => {
        // 開始 (0, 0) の後に positions をたどって途中まで動かした BezierCommand
        function dragging(positions) {
            const { model, command } = createCommand(BezierCommand);
            command.onDragStart({ x: 0, y: 0 });
            for (const position of positions)
                command.onDragging(position);
            return { model, command, figure: model.figures[0] };
        }

        test("間引き: 最後に採用した点から 16 を超えたときだけ追加する", () => {
            const { command } = dragging([{ x: 15, y: 0 }, { x: 16, y: 0 }, { x: 16.001, y: 0 }, { x: 32, y: 0 }, { x: 32.002, y: 0 }]);
            assert.deepEqual(command.positions, [{ x: 0, y: 0 }, { x: 16.001, y: 0 }, { x: 32.002, y: 0 }]);
        });

        test("3 点たまるまでは図形に通過点を設定せず、何も描かない", () => {
            const { command, figure } = dragging([{ x: 20, y: 0 }]);
            assert.equal(command.positions.length, 2);
            assert.deepEqual(figure.positions, []);
            command.onDragging({ x: 40, y: 10 });
            assert.equal(figure.positions.length, 3);
        });

        test("確定 (置換): 2 点以上あり、終点が最後の点から 16 以内なら、最後の点を終点で置き換える", () => {
            const { command, figure } = dragging([{ x: 20, y: 0 }, { x: 40, y: 0 }]);
            command.onDragEnd({ x: 40, y: 16 });
            assert.deepEqual(command.positions, [{ x: 0, y: 0 }, { x: 20, y: 0 }, { x: 40, y: 16 }]);
            assert.deepEqual(figure.positions, command.positions);
        });

        test("確定 (追加): 終点が最後の点から 16 を超えるなら追加する", () => {
            const { command } = dragging([{ x: 20, y: 0 }, { x: 40, y: 0 }]);
            command.onDragEnd({ x: 40, y: 16.001 });
            assert.equal(command.positions.length, 4);
        });

        test("確定 (追加): 1 点だけなら、近くても追加する", () => {
            const { command, figure } = dragging([{ x: 5, y: 0 }]);
            command.onDragEnd({ x: 5, y: 0 });
            assert.deepEqual(command.positions, [{ x: 0, y: 0 }, { x: 5, y: 0 }]);
            // 2 点以下で確定した曲線はモデルに残るが、何も描かない
            assert.deepEqual(figure.positions, []);
            const context = new FakeContext({});
            figure.draw(context);
            assert.deepEqual(context.callsOf("moveTo", "bezierCurveTo", "lineTo"), []);
        });

        test("確定 (同じ点): 終点が最後の点と同じ座標 (別のオブジェクト) なら追加しない", () => {
            const { command } = dragging([]);
            command.onDragEnd({ x: 0, y: 0 });
            assert.deepEqual(command.positions, [{ x: 0, y: 0 }]);
        });

        test("確定した曲線は終点を通る", () => {
            const { command, figure } = dragging([{ x: 20, y: 0 }, { x: 40, y: 10 }, { x: 60, y: 0 }]);
            command.onDragEnd({ x: 70, y: 5 });
            assert.deepEqual(figure.positions[figure.positions.length - 1], { x: 70, y: 5 });
            const context = new FakeContext({});
            figure.draw(context);
            const curves = context.callsOf("bezierCurveTo");
            assert.equal(curves.length, figure.positions.length - 1);
            assert.deepEqual(curves[curves.length - 1].slice(5), [70, 5]);
        });
    });
});

describe("BezierCanvas の Controller", () => {
    test("Command → Model の順でも Model → Command の順でも、Command に Model が渡る", () => {
        const model = new Model();
        const controller1 = new Controller();
        const command1    = new LineCommand();
        controller1.command = command1;
        controller1.model   = model;
        assert.equal(command1.model, model);

        const controller2 = new Controller();
        controller2.model   = model;
        const command2    = new LineCommand();
        controller2.command = command2;
        assert.equal(command2.model, model);
    });

    test("受け取った操作を現在の Command に委譲する", () => {
        const model      = new Model();
        const controller = new Controller();
        controller.model = model;
        controller.onDragStart({ x: 0, y: 0 });   // Command が未設定なら何もしない
        controller.command = new LineCommand();
        controller.onDragStart({ x: 0, y: 0 });
        controller.onDragging({ x: 20, y: 0 });
        controller.onDragEnd({ x: 30, y: 0 });
        assert.equal(model.figures.length, 1);
    });
});

describe("BezierCanvas の図形と GraphicHelper", () => {
    test("線分・矩形の描画命令と線の属性", () => {
        const line = new LineFigure({ x: 1, y: 2 });
        line.end = { x: 3, y: 4 };
        line.stroke = "black";
        line.strokeThickness = 3;
        const rectangle = new RectangleFigure();
        Object.assign(rectangle, { x: 5, y: 6, width: 7, height: 8, stroke: "black", strokeThickness: 3 });
        const context = new FakeContext({});
        line.draw(context);
        rectangle.draw(context);
        assert.deepEqual(context.callsOf("moveTo", "lineTo", "rect", "stroke"), [
            ["moveTo", 1, 2], ["lineTo", 3, 4], ["stroke", "black", 3],
            ["rect", 5, 6, 7, 8], ["stroke", "black", 3]
        ]);
        // 描画の後は、context の属性が元に戻る
        assert.equal(context.lineWidth, 1);
    });

    test("線の色が無い図形は描かない", () => {
        const context = new FakeContext({});
        new LineFigure({ x: 1, y: 2 }).draw(context);
        assert.deepEqual(context.calls, []);
    });

    test("addBezierPath は moveTo と、区間ごとの bezierCurveTo になる", () => {
        const points  = [{ x: 0, y: 0 }, { x: 30, y: 20 }, { x: 60, y: 0 }, { x: 90, y: 30 }];
        const bezier  = Geometry.toSplineBezier(points);
        const context = new FakeContext({});
        GraphicHelper.addBezierPath(context, points);
        assert.deepEqual(context.calls[0], ["moveTo", 0, 0]);
        assert.equal(context.calls.length, 1 + 3);
        for (let segment = 0; segment < 3; segment++) {
            const [c1, c2, end] = bezier.slice(3 * segment + 1, 3 * segment + 4);
            assert.deepEqual(context.calls[1 + segment], ["bezierCurveTo", c1.x, c1.y, c2.x, c2.y, end.x, end.y]);
        }
    });

    test("addBezierPath は 2 点未満なら何もしない", () => {
        const context = new FakeContext({});
        GraphicHelper.addBezierPath(context, [{ x: 0, y: 0 }]);
        assert.deepEqual(context.calls, []);
    });

    test("addBezierPolygonPath は 3 点以上で閉じた曲線、2 点で直線", () => {
        const context = new FakeContext({});
        GraphicHelper.addBezierPolygonPath(context, [{ x: 0, y: 0 }, { x: 30, y: 20 }, { x: 60, y: 0 }]);
        assert.equal(context.callsOf("bezierCurveTo").length, 3);
        assert.deepEqual(context.callsOf("bezierCurveTo")[2].slice(5), [0, 0]);
        assert.deepEqual(context.calls[context.calls.length - 1], ["closePath"]);
        const lineContext = new FakeContext({});
        GraphicHelper.addBezierPolygonPath(lineContext, [{ x: 0, y: 0 }, { x: 30, y: 20 }]);
        assert.deepEqual(lineContext.calls, [["moveTo", 0, 0], ["lineTo", 30, 20]]);
    });
});

describe("BezierCanvas の View", () => {
    let browser;
    beforeEach(() => {
        browser = installFakeBrowser();
    });

    function createView() {
        const canvas     = new FakeCanvas();
        const model      = new Model();
        const view       = new View(canvas, model);
        const controller = new Controller();
        view.controller  = controller;
        return { canvas, model, view, controller };
    }

    // 偽のポインターのイベントで、描画領域を positions[0] から順にドラッグする (release が false なら離さない)
    function dragOnCanvas(canvas, positions, release = true) {
        const event = (type, position, properties) => canvas.dispatch(type, Object.assign({
            currentTarget: canvas, pointerId: 1, isPrimary: true, button: -1, buttons: 1,
            clientX: position.x, clientY: position.y, preventDefault() {}
        }, properties));
        event("pointerdown", positions[0], { button: 0 });
        for (const position of positions.slice(1))
            event("pointermove", position, {});
        if (release)
            event("pointerup", positions[positions.length - 1], { button: 0, buttons: 0 });
    }

    test("表示サイズと devicePixelRatio に合わせて内部の画素数を決め、CSS ピクセルに合わせる変換で描く", () => {
        const { canvas, model } = createView();
        globalThis.devicePixelRatio = 1.5;
        browser.resize(201, 100);
        assert.equal(canvas.width, 302);   // 201 × 1.5 = 301.5 を丸める
        assert.equal(canvas.height, 150);
        assert.deepEqual(canvas.context.callsOf("setTransform").pop(), ["setTransform", 302 / 201, 0, 0, 150 / 100, 0, 0]);
        assert.equal(model.figures.length, 0);
    });

    test("ドラッグ中の図形と確定済みの図形の両方が、毎回の再描画で描かれる", () => {
        const { canvas, model, controller } = createView();
        browser.resize(400, 300);
        controller.command = new LineCommand();
        dragOnCanvas(canvas, [{ x: 0, y: 0 }, { x: 50, y: 0 }]);
        controller.command = new BezierCommand();
        dragOnCanvas(canvas, [{ x: 0, y: 100 }, { x: 20, y: 110 }, { x: 40, y: 100 }, { x: 60, y: 120 }], false);
        assert.equal(model.figures.length, 2);
        canvas.context.calls = [];
        browser.runAnimationFrames();
        const calls = canvas.context.callsOf("clearRect", "lineTo", "bezierCurveTo");
        assert.deepEqual(calls[0], ["clearRect", 0, 0, 400, 300]);
        assert.deepEqual(calls[1], ["lineTo", 50, 0]);
        assert.ok(calls.slice(2).length >= 2 && calls.slice(2).every(call => call[0] === "bezierCurveTo"));
    });

    test("再描画の予約は、予約済みなら重ねない", () => {
        const { model } = createView();
        browser.resize(400, 300);
        model.update();
        model.update();
        assert.equal(browser.animationFrames.length, 1);
        browser.runAnimationFrames();
        model.update();
        assert.equal(browser.animationFrames.length, 1);
    });

    test("リサイズと devicePixelRatio の変化の後も、すべての図形が描き直される", () => {
        const { canvas, model, controller } = createView();
        browser.resize(400, 300);
        controller.command = new RectangleCommand();
        dragOnCanvas(canvas, [{ x: 10, y: 10 }, { x: 60, y: 40 }]);
        dragOnCanvas(canvas, [{ x: 100, y: 10 }, { x: 160, y: 40 }]);
        assert.equal(model.figures.length, 2);

        canvas.context.calls = [];
        browser.resize(200, 100);
        assert.equal(canvas.width, 200);
        assert.deepEqual(canvas.context.callsOf("rect"), [["rect", 10, 10, 50, 30], ["rect", 100, 10, 60, 30]]);

        canvas.context.calls = [];
        browser.changeDevicePixelRatio(2);
        assert.equal(canvas.width, 400);
        assert.equal(canvas.height, 200);
        assert.deepEqual(canvas.context.callsOf("setTransform").pop(), ["setTransform", 2, 0, 0, 2, 0, 0]);
        assert.deepEqual(canvas.context.callsOf("rect"), [["rect", 10, 10, 50, 30], ["rect", 100, 10, 60, 30]]);
        assert.deepEqual(canvas.context.callsOf("stroke").map(call => call[2]), [3, 3]);   // 線の太さは CSS ピクセルのまま
        // 新しい値で監視し直している
        assert.equal(browser.mediaQueries.length, 1);
        assert.equal(browser.mediaQueries[0].media, "(resolution: 2dppx)");
    });

    test("内部の画素数は、変わったときだけ設定する (設定すると内容が消えるため)", () => {
        const { canvas } = createView();
        browser.resize(400, 300);
        const count = canvas.sizeSetCount;
        browser.resize(400, 300);
        assert.equal(canvas.sizeSetCount, count);
    });

    test("Controller を差し替えると、古い Controller には操作が届かない", () => {
        const { canvas, model, view, controller } = createView();
        controller.command  = new LineCommand();
        const controller2   = new Controller();
        view.controller     = controller2;
        controller2.command = new LineCommand();
        // 両方の Controller の Command が同じ Model に描くので、古い方にも届いていれば図形が 2 個になる
        dragOnCanvas(canvas, [{ x: 0, y: 0 }, { x: 50, y: 0 }]);
        assert.equal(model.figures.length, 1);
        assert.equal(controller2.model, model);
    });
});

describe("BezierCanvas のページ", () => {
    function createPage() {
        const browser = installFakeBrowser();
        const canvas  = new FakeCanvas();
        const page    = loadPage("BezierCanvas", canvas, {
            devicePixelRatio     : 1,
            requestAnimationFrame: globalThis.requestAnimationFrame,
            ResizeObserver       : globalThis.ResizeObserver,
            matchMedia           : globalThis.matchMedia
        });
        browser.resize(400, 300);
        const pointer = (type, x, y, properties) => canvas.dispatch(type, pointerEvent(canvas, Object.assign({
            clientX: x, clientY: y
        }, properties)));
        return { browser, canvas, page, pointer };
    }

    test("index.html のスクリプトを 1 つのスコープで順に読み込み、ツールバーで描画モードを切り替えて描ける", () => {
        const { page, pointer } = createPage();
        assert.deepEqual(page.sources, [
            "geometry.js", "graphic-helper.js", "mouse-event-converter.js", "model.js", "figures.js",
            "command.js", "controller.js", "view.js", "main.js"
        ]);
        const dragLine = () => {
            pointer("pointerdown", 0, 0, { button: 0 });
            pointer("pointermove", 50, 0, {});
            pointer("pointerup", 60, 0, { button: 0, buttons: 0 });
        };
        // 起動直後は Edit モードで、どのボタンも選ばれていない
        assert.equal(page.evaluate("controller.command.constructor.name"), "EditCommand");
        assert.ok(page.buttons.every(button => button.getAttribute("aria-pressed") === "false"));
        dragLine();
        assert.equal(page.evaluate("model.figures.length"), 0);

        for (const [index, name] of ["LineCommand", "RectangleCommand", "BezierCommand"].entries()) {
            page.buttons[index].click();
            assert.equal(page.evaluate("controller.command.constructor.name"), name);
            assert.deepEqual(page.buttons.map(button => button.getAttribute("aria-pressed")), page.buttons.map((button, other) => String(other === index)));
        }
        page.buttons[0].click();
        dragLine();
        dragLine();
        assert.equal(page.evaluate("model.figures.length"), 2);
    });

    for (const selected of [0, 1]) {
        for (const endType of ["pointerup", "pointercancel", "lostpointercapture"]) {
            test(`ドラッグ中のボタン ${selected} の選択は次の作図から適用される (${endType})`, () => {
                const { browser, page, pointer } = createPage();
                page.buttons[0].click();
                const originalCommand = page.evaluate("controller.command");
                const originalEnd = originalCommand.onDragEnd;
                let endCount = 0;
                originalCommand.onDragEnd = position => {
                    endCount++;
                    originalEnd.call(originalCommand, position);
                };
                pointer("pointerdown", 0, 0, { button: 0 });
                pointer("pointermove", 20, 0, {});
                const line = page.evaluate("model.figures[0]");
                page.buttons[selected].click();
                const nextCommand = page.evaluate("controller.command");
                assert.notEqual(nextCommand, originalCommand);
                assert.equal(page.buttons[selected].getAttribute("aria-pressed"), "true");
                pointer("pointermove", 40, 20, {});
                assert.equal(page.evaluate("model.figures.length"), 1);
                browser.runAnimationFrames();
                pointer(endType, 60, 30, { button: 0, buttons: 0 });
                pointer("lostpointercapture", 60, 30, { buttons: 0 });
                pointer("pointerup", 60, 30, { button: 0, buttons: 0 });
                const end = endType === "pointerup" ? [60, 30] : [40, 20];
                assert.deepEqual([line.start.x, line.start.y, line.end.x, line.end.y], [0, 0, ...end]);
                assert.equal(endCount, 1);
                assert.equal(originalCommand.figure, null);
                assert.equal(nextCommand.figure, null);
                assert.doesNotThrow(() => page.evaluate("view.render()"));
                pointer("pointerdown", 10, 10, { button: 0 });
                pointer("pointermove", 30, 25, {});
                pointer("pointerup", 50, 40, { button: 0, buttons: 0 });
                assert.equal(page.evaluate("model.figures.length"), 2);
                const nextFigure = page.evaluate("model.figures[1]");
                if (selected === 0)
                    assert.deepEqual([nextFigure.start.x, nextFigure.start.y, nextFigure.end.x, nextFigure.end.y], [10, 10, 50, 40]);
                else
                    assert.deepEqual([nextFigure.x, nextFigure.y, nextFigure.width, nextFigure.height], [10, 10, 40, 30]);
                browser.runAnimationFrames();
                assert.doesNotThrow(() => page.evaluate("view.render()"));
                assert.equal(endCount, 1);
            });
        }
    }

    for (const selected of [0, 1]) {
        for (const lastMove of [null, 9]) {
            test(`解放時に初めて閾値を超えても図形 ${selected} を追加する (最後の移動: ${lastMove})`, () => {
                const { browser, page, pointer } = createPage();
                page.buttons[selected].click();
                pointer("pointerdown", 0, 0, { button: 0 });
                if (lastMove !== null)
                    pointer("pointermove", lastMove, 0, {});
                pointer("pointerup", 30, 40, { button: 0, buttons: 0 });
                assert.equal(page.evaluate("model.figures.length"), 1);
                const figure = page.evaluate("model.figures[0]");
                if (selected === 0)
                    assert.deepEqual([figure.start.x, figure.start.y, figure.end.x, figure.end.y], [0, 0, 30, 40]);
                else
                    assert.deepEqual([figure.x, figure.y, figure.width, figure.height], [0, 0, 30, 40]);
                browser.runAnimationFrames();
            });
        }
    }
});
