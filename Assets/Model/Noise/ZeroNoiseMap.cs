
public class ZeroNoiseMap : Noise {

    public ZeroNoiseMap (int noise_resolution, int seed) : base(noise_resolution, seed) {
        this.generateNoise();
    }

    override
    public float[,] generateNoiseValues() {
        float[,] zeros = new float[this.getNoiseRes(), this.getNoiseRes()];
        for (int i = 0; i < this.getNoiseRes(); i++) {
            for (int j = 0; j < this.getNoiseRes(); j++) {
                zeros[i, j] = 0;
            }
        }
        return zeros;
    }
}
