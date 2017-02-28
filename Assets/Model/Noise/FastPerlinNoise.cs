using System;
using UnityEngine;

public class FastPerlinNoise : Noise {

    float amplitude;
    float persistance;
    int octaves;
    int levels;

    public FastPerlinNoise(int noise_resolution, int seed, float amplitude, float persistance, int octaves = 8, int levels = 5) : base(noise_resolution, seed) {
        this.amplitude = amplitude;
        this.persistance = persistance;
        this.octaves = octaves;
        this.levels = levels;
        generateNoise();
    }

    override
    public float[,] generateNoiseValues() {
        return generateMultipleLevelPerlinNoise(octaves, levels);
    }

    private float[,] generateMultipleLevelPerlinNoise(int octaveCount, int levels) {
        float[,] perlinNoiseCombined = new float[getNoiseRes(), getNoiseRes()];
        // generate 0,1,...,levels of perlin noise patterns and merge these
        for (int i = 1; i <= levels; i++) {
            float[,] baseNoise = generateWhiteNoise(getNoiseRes());
            float[,] perlinNoise = generatePerlinNoise(baseNoise, octaveCount);
            // merge results of new perlin level with previous perlinNoise
            perlinNoiseCombined = Utilities.mergeArrays(perlinNoise, perlinNoiseCombined, 1f/levels, 1);
        }
        return perlinNoiseCombined;
    }

    private float[,] generateWhiteNoise(int size) {
        float[,] noise = new float[size, size];
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                noise[i,j] = (float) UnityEngine.Random.value;
            }
        }
        return noise;
    }

    private float[,] generateSmoothNoise(float[,] baseNoise, int octave) {
        int length = baseNoise.GetLength(0);
        float[,] smoothNoise = new float[length,length];

        int samplePeriod = (int)(2 * octave + 1); // calculates 2 ^ k
        float sampleFrequency = 1.0f / samplePeriod;

        for (int i = 0; i < length; i++) {
            //calculate the horizontal sampling indices
            int sample_i0 = (i / samplePeriod) * samplePeriod;
            int sample_i1 = (sample_i0 + samplePeriod) % length; //wrap around
            float horizontal_blend = (i - sample_i0) * sampleFrequency;

            for (int j = 0; j < length; j++) {
                //calculate the vertical sampling indices
                int sample_j0 = (j / samplePeriod) * samplePeriod;
                int sample_j1 = (sample_j0 + samplePeriod) % length; //wrap around
                float vertical_blend = (j - sample_j0) * sampleFrequency;

                //blend the top two corners
                float top = Interpolate(baseNoise[sample_i0,sample_j0],
                        baseNoise[sample_i1,sample_j0], horizontal_blend);

                //blend the bottom two corners
                float bottom = Interpolate(baseNoise[sample_i0,sample_j1],
                        baseNoise[sample_i1,sample_j1], horizontal_blend);

                //final blend
                smoothNoise[i,j] = Interpolate(top, bottom, vertical_blend);
            }
        }
        return smoothNoise;
    }

    private float[,] generatePerlinNoise(float[,] baseNoise, int octaveCount) {
        int length = baseNoise.GetLength(0);
        float[][,] smoothNoise = new float[octaveCount][,]; //an array of 2D arrays

        //generate smooth noise
        for (int i = 0; i < octaveCount; i++) {
            smoothNoise[i] = generateSmoothNoise(baseNoise, i);
        }

        float[,] perlinNoise = new float[length,length]; //an array of floats initialized to 0

        float totalAmplitude = 0.0f;

        float _amplitude = amplitude;

        //blend noise together
        for (int octave = octaveCount - 1; octave >= 0; octave--) {
            _amplitude *= persistance;
            totalAmplitude += _amplitude;

            for (int i = 0; i < length; i++) {
                for (int j = 0; j < length; j++) {
                    perlinNoise[i,j] += smoothNoise[octave][i,j] * _amplitude;
                }
            }
        }

        //normalisation
        for (int i = 0; i < length; i++) {
            for (int j = 0; j < length; j++) {
                perlinNoise[i,j] /= totalAmplitude;
            }
        }

        return perlinNoise;
    }

    private float Interpolate(float x0, float x1, float alpha) {
        return x0 * (1 - alpha) + alpha * x1;
    }
}
