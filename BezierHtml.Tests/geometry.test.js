"use strict";

// BezierCanvas / BezierSvg の geometry.js (3 次スプライン曲線と Catmull-Rom 版) のテスト。
// 両版は同じ内容を別々のファイルに持っているので、同じテストを両方に当てる

const { describe, test } = require("node:test");
const assert = require("node:assert/strict");
const BezierAssert = require("./bezier-assert.js");

const versions = ["BezierCanvas"];

function loadGeometry(version) {
    return require(`../${version}/geometry.js`).Geometry;
}

for (const version of versions) {
    describe(`${version}/geometry.js`, () => {
        const Geometry = loadGeometry(version);

        for (const count of [3, 4, 10, 200]) {
            test(`開いた曲線 (${count} 点) はすべての通過点を通り C² 連続`, () => {
                const points = BezierAssert.samplePoints(count);
                BezierAssert.isC2Spline(points, Geometry.toSplineBezier(points), false);
            });
        }

        test("開いた曲線の両端で 2 階微分が 0 (自然境界条件)", () => {
            const points = BezierAssert.samplePoints(10);
            const bezier = Geometry.toSplineBezier(points);
            BezierAssert.equal({ x: 0, y: 0 }, BezierAssert.startSecondDerivative(bezier, 0));
            BezierAssert.equal({ x: 0, y: 0 }, BezierAssert.endSecondDerivative  (bezier, 8));
        });

        test("2 点の開いた曲線は直線", () => {
            const bezier = Geometry.toSplineBezier([{ x: 10, y: 20 }, { x: 70, y: 50 }]);
            assert.equal(bezier.length, 4);
            BezierAssert.equal({ x: 10, y: 20 }, bezier[0]);
            BezierAssert.equal({ x: 30, y: 30 }, bezier[1]);
            BezierAssert.equal({ x: 50, y: 40 }, bezier[2]);
            BezierAssert.equal({ x: 70, y: 50 }, bezier[3]);
        });

        test("0 点・1 点の開いた曲線", () => {
            const point = { x: 3, y: 4 };
            assert.deepEqual(Geometry.toSplineBezier([]), []);
            const bezier = Geometry.toSplineBezier([point]);
            assert.deepEqual(bezier, [{ x: 3, y: 4 }]);
            assert.notEqual(bezier[0], point);
            assert.deepEqual(Geometry.toSplineTangents([]), []);
            assert.deepEqual(Geometry.toSplineTangents([point]), [{ x: 0, y: 0 }]);
        });

        test("変換は入力の配列と点を変えない", () => {
            const points   = BezierAssert.samplePoints(10);
            const snapshot = JSON.parse(JSON.stringify(points));
            const bezierPoints = [
                Geometry.toSplineBezier(points), Geometry.toClosedSplineBezier(points),
                Geometry.toBezierPolyline(points), Geometry.toBezierPolygon(points)
            ];
            assert.deepEqual(points, snapshot);
            // 戻り値の点は入力の点と共有しない
            for (const bezier of bezierPoints)
                assert.ok(bezier.every(point => !points.includes(point)));
        });

        for (const count of [3, 10, 200]) {
            test(`閉じた曲線 (${count} 点) は閉じ目も含めて C² 連続`, () => {
                const points = BezierAssert.samplePoints(count);
                BezierAssert.isC2Spline(points, Geometry.toClosedSplineBezier(points), true);
            });
        }

        test("2 点以下の閉じた曲線", () => {
            const points = [{ x: 10, y: 20 }, { x: 70, y: 50 }];
            assert.deepEqual(Geometry.toClosedSplineBezier([]), []);
            assert.deepEqual(Geometry.toClosedSplineBezier(points.slice(0, 1)), [{ x: 10, y: 20 }]);
            assert.deepEqual(Geometry.toClosedSplineBezier(points), points);
            assert.notEqual(Geometry.toClosedSplineBezier(points)[0], points[0]);
            // 接線は開いた曲線と同じ
            assert.deepEqual(Geometry.toClosedSplineTangents(points), Geometry.toSplineTangents(points));
            assert.deepEqual(Geometry.toClosedSplineTangents([]), []);
            assert.deepEqual(Geometry.toClosedSplineTangents(points.slice(0, 1)), [{ x: 0, y: 0 }]);
        });

        test("Catmull-Rom 版の開いた曲線", () => {
            for (let count = 0; count < 4; count++) {
                const points = BezierAssert.samplePoints(count);
                assert.deepEqual(Geometry.toBezierPolyline(points), points);
            }
            const points = BezierAssert.samplePoints(10);
            const bezier = Geometry.toBezierPolyline(points);
            assert.equal(bezier.length, 1 + 3 * (10 - 3));
            // 区間 P[i+1] → P[i+2] の制御点は P[i+1] + (P[i+2] − P[i]) / 6、P[i+2] − (P[i+3] − P[i+1]) / 6
            for (let index = 0; index < 10 - 3; index++) {
                const [p0, p1, p2, p3] = points.slice(index, index + 4);
                BezierAssert.equal(p1, bezier[3 * index]);
                BezierAssert.equal({ x: p1.x + (p2.x - p0.x) / 6, y: p1.y + (p2.y - p0.y) / 6 }, bezier[3 * index + 1]);
                BezierAssert.equal({ x: p2.x - (p3.x - p1.x) / 6, y: p2.y - (p3.y - p1.y) / 6 }, bezier[3 * index + 2]);
                BezierAssert.equal(p2, bezier[3 * index + 3]);
                // 1 区間分は toBezier と同じ
                const segment = Geometry.toBezier(p0, p1, p2, p3);
                for (let point = 0; point < 4; point++)
                    BezierAssert.equal(segment[point], bezier[3 * index + point]);
            }
        });

        test("Catmull-Rom 版の閉じた曲線", () => {
            for (let count = 0; count < 3; count++) {
                const points = BezierAssert.samplePoints(count);
                assert.deepEqual(Geometry.toBezierPolygon(points), points);
            }
            const points = BezierAssert.samplePoints(5);
            const bezier = Geometry.toBezierPolygon(points);
            assert.equal(bezier.length, 1 + 3 * 5);
            // P1 から始まり、1 周して P1 に戻る
            for (let segment = 0; segment <= 5; segment++)
                BezierAssert.equal(points[(segment + 1) % 5], bezier[3 * segment]);
        });
    });
}
