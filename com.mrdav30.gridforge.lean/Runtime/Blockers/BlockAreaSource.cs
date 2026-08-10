namespace GridForge.Blockers
{
    /// <summary>
    /// Sources that can define the world-space area covered by a bounds blocker.
    /// </summary>
    public enum BlockAreaSource
    {
        /// <summary>Uses explicitly authored fixed-point bounds.</summary>
        Manual = 0,
        /// <summary>Uses the component transform position and scale.</summary>
        Transform = 1,
        /// <summary>Uses enabled collider bounds.</summary>
        Collider = 2,
        /// <summary>Uses enabled renderer bounds.</summary>
        Renderer = 3
    }
}
