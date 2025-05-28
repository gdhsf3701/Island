using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelToolkit
{
    public struct Quad
    {
        public readonly byte StartX, StartY, StartZ;
        public readonly byte EndX, EndY, EndZ;
        public readonly FaceOrientation Orientation;
        public readonly byte Material;

        public Quad(int3 start, int3 end, FaceOrientation orientation, byte material)
        {
            Orientation = orientation;
            StartX = (byte)start.x;
            StartY = (byte)start.y;
            StartZ = (byte)start.z;
            
            EndX = (byte)end.x;
            EndY = (byte)end.y;
            EndZ = (byte)end.z;
            
            Material = material;
        }
    }
    
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance, DisableSafetyChecks = true)]
    public struct QuadGenerationJob : IJob // TODO: Split to variants for 3 possible options
    {
        public NativeArray<Face> Faces;
        [ReadOnly] public int ChunkSize;
        [ReadOnly] public NativeArray<TransformedMaterial> Palette;

        [WriteOnly] public NativeList<Quad> OpaqueQuads;
        [WriteOnly] public NativeList<Quad> TransparentQuads;
        public bool IgnoreMaterials;
        public bool IgnoreHashes;

        private int GetLineLength(int4 position, int4 direction, int material, FaceOrientation orientation, int4 size, int chunkSizeSquared)
        {
            var endPosition = position;
            var count = 0;
            var multiplier = new int4(ChunkSize, chunkSizeSquared, 1, 0);
            var originalMaterial = Palette[material];
            var originMaterialHash = originalMaterial.Hash;
            var ignoringBothProperties = IgnoreHashes && IgnoreMaterials;
            for (; math.all(endPosition < size); endPosition += direction)
            {
                var currentIndex = math.dot(multiplier, endPosition);
                var currentFace = Faces[currentIndex];
                            
                if ((currentFace.Faces & orientation) == FaceOrientation.None)
                    break;

                if (!IgnoreMaterials && currentFace.Material != material)
                    break;
                
                var materialData = Palette[currentFace.Material];
                if (!IgnoreHashes && originMaterialHash != materialData.Hash)
                    break;
                
                if (ignoringBothProperties && materialData.MaterialType != originalMaterial.MaterialType)
                    break;

                count++;
            }

            return count;
        }
        
        private void ProcessOrientationGreedyOptimized(FaceOrientation expectedOrientation)
        {
            var firstDirection = int4.zero;
            var secondDirection = int4.zero;
            var orientationMask = ~expectedOrientation;

            switch (expectedOrientation)
            {
                case FaceOrientation.Top:
                    firstDirection = new int4(0, 0, 1, 0);
                    secondDirection = new int4(1, 0, 0, 0);
                    break;

                case FaceOrientation.Bottom:
                    firstDirection = new int4(0, 0, 1, 0);
                    secondDirection = new int4(1, 0, 0, 0);
                    break;

                case FaceOrientation.Closer:
                    firstDirection = new int4(1, 0, 0, 0);
                    secondDirection = new int4(0, 1, 0, 0);
                    break;

                case FaceOrientation.Further:
                    firstDirection = new int4(1, 0, 0, 0);
                    secondDirection = new int4(0, 1, 0, 0);
                    break;

                case FaceOrientation.Left:
                    firstDirection = new int4(0, 0, 1, 0);
                    secondDirection = new int4(0, 1, 0, 0);
                    break;

                case FaceOrientation.Right:
                    firstDirection = new int4(0, 0, 1, 0);
                    secondDirection = new int4(0, 1, 0, 0);
                    break;
            }

            var chunkSizeSquared = ChunkSize * ChunkSize;

            var size = new int4(ChunkSize, ChunkSize, ChunkSize, ChunkSize);
            var multiplier = new int4(ChunkSize, chunkSizeSquared, 1, 0);
            
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    for (var z = 0; z < ChunkSize; z++)
                    {
                        var currentPosition = new int4(x, y, z, 0);
                        var center = math.dot(multiplier, currentPosition);
                        
                        var face = Faces[center];
                        if ((face.Faces & expectedOrientation) == FaceOrientation.None)
                            continue;

                        var length = GetLineLength(new int4(x, y, z, 0), 
                            firstDirection, 
                            face.Material, 
                            expectedOrientation, 
                            size, 
                            chunkSizeSquared);

                        for (var index = 0; index < length; index++)
                        {
                            var position = currentPosition + firstDirection * index;
                            var currentIndex = math.dot(multiplier, position);
                            var faceToUpdate = Faces[currentIndex];
                            faceToUpdate.Faces &= orientationMask;
                            Faces[currentIndex] = faceToUpdate;
                        }

                        var finalPosition = currentPosition + secondDirection;
                        var secondaryShift = 1;
                        for (; math.all(finalPosition < size); finalPosition += secondDirection)
                        {
                            var currentLength = GetLineLength(finalPosition, 
                                firstDirection, 
                                face.Material, 
                                expectedOrientation, 
                                size, 
                                chunkSizeSquared);
                            
                            if (currentLength < length)
                                break;
                            
                            for (var index = 0; index < length; index++)
                            {
                                var position = finalPosition + firstDirection * index;
                                var currentIndex = math.dot(multiplier, position);
                                var faceToUpdate = Faces[currentIndex];
                                faceToUpdate.Faces &= orientationMask;
                                Faces[currentIndex] = faceToUpdate;
                            }

                            secondaryShift++;
                        }
                        
                        var endPosition = currentPosition + firstDirection * length + secondDirection * secondaryShift;

                        var isTransparent = Palette[face.Material].MaterialType == MaterialType.Transparent;
                        
                        var target = isTransparent ? TransparentQuads : OpaqueQuads;
                        target.Add(new Quad(currentPosition.xyz, endPosition.xyz, expectedOrientation, face.Material));

                        face.Faces &= orientationMask;
                        Faces[center] = face;
                    }
                }
            }
        }

        public void Execute()
        {
            ProcessOrientationGreedyOptimized(FaceOrientation.Top);
            ProcessOrientationGreedyOptimized(FaceOrientation.Bottom);

            ProcessOrientationGreedyOptimized(FaceOrientation.Closer);
            ProcessOrientationGreedyOptimized(FaceOrientation.Further);

            ProcessOrientationGreedyOptimized(FaceOrientation.Left);
            ProcessOrientationGreedyOptimized(FaceOrientation.Right);
        }
    }
}