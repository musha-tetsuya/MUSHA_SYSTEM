#if !UNITY_EDITOR
/// </summary>
/// 【Unity】リリース時にDebug.Logを出力しないようにする
/// https://noracle.jp/unity-no-debug/
/// 【Unity】ログ出力を無効化する時に「logEnabledプロパティ」を使った場合と「Conditional属性」を使った場合の処理時間の検証
/// http://baba-s.hatenablog.com/entry/2017/02/08/100000
/// Conditional 属性は引数の評価がスキップされるため logEnabled プロパティよりも高速
/// リリース版のアプリでログ出力を無効化する場合は Conditional 属性の方が良い
/// </summary>
using System;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// リリース版でデバッグログを出力させないようにするラッパー処理
/// </summary>
public static class Debug
{
	private static void Call(Action action1, Action action2)
	{
		if (UnityEngine.Debug.isDebugBuild)
		{
			action1?.Invoke();
		}
		else
		{
			action2?.Invoke();
		}
	}

	[Conditional("DEBUG")] public static void Assert(bool condition) => Call(() => UnityEngine.Debug.Assert(condition), () => { if (!condition) Console.Error.WriteLine(); });
	[Conditional("DEBUG")] public static void Assert(bool condition, string message) => Call(() => UnityEngine.Debug.Assert(condition, message), () => { if (!condition) Console.Error.WriteLine(message); });
	[Conditional("DEBUG")] public static void Assert(bool condition, object message) => Call(() => UnityEngine.Debug.Assert(condition, message), () => { if (!condition) Console.Error.WriteLine(message); });
	[Conditional("DEBUG")] public static void Assert(bool condition, UnityEngine.Object context) => Call(() => UnityEngine.Debug.Assert(condition, context), () => { if (!condition) Console.Error.WriteLine(context.name); });
	[Conditional("DEBUG")] public static void Assert(bool condition, string message, UnityEngine.Object context) => Call(() => UnityEngine.Debug.Assert(condition, message, context), () => { if (!condition) Console.Error.WriteLine($"{message}, {context.name}"); });
	[Conditional("DEBUG")] public static void Assert(bool condition, object message, UnityEngine.Object context) => Call(() => UnityEngine.Debug.Assert(condition, message, context), () => { if (!condition) Console.Error.WriteLine($"{message}, {context.name}"); });
	[Conditional("DEBUG")] public static void AssertFormat(bool condition, string format, params object[] args) => Call(() => UnityEngine.Debug.AssertFormat(condition, format, args), () => { if (!condition) Console.Error.WriteLine(format, args); });
	[Conditional("DEBUG")] public static void AssertFormat(bool condition, UnityEngine.Object context, string format, params object[] args) => Call(() => UnityEngine.Debug.AssertFormat(condition, context, format, args), () => { if (!condition) Console.Error.WriteLine($"{format}, {context.name}", args); });
	[Conditional("DEBUG")] public static void Break() => Call(() => UnityEngine.Debug.Break(), null);
	[Conditional("DEBUG")] public static void ClearDeveloperConsole() => Call(() => UnityEngine.Debug.ClearDeveloperConsole(), null);
	[Conditional("DEBUG")] public static void DebugBreak() => Call(() => UnityEngine.Debug.DebugBreak(), null);
	[Conditional("DEBUG")] public static void DrawLine(Vector3 start, Vector3 end) => Call(() => UnityEngine.Debug.DrawLine(start, end), null);
	[Conditional("DEBUG")] public static void DrawLine(Vector3 start, Vector3 end, Color color) => Call(() => UnityEngine.Debug.DrawLine(start, end, color), null);
	[Conditional("DEBUG")] public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) => Call(() => UnityEngine.Debug.DrawLine(start, end, color, duration), null);
	[Conditional("DEBUG")] public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest) => Call(() => UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest), null);
	[Conditional("DEBUG")] public static void DrawRay(Vector3 start, Vector3 dir) => Call(() => UnityEngine.Debug.DrawRay(start, dir), null);
	[Conditional("DEBUG")] public static void DrawRay(Vector3 start, Vector3 dir, Color color) => Call(() => UnityEngine.Debug.DrawRay(start, dir, color), null);
	[Conditional("DEBUG")] public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration) => Call(() => UnityEngine.Debug.DrawRay(start, dir, color, duration), null);
	[Conditional("DEBUG")] public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration, bool depthTest) => Call(() => UnityEngine.Debug.DrawRay(start, dir, color, duration, depthTest), null);
	[Conditional("DEBUG")] public static void Log(object message) => Call(() => UnityEngine.Debug.Log(message), () => Console.WriteLine(message));
	[Conditional("DEBUG")] public static void Log(object message, UnityEngine.Object context) => Call(() => UnityEngine.Debug.Log(message, context), () => Console.WriteLine($"{message}, {context.name}"));
	[Conditional("DEBUG")] public static void LogAssertion(object message) => Call(() => UnityEngine.Debug.LogAssertion(message), () => Console.Error.WriteLine(message));
	[Conditional("DEBUG")] public static void LogAssertion(object message, UnityEngine.Object context) => Call(() => UnityEngine.Debug.LogAssertion(message, context), () => Console.Error.WriteLine($"{message}, {context.name}"));
	[Conditional("DEBUG")] public static void LogAssertionFormat(string format, params object[] args) => Call(() => UnityEngine.Debug.LogAssertionFormat(format, args), () => Console.Error.WriteLine(format, args));
	[Conditional("DEBUG")] public static void LogAssertionFormat(UnityEngine.Object context, string format, params object[] args) => Call(() => UnityEngine.Debug.LogAssertionFormat(context, format, args), () => Console.Error.WriteLine($"{format}, {context.name}", args));
	[Conditional("DEBUG")] public static void LogError(object message) => Call(() => UnityEngine.Debug.LogError(message), () => Console.Error.WriteLine(message));
	[Conditional("DEBUG")] public static void LogError(object message, UnityEngine.Object context) => Call(() => UnityEngine.Debug.LogError(message, context), () => Console.Error.WriteLine($"{message}, {context.name}"));
	[Conditional("DEBUG")] public static void LogErrorFormat(string format, params object[] args) => Call(() => UnityEngine.Debug.LogErrorFormat(format, args), () => Console.Error.WriteLine(format, args));
	[Conditional("DEBUG")] public static void LogErrorFormat(UnityEngine.Object context, string format, params object[] args) => Call(() => UnityEngine.Debug.LogErrorFormat(context, format, args), () => Console.Error.WriteLine($"{format}, {context.name}", args));
	[Conditional("DEBUG")] public static void LogException(Exception exception) => Call(() => UnityEngine.Debug.LogException(exception), () => Console.Error.WriteLine(exception.Message));
	[Conditional("DEBUG")] public static void LogException(Exception exception, UnityEngine.Object context) => Call(() => UnityEngine.Debug.LogException(exception, context), () => Console.Error.WriteLine($"{exception.Message}, {context.name}"));
	[Conditional("DEBUG")] public static void LogFormat(string format, params object[] args) => Call(() => UnityEngine.Debug.LogFormat(format, args), () => Console.WriteLine(format, args));
	[Conditional("DEBUG")] public static void LogFormat(UnityEngine.Object context, string format, params object[] args) => Call(() => UnityEngine.Debug.LogFormat(context, format, args), () => Console.WriteLine($"{format}, {context.name}", args));
	[Conditional("DEBUG")] public static void LogWarning(object message) => Call(() => UnityEngine.Debug.LogWarning(message), () => Console.WriteLine(message));
	[Conditional("DEBUG")] public static void LogWarning(object message, UnityEngine.Object context) => Call(() => UnityEngine.Debug.LogWarning(message, context), () => Console.WriteLine($"{message}, {context.name}"));
	[Conditional("DEBUG")] public static void LogWarningFormat(string format, params object[] args) => Call(() => UnityEngine.Debug.LogWarningFormat(format, args), () => Console.WriteLine(format, args));
	[Conditional("DEBUG")] public static void LogWarningFormat(UnityEngine.Object context, string format, params object[] args) => Call(() => UnityEngine.Debug.LogWarningFormat(context, format, args), () => Console.WriteLine($"{format}, {context.name}", args));
}

#endif