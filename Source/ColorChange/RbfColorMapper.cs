
using System;
using UnityEngine;
using NineSolsAPI;
using NineSolsAPI.Utils;

namespace EnlightenedJi;

public class RbfColorMapper
{
    // small Gauss-Jordan solver for NxN matrix (N small)
    static float[] SolveLinearSystem(float[,] A, float[] b, int N)
    {
        // Build augmented matrix
        float[,] M = new float[N, N + 1];
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++) M[i, j] = A[i, j];
            M[i, N] = b[i];
        }

        // Gauss-Jordan
        for (int col = 0; col < N; col++)
        {
            // find pivot
            int pivot = col;
            for (int r = col; r < N; r++)
                if (Math.Abs(M[r, col]) > Math.Abs(M[pivot, col])) pivot = r;
            // swap
            if (pivot != col)
                for (int c = col; c <= N; c++) { var tmp = M[col, c]; M[col, c] = M[pivot, c]; M[pivot, c] = tmp; }

            float diag = M[col, col];
            if (Math.Abs(diag) < 1e-8f) throw new Exception("Matrix singular or too small pivot.");

            // normalize row
            for (int c = col; c <= N; c++) M[col, c] /= diag;

            // eliminate other rows
            for (int r = 0; r < N; r++) if (r != col)
            {
                float factor = M[r, col];
                if (Math.Abs(factor) < 1e-12f) continue;
                for (int c = col; c <= N; c++) M[r, c] -= factor * M[col, c];
            }
        }

        float[] x = new float[N];
        for (int i = 0; i < N; i++) x[i] = M[i, N];
        return x;
    }

    static float KernelGaussian(float r, float eps) => (float)Math.Exp(-(eps * r) * (eps * r));

    // Computes weights for one output channel
    // srcColors and dstChannelVals are length N arrays (srcColors in [0..1] RGB)
    public static float[] ComputeWeights(Vector3[] srcColors, float[] dstChannelVals, float epsilon)
    {
        int N = srcColors.Length;
        float[,] A = new float[N, N];
        for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            {
                float r = Vector3.Distance(srcColors[i], srcColors[j]);
                A[i, j] = KernelGaussian(r, epsilon);
            }

        return SolveLinearSystem(A, dstChannelVals, N);
    }

    // Helper to compute weights for R,G,B channels
    public static void ComputeWeightsRGB(Vector3[] srcColors, Vector3[] dstColors, float eps,
                                        out float[] wR, out float[] wG, out float[] wB)
    {
        int N = srcColors.Length;
        float[] rVals = new float[N], gVals = new float[N], bVals = new float[N];
        for (int i = 0; i < N; i++) { rVals[i] = dstColors[i].x; gVals[i] = dstColors[i].y; bVals[i] = dstColors[i].z; }
        wR = ComputeWeights(srcColors, rVals, eps);
        wG = ComputeWeights(srcColors, gVals, eps);
        wB = ComputeWeights(srcColors, bVals, eps);
    }

    // Call after ComputeWeightsRGB or after you build your rbf functions/arrays
    public static void ValidateWeights(Vector3[] src, Vector3[] dst, float[] wR, float[] wG, float[] wB, float eps)
    {
        int N = src.Length;
        for (int i = 0; i < N; i++)
        {
            Vector3 p = src[i];
            float r = 0f, g = 0f, b = 0f;
            for (int j = 0; j < N; j++)
            {
                float dist = Vector3.Distance(p, src[j]);
                float k = Mathf.Exp(- (eps * dist) * (eps * dist)); // match your kernel
                r += wR[j] * k;
                g += wG[j] * k;
                b += wB[j] * k;
            }
            ToastManager.Toast($"Point {i}: expected {dst[i]}, rbf -> ({r:F4},{g:F4},{b:F4})");
        }
    }

}
