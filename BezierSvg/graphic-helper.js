"use strict";

/**
 * 通過点の列から、ベジエ曲線を SVG の <path> の d 属性 (パスデータ) として作るヘルパー (BezierWpf の GraphicHelper に対応)。
 * WPF 版は PathGeometry を作るが、SVG ではパスを "M x y C …" という文字列で表す。
 */
const GraphicHelper = {
    /**
     * 通過点 points をすべて通る、開いた 3 次スプライン曲線のパスデータを返す。
     * 最初と最後の区間も含め、n − 1 個の区間すべてが 3 次ベジエ曲線になる。点が 2 個なら 1 本の直線になる。
     * @returns "M x0 y0 C x1 y1 x2 y2 x3 y3 C …"。点が 2 個未満なら空文字列
     */
    toPathData(points) {
        if (points.length < 2)
            return "";
        // bezierPoints は [points[0], 制御点1, 制御点2, points[1], 制御点1, 制御点2, points[2], …] の形
        return GraphicHelper.toBezierSegments(Geometry.toSplineBezier(points));
    },

    /**
     * 通過点 points をすべて通って最初の点に戻る、閉じた 3 次スプライン曲線のパスデータを返す。
     * 点が 2 個のときは 1 本の直線になる。
     * @returns "M x0 y0 C … Z"。点が 2 個なら "M x0 y0 L x1 y1"、2 個未満なら空文字列
     */
    toPolygonPathData(points) {
        // 2 点だけでは曲がる向きが決まらないので、直線にする
        if (points.length === 2)
            return `M ${points[0].x} ${points[0].y} L ${points[1].x} ${points[1].y}`;
        if (points.length < 3)
            return "";
        // toClosedSplineBezier() の結果は points[0] から始まり、最後の区間で points[0] に戻る。
        // Z で閉じた図形として扱うと、始点のつなぎ目も線の結合 (stroke-linejoin) で描かれる
        return `${GraphicHelper.toBezierSegments(Geometry.toClosedSplineBezier(points))} Z`;
    },

    // [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] の点列を、"M 始点 C 制御点1 制御点2 終点 C …" にする。
    // 数値は丸めずにそのまま書く (2 区間目以降の C は省略できるが、読みやすさのために付ける)
    toBezierSegments(bezierPoints) {
        const commands = [`M ${bezierPoints[0].x} ${bezierPoints[0].y}`];
        for (let index = 1; index + 2 < bezierPoints.length; index += 3)
            commands.push(`C ${bezierPoints[index    ].x} ${bezierPoints[index    ].y} ` +
                            `${bezierPoints[index + 1].x} ${bezierPoints[index + 1].y} ` +
                            `${bezierPoints[index + 2].x} ${bezierPoints[index + 2].y}`);
        return commands.join(" ");
    }
};

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { GraphicHelper };
