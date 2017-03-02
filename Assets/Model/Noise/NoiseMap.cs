using System;
using UnityEngine;

public class NoiseMap {

    public static float[,] adjustNoise (float[,] elevations, int noise_function) {
        switch (noise_function) {
            case 0:
                elevations = applyNormalizedHalfSphere(elevations, elevations.GetLength(0), 1f);
                break;
            case 1:
                applyLogisticsFunctionToElevations(elevations);
                break;
            case 2:
                amplifyElevations(elevations, 2, 2);
                break;
            case 3:
                amplifyElevations(elevations, 1, 6f); // applies exponential function to elevation[i][j]
                elevations = applyNormalizedHalfSphere(elevations, elevations.GetLength(0), 0.5f, 0.75f); // makes center higher elevations
                logarithmicClamp(elevations, 1f, 1);
                break;
            case 5:
                break;
            default:
                break;
        }
        Utilities.normalize(elevations);
        //normalizeToNElevationLevels(elevations, 50);
        return elevations;
    }

    private static void normalizeToNElevationLevels(float[,] elevations, int levels) {
        for (int i = 0; i < elevations.GetLength(0); i++) {
            for (int j = 0; j < elevations.GetLength(0); j++) {
                elevations[i, j] = ((int)(elevations[i, j] * levels)) / (levels * 1f);
            }
        }
    }

    private static float logarithmic(float value, float start, float intensity) {
        return (float)(start + Math.Log(1 - start + value / intensity));
    }

    // flattens terrain
    private static void logarithmicClamp(float[,] elevations, float log_clamp_threshold, float intensity) {
        for (int i = 0; i < elevations.GetLength(0); i++) {
            for (int j = 0; j < elevations.GetLength(0); j++) {
                if (elevations[i, j] > log_clamp_threshold) {
                    elevations[i, j] = logarithmic(elevations[i, j], log_clamp_threshold, intensity);
                }
            }
        }
    }

    /*
        // smooth_faactor < 0.2 gives best results
        public void smoothenConvolutionFilter(float[][] elevations, float smooth_factor) {
            float weights[,] = {{smooth_factor,smooth_factor,smooth_factor},
                            {smooth_factor,0,smooth_factor
},
                            {smooth_factor,smooth_factor,smooth_factor}};
        convolutionFilter(elevations, weights);
    }

    // smooth_faactor < 0.2 gives best results
    public void embossConvolutionFilter(float[,] elevations, float amplify_factor) {
    float weights[,] = {{-2* amplify_factor,-amplify_factor,0},
                            {-amplify_factor,amplify_factor,amplify_factor},
                            {0,amplify_factor,2* amplify_factor}};
        convolutionFilter(elevations, weights);
    }
    */

    public static float logisticsFunction(float value) {
        float growth_rate = 5.0f;
        return (float)(1.0 / (1 + Math.Exp(growth_rate / 2 + -growth_rate * value)));
    }

    // flattens the terrain
    public static void applyLogisticsFunctionToElevations(float[,] elevations) {
        for (int i = 0; i < elevations.GetLength(0); i++) {
            for (int j = 0; j < elevations.GetLength(0); j++) {
                elevations[i, j] = logisticsFunction(elevations[i, j]);
            }
        }
    }

    // results in elevation = (amplify_factor * elevation) ^ amplify_factor
    public static void amplifyElevations(float[,] elevations, float scale_factor, float amplify_factor) {
        for (int i = 0; i < elevations.GetLength(0); i++) {
            for (int j = 0; j < elevations.GetLength(0); j++) {
                elevations[i, j] = (float)Math.Pow(scale_factor * elevations[i, j], amplify_factor);
            }
        }
        Utilities.normalize(elevations);
    }

    public static void convolutionFilter(float[,] elevations, float[,] weights) {
        for (int i = 1; i < elevations.GetLength(0) - 1; i++) {
            for (int j = 1; j < elevations.GetLength(0) - 1; j++) {
                for (int ii = -1; ii < 2; ii++) {
                    for (int jj = -1; jj < 2; jj++) {
                        elevations[i, j] += weights[ii + 1, jj + 1] * elevations[i + ii, j + jj];
                    }
                }
                if (elevations[i, j] < 0) {
                    elevations[i, j] = 0;
                }
                if (elevations[i, j] > 1) {
                    elevations[i, j] = 1;
                }
            }
        }
    }

    public static float[,] applyNormalizedHalfSphere(float[,] elevations, int size, float intensity, float threshold=1f) {
        Debug.Log("applying sphere of size " + size);
        float[,] temp = new float[size, size];
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                float val = (float)(Math.Pow(size / 2, 2) - Math.Pow(i - size / 2, 2) - Math.Pow(j - size / 2, 2));
                val = (val > 0) ? val : 0;
                temp[i, j] = (float)Math.Sqrt(val);
            }
        }
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                temp[i, j] /= size;
                temp[i, j] = (temp[i, j] > threshold) ? threshold : temp[i, j];
            }
        }
        return Utilities.mergeArrays(temp, elevations, intensity, 1);
    }
}
