using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Mh2RenderingUtils
{
    //UnityEngine.Rendering.Universal.RenderingUtils 内相关方法。
    public static void CreateRendererListWithRenderStateBlock(ScriptableRenderContext context, RenderingData data, DrawingSettings ds, FilteringSettings fs, RenderStateBlock rsb, ref RendererList rl)
    {
        RendererListParams param = new RendererListParams();
        unsafe
        {
            // Taking references to stack variables in the current function does not require any pinning (as long as you stay within the scope)
            // so we can safely alias it as a native array
            RenderStateBlock* rsbPtr = &rsb;
            var stateBlocks = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<RenderStateBlock>(rsbPtr, 1, Allocator.None);

            var shaderTag = ShaderTagId.none;
            var tagValues = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ShaderTagId>(&shaderTag, 1, Allocator.None);

            // Inside CreateRendererList (below), we pass the NativeArrays to C++ by calling GetUnsafeReadOnlyPtr
            // This will check read access but NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray does not set up the SafetyHandle (by design) so create/add it here
            // NOTE: we explicitly share the handle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandle = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetAllowReadOrWriteAccess(safetyHandle, true);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref stateBlocks, safetyHandle);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tagValues, safetyHandle);
#endif

            // Create & schedule the RL
            param = new RendererListParams(data.cullResults, ds, fs)
            {
                tagValues = tagValues,
                stateBlocks = stateBlocks

            };

            rl = context.CreateRendererList(ref param);

            // we need to explicitly release the SafetyHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(safetyHandle);
#endif
        }
    }
    
 
}
