using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelToolkit
{
    [Flags]
    public enum FaceOrientation : byte
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8,
        Closer = 16,
        Further = 32,
    }
    
    public struct Face
    {
        public FaceOrientation Faces;
        public readonly byte Material;

        public Face(FaceOrientation faces, byte material)
        {
            Faces = faces;
            Material = material;
        }
    }
    
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance, DisableSafetyChecks = true)]
    public struct FacesGenerationJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<Voxel> Voxels;
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<Voxel> UpperChunk;
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<Voxel> LowerChunk;
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<Voxel> LeftChunk;
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<Voxel> RightChunk;
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<Voxel> CloserChunk;
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<Voxel> FurtherChunk;
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<TransformedMaterial> Palette;

        [ReadOnly] public int ChunkSize;
        [ReadOnly] public int LastChunkIndex;
        [ReadOnly] public int ChunkSizeSquared;

        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<Face> Faces;

        public void Execute(int index)
        {
            var multiplier = new int4(ChunkSize, ChunkSizeSquared, 1, 0);

            var y = index / ChunkSizeSquared;
            var leftover = index - (y * ChunkSizeSquared);
            var x = leftover / ChunkSize;
            var z = leftover - (x * ChunkSize);
            
            var center = math.dot(multiplier, new int4(x, y, z, 0));
            var centerVoxel = Voxels[center];
            if (centerVoxel.Material == 0)
            {
                Faces[center] = new Face();
                return;
            }

            var shouldSwapLarger = new int3(x, y, z) == LastChunkIndex;
            var shouldSwapSmaller = new int3(x, y, z) == 0; 
            
            var higherChunk = shouldSwapLarger.y ? UpperChunk : Voxels;
            var lowerChunk = shouldSwapSmaller.y ? LowerChunk : Voxels;
            var closerChunk = shouldSwapSmaller.z ? CloserChunk : Voxels;
            var furtherChunk = shouldSwapLarger.z ? FurtherChunk : Voxels;
            var leftChunk = shouldSwapSmaller.x ? LeftChunk : Voxels;
            var rightChunk = shouldSwapLarger.x ? RightChunk : Voxels;

            var higher =
                math.dot(multiplier, shouldSwapLarger.y ? new int4(x, 0, z, 0) : new int4(x, y + 1, z, 0));

            var lower =
                math.dot(multiplier, shouldSwapSmaller.y ? new int4(x, LastChunkIndex, z, 0) : new int4(x, y - 1, z, 0));

            var closer =
                math.dot(multiplier, shouldSwapSmaller.z ? new int4(x, y, LastChunkIndex, 0) : new int4(x, y, z - 1, 0));

            var further =
                math.dot(multiplier, shouldSwapLarger.z ? new int4(x, y, 0, 0) : new int4(x, y, z + 1, 0));

            var left =
                math.dot(multiplier, shouldSwapSmaller.x ? new int4(LastChunkIndex, y, z, 0) : new int4(x - 1, y, z, 0));

            var right =
                math.dot(multiplier, shouldSwapLarger.x ? new int4(0, y, z, 0) : new int4(x + 1, y, z, 0));

            var centerMaterial = Palette[centerVoxel.Material];
            var centerMaterialType = centerMaterial.MaterialType;

            var voxelHigher = higherChunk[higher];
            var higherMaterial = Palette[voxelHigher.Material].MaterialType;
            var voxelLower = lowerChunk[lower];
            var lowerMaterial = Palette[voxelLower.Material].MaterialType;
            var voxelCloser = closerChunk[closer];
            var closerMaterial = Palette[voxelCloser.Material].MaterialType;
            var voxelFurther = furtherChunk[further];
            var furtherMaterial = Palette[voxelFurther.Material].MaterialType;
            var voxelOnTheLeft = leftChunk[left];
            var leftMaterial = Palette[voxelOnTheLeft.Material].MaterialType;
            var voxelOnTheRight = rightChunk[right];
            var rightMaterial = Palette[voxelOnTheRight.Material].MaterialType;

            var faces = FaceOrientation.None;
            faces |= (FaceOrientation)math.select((int)FaceOrientation.Top, 0, higherMaterial == centerMaterialType);
            faces |= (FaceOrientation)math.select((int)FaceOrientation.Bottom, 0, lowerMaterial == centerMaterialType);
            faces |= (FaceOrientation)math.select((int)FaceOrientation.Closer, 0, closerMaterial == centerMaterialType);
            faces |= (FaceOrientation)math.select((int)FaceOrientation.Further, 0, furtherMaterial == centerMaterialType);
            faces |= (FaceOrientation)math.select((int)FaceOrientation.Left, 0, leftMaterial == centerMaterialType);
            faces |= (FaceOrientation)math.select((int)FaceOrientation.Right, 0, rightMaterial == centerMaterialType);

            Faces[center] = new Face(faces, centerVoxel.Material);
        }
    }
}