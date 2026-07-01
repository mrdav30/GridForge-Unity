namespace GridForge.Blockers
{
    /// <summary>
    /// Sources that can define the world-space area covered by a bounds blocker.
    /// </summary>
    public enum BlockAreaSource
    {
        Manual = 0,
        Transform = 1,
        Collider = 2,
        Renderer = 3
    }
}
