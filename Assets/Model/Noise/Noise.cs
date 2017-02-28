using UnityEngine;
using System;

public abstract class Noise {

    private int noise_resolution = 500;
    private float[,] noise_elevations;
    private bool generated = false;

    public Noise(int noise_resolution, int seed) {
        this.noise_resolution = noise_resolution;
        UnityEngine.Random.InitState(seed);
    }

    public void generateNoise() {
        if (generated) {
            Debug.Log("Noise already generated.");
            return;
        }
        noise_elevations = generateNoiseValues();
        generated = true;
    }
    
    // extending noise classes must implement this method
    public abstract float[,] generateNoiseValues();

    public void regenerate() {
        generated = false;
        generateNoise();
    }

    public bool getGenerated() {
        return generated;
    }

    public float[,] getNoiseValues() {
        return this.noise_elevations;
    }

    public void setNoiseValues(float[,] noise_values) {
        this.noise_elevations = noise_values;
    }

    // returns interpolated weighted average of a local area of 4 pixels
    public float getNoiseValueAt(int baseX, int baseY, int REGION_SIZE) {
        int noiseIndex = getNoiseRes() - 1;
        int xF, yF, xC, yC;
        float xVal = (float)noiseIndex / REGION_SIZE * baseX,
            yVal = ((float)noiseIndex / REGION_SIZE * baseY);
        // interpolate
        xF = (int)Math.Floor(xVal);
        yF = (int)Math.Floor(yVal);
        xC = (int)Math.Ceiling(xVal);
        yC = (int)Math.Ceiling(yVal);
        float val1, val2, val3, val4;
        val1 = (noise_elevations[xF, yF]);
        val2 = (noise_elevations[xF, yC]);
        val3 = (noise_elevations[xC, yF]);
        val4 = (noise_elevations[xC, yC]);
        float u, v;
        if (Math.Abs(xC - xF) > 1e-9)
            u = (float)(xVal - xF) / (float)(xC - xF);
        else
            u = 0;
        if (Math.Abs(yC - yF) > 1e-9)
            v = (float)(yVal - yF) / (float)(yC - yF);
        else
            v = 0;
        return PixelLerp(val1, val3, val2, val4, u, v);
    }

    public float PixelLerp(float a, float b, float c, float d, float u, float v) {
        float abu = Mathf.Lerp(a, b, u);
        float dcu = Mathf.Lerp(d, c, u);
        return Mathf.Lerp(abu, dcu, v);
    }

    public int getNoiseRes() {
        return noise_resolution;
    }
}
