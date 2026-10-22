"use strict";

/**
 * 通過点の列を、ベジエ曲線用の点列に変換する関数群 (BezierWpf の Geo / GeometryExtensions に対応)。
 * 点やベクトルは {x, y} の普通のオブジェクトで表す。DOM・Canvas・SVG には依存しない。
 *
 * 通過点の列の変換には、すべての点を通る 3 次スプライン曲線 (toSplineBezier / toClosedSplineBezier、描画に使う) と、
 * Catmull-Rom 版 (toBezierPolyline / toBezierPolygon、3 次スプライン曲線と比べるためのもの) がある。
 * どちらも、各通過点での接線 m から 3 次ベジエの制御点を P ± m / 3 として求める点は同じで、違うのは接線の求め方だけである。
 * どの関数も入力を変更せず、新しい配列と新しい点のオブジェクトを返す。
 */
const Geometry = (() => {
    // 以下の 4 つは、点を位置ベクトルとして扱うベクトル演算

    // 2 つの点をベクトルとして足す
    function add(point1, point2) {
        return { x: point1.x + point2.x, y: point1.y + point2.y };
    }

    // point2 から point1 へ向かうベクトル (point1 − point2) を返す
    function subtract(point1, point2) {
        return { x: point1.x - point2.x, y: point1.y - point2.y };
    }

    // ベクトルを rate 倍する
    function multiply(point, rate) {
        return { x: point.x * rate, y: point.y * rate };
    }

    // 点の写しを作る (戻り値が入力の点を共有しないようにする)
    function copy(point) {
        return { x: point.x, y: point.y };
    }

    /**
     * 2 点間の距離を返す。
     */
    function distance(point1, point2) {
        return Math.hypot(point1.x - point2.x, point1.y - point2.y);
    }

    /**
     * 4 つの通過点 P0〜P3 から、中央の区間 P1 → P2 を描く 3 次ベジエ曲線の
     * [始点 P1, 制御点 C1, 制御点 C2, 終点 P2] を返す (toBezierPolyline の 1 区間分)。
     * point1 (P0) と point4 (P3) は、P1 と P2 での向きを決めるためだけに使う。
     */
    function toBezier(point1, point2, point3, point4) {
        // Catmull-Rom スプラインの接線 (P2 − P0) / 2 と、3 次ベジエの「制御点は端点から接線の 1/3 の位置」を掛けて 1/2 × 1/3 = 1/6
        const a = 1.0 / 6.0;

        return [
            copy(point2),
            // C1 = P1 + (P2 − P0) / 6: P1 での向きを、両隣を結ぶ弦 P0 → P2 と平行にする
            add(point2, multiply(subtract(point3, point1), a)),
            // C2 = P2 − (P3 − P1) / 6: P2 での向きを、両隣を結ぶ弦 P1 → P3 と平行にする
            add(point3, multiply(subtract(point2, point4), a)),
            copy(point3)
        ];
    }

    /**
     * 通過点の列 P0〜P(n−1) を、3 次ベジエ曲線用の点列 [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] に変換する (Catmull-Rom 版)。
     * 前後に点がそろっている内側の n − 3 区間 (P1 → P2 〜 P(n−3) → P(n−2)) だけを変換するので、
     * 結果は P1 から始まり P(n−2) で終わる。最初の区間 (P0 → P1) と最後の区間 (P(n−2) → P(n−1)) は
     * 片側の点が無く向きが決まらないので含まない (描くなら呼び出し側で直線にする)。
     * 描画には 3 次スプライン曲線 (toSplineBezier) を使い、この Catmull-Rom 版は 3 次スプライン曲線と比べるために用意している。
     * @returns 点が 4 個以上なら 1 + 3 × (n − 3) 個の点。4 個未満ならベジエの区間が作れないので、通過点の写しを返す。
     */
    function toBezierPolyline(points) {
        if (points.length < 4)
            return points.map(copy);

        // 1/6 = Catmull-Rom の接線 (1/2) × ベジエの制御点の位置 (1/3)
        const a = 1.0 / 6.0;
        const bezierPoints = [copy(points[1])];
        // 区間 P[i+1] → P[i+2] ごとに、制御点 2 つと終点を加える (始点は 1 つ前の区間の終点と共有する)
        for (let index = 0; index < points.length - 3; index++) {
            // C1 = P[i+1] + (P[i+2] − P[i]) / 6: 始点での向きを、両隣を結ぶ弦 P[i] → P[i+2] と平行にする
            bezierPoints.push(add(points[index + 1], multiply(subtract(points[index + 2], points[index + 0]), a)));
            // C2 = P[i+2] − (P[i+3] − P[i+1]) / 6: 終点での向きを、両隣を結ぶ弦 P[i+1] → P[i+3] と平行にする。
            // 次の区間の C1 も同じ弦と平行になるので、通過点の前後で向きがそろい、つなぎ目が滑らかになる
            bezierPoints.push(add(points[index + 2], multiply(subtract(points[index + 1], points[index + 3]), a)));
            bezierPoints.push(copy(points[index + 2]));
        }
        return bezierPoints;
    }

    /**
     * 通過点の列 P0〜P(n−1) を、最後の点から最初の点へ戻って 1 周する閉じた曲線用の点列
     * [始点, 制御点1, 制御点2, 終点, …] に変換する (Catmull-Rom 版)。結果は P1 から始まり、n 区間を経て P1 に戻る。
     * @returns 点が 3 個以上なら 1 + 3 × n 個の点。3 個未満なら通過点の写しを返す。
     */
    function toBezierPolygon(points) {
        if (points.length < 3)
            return points.map(copy);
        // 末尾に先頭の 3 点 (P0, P1, P2) を付け足して [P0, …, P(n−1), P0, P1, P2] とし、開いた曲線と同じ変換をする。
        // n + 3 個の点から n 区間 (P1 → P2, …, P(n−1) → P0, P0 → P1) ができ、
        // 開いた曲線では直線になる区間にも前後の点がそろうので、すべての区間がベジエ曲線になって 1 周つながる
        return toBezierPolyline(points.concat(points.slice(0, 3)));
    }

    /**
     * すべての通過点を通る自然 3 次スプライン曲線の、各通過点での接線 (パラメーター t についての微分) を返す。
     *
     * 各区間をパラメーター t (0〜1、どの区間も同じ幅) の 3 次式とし、つなぎ目で 1 階微分と 2 階微分が一致するという条件から、
     * 内側の点では m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1)) が成り立つ。
     * 両端には自然スプラインの条件 (端点で 2 階微分が 0) を足す: 2·m0 + m1 = 3·(P1 − P0)、m(n−2) + 2·m(n−1) = 3·(P(n−1) − P(n−2))。
     * 係数行列は三重対角なので、トーマス法で O(n) で解ける。
     * 両隣の接線が自分と同じと仮定すると m(i) = (P(i+1) − P(i−1)) / 2 になり、Catmull-Rom (toBezierPolyline) の接線に一致する。
     * @returns n 個の接線。0 点なら []、1 点なら [{x: 0, y: 0}]。
     */
    function toSplineTangents(points) {
        return solveSplineTangents(points);
    }

    /**
     * 通過点の列 P0〜P(n−1) を、すべての点を通る自然 3 次スプライン曲線用の点列
     * [P0, 制御点1, 制御点2, P1, 制御点1, 制御点2, P2, …, P(n−1)] に変換する。
     * 区間 P(i) → P(i+1) の制御点は、接線 m (toSplineTangents) から 制御点1 = P(i) + m(i) / 3、制御点2 = P(i+1) − m(i+1) / 3 とする。
     * @returns 点が 2 個以上なら 1 + 3 × (n − 1) 個の点。2 個未満なら通過点の写しを返す。
     */
    function toSplineBezier(points) {
        return points.length < 2 ? points.map(copy) : toHermiteBezier(points, toSplineTangents(points), false);
    }

    /**
     * 通過点の列を環状とみなした、閉じた (周期) 3 次スプライン曲線の、各通過点での接線を返す。
     *
     * 最後の点の次を最初の点とみなし、すべての i について m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1)) (添字は n を法とする) を解く。
     * 係数行列は左下と右上の角にも 1 が入る巡回三重対角になるので、Sherman-Morrison の公式で、
     * 角を除いた三重対角の連立方程式 (トーマス法) に分けて解く。点が 3 個未満のときは開いた曲線の接線を返す。
     */
    function toClosedSplineTangents(points) {
        return points.length < 3 ? toSplineTangents(points) : solveClosedSplineTangents(points);
    }

    /**
     * 通過点の列 P0〜P(n−1) を、すべての点を通り最後の点から最初の点へ戻って 1 周する、閉じた 3 次スプライン曲線用の点列
     * [P0, 制御点1, 制御点2, P1, …, P(n−1), 制御点1, 制御点2, P0] に変換する。
     * 制御点の求め方は toSplineBezier と同じで、接線に toClosedSplineTangents を使う。
     * @returns 点が 3 個以上なら 1 + 3 × n 個の点。3 個未満なら通過点の写しを返す。
     */
    function toClosedSplineBezier(points) {
        return points.length < 3 ? points.map(copy) : toHermiteBezier(points, toClosedSplineTangents(points), true);
    }

    // 自然 3 次スプラインの各通過点での接線を、三重対角の連立方程式を解いて求める
    function solveSplineTangents(points) {
        const count    = points.length;
        const lower    = new Array(count);
        const diagonal = new Array(count);
        const upper    = new Array(count);
        const rightX   = new Array(count);
        const rightY   = new Array(count);
        for (let index = 0; index < count; index++) {
            // 内側の点: m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1))。
            // 両端 (自然スプラインの条件): 2·m0 + m1 = 3·(P1 − P0)、m(n−2) + 2·m(n−1) = 3·(P(n−1) − P(n−2))。
            // 端では、無い方の隣の点の代わりに自分自身を使うと、右辺が同じ式で書ける
            const previous  = Math.max(index - 1, 0        );
            const next      = Math.min(index + 1, count - 1);
            lower[index]    = index === 0                          ? 0.0 : 1.0;
            upper[index]    = index === count - 1                  ? 0.0 : 1.0;
            diagonal[index] = index === 0 || index === count - 1 ? 2.0 : 4.0;
            const right     = multiply(subtract(points[next], points[previous]), 3.0);
            rightX[index]   = right.x;
            rightY[index]   = right.y;
        }
        // x 座標と y 座標は同じ係数行列なので、右辺だけを替えて 2 回解く
        return toPoints(solveTridiagonal(lower, diagonal, upper, rightX), solveTridiagonal(lower, diagonal, upper, rightY));
    }

    // 閉じた曲線 (点が 3 個以上) の各通過点での接線を、巡回三重対角の連立方程式 A·m = r として解く。
    // A は対角 4、その左右 1 で、角 A[0, n−1] と A[n−1, 0] も 1 になる。
    // Sherman-Morrison の公式: A = T + u·vᵀ (u = (γ, 0, …, 0, 1)ᵀ、v = (1, 0, …, 0, 1/γ)ᵀ) と分けると、
    // T (角の無い三重対角) について T·w = r と T·z = u を解いて、m = w − (vᵀw / (1 + vᵀz))·z になる
    function solveClosedSplineTangents(points) {
        // γ を対角と同じ大きさ (符号は逆) にすると、T の両端の対角が 4 − γ = 8、4 − 1/γ = 4.25 になり、対角優位が保たれる
        const gamma = -4.0;

        const count    = points.length;
        const lower    = new Array(count);
        const diagonal = new Array(count);
        const upper    = new Array(count);
        const rightX   = new Array(count);
        const rightY   = new Array(count);
        const u        = new Array(count).fill(0.0);
        for (let index = 0; index < count; index++) {
            lower[index]    = index === 0         ? 0.0 : 1.0;
            upper[index]    = index === count - 1 ? 0.0 : 1.0;
            diagonal[index] = 4.0;
            // 右辺 3·(P(i+1) − P(i−1))。添字は環状 (n を法とする) にたどる
            const right     = multiply(subtract(points[(index + 1) % count], points[(index + count - 1) % count]), 3.0);
            rightX[index]   = right.x;
            rightY[index]   = right.y;
        }
        // A から u·vᵀ を引いた残りが T。u·vᵀ は (0, 0) に γ、(n−1, n−1) に 1/γ、二つの角に 1 を持つ
        diagonal[0]         -= gamma;
        diagonal[count - 1] -= 1.0 / gamma;
        u[0]                 = gamma;
        u[count - 1]         = 1.0;

        const x = solveTridiagonal(lower, diagonal, upper, rightX);
        const y = solveTridiagonal(lower, diagonal, upper, rightY);
        const z = solveTridiagonal(lower, diagonal, upper, u);
        // vᵀz と vᵀw (w は x 座標・y 座標それぞれの解) から、z をどれだけ引けばよいかを求める
        const denominator = 1.0 + z[0] + z[count - 1] / gamma;
        const factorX     = (x[0] + x[count - 1] / gamma) / denominator;
        const factorY     = (y[0] + y[count - 1] / gamma) / denominator;
        for (let index = 0; index < count; index++) {
            x[index] -= factorX * z[index];
            y[index] -= factorY * z[index];
        }
        return toPoints(x, y);
    }

    // 三重対角の連立方程式 lower[i]·x(i−1) + diagonal[i]·x(i) + upper[i]·x(i+1) = right[i] を、
    // トーマス法 (前進消去と後退代入) で解く。lower[0] と upper[n−1] は使わない。
    // 対角成分が優位な行列を前提にしているので、ピボット選択はしない
    function solveTridiagonal(lower, diagonal, upper, right) {
        const count = diagonal.length;
        const x     = new Array(count).fill(0.0);
        if (count === 0)
            return x;
        const reducedUpper = new Array(count);  // 前進消去後の upper (対角を 1 に正規化したもの)
        const reducedRight = new Array(count);  // 前進消去後の right
        // 前進消去: 各行から 1 つ上の行の lower 倍を引き、左側の係数を消していく
        for (let index = 0; index < count; index++) {
            const previousUpper = index === 0 ? 0.0 : reducedUpper[index - 1];
            const previousRight = index === 0 ? 0.0 : reducedRight[index - 1];
            const denominator   = diagonal[index] - lower[index] * previousUpper;
            reducedUpper[index] = upper[index] / denominator;
            reducedRight[index] = (right[index] - lower[index] * previousRight) / denominator;
        }
        // 後退代入: 最後の行から順に、1 つ下の行の解を使って求める
        x[count - 1] = reducedRight[count - 1];
        for (let index = count - 2; index >= 0; index--)
            x[index] = reducedRight[index] - reducedUpper[index] * x[index + 1];
        return x;
    }

    // x 座標の列と y 座標の列を、点 (ベクトル) の列にまとめる
    function toPoints(x, y) {
        return x.map((pointX, index) => ({ x: pointX, y: y[index] }));
    }

    // 通過点と各通過点での接線から、ベジエ曲線用の点列 [P0, 制御点1, 制御点2, P1, …] を作る。
    // 区間 P(i) → P(i+1) の制御点は 制御点1 = P(i) + m(i) / 3、制御点2 = P(i+1) − m(i+1) / 3 (3 次エルミート曲線をベジエで表したもの)。
    // isClosed が true なら、最後に P(n−1) → P0 の区間も加えて閉じる
    function toHermiteBezier(points, tangents, isClosed) {
        const a = 1.0 / 3.0;

        const count        = points.length;
        const segmentCount = isClosed ? count : count - 1;
        const bezierPoints = [copy(points[0])];
        for (let index = 0; index < segmentCount; index++) {
            const next = (index + 1) % count;
            bezierPoints.push(add     (points[index], multiply(tangents[index], a)));
            bezierPoints.push(subtract(points[next ], multiply(tangents[next ], a)));
            bezierPoints.push(copy    (points[next ]));
        }
        return bezierPoints;
    }

    return {
        distance,
        toBezier,
        toBezierPolyline,
        toBezierPolygon,
        toSplineTangents,
        toSplineBezier,
        toClosedSplineTangents,
        toClosedSplineBezier
    };
})();

// Node (テスト) から require されたときだけ公開する。ブラウザ (classic script) では module が無いので何もしない
if (typeof module === "object" && module.exports)
    module.exports = { Geometry };
