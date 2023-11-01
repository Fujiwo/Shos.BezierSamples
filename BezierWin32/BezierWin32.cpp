#include <windows.h>
#include <windowsx.h>
#include <tchar.h>
#include <cassert>
#include <vector>
using namespace std;

namespace BezeirWin32 {
    class Win32Geometry
    {
    public:
        static POINT Add(const POINT& point, const SIZE& size)
        {
            return POINT{ point.x + size.cx, point.y + size.cy };
        }

        static SIZE Subtract(const POINT& point1, const POINT& point2)
        {
            return SIZE{ point1.x - point2.x, point1.y - point2.y };
        }

        static SIZE Multiply(const SIZE& size, double multiplier)
        {
            return SIZE{ Round(size.cx * multiplier), Round(size.cy * multiplier) };
        }

        static long Round(double value)
        {
            return long(::floor(value + 0.5));
        }
    };

    class Win32Graphics
    {
    public:
        static void DrawBezierLine(HDC hdc, vector<POINT>& points)
        {
            if (points.size() < 2U)
                return;
            ::Polyline(hdc, points.data(), 2);
            auto bezierPoints = ToBezierPolyline(points);
            if (bezierPoints.size() >= 4)
                DrawBeziers(hdc, bezierPoints);
            if (points.size() > 2)
                ::Polyline(hdc, points.data() + points.size() - 2, 2);
        }

    private:
        static void DrawBeziers(HDC hdc, const vector<POINT>& points)
        {
            assert(points.size() >= 4);
            assert((points.size() - 4) % 3 == 0);
            ::MoveToEx(hdc, points[0].x, points[0].y, nullptr);
            if (points.size() == 2) {
                ::LineTo(hdc, points[1].x, points[1].y);
                return;
            }
            for (auto index = 1U; index + 2 < points.size(); index += 3)
                ::PolyBezierTo(hdc, points.data() + index, 3);
        }

        static vector<POINT> ToBezierPolyline(const vector<POINT>& points)
        {
            vector<POINT> bezierPoints;
            if (points.size() < 4) {
                for (auto point : points)
                    bezierPoints.push_back(point);
            } else {
                const double a = 1.0 / 3.0 / 2.0;
                bezierPoints.push_back(points[1]);
                for (auto index = 0U; index < points.size() - 3U; index++) {
                    bezierPoints.push_back(Win32Geometry::Add(points[index + 1], Win32Geometry::Multiply(Win32Geometry::Subtract(points[index + 2], points[index + 0]), a)));
                    bezierPoints.push_back(Win32Geometry::Add(points[index + 2], Win32Geometry::Multiply(Win32Geometry::Subtract(points[index + 1], points[index + 3]), a)));
                    bezierPoints.push_back(points[index + 2]);
                }
            }
            return bezierPoints;
        }
    };

    namespace Sample {
        class MainWindow
        {
            static const TCHAR windowClassName[];
            static const TCHAR title[];
            static HINSTANCE   instanceHandle;
            HWND               handle;

        public:
            MainWindow() : handle(nullptr)
            {}

            bool Create(HINSTANCE instanceHandle, int showCommand)
            {
                MainWindow::instanceHandle = instanceHandle;
                RegisterClass(instanceHandle);
                handle = ::CreateWindow(windowClassName, title, WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, nullptr, nullptr, instanceHandle, nullptr);
                if (handle == nullptr)
                    return false;
                ::ShowWindow(handle, showCommand);
                ::UpdateWindow(handle);
                return true;
            }

        private:
            static bool IsClassExisting(HINSTANCE instanceHandle)
            {
                WNDCLASSEX wcex;
                wcex.cbSize = sizeof(WNDCLASSEX);
                return ::GetClassInfoEx(instanceHandle, windowClassName, &wcex);
            }

            static bool RegisterClass(HINSTANCE instanceHandle)
            {
                if (IsClassExisting(instanceHandle))
                    return false;
                WNDCLASSEX wcex;
                wcex.cbSize = sizeof(WNDCLASSEX);
                wcex.style = CS_HREDRAW | CS_VREDRAW;
                wcex.lpfnWndProc = WindowProcedure;
                wcex.cbClsExtra = 0;
                wcex.cbWndExtra = 0;
                wcex.hInstance = instanceHandle;
                wcex.hIcon = nullptr;
                wcex.hCursor = ::LoadCursor(nullptr, IDC_ARROW);
                wcex.hbrBackground = HBRUSH(COLOR_WINDOW + 1);
                wcex.lpszMenuName = nullptr;
                wcex.lpszClassName = windowClassName;
                wcex.hIconSm = nullptr;
                return ::RegisterClassEx(&wcex) != 0;
            }

            static vector<POINT> points;

            static LRESULT CALLBACK WindowProcedure(HWND windowHandle, UINT message, WPARAM wParam, LPARAM lParam)
            {
                switch (message) {
                case WM_RBUTTONUP:
                    points.clear();
                    ::InvalidateRect(windowHandle, nullptr, true);
                    break;

                case WM_LBUTTONUP:
                {
                    auto point = POINT{ GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
                    points.push_back(point);
                    ::InvalidateRect(windowHandle, nullptr, true);
                }
                break;

                case WM_PAINT:
                {
                    PAINTSTRUCT ps;
                    auto hdc = ::BeginPaint(windowHandle, &ps);
                    Win32Graphics::DrawBezierLine(hdc, points);
                    DrawPointMarks(hdc, points);
                    ::EndPaint(windowHandle, &ps);
                }
                break;

                case WM_DESTROY:
                    ::PostQuitMessage(0);
                    break;

                default:
                    return ::DefWindowProc(windowHandle, message, wParam, lParam);
                }
                return 0;
            }

            static void DrawPointMarks(HDC hdc, const vector<POINT>& points)
            {
                for (auto point : points)
                    DrawPointMark(hdc, point);
            }

            static void DrawPointMark(HDC hdc, const POINT& point)
            {
                const long radius = 5l;
                ::Ellipse(hdc, point.x - radius, point.y - radius, point.x + radius, point.y + radius);
            }
        };

        HINSTANCE     MainWindow::instanceHandle = nullptr;
        const TCHAR   MainWindow::windowClassName[] = _T("BezierWin32MainWindow");
        const TCHAR   MainWindow::title[] = _T("BezierWin32");
        vector<POINT> MainWindow::points;

        class Application
        {
            MainWindow mainWindow;

        public:
            int Run(HINSTANCE instanceHandle, int showCommand)
            {
                return mainWindow.Create(instanceHandle, showCommand) ? MessageLoop() : 0;
            }

        private:
            int MessageLoop()
            {
                MSG msg;
                while (::GetMessage(&msg, nullptr, 0, 0)) {
                    ::TranslateMessage(&msg);
                    ::DispatchMessage(&msg);
                }
                return int(msg.wParam);
            }
        };
    } // namespace Sample
} // namespace BezeirWin32

int APIENTRY wWinMain(_In_     HINSTANCE instanceHandle,
    _In_opt_ HINSTANCE previousInstanceHandle,
    _In_     LPTSTR    commandLine,
    _In_     int       showCommand)
{
    UNREFERENCED_PARAMETER(previousInstanceHandle);
    UNREFERENCED_PARAMETER(commandLine);
    return BezeirWin32::Sample::Application().Run(instanceHandle, showCommand);
}
