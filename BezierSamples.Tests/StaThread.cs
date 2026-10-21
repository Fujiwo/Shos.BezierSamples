namespace BezierSamples.Tests;

/// <summary>
/// WPF の UI 要素 (Canvas など) は STA スレッドでしか作れないので、テストの処理を STA スレッドで実行する。
/// </summary>
static class StaThread
{
    /// <summary>
    /// <paramref name="action"/> を STA スレッドで実行し、終わるまで待つ。中で起きた例外 (Assert の失敗を含む) は呼び出し元へ投げ直す。
    /// </summary>
    public static void Run(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() => {
            try {
                action();
            } catch (Exception e) {
                exception = e;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
    }
}
