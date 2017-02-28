using System;
using UnityEngine;

namespace MapGeneration {

    [System.Serializable]
    public class MapGeneratorInput {

        // region inputs
        public string preset;

        public int regionSeed;

        [Range(1, 25000)]
        public int regionSize;

        [Range(1, 200000)]
        public int regionN;

        [Range(0, 1000)]
        public int regionElevation;

        [Range(0, 100)]
        public int regionWaterSources;

        [Range(0f, 1f)]
        public float regionWaterLevel;

        // noise inputs
        [Range(0,500)]
        public int noiseResolution;

        [Range(0.001f, 500f)]
        public float noiseAmplitude;

        [Range(0.001f, 1f)]
        public float noisePersistance;

        // constructor
        public MapGeneratorInput() {
        }

        public void Initialize(bool useRandomSeed = true) {
            Debug.Log("Loading preset " + preset);
            if (!preset.Equals("custom")) {
                this.regionN = int.Parse(Utilities.statsXMLreader.getParameterFromXML("MapGeneratorInput/" + this.preset, "n"));
                this.regionSize = int.Parse(Utilities.statsXMLreader.getParameterFromXML("MapGeneratorInput/" + this.preset, "region_size"));
                this.regionElevation = int.Parse(Utilities.statsXMLreader.getParameterFromXML("MapGeneratorInput/" + this.preset, "elevation"));
                this.regionWaterSources = int.Parse(Utilities.statsXMLreader.getParameterFromXML("MapGeneratorInput/" + this.preset, "rivers"));
                this.regionWaterLevel = float.Parse(Utilities.statsXMLreader.getParameterFromXML("MapGeneratorInput/" + this.preset, "water"));
                this.noiseResolution = int.Parse(Utilities.statsXMLreader.getParameterFromXML("MapGeneratorInput/" + this.preset, "noise_resolution"));
                this.noiseAmplitude = float.Parse(Utilities.statsXMLreader.getParameterFromXML("MapGeneratorInput/" + this.preset, "noise_amplitude"));
                this.noisePersistance = float.Parse(Utilities.statsXMLreader.getParameterFromXML("MapGeneratorInput/" + this.preset, "noise_persistance"));
            }
            if (useRandomSeed)
                this.regionSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        override
        public string ToString() {
            string s = "Region: ";
            s += preset + " with n=" + regionN + ", seed=" + regionSeed + ", size=" + regionSize + ", elevation=" + regionElevation + 
                ", water sources=" + regionWaterSources + ", water=" + regionWaterLevel;
            s += "\nNoise: res=" + noiseResolution + ", amplitude=" + noiseAmplitude + ", persistance=" + noisePersistance; 
            return s;
        }
    }

    public class MapGenerator {
        // map generator private vars
        private ViewableRegion region;
        private Noise noise;

        public MapGenerator(MapGeneratorInput m) {
            initializeNoise(m.preset, m.regionSeed, m.noiseResolution, m.noiseAmplitude, m.noisePersistance);
            generateRegion(m.regionN, m.regionSeed, m.regionSize, m.regionElevation, m.regionWaterLevel, m.regionWaterSources);
            Debug.Log("Constucted MapGenerator with:\n" + m);
        }

        public void initializeNoise(String preset, int seed, int noise_resolution, float amplitude, float persistance) {
            int noisePreset = 3;
            if (preset.Equals("default")) {
                noisePreset = 3;
            } else if (preset.Equals("amplified")) {
                noisePreset = 1;
            } else { // revert to default
                noisePreset = 3;
            }
            noise = new FastPerlinNoise(noise_resolution, seed, amplitude, persistance);
            noise.setNoiseValues(NoiseMap.adjustNoise(noise.getNoiseValues(), noisePreset));
        }

        public void generateRegion(int n, int seed, int region_size, int elevation, float water, int rivers) {
            this.region = new HexRegion(n, seed, region_size, elevation, water, rivers, noise);
        }

        public ViewableRegion getRegion() {
            return this.region;
        }

        public Noise getNoise() {
            return this.noise;
        }
    }
}