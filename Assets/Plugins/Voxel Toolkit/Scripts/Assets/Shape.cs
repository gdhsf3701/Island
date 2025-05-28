using System.Collections.Generic;
using UnityEngine;

namespace VoxelToolkit
{
    /// <summary>
    /// Represents the shape object which consists of a certain number of #VoxelToolkit.Model
    /// </summary>
    [System.Serializable]
    public class Shape : HierarchyNode
    {
        [SerializeField] private List<Model> models = new List<Model>();

        /// <summary>
        /// Number of models in the shape
        /// </summary>
        public int ModelsCount => models.Count;

        /// <summary>
        /// Provides access to a model by an index
        /// </summary>
        /// <param name="index">The index of the model to be accessed</param>
        public Model this[int index] => models[index];

        /// <summary>
        /// Returns copy of the models list
        /// </summary>
        public List<Model> Models => new List<Model>(models);

        /// <summary>
        /// Adds the model to the shape
        /// </summary>
        /// <param name="model">The model to be added</param>
        public void AddModel(Model model)
        {
            models.Add(model);
        }

        /// <summary>
        /// Removes a model from the shape
        /// </summary>
        /// <param name="model">Model to be removed</param>
        public void RemoveModel(Model model)
        {
            models.Remove(model);
        }

        /// <summary>
        /// Removes a model by the index
        /// </summary>
        /// <param name="index">The index of the model to be removed</param>
        public void RemoveModelAt(int index)
        {
            models.RemoveAt(index);
        }
    }
}