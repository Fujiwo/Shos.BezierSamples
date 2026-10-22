"use strict";

/**
 * 通過点の列から、ベジエ曲線を Canvas 2D のパスとして描くヘルパー (BezierWpf の GraphicHelper に対応)。
 * WPF 版は PathGeometry を作るが、Canvas 2D (即時モード) では描画コンテキストに直接パスを組み立てる。
 * どちらの関数も、パスを足すだけで beginPath / stroke は呼ばない (呼び出し側で線の属性と一緒に扱う)。
 */
const GraphicHelper = {
    /**
     * 通過点 points をすべて通る、開いた 3 次スプライン曲線のパスを context に足す。
     * 最初と最後の区間も含め、n − 1 個の区間すべてが 3 次ベジエ曲線になる。点が 2 個なら 1 本の直線になる。
     */
    addBezierPath(context, points) {
        if (points.length < 2)
            return;
        // bezierPoints は [points[0], 制御点1, 制御点2, points[1], 制御点1, 制御点2, points[2], …] の形
        GraphicHelper.addBezierSegments(context, Geometry.toSplineBezier(points));
    },

    /**
     * 通過点 points をすべて通って最初の点に戻る、閉じた 3 次スプライン曲線のパスを context に足す。
     * 点が 2 個のときは 1 本の直線になる。
     */
    addBezierPolygonPath(context, points) {
        // 2 点だけでは曲がる向きが決まらないので、直線にする
        if (points.length === 2) {
            context.moveTo(points[0].x, points[0].y);
            context.lineTo(points[1].x, points[1].y);
        } else if (points.length >= 3) {
            // toClosedSplineBezier() の結果は points[0] から始まり、最後の区間で points[0] に戻る。
            // closePath() で閉じた図形として扱うと、始点のつなぎ目も線の結合 (lineJoin) で描かれる
            GraphicHelper.addBezierSegments(context, Geometry.toClosedSplineBezier(points));
            context.closePath();
        }
    },

    // [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] の点列を、moveTo と bezierCurveTo の並びにする。
    // bezierPoints[0] は始点なので、index 1 から [制御点1, 制御点2, 終点] の 3 個ずつ bezierCurveTo にする
    addBezierSegments(context, bezierPoints) {
        context.moveTo(bezierPoints[0].x, bezierPoints[0].y);
        for (let index = 1; index + 2 < bezierPoints.length; index += 3)
            context.bezierCurveTo(bezierPoints[index    ].x, bezierPoints[index    ].y,
                                  bezierPoints[index + 1].x, bezierPoints[index + 1].y,
                                  bezierPoints[index + 2].x, bezierPoints[index + 2].y);
    }
};

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { GraphicHelper };
