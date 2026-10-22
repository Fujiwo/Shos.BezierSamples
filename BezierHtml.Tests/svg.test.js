"use strict";

// BezierSvg のコマンド・モデル・GraphicHelper・View のテスト。
// SVG の要素は偽の document で作り、属性の値で形を確かめる (実際に表示されることは、ブラウザでの手動確認に残る)

const { describe, test } = require("node:test");
const assert = require("node:assert/strict");
const { loadScripts, FakeSvgElement, installFakeDocument, loadPage } = require("./fakes.js");

installFakeDocument();
const {
    Geometry, GraphicHelper, Model, EditCommand, LineCommand, RectangleCommand, BezierCommand, Controller, View
} = loadScripts("BezierSvg", ["geometry", "graphic-helper", "mouse-event-converter", "model", "command", "controller", "view"]);

const svgNamespace = "http://www.w3.org/2000/svg";

// command に、start から positions を順にたどるドラッグを与える (最後の位置で dragEnd)
function drag(command, start, positions) {
    command.onDragStart(start);
    for (const position of positions.slice(0, -1))
        command.onDragging(position);
    command.onDragEnd(positions[positions.length - 1]);
}

function createCommand(CommandClass) {
    const svg     = new FakeSvgElement(svgNamespace, "svg");
    const model   = new Model(svg);
    const command = new CommandClass();
    command.model = model;
    return { svg, model, command };
}

describe("BezierSvg のコマンド", () => {
    test("EditCommand ではクリックとドラッグで要素が増えない", () => {
        const { svg, command } = createCommand(EditCommand);
        command.onClick({ x: 1, y: 1 });
        drag(command, { x: 0, y: 0 }, [{ x: 50, y: 0 }, { x: 100, y: 0 }]);
        assert.equal(svg.children.length, 0);
    });

    test("クリックだけでは図形を追加しない", () => {
        for (const CommandClass of [LineCommand, RectangleCommand, BezierCommand]) {
            const { svg, command } = createCommand(CommandClass);
            command.onClick({ x: 1, y: 1 });
            assert.equal(svg.children.length, 0);
        }
    });

    test("線分は SVG の名前空間の <line> で、線の属性は黒・太さ 3・塗りつぶしなし", () => {
        const { svg, command } = createCommand(LineCommand);
        command.onDragStart({ x: 10, y: 20 });
        command.onDragging({ x: 30, y: 40 });
        const line = svg.children[0];
        assert.equal(line.namespaceURI, svgNamespace);
        assert.equal(line.tagName, "line");
        assert.deepEqual(line.attributes, {
            "stroke": "black", "stroke-width": "3", "fill": "none", "stroke-miterlimit": "10",
            "x1": "10", "y1": "20", "x2": "30", "y2": "40"
        });
        command.onDragEnd({ x: 50, y: 60 });
        assert.deepEqual([line.getAttribute("x2"), line.getAttribute("y2")], ["50", "60"]);
    });

    test("矩形はどの方向へのドラッグでも、2 点を対角とする正規化した <rect> になる", () => {
        for (const end of [{ x: 40, y: 70 }, { x: -20, y: 70 }, { x: 40, y: -30 }, { x: -20, y: -30 }]) {
            const { svg, command } = createCommand(RectangleCommand);
            drag(command, { x: 10, y: 20 }, [end, end]);
            const rect = svg.children[0];
            assert.equal(rect.tagName, "rect");
            assert.deepEqual(["x", "y", "width", "height"].map(name => rect.getAttribute(name)),
                             [Math.min(10, end.x), Math.min(20, end.y), Math.abs(end.x - 10), Math.abs(end.y - 20)].map(String));
        }
    });

    test("ドラッグ 1 回で要素は 1 個だけ増え、属性の書き換えで形が変わる", () => {
        const { svg, command } = createCommand(LineCommand);
        command.onDragStart({ x: 0, y: 0 });
        assert.equal(svg.children.length, 0);
        command.onDragging({ x: 10, y: 0 });
        command.onDragging({ x: 20, y: 0 });
        assert.equal(svg.children.length, 1);
        assert.equal(svg.children[0].getAttribute("x2"), "20");
        command.onDragEnd({ x: 30, y: 0 });
        assert.equal(svg.children.length, 1);
        assert.equal(command.shape, null);
        drag(command, { x: 0, y: 10 }, [{ x: 20, y: 10 }, { x: 30, y: 10 }]);
        assert.equal(svg.children.length, 2);
        assert.notEqual(svg.children[0], svg.children[1]);
    });

    describe("曲線", () => {
        // 開始 (0, 0) の後に positions をたどって途中まで動かした BezierCommand
        function dragging(positions) {
            const { svg, command } = createCommand(BezierCommand);
            command.onDragStart({ x: 0, y: 0 });
            for (const position of positions)
                command.onDragging(position);
            return { command, path: svg.children[0] };
        }

        test("間引き: 最後に採用した点から 16 を超えたときだけ追加する", () => {
            const { command } = dragging([{ x: 15, y: 0 }, { x: 16, y: 0 }, { x: 16.001, y: 0 }, { x: 32, y: 0 }, { x: 32.002, y: 0 }]);
            assert.deepEqual(command.positions, [{ x: 0, y: 0 }, { x: 16.001, y: 0 }, { x: 32.002, y: 0 }]);
        });

        test("3 点たまるまでは d 属性を設定しない", () => {
            const { command, path } = dragging([{ x: 20, y: 0 }]);
            assert.equal(path.tagName, "path");
            assert.equal(path.hasAttribute("d"), false);
            command.onDragging({ x: 40, y: 10 });
            assert.equal(path.getAttribute("d"), GraphicHelper.toPathData(command.positions));
        });

        test("確定 (置換): 2 点以上あり、終点が最後の点から 16 以内なら、最後の点を終点で置き換える", () => {
            const { command, path } = dragging([{ x: 20, y: 0 }, { x: 40, y: 0 }]);
            command.onDragEnd({ x: 40, y: 16 });
            assert.deepEqual(command.positions, [{ x: 0, y: 0 }, { x: 20, y: 0 }, { x: 40, y: 16 }]);
            assert.ok(path.getAttribute("d").endsWith(" 40 16"));
        });

        test("確定 (追加): 終点が最後の点から 16 を超えるなら追加する", () => {
            const { command } = dragging([{ x: 20, y: 0 }, { x: 40, y: 0 }]);
            command.onDragEnd({ x: 40, y: 16.001 });
            assert.equal(command.positions.length, 4);
        });

        test("確定 (追加): 1 点だけなら近くても追加するが、2 点では d 属性の無い <path> が残る", () => {
            const { command, path } = dragging([{ x: 5, y: 0 }]);
            command.onDragEnd({ x: 5, y: 0 });
            assert.deepEqual(command.positions, [{ x: 0, y: 0 }, { x: 5, y: 0 }]);
            assert.equal(path.hasAttribute("d"), false);
        });

        test("確定 (同じ点): 終点が最後の点と同じ座標 (別のオブジェクト) なら追加しない", () => {
            const { command } = dragging([]);
            command.onDragEnd({ x: 0, y: 0 });
            assert.deepEqual(command.positions, [{ x: 0, y: 0 }]);
        });
    });
});

describe("BezierSvg の Controller", () => {
    test("Command → Model の順でも Model → Command の順でも、Command に Model が渡る", () => {
        const model = new Model(new FakeSvgElement(svgNamespace, "svg"));
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
});

describe("BezierSvg の GraphicHelper", () => {
    test("toPathData は M と、区間ごとの C になる (数値は丸めない)", () => {
        const points = [{ x: 0, y: 0 }, { x: 30, y: 20 }, { x: 60, y: 0 }, { x: 90, y: 30 }];
        const bezier = Geometry.toSplineBezier(points);
        const expected = ["M 0 0"];
        for (let segment = 0; segment < 3; segment++) {
            const [c1, c2, end] = bezier.slice(3 * segment + 1, 3 * segment + 4);
            expected.push(`C ${c1.x} ${c1.y} ${c2.x} ${c2.y} ${end.x} ${end.y}`);
        }
        assert.equal(GraphicHelper.toPathData(points), expected.join(" "));
    });

    test("toPathData は 2 点で直線状の 1 区間、2 点未満で空文字列", () => {
        assert.equal(GraphicHelper.toPathData([{ x: 10, y: 20 }, { x: 70, y: 50 }]), "M 10 20 C 30 30 50 40 70 50");
        assert.equal(GraphicHelper.toPathData([{ x: 10, y: 20 }]), "");
        assert.equal(GraphicHelper.toPathData([]), "");
    });

    test("toPolygonPathData は 3 点以上で Z で閉じた曲線、2 点で直線", () => {
        const data = GraphicHelper.toPolygonPathData([{ x: 0, y: 0 }, { x: 30, y: 20 }, { x: 60, y: 0 }]);
        assert.equal(data.match(/C/g).length, 3);
        assert.ok(data.startsWith("M 0 0 C "));
        assert.ok(data.endsWith(" 0 0 Z"));
        assert.equal(GraphicHelper.toPolygonPathData([{ x: 0, y: 0 }, { x: 30, y: 20 }]), "M 0 0 L 30 20");
        assert.equal(GraphicHelper.toPolygonPathData([{ x: 0, y: 0 }]), "");
    });
});

describe("BezierSvg の View", () => {
    // 偽のポインターのイベントで、描画領域を positions[0] から順にドラッグして離す
    function dragOnSvg(svg, positions) {
        const event = (type, position, properties) => svg.dispatch(type, Object.assign({
            currentTarget: svg, pointerId: 1, isPrimary: true, button: -1, buttons: 1,
            clientX: position.x, clientY: position.y, preventDefault() {}
        }, properties));
        event("pointerdown", positions[0], { button: 0 });
        for (const position of positions.slice(1))
            event("pointermove", position, {});
        event("pointerup", positions[positions.length - 1], { button: 0, buttons: 0 });
    }

    test("ポインターの操作が Controller を通じて Command に届き、Controller を差し替えると古い方には届かない", () => {
        const svg        = new FakeSvgElement(svgNamespace, "svg");
        svg.rect         = { left: 100, top: 40 };
        const model      = new Model(svg);
        const view       = new View(svg, model);
        const controller = new Controller();
        view.controller  = controller;
        controller.command = new LineCommand();
        dragOnSvg(svg, [{ x: 110, y: 50 }, { x: 160, y: 50 }]);
        assert.equal(model.shapes.length, 1);
        assert.deepEqual(["x1", "y1", "x2", "y2"].map(name => svg.children[0].getAttribute(name)), ["10", "10", "60", "10"]);

        const controller2 = new Controller();
        view.controller   = controller2;
        controller2.command = new LineCommand();
        // 両方の Controller の Command が同じ Model に描くので、古い方にも届いていれば要素が 2 個増える
        dragOnSvg(svg, [{ x: 110, y: 80 }, { x: 160, y: 80 }]);
        assert.equal(model.shapes.length, 2);
    });
});

describe("BezierSvg のページ", () => {
    test("index.html のスクリプトを 1 つのスコープで順に読み込み、ツールバーで描画モードを切り替えて描ける", () => {
        const svg  = new FakeSvgElement(svgNamespace, "svg");
        const page = loadPage("BezierSvg", svg, {});
        assert.deepEqual(page.sources, [
            "geometry.js", "graphic-helper.js", "mouse-event-converter.js", "model.js",
            "command.js", "controller.js", "view.js", "main.js"
        ]);
        const pointer = (type, x, y, properties) => svg.dispatch(type, Object.assign({
            currentTarget: svg, pointerId: 1, isPrimary: true, button: -1, buttons: 1, clientX: x, clientY: y, preventDefault() {}
        }, properties));
        const dragLine = () => {
            pointer("pointerdown", 0, 0, { button: 0 });
            pointer("pointermove", 50, 0, {});
            pointer("pointerup", 60, 0, { button: 0, buttons: 0 });
        };
        // 起動直後は Edit モードで、どのボタンも選ばれていない
        assert.equal(page.evaluate("controller.command.constructor.name"), "EditCommand");
        assert.ok(page.buttons.every(button => button.getAttribute("aria-pressed") === "false"));
        dragLine();
        assert.equal(svg.children.length, 0);

        for (const [index, name] of ["LineCommand", "RectangleCommand", "BezierCommand"].entries()) {
            page.buttons[index].click();
            assert.equal(page.evaluate("controller.command.constructor.name"), name);
            assert.deepEqual(page.buttons.map(button => button.getAttribute("aria-pressed")), page.buttons.map((button, other) => String(other === index)));
        }
        page.buttons[1].click();
        dragLine();
        dragLine();
        assert.equal(svg.children.length, 2);
        assert.ok(svg.children.every(child => child.tagName === "rect" && child.namespaceURI === svgNamespace));
    });
});
