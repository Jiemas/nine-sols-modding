using UnityEngine;
using System.IO;
using System.Reflection;
using System;

using NineSolsAPI;
using NineSolsAPI.Utils;

namespace EnlightenedJi;

/// <summary>
/// Converts a 2D LUT image into a Texture3D. Supports several common layouts.
/// </summary>
public static class LutUtils
{
    public static Texture3D GenerateLutFromRbf(Vector3[] srcColors, Vector3[] dstColors, int size, float eps)
    {
        int N = srcColors.Length;
        // compute weights
        RbfColorMapper.ComputeWeightsRGB(srcColors, dstColors, eps, out var wR, out var wG, out var wB);

        Color[] voxels = new Color[size * size * size];
        int idx = 0;
        for (int b = 0; b < size; b++) {
            for (int g = 0; g < size; g++) {
                for (int r = 0; r < size; r++) {
                    Vector3 c = new Vector3((r + 0.5f)/size, (g + 0.5f)/size, (b + 0.5f)/size);
                    float outR = 0, outG = 0, outB = 0;
                    for (int i = 0; i < N; i++) {
                        float dist = Vector3.Distance(c, srcColors[i]);
                        float k = Mathf.Exp(- (eps * dist) * (eps * dist));
                        outR += wR[i] * k;
                        outG += wG[i] * k;
                        outB += wB[i] * k;
                    }
                    voxels[idx++] = new Color(outR, outG, outB, 1f);
                }
            }
        }
        Texture3D tex = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
        tex.SetPixels(voxels);
        tex.Apply();

        // RbfColorMapper.ValidateWeights(srcColors, dstColors, wR, wG, wB, eps);
        return tex;
    }

    public static void SaveTexture3DPreview(Texture3D tex3D, string filename)
    {
        int size = tex3D.width;
        if (tex3D.height != size || tex3D.depth != size) ToastManager.Toast("Texture3D not cubic?");
        Color[] voxels = tex3D.GetPixels(); // gets size^3 order x + y*size + z*size*size

        int outW = size * size;
        int outH = size;
        Texture2D dst = new Texture2D(outW, outH, TextureFormat.RGBA32, false);

        Color[] outPixels = new Color[outW * outH];
        for (int z = 0; z < size; z++)
        {
            int sliceCol = z % size;
            int sliceRow = z / size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int srcIndex = x + y * size + z * size * size;
                    int px = sliceCol * size + x;
                    int py = sliceRow * size + y;
                    outPixels[py * outW + px] = voxels[srcIndex];
                }
            }
        }

        dst.SetPixels(outPixels);
        dst.Apply();

        string path = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllBytes(path, dst.EncodeToPNG());
        ToastManager.Toast($"Saved LUT preview to {path}");
    }


    public enum Layout
    {
        FlattenedHorizontal, // width = size * size, height = size
        FlattenedVertical,   // width = size, height = size * size
        TiledGrid,           // tiles arranged in grid (tilesPerRow given)
        Unknown
    }

    // Main API: convert a Texture2D (must be readable) to Texture3D
    // - src: source Texture2D
    // - size: LUT size (samples per axis). If <= 0, the function will attempt to infer size (only for flattened horizontal).
    // - layout: source layout. If Unknown, tool will try to infer flattened horizontal.
    // - tilesPerRow: only used when Layout == TiledGrid. (if 0, will attempt to infer)
    public static Texture3D Texture2DToTexture3D(Texture2D src, int size = 0, Layout layout = Layout.Unknown, int tilesPerRow = 0)
    {
        if (src == null) throw new ArgumentNullException(nameof(src));
        if (!src.isReadable) Debug.LogWarning("Texture2D is not readable. Ensure Read/Write Enabled is true in import settings.");

        int w = src.width;
        int h = src.height;

        // Try to infer flattened horizontal: width == size*size && height == size
        if (layout == Layout.Unknown)
        {
            if (h > 0 && w == h * h)
            {
                layout = Layout.FlattenedHorizontal;
                size = h;
            }
            else if (w == h * h)
            {
                layout = Layout.FlattenedVertical;
                size = w;
            }
            else
            {
                // fallback to treated as tiled grid if it is square and larger than 1
                if (w == h)
                {
                    layout = Layout.TiledGrid;
                    // We'll try to infer tilesPerRow as sqrt(numTiles). We'll guess size = ??? 
                    // The most common tiled format is where numTiles = size (i.e., tilesPerRow = size)
                    // So we attempt to find integer size where tilesPerRow = size and tileSize = size -> width = size*size
                    // which is same as flattenedHorizontal and would have been caught. So for square ambiguous cases
                    // we require the caller to pass either size or tilesPerRow.
                    if (size <= 0 && tilesPerRow <= 0)
                        throw new ArgumentException("Ambiguous LUT layout for square image. Please provide 'size' or 'tilesPerRow'.");
                }
                else
                {
                    throw new ArgumentException($"Cannot infer LUT layout from texture size {w}x{h}. Please specify layout and size.");
                }
            }
        }

        if (size <= 0 && layout != Layout.TiledGrid)
            throw new ArgumentException("size must be > 0 or inferable from the texture for the chosen layout.");

        Color[] srcPixels = src.GetPixels();

        Texture3D tex3D = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
        tex3D.wrapMode = TextureWrapMode.Clamp;
        tex3D.filterMode = FilterMode.Trilinear;

        Color[] voxels = new Color[size * size * size];

        if (layout == Layout.FlattenedHorizontal)
        {
            // width = size*size, height = size
            // indexing: for sliceZ in 0..size-1:
            //   sliceX = sliceZ % size
            //   sliceY = sliceZ / size
            // but flattened horizontal usually arranges slices left-to-right across rows, so:
            // row = z / size
            // col = z % size
            // pixel coordinate within slice: (x, y)
            for (int z = 0; z < size; z++)
            {
                int sliceRow = z / size;    // which vertical row of slices (0..size-1)
                int sliceCol = z % size;    // which horizontal column of slices (0..size-1)
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int px = sliceCol * size + x;
                        int py = sliceRow * size + y;
                        int srcIndex = py * (size * size) + px; // because width = size*size
                        Color c = srcPixels[srcIndex];
                        int dstIndex = x + y * size + z * size * size;
                        voxels[dstIndex] = c;
                    }
                }
            }
        }
        else if (layout == Layout.FlattenedVertical)
        {
            // width = size, height = size*size. Similar logic rotated.
            for (int z = 0; z < size; z++)
            {
                int sliceCol = z / size;
                int sliceRow = z % size;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int px = x;
                        int py = sliceCol * size + y; // careful: alternative conventions exist
                        // We'll assume vertical stacking where slices go top->bottom then left->right.
                        int srcIndex = py * size + px;
                        Color c = srcPixels[srcIndex];
                        int dstIndex = x + y * size + z * size * size;
                        voxels[dstIndex] = c;
                    }
                }
            }
        }
        else // TiledGrid
        {
            // Require tilesPerRow or infer
            if (tilesPerRow <= 0)
            {
                // try infer by finding integer tile size
                // tileSize must divide width and height. try all divisors:
                bool found = false;
                for (int candidateTile = 1; candidateTile <= Mathf.Min(w, h); candidateTile++)
                {
                    if (w % candidateTile == 0 && h % candidateTile == 0)
                    {
                        int cols = w / candidateTile;
                        int rows = h / candidateTile;
                        if (cols * rows == candidateTile) // total tiles = size
                        {
                            tilesPerRow = cols;
                            size = candidateTile;
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                    throw new ArgumentException("Could not infer tilesPerRow and tile size for TiledGrid; please supply tilesPerRow and size.");
            }

            int tileSize = size; // each tile is size x size
            int tilesPerCol = (h / tileSize);
            int tilesPerColCheck = (h / tileSize);
            // iterate tiles by tileY, tileX; each tile corresponds to one z slice
            int zIndex = 0;
            for (int tileY = 0; tileY < tilesPerCol; tileY++)
            {
                for (int tileX = 0; tileX < tilesPerRow; tileX++)
                {
                    if (zIndex >= size) break;
                    for (int y = 0; y < tileSize; y++)
                    {
                        for (int x = 0; x < tileSize; x++)
                        {
                            int px = tileX * tileSize + x;
                            int py = tileY * tileSize + y;
                            int srcIndex = py * w + px;
                            Color c = srcPixels[srcIndex];
                            int dstIndex = x + y * size + zIndex * size * size;
                            voxels[dstIndex] = c;
                        }
                    }
                    zIndex++;
                }
            }
            if (zIndex < size)
                Debug.LogWarning($"Tiled LUT seems to have fewer tiles ({zIndex}) than expected size ({size}).");
        }

        tex3D.SetPixels(voxels);
        tex3D.Apply(false, false);
        return tex3D;
    }

    // Helper: load PNG from file path into a readable Texture2D
    public static Texture2D LoadTexture2DFromFile(string path, bool linear = false)
    {
        if (!File.Exists(path)) throw new FileNotFoundException(path);
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
        if (!tex.LoadImage(bytes)) throw new Exception("Failed to load image bytes into texture.");
        // tex.LoadImage makes it readable by default
        return tex;
    }

    public static Texture3D LoadTexture3DFromFile(string path, bool linear = false)
    {
        return Texture2DToTexture3D(
            LoadTexture2DFromFile(path, linear),
            size: 32,
            layout: Layout.FlattenedHorizontal
        );
    }

    // Returns the directory where the current assembly (the plugin) lives.
    public static string GetPluginDirectory()
    {
        var asm = Assembly.GetExecutingAssembly();
        // Assembly.Location is path to the DLL file (BepInEx/plugins/YourMod/YourMod.dll)
        var asmPath = asm.Location;
        return Path.GetDirectoryName(asmPath);
    }

    // Compose path for a file that sits next to your plugin DLL.
    // e.g. put "myLut.png" in the same folder as your mod DLL in the r2modman-installed mod folder.
    public static string GetFilePathRelativeToPlugin(string relativeFilename)
    {
        string pluginDir = GetPluginDirectory();
        if (string.IsNullOrEmpty(pluginDir)) throw new Exception("Unable to locate plugin directory.");
        return Path.Combine(pluginDir, relativeFilename);
    }
}
