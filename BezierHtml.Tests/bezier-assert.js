"use strict";

// ベジエ点列 [始点, C1, C2, 終点, C1, C2, 終点, …] が満たすべき性質を調べるための関数 (BezierSamples.Tests の BezierAssert に対応)。
//
// 3 次ベジエ曲線 B0, B1, B2, B3 の端での微分は、次のとおり。
// 1 階微分: C′(0) = 3·(B1 − B0)、C′(1) = 3·(B3 − B2)。
// 2 階微分: C″(0) = 6·(B0 − 2·B1 + B2)、C″(1) = 6·(B1 − 2·B2 + B3)。

const assert = require("node:assert/strict");

// 座標の比較に使う許容誤差
const tolerance = 1e-9;

function subtract(point1, point2) {
    return { x: point1.x - point2.x, y: point1.y - point2.y };
}

function add(point1, point2) {
    return { x: point1.x + point2.x, y: point1.y + point2.y };
}

function multiply(rate, point) {
    return { x: rate * point.x, y: rate * point.y };
}

// 区間 segment の始点での 1 階微分
function startDerivative(bezier, segment) {
    return multiply(3.0, subtract(bezier[3 * segment + 1], bezier[3 * segment]));
}

// 区間 segment の終点での 1 階微分
function endDerivative(bezier, segment) {
    return multiply(3.0, subtract(bezier[3 * segment + 3], bezier[3 * segment + 2]));
}

// 区間 segment の始点での 2 階微分
function startSecondDerivative(bezier, segment) {
    return multiply(6.0, add(subtract(bezier[3 * segment], bezier[3 * segment + 1]), subtract(bezier[3 * segment + 2], bezier[3 * segment + 1])));
}

// 区間 segment の終点での 2 階微分
function endSecondDerivative(bezier, segment) {
    return multiply(6.0, add(subtract(bezier[3 * segment + 1], bezier[3 * segment + 2]), subtract(bezier[3 * segment + 3], bezier[3 * segment + 2])));
}

// 2 つの点 (ベクトル) が許容誤差の範囲で一致することを確かめる
function equal(expected, actual, allowance = tolerance) {
    const difference = subtract(expected, actual);
    assert.ok(Math.hypot(difference.x, difference.y) <= allowance,
              `expected (${expected.x}, ${expected.y}), actual (${actual.x}, ${actual.y})`);
}

// ベジエ点列が、通過点 points をすべて順に通り、つなぎ目で 1 階微分と 2 階微分が一致する (C² 連続) ことを確かめる
function isC2Spline(points, bezier, isClosed) {
    const segmentCount = isClosed ? points.length : points.length - 1;
    assert.equal(bezier.length, 1 + 3 * segmentCount);
    // 各区間の始点と終点が通過点になっている
    for (let segment = 0; segment <= segmentCount; segment++)
        equal(points[segment % points.length], bezier[3 * segment]);
    // つなぎ目 (区間 segment の終点 = 区間 segment + 1 の始点) で微分がそろう
    for (let segment = 0; segment < segmentCount - 1; segment++) {
        equal(endDerivative      (bezier, segment), startDerivative      (bezier, segment + 1));
        equal(endSecondDerivative(bezier, segment), startSecondDerivative(bezier, segment + 1));
    }
    if (isClosed) {
        // 閉じた曲線では、最後の区間の終点 (P0) と最初の区間の始点もつながる
        equal(endDerivative      (bezier, segmentCount - 1), startDerivative      (bezier, 0));
        equal(endSecondDerivative(bezier, segmentCount - 1), startSecondDerivative(bezier, 0));
    }
}

// テストに使う、間隔と曲がり具合がばらばらな通過点の列 (隣り合う点は 8 より離れている)。BezierAssert.SamplePoints と同じ式
function samplePoints(count) {
    const points = [];
    for (let index = 0; index < count; index++)
        points.push({ x: 30.0 * index + 7.0 * Math.sin(index * 1.3), y: 120.0 + 80.0 * Math.sin(index * 0.7) + 25.0 * Math.cos(index * 2.1) });
    return points;
}

module.exports = {
    tolerance,
    startDerivative,
    endDerivative,
    startSecondDerivative,
    endSecondDerivative,
    equal,
    isC2Spline,
    samplePoints
};
