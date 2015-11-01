using SharpGL;

namespace ShaderBaker.GlRenderer
{

/// <summary>
/// An interface marking a class as a repository for OpenGL objects.
/// </summary>
/// <remarks>
/// Since OpenGL has some unique requirements for deleting OpenGL objects, it is
/// not correct for classes storing OpenGL objects to provide a finalizer or
/// implement IDisposable. A finalizer may be invoked on any thread, and it is too
/// easy for a Dispose() method to be called on an arbitrary thread as well. Deleting
/// an object on a thread other than the one that created it is an invalid action.
/// This interface marks a class as one that is responsible for managing OpenGL objects.
/// Authors of classes implementing this interface, and users of these classes, beware
/// of these issues, and please take them into consideration.
/// </remarks>
interface IGlCache
{
    /// <summary>
    /// Delete any OpenGL objects in this class, and remove any references to them.
    /// </summary>
    /// <remarks>
    /// Since OpenGL has some unique requirements for deleting OpenGL objects, it is
    /// not correct for OpenGL classes to provide a finalizer or implement IDisposable.
    /// A finalizer may be invoked on any thread, and it is too easy for a Dispose()
    /// method to be called on an arbitrary thread as well. Deleting an object on a
    /// thread other than the one that created it is an invalid action, so 
    /// 
    /// This must be called from the same thread in which OpenGL objects in the
    /// cache are created.
    /// </remarks>
    /// <param name="gl">An OpenGL context used to delete objects</param>
    void ClearCache(OpenGL gl);
}

}
