"use strict";

// BezierWpf の MainWindow に当たる。Model・View・Controller を結線し、ツールバーの選択に応じて Controller の Command (描画モード) を切り替える。
// ここのトップレベルの const (model / view / controller) は、DevTools のコンソールから参照できる (例: model.figures.length)

const model      = new Model();
const view       = new View(document.getElementById("drawing-area"), model);
const controller = new Controller();

// view.controller の設定で controller.model に Model が渡り、ポインターのイベントが Controller に流れるようになる
view.controller    = controller;
// 起動直後は、何も描かない EditCommand にしておく
controller.command = new EditCommand();

// ツールバーのボタン (data-command 属性) ごとに、選んだときに作る Command。
// Controller は受け取ったマウス操作を現在の Command に委譲するだけなので、モードを増やしても Controller は変わらない
const commandFactories = {
    line     : () => new LineCommand(),
    rectangle: () => new RectangleCommand(),
    bezier   : () => new BezierCommand()
};

for (const button of document.querySelectorAll("[data-command]")) {
    button.addEventListener("click", () => {
        controller.command = commandFactories[button.dataset.command]();
        // 選んでいるボタンを示す (WPF 版のメニューには表示が無いが、ブラウザでは状態が分からないと操作しにくいため)
        for (const other of document.querySelectorAll("[data-command]"))
            other.setAttribute("aria-pressed", String(other === button));
    });
}
