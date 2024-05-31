using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ReusableModules.Profiling;

/// <summary>
/// Allows arbitrary regions of code to be profiled for stats like time taken to complete
/// and difference in allocated memory before and after execution.
/// </summary>
public static class ProcessProfiler
{
	/// <summary>
	/// Token that represents the tracking of a process's stats, such as how long it takes the process to complete.
	/// A "process" can be any arbitrary region of code you'd like to profile.
	/// Tracking starts when this object is created.
	/// Call EndTracking() when the process that this token tracks is finished.
	/// </summary>
	public abstract class ProfilingToken
	{
		/// <summary>
		/// Stops tracking process and finalizes profiling stats.
		/// Only call this method in the same thread that the token was created in.
		/// </summary>
		public abstract void EndTracking();
	}

	private sealed class ProfilingStats : ProfilingToken
	{
		private static readonly double TICKS_PER_MS = Stopwatch.Frequency / 1000d;
		private const double BYTES_PER_MB = 1000 * 1000;

		// Right now there's no thread synchronization strategy for accessing this class's fields because reasonably we shouldn't
		// ever need to access the same profiling token across multiple threads.
		// Hypothetically though, if EndTracking() was called on the same object at the same time on multiple threads, that could
		// potentially cause incorrect behavior.
		// This class is not thread-safe for this reason, but as long as you only call EndTracking() on the same thread that you
		// created the token on (which is the expected use case), there should be no problems.
		public readonly long StartTimestamp;
		public long EndTimestamp = -1;

		public readonly long StartMemoryInBytes;
		public long EndMemoryInBytes = -1;

		public readonly string ProcessName;
		public readonly int ThreadId;

		private bool _running = true;

		public ProfilingStats(string processName)
		{
			StartTimestamp = Stopwatch.GetTimestamp();
			StartMemoryInBytes = GC.GetTotalMemory(false);
			ProcessName = processName;
			ThreadId = Environment.CurrentManagedThreadId;
		}

		~ProfilingStats()
		{
			if (!_running) return;

			Console.WriteLine($"Tracking of process [{ProcessName}] was terminated before {nameof(EndTracking)}() was called");
		}

		public override void EndTracking()
		{
			if (!_running) return;

			_running = false;

			EndTimestamp = Stopwatch.GetTimestamp();
			EndMemoryInBytes = GC.GetTotalMemory(false);

#if DEBUG
			Console.WriteLine(this);
#endif

#if PROFILING
			lock (_trackedProcessTimeline)
			{
				_trackedProcessTimeline.Add(this);
			}
#endif
		}

		public override string ToString()
		{
			if (_running) return string.Empty;

			const int MAX_PROC_NAME_LENGTH = 100;
			const int BEFORE_ELLIPSIS = (MAX_PROC_NAME_LENGTH * 3) / 5;
			const int AFTER_ELLIPSIS = (MAX_PROC_NAME_LENGTH - BEFORE_ELLIPSIS) - 3;

			StringBuilder sb = new();

			sb.Append('[');
			if (ProcessName.Length > MAX_PROC_NAME_LENGTH)
			{
				sb.Append(ProcessName, 0, BEFORE_ELLIPSIS);
				sb.Append('.', 3);
				sb.Append(ProcessName, ProcessName.Length - AFTER_ELLIPSIS, AFTER_ELLIPSIS);
				sb.Append(']');
			}
			else
			{
				sb.Append(ProcessName);
				sb.Append(']');
				sb.Append(' ', MAX_PROC_NAME_LENGTH - ProcessName.Length);
			}

			sb.Append(" : Thread ");
			sb.Append(ThreadId.ToString().PadLeft(5));
			sb.Append('\n');

			long elapsed = EndTimestamp - StartTimestamp;
			if (elapsed < 0) elapsed = 0; // This can happen if processes complete too fast on machines with variable-speed CPUs
			double elapsedInMs = elapsed / TICKS_PER_MS;

			long memoryDiffInBytes = EndMemoryInBytes - StartMemoryInBytes;
			double memoryDiffInMB = memoryDiffInBytes / BYTES_PER_MB;
			string memoryDiffString = memoryDiffInMB >= 0 ? '+' + memoryDiffInMB.ToString("F4") : memoryDiffInMB.ToString("F4");

			sb.Append($"\tProcess Took: {elapsedInMs,12:F4} ms {{ Timestamps: {StartTimestamp,16} -> {EndTimestamp,16} }}\n");
			sb.Append($"\tMemory Diff:  {memoryDiffString,12} MB {{ Bytes: {StartMemoryInBytes,16} -> {EndMemoryInBytes,16} }}\n");

			return sb.ToString();
		}
	}

#if PROFILING
	// We can use the data from the tokens to paint a highly detailed profile of the application's performance
	private static readonly List<InternalTrackingToken> _trackedProcessTimeline = new();
#endif

	public static ProfilingToken BeginTracking([CallerMemberName] string processName = "Anonymous Process") => new ProfilingStats(processName);
}
