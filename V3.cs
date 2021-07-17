namespace Geotiff2STLConverter
{
    public class V3
    {
        private float x;
        private float y;
        private float z;

        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }
        public float Z { get => z; set => z = value; }

        public V3()
        {
            x = .0f;
            y = .0f;
            z = .0f;
        }

        public V3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
