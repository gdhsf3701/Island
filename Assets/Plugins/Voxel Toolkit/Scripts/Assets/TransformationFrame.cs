using Unity.Mathematics;

namespace VoxelToolkit
{
    /// <summary>
    /// Represents a frame of an animation
    /// </summary>
    [System.Serializable]
    public struct TransformationFrame
    {
        /// <summary>
        /// The transformation of the frame
        /// </summary>
        public int4x4 Transformation;
    }
}