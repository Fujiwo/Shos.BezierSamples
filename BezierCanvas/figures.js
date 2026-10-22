"use strict";

/**
 * Model に入れる図形の基底クラス (WPF 版の Shape に当たる)。
 * WPF の Shape は自分を描く処理を WPF が持っているが、Canvas 2D では図形ごとの描画をアプリが書く。
 * 線の属性 (stroke / strokeThickness) は AddCommand が設定する。
 */
class Figure {
    constructor() {
        // 線の色 (CSS の色)。null なら線を描かない (WPF の Shape.Stroke の既定と同じ)
        this.stroke          = null;
        // 線の太さ (CSS ピクセル)
        this.strokeThickness = 1.0;
    }

    /**
     * 図形を context に描く。座標は CSS ピクセルで、高 DPI への対応は View が context の変換で行う。
     */
    draw(context) {
        if (this.stroke === null)
            return;
        context.save();
        context.strokeStyle = this.stroke;
        context.lineWidth   = this.strokeThickness;
        context.beginPath();
        this.addPath(context);
        context.stroke();
        context.restore();
    }

    /**
     * 図形の形をパスとして context に足す。派生クラスが実装する。
     */
    addPath(context) {}
}

/**
 * 線分 (WPF 版の Line)。
 */
class LineFigure extends Figure {
    constructor(start) {
        super();
        this.start = start;
        this.end   = start;
    }

    addPath(context) {
        context.moveTo(this.start.x, this.start.y);
        context.lineTo(this.end.x  , this.end.y  );
    }
}

/**
 * 矩形 (WPF 版の Rectangle)。左上の位置と、0 以上の幅・高さで表す。
 */
class RectangleFigure extends Figure {
    constructor() {
        super();
        this.x      = 0.0;
        this.y      = 0.0;
        this.width  = 0.0;
        this.height = 0.0;
    }

    addPath(context) {
        // WPF の Rectangle は線を図形の内側に描くが、Canvas 2D は輪郭の中心に描くので、線の太さの半分だけ外にはみ出す
        context.rect(this.x, this.y, this.width, this.height);
    }
}

/**
 * すべての通過点を通る曲線 (WPF 版の Path)。通過点が 3 個以上のときだけ描く。
 */
class BezierFigure extends Figure {
    constructor() {
        super();
        // 通過点の列。BezierCommand が、3 個以上たまったときに設定する (WPF 版で Path.Data を設定するのと同じ)
        this.positions = [];
    }

    addPath(context) {
        // 2 個以下で確定した曲線は、Model に残るが何も描かない (WPF 版で Data の無い Path が残るのと同じ)
        if (this.positions.length > 2)
            GraphicHelper.addBezierPath(context, this.positions);
    }
}

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { Figure, LineFigure, RectangleFigure, BezierFigure };
