using SharpGL;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ShaderBaker.GlRenderer.Task
{

//TODO it may not be a bad idea for this class to accept the C# equivalent of a Java Executor as a constructor parameter. This would allow it to manage task execution internally.

/// <summary>
/// An object for storing tasks that need executed on an OpenGL thread.
/// </summary>
public sealed class GlTaskQueue
{
    private readonly ConcurrentQueue<IGlTask> tasks = new ConcurrentQueue<IGlTask>();

    /// <summary>
    /// Submits a task for execution
    /// </summary>
    /// <remarks>
    /// <para>
    /// Submitting a task does not guarantee that it will be executed right away, or
    /// even in the near future - only that it will be executed eventually.
    /// </para>
    /// <para>
    /// Though this class is called a queue, submitted tasks are not guaranteed to
    /// execute in the order that they are submitted. If one task has a dependency
    /// on another, the depending task should not be submitted until the dependent
    /// task has completed. It is safe to submit a task from another task, so this
    /// is a simple option for handling that use-case.
    /// </para>
    /// </remarks>
    /// <param name="task">The task to execute</param>
    public void SubmitTask(IGlTask task)
    {
        Debug.Assert(task != null, "Cannot submit a null task");
        tasks.Enqueue(task);
    }

    /// <summary>
    /// Executes all of the currently enqueued tasks
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read the short description again, carefully. This method executes only
    /// the <i>currently enqueued</i> tasks. If more tasks are enqueued while
    /// this method is running, they will not get executed this time around. This
    /// method is intended to be called repeatedly in the rendering loop.
    /// </para>
    /// <para>
    /// This method should only be called from a thread containing an OpenGL
    /// context on which it is appropriate to execute tasks submitted to this
    /// queue, and it should not be called from multiple threads at the same
    /// time. Managing OpenGL across multiple threads can get hairy, so if this
    /// method is called from different threads, it should be done with caution.
    /// </para>
    /// </remarks>
    /// <param name="gl"></param>
    public void ExecuteTasks(OpenGL gl)
    {
        // Bound the number of tasks to run to the current size of the queue.
        // This prevents this method from running forever in the case that
        // one of the tasks submits more tasks. It also puts an upper bound
        // on the time this method runs, since tasks can be submitted as
        // this method is running.
        int tasksToRun = tasks.Count;
        for (int i = 0; i < tasksToRun; ++i)
        {
            IGlTask task;
            tasks.TryDequeue(out task);
            task.Execute(gl);
        }
    }
}

}
