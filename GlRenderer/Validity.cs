namespace ShaderBaker.GlRenderer
{

 public enum Validity
{
    /// <summary>
    /// The resource has been verified as valid
    /// </summary>
    Valid,

    /// <summary>
    /// The resource has been verified, and the verification returned errors
    /// </summary>
    Invalid,

    /// <summary>
    /// The resource has not yet been verified
    /// </summary>
    Unknown
}

}
