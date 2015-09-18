using SharpGL;
using System;
using System.Diagnostics;
using System.Threading;

namespace ShaderBaker.GlRenderer.Task
{

/// <summary>
/// A task that can be placed into a <see cref="GlTaskQueue"/> at most once.
/// </summary>
/// <remarks>
/// <para>
/// This class is useful for tasks whose inputs are updated frequently, but
/// only need to be executed once, with the latest set of parameters. For
/// example, the source code for a shader changes frequently if the user is
/// typing it in a GUI, but only the latest code that they typed matters.
/// Changing the source requires a shader to be recompiled, which will also
/// trigger any programs with the shader attached to link again. This class
/// could be used to prevent clients from overloading the queue with copious
/// amounts of compile and link tasks.
/// </para>
/// <para>
/// This kind of task has a little more overhead than a standard task, so
/// it should not be used for operations that will run quickly anyways.
/// However, in the case of expensive operations, it can help improve
/// overall throughput.
/// </para>
/// </remarks>
public sealed class SingletonGlTask
{
    private readonly Impl impl;
    
    /// <summary>
    /// Create a new <c>SingletonGlTask</c> that confines tasks to the
    /// given queue.
    /// </summary>
    /// <param name="taskQueue">The queue to which tasks will be confined</param>
    public SingletonGlTask(GlTaskQueue taskQueue)
    {
        if (taskQueue == null)
        {
            throw new NullReferenceException("taskQueue");
        }

        this.impl = new Impl(taskQueue);
    }
   
    /// <summary>
    /// Submit the given task to the task queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If this task already has a task in the queue, the currently enqueued
    /// task will be replaced with the given one.
    /// </para>
    /// <para>
    /// Since tasks are most likely run from a separate thread than that in
    /// which they are created, it is recommended that the provided task be
    /// immutable, which will guarantee its thread-safety. Using a mutable
    /// task object will most likely be a race condition between the tasks
    /// queue and the task itself.
    /// </para>
    /// </remarks>
    /// <param name="task">The task to submit</param>
    public void Submit(IGlTask task)
    {
        impl.Submit(task);
    }

    // A private class is used to hide the IGlTask interface from outside this
    // class. This ensures that nobody but this class can submit it to the queue.
    private struct Impl : IGlTask
    {
        private readonly GlTaskQueue taskQueue;

        private IGlTask wrappedTask;
            
        public Impl(GlTaskQueue taskQueue)
        {
            this.taskQueue = taskQueue;
            this.wrappedTask = null;
        }

        public void Submit(IGlTask task)
        {
            // Use an atomic operation to ensure that the task is submitted exactly once.
            // Set the task to a value, and if no task was there already, we have the
            // go-ahead to submit the task.
            bool shouldSubmit = Interlocked.Exchange(ref wrappedTask, task) == null;
            if (shouldSubmit)
            {
                taskQueue.SubmitTask(this);
            }
        }

        public void Execute(OpenGL gl)
        {
            IGlTask task = Interlocked.Exchange(ref wrappedTask, null);
            Debug.Assert(
                task != null,
                "Wrapped task should not be null during SingletonTask execution");
            task.Execute(gl);
        }
    }
}

}
