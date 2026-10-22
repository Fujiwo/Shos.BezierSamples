#include <windows.h>
#include <windowsx.h>
#include <cassert>
#include <cmath>
#include <cstddef>
#include <span>
#include <vector>

namespace BezierWin32 {
    using std::span;
    using std::vector;

    // POINT (位置) と SIZE (2 点間の差 = ベクトル) の演算をまとめたもの。
    // Win32 の POINT / SIZE は整数座標なので、実数倍した結果は四捨五入して整数に戻す。
    class Win32Geometry
    {
    public:
        // 位置 point をベクトル size だけずらした位置を返す。
        [[nodiscard]] static constexpr POINT Add(const POINT& point, const SIZE& size) noexcept
        {
            return POINT{ .x = point.x + size.cx, .y = point.y + size.cy };
        }

        // point2 から point1 へ向かうベクトル (point1 − point2) を返す。
        [[nodiscard]] static constexpr SIZE Subtract(const POINT& point1, const POINT& point2) noexcept
        {
            return SIZE{ .cx = point1.x - point2.x, .cy = point1.y - point2.y };
        }

        // ベクトルを multiplier 倍し、各成分を整数に丸めて返す。
        [[nodiscard]] static SIZE Multiply(const SIZE& size, double multiplier) noexcept
        {
            return SIZE{ .cx = Round(size.cx * multiplier), .cy = Round(size.cy * multiplier) };
        }

        // 最も近い整数に丸める (0.5 を足して切り捨てるので、ちょうど .5 のときは正負とも大きい側へ丸める)。
        [[nodiscard]] static long Round(double value) noexcept
        {
            return static_cast<long>(std::floor(value + 0.5));
        }
    };

    // 実数の 2 次元ベクトル。3 次スプライン曲線の接線のように、整数の POINT / SIZE では精度が足りない計算に使う。
    struct Vector2
    {
        double x = 0.0;
        double y = 0.0;
    };

    // 通過点の列を 3 次スプライン曲線 (3 次ベジエ曲線の列) に変換して GDI で描く処理をまとめたもの。
    // 点列は std::span で受け取るので、vector でも配列でもコピーせずに渡せる。
    class Win32Graphics
    {
    public:
        // 通過点 points をすべて通る、滑らかな 3 次スプライン曲線を描く。
        // 最初の区間 (P0 → P1) と最後の区間 (P(n−2) → P(n−1)) も含め、n − 1 個の区間すべてをベジエ曲線で描く。
        // 両端の向きは、自然スプラインの条件 (端点で曲がり具合 = 2 階微分が 0) から決まる。
        static void DrawBezierLine(HDC hdc, span<const POINT> points)
        {
            if (points.size() < 2U)
                return;
            // 2 点だけなら直線になる (ベジエ曲線にしても直線になるので、そのまま線を引く)。
            if (points.size() == 2U) {
                ::Polyline(hdc, points.data(), 2);
                return;
            }
            DrawBeziers(hdc, ToSplineBezier(points));
        }

    private:
        // [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] の点列を、
        // つながった 3 次ベジエ曲線として描く。点の数は 1 + 3k (k ≧ 1) でなければならない。
        // 始点に MoveToEx でペンを移し、以降は 3 点 (制御点1, 制御点2, 終点) ずつ PolyBezierTo に渡す。
        // PolyBezierTo は現在のペン位置を始点とし、描き終えた終点が次の区間の始点になる。
        static void DrawBeziers(HDC hdc, span<const POINT> points)
        {
            assert(points.size() >= 4);
            assert((points.size() - 4) % 3 == 0);
            ::MoveToEx(hdc, points[0].x, points[0].y, nullptr);
            for (std::size_t index = 1; index + 2 < points.size(); index += 3)
                ::PolyBezierTo(hdc, points.subspan(index, 3).data(), 3);
        }

        // 通過点の列 P0〜P(n−1) を、すべての点を通る自然 3 次スプライン曲線のベジエ描画用の点列
        // [P0, 制御点1, 制御点2, P1, 制御点1, 制御点2, P2, …, P(n−1)] (1 + 3(n − 1) 点) に変換する。
        // 区間 P(i) → P(i+1) の制御点は、各通過点での接線 m (ToSplineTangents) から
        // 制御点1 = P(i) + m(i) / 3、制御点2 = P(i+1) − m(i+1) / 3 とする (3 次エルミート曲線をベジエで表したもの)。
        // 接線は実数で求め、制御点を整数座標に丸めるのは最後だけにする。通過点が 2 個未満のときは通過点をそのまま返す。
        [[nodiscard]] static vector<POINT> ToSplineBezier(span<const POINT> points)
        {
            if (points.size() < 2)
                return vector<POINT>(points.begin(), points.end());

            const auto tangents = ToSplineTangents(points);
            // 位置 point を、ベクトル tangent の rate 倍だけずらして整数座標に丸める。
            const auto offset = [](const POINT& point, const Vector2& tangent, double rate) {
                return POINT{ .x = Win32Geometry::Round(point.x + tangent.x * rate), .y = Win32Geometry::Round(point.y + tangent.y * rate) };
            };

            vector<POINT> bezierPoints;
            bezierPoints.reserve(1 + 3 * (points.size() - 1));
            bezierPoints.push_back(points[0]);
            for (std::size_t index = 0; index + 1 < points.size(); index++) {
                bezierPoints.push_back(offset(points[index    ], tangents[index    ],  1.0 / 3.0));
                bezierPoints.push_back(offset(points[index + 1], tangents[index + 1], -1.0 / 3.0));
                bezierPoints.push_back(points[index + 1]);
            }
            return bezierPoints;
        }

        // すべての通過点を通る自然 3 次スプライン曲線の、各通過点での接線 (パラメーター t についての微分) を返す。
        // 各区間をパラメーター t (0〜1、どの区間も同じ幅) の 3 次式とし、つなぎ目で 1 階微分と 2 階微分が一致するという条件から、
        // 内側の点では m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1)) が成り立つ。
        // 両端には自然スプラインの条件 (端点で 2 階微分が 0) を足す: 2·m0 + m1 = 3·(P1 − P0)、m(n−2) + 2·m(n−1) = 3·(P(n−1) − P(n−2))。
        // 両隣の接線が自分と同じと仮定すると m(i) = (P(i+1) − P(i−1)) / 2 になり、Catmull-Rom (ToBezierPolyline) の接線に一致する。
        [[nodiscard]] static vector<Vector2> ToSplineTangents(span<const POINT> points)
        {
            const auto count = points.size();
            vector<double> lower(count), diagonal(count), upper(count), rightX(count), rightY(count);
            for (std::size_t index = 0; index < count; index++) {
                // 端では、無い方の隣の点の代わりに自分自身を使うと、右辺が内側の点と同じ式で書ける
                const auto previous = index == 0         ? index : index - 1;
                const auto next     = index == count - 1 ? index : index + 1;
                lower   [index] = index == 0                         ? 0.0 : 1.0;
                upper   [index] = index == count - 1                 ? 0.0 : 1.0;
                diagonal[index] = index == 0 || index == count - 1 ? 2.0 : 4.0;
                rightX  [index] = 3.0 * (static_cast<double>(points[next].x) - points[previous].x);
                rightY  [index] = 3.0 * (static_cast<double>(points[next].y) - points[previous].y);
            }
            // x 座標と y 座標は同じ係数行列なので、右辺だけを替えて 2 回解く
            const auto x = SolveTridiagonal(lower, diagonal, upper, rightX);
            const auto y = SolveTridiagonal(lower, diagonal, upper, rightY);
            vector<Vector2> tangents(count);
            for (std::size_t index = 0; index < count; index++)
                tangents[index] = Vector2{ .x = x[index], .y = y[index] };
            return tangents;
        }

        // 三重対角の連立方程式 lower[i]·x(i−1) + diagonal[i]·x(i) + upper[i]·x(i+1) = right[i] を、
        // トーマス法 (前進消去と後退代入) で O(n) で解く。lower[0] と upper[n−1] は使わない。
        // 対角成分が優位な行列 (|4| > |1| + |1|、|2| > |1|) を前提にしているので、ピボット選択はしない。
        [[nodiscard]] static vector<double> SolveTridiagonal(span<const double> lower, span<const double> diagonal, span<const double> upper, span<const double> right)
        {
            const auto count = diagonal.size();
            vector<double> x(count);
            if (count == 0)
                return x;
            vector<double> reducedUpper(count);   // 前進消去後の upper (対角を 1 に正規化したもの)
            vector<double> reducedRight(count);   // 前進消去後の right
            // 前進消去: 各行から 1 つ上の行の lower 倍を引き、左側の係数を消していく
            for (std::size_t index = 0; index < count; index++) {
                const auto previousUpper = index == 0 ? 0.0 : reducedUpper[index - 1];
                const auto previousRight = index == 0 ? 0.0 : reducedRight[index - 1];
                const auto denominator   = diagonal[index] - lower[index] * previousUpper;
                reducedUpper[index] = upper[index] / denominator;
                reducedRight[index] = (right[index] - lower[index] * previousRight) / denominator;
            }
            // 後退代入: 最後の行から順に、1 つ下の行の解を使って求める
            x[count - 1] = reducedRight[count - 1];
            for (auto index = count - 1; index-- > 0; )
                x[index] = reducedRight[index] - reducedUpper[index] * x[index + 1];
            return x;
        }

        // 通過点の列を、Catmull-Rom スプラインのベジエ描画用の点列 [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] に変換する。
        // 前後に隣の点がそろっている内側の区間 P1 → P2, P2 → P3, …, P(n−3) → P(n−2) (n − 3 区間) だけを変換する。
        // 通過点が 4 個未満のときはベジエにできる区間が無いので、通過点をそのまま返す。
        // 描画には 3 次スプライン曲線 (ToSplineBezier) を使い、この関数は 3 次スプライン曲線と比べるために用意している。
        [[nodiscard]] static vector<POINT> ToBezierPolyline(span<const POINT> points)
        {
            if (points.size() < 4)
                return vector<POINT>(points.begin(), points.end());

            // 制御点を通過点から離す割合 1/6。
            // Catmull-Rom スプラインの接線 (P[i+2] − P[i]) / 2 に、
            // 3 次ベジエで端点から接線の 1/3 の位置に制御点を置く規則を掛けて 1/2 × 1/3 = 1/6 になる。
            constexpr double a = 1.0 / 3.0 / 2.0;

            vector<POINT> bezierPoints;
            // 始点 1 個 + 区間ごとに 3 個 (制御点1, 制御点2, 終点)。
            bezierPoints.reserve(1 + 3 * (points.size() - 3));
            // 最初のベジエ区間の始点は P1 (P0 → P1 は P0 の手前の点が無く、向きが決まらないので含めない)。
            bezierPoints.push_back(points[1]);
            // 区間 P[i+1] → P[i+2] ごとに、制御点 2 個と終点を追加する。
            for (std::size_t index = 0; index < points.size() - 3; index++) {
                // 制御点1 = P[i+1] + (P[i+2] − P[i]) / 6
                // 始点 P[i+1] での向きを、両隣を結ぶ弦 P[i] → P[i+2] と平行にする。
                bezierPoints.push_back(Win32Geometry::Add(points[index + 1], Win32Geometry::Multiply(Win32Geometry::Subtract(points[index + 2], points[index + 0]), a)));
                // 制御点2 = P[i+2] − (P[i+3] − P[i+1]) / 6 (= P[i+2] + (P[i+1] − P[i+3]) / 6)
                // 終点 P[i+2] での向きを、両隣を結ぶ弦 P[i+1] → P[i+3] と平行にする。
                // 隣の区間とは通過点での向きがそろうので、つなぎ目が滑らかになる。
                bezierPoints.push_back(Win32Geometry::Add(points[index + 2], Win32Geometry::Multiply(Win32Geometry::Subtract(points[index + 1], points[index + 3]), a)));
                // 終点 P[i+2]。次の区間の始点も兼ねる。
                bezierPoints.push_back(points[index + 2]);
            }
            return bezierPoints;
        }
    };

    namespace Sample {
        // メイン ウィンドウ。左クリックした位置を通過点として追加し、右クリックで全消去する。
        // 通過点を受け取るたびに再描画を要求し、WM_PAINT で通過点を通る曲線と点の印を描き直す。
        class MainWindow
        {
            // プロジェクトは Unicode 版 (wWinMain) なので、文字列は wchar_t で持つ。
            static constexpr wchar_t windowClassName[] = L"BezierWin32MainWindow";
            static constexpr wchar_t title[]           = L"BezierWin32";
            HWND                     handle            = nullptr;

        public:
            // ウィンドウ クラスを登録してウィンドウを作成し、表示する。失敗したら false を返す。
            [[nodiscard]] bool Create(HINSTANCE instanceHandle, int showCommand)
            {
                if (!RegisterWindowClass(instanceHandle))
                    return false;
                handle = ::CreateWindow(windowClassName, title, WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, nullptr, nullptr, instanceHandle, nullptr);
                if (handle == nullptr)
                    return false;
                ::ShowWindow(handle, showCommand);
                ::UpdateWindow(handle);
                return true;
            }

        private:
            // 同じ名前のウィンドウ クラスが既に登録済みかどうかを返す (二重登録を避けるため)。
            [[nodiscard]] static bool IsClassExisting(HINSTANCE instanceHandle)
            {
                WNDCLASSEX wcex{ .cbSize = sizeof(WNDCLASSEX) };
                return ::GetClassInfoEx(instanceHandle, windowClassName, &wcex) != FALSE;
            }

            // RegisterClass は Win32 のマクロ (RegisterClassW) と衝突するため別名にする
            [[nodiscard]] static bool RegisterWindowClass(HINSTANCE instanceHandle)
            {
                if (IsClassExisting(instanceHandle))
                    return true;
                // 指示付き初期化子で、設定する項目を名前で書く (書かなかった項目は 0 / nullptr になる)。
                const WNDCLASSEX wcex{
                    .cbSize        = sizeof(WNDCLASSEX),
                    // ウィンドウの大きさが変わったら全体を再描画する。
                    .style         = CS_HREDRAW | CS_VREDRAW,
                    .lpfnWndProc   = WindowProcedure,
                    .cbClsExtra    = 0,
                    .cbWndExtra    = 0,
                    .hInstance     = instanceHandle,
                    .hIcon         = nullptr,
                    .hCursor       = ::LoadCursor(nullptr, IDC_ARROW),
                    // システムカラーの番号 + 1 をブラシの代わりに渡す、Win32 の決まった書き方。
                    .hbrBackground = reinterpret_cast<HBRUSH>(static_cast<INT_PTR>(COLOR_WINDOW + 1)),
                    .lpszMenuName  = nullptr,
                    .lpszClassName = windowClassName,
                    .hIconSm       = nullptr
                };
                return ::RegisterClassEx(&wcex) != 0;
            }

            // クリックで追加された通過点 (クライアント座標)。
            // ウィンドウ プロシージャが static 関数なので、データも static にして共有する。
            static inline vector<POINT> points;

            // ウィンドウに届くメッセージを処理する。
            // マウス操作では通過点の列を変えて InvalidateRect で再描画を要求するだけにし、
            // 実際の描画はすべて WM_PAINT でまとめて行う。
            static LRESULT CALLBACK WindowProcedure(HWND windowHandle, UINT message, WPARAM wParam, LPARAM lParam)
            {
                switch (message) {
                // 右クリック: 通過点をすべて消す。
                case WM_RBUTTONUP:
                    points.clear();
                    ::InvalidateRect(windowHandle, nullptr, TRUE);
                    break;

                // 左クリック: 離した位置を通過点として末尾に追加する。
                // lParam の下位/上位 16 ビットがクライアント座標の x / y。
                // 負の座標も正しく取り出せるよう GET_X_LPARAM / GET_Y_LPARAM を使う。
                case WM_LBUTTONUP:
                    points.push_back(POINT{ .x = GET_X_LPARAM(lParam), .y = GET_Y_LPARAM(lParam) });
                    ::InvalidateRect(windowHandle, nullptr, TRUE);
                    break;

                // 再描画: 通過点を通る曲線を描き、その上に各通過点の印を重ねる。
                case WM_PAINT:
                {
                    PAINTSTRUCT ps;
                    const auto hdc = ::BeginPaint(windowHandle, &ps);
                    Win32Graphics::DrawBezierLine(hdc, points);
                    DrawPointMarks(hdc, points);
                    ::EndPaint(windowHandle, &ps);
                }
                break;

                // ウィンドウが閉じられたら WM_QUIT を送り、メッセージ ループを終わらせる。
                case WM_DESTROY:
                    ::PostQuitMessage(0);
                    break;

                default:
                    return ::DefWindowProc(windowHandle, message, wParam, lParam);
                }
                return 0;
            }

            // すべての通過点に印 (小さな円) を描く。
            static void DrawPointMarks(HDC hdc, span<const POINT> passingPoints)
            {
                for (const auto& point : passingPoints)
                    DrawPointMark(hdc, point);
            }

            // 通過点 point を中心とする半径 5 の円を描く。
            static void DrawPointMark(HDC hdc, const POINT& point)
            {
                constexpr long radius = 5;
                ::Ellipse(hdc, point.x - radius, point.y - radius, point.x + radius, point.y + radius);
            }
        };

        // アプリケーション全体。メイン ウィンドウを作成してメッセージ ループを回す。
        class Application
        {
            MainWindow mainWindow;

        public:
            // メイン ウィンドウを作成し、終了するまでメッセージを処理する。終了コードを返す。
            [[nodiscard]] int Run(HINSTANCE instanceHandle, int showCommand)
            {
                return mainWindow.Create(instanceHandle, showCommand) ? MessageLoop() : 0;
            }

        private:
            // WM_QUIT を受け取る (GetMessage が 0 を返す) までメッセージを取り出してウィンドウ プロシージャへ送る。
            // WM_QUIT の wParam には PostQuitMessage に渡した終了コードが入っている。
            [[nodiscard]] static int MessageLoop()
            {
                MSG msg{};
                while (::GetMessage(&msg, nullptr, 0, 0)) {
                    ::TranslateMessage(&msg);
                    ::DispatchMessage(&msg);
                }
                return static_cast<int>(msg.wParam);
            }
        };
    } // namespace Sample
} // namespace BezierWin32

// Win32 アプリケーション (Unicode 版) のエントリ ポイント。
int APIENTRY wWinMain(_In_     HINSTANCE instanceHandle,
                      _In_opt_ HINSTANCE /* previousInstanceHandle */,
                      _In_     LPWSTR    /* commandLine */,
                      _In_     int       showCommand)
{
    return BezierWin32::Sample::Application().Run(instanceHandle, showCommand);
}
