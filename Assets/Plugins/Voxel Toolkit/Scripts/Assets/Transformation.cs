using System;
using UnityEngine;

namespace VoxelToolkit
{
    /// <summary>
    /// Represents the transformation of the group/shape etc
    /// </summary>
    [System.Serializable]
    public class Transformation : HierarchyNode, INamedObject
    {
        [SerializeField] private HierarchyNode child;
        [SerializeField] private Layer layer;
        [SerializeField] private TransformationFrame[] frames;
        [SerializeField] private string transformationName;

        /// <summary>
        /// Transformation name
        /// </summary>
        public string Name
        {
            get => transformationName;
            set => transformationName = value;
        }
        
        /// <summary>
        /// The related objects of the node
        /// </summary>
        public override ScriptableObject[] RelatedObjects =>
            child == null ? Array.Empty<ScriptableObject>() : new[] { child };

        /// <summary>
        /// The child of the transformation
        /// </summary>
        public HierarchyNode Child
        {
            get => child;
            set => child = value;
        }

        /// <summary>
        /// The layer of the transformation
        /// </summary>
        public Layer Layer
        {
            get => layer;
            set => layer = value;
        }

        /// <summary>
        /// The frames contained in the transformation
        /// </summary>
        public TransformationFrame[] Frames
        {
            get => frames;
            set => frames = value;
        }
    }
}