namespace VocaluxeLib.Menu
{
    public abstract class CMenuElementBase
    {
        public bool Visible { get; set; }
        public bool Highlighted { get; set; }
        public virtual bool Selected { get; set; }

        public virtual SRectF Rect
        {
            get { return MaxRect; }
        }

        // Those values are the ones from MaxRect for easier accessibility
        public virtual float X { get; set; }
        public virtual float Y { get; set; }
        public virtual float Z { get; set; }
        public virtual float W { get; set; }
        public float H { get; set; }

        public virtual SRectF MaxRect
        {
            get { return new SRectF(X, Y, W, H, Z); }
            set
            {
                X = value.X;
                Y = value.Y;
                W = value.W;
                H = value.H;
                Z = value.Z;
            }
        }

        protected CMenuElementBase()
        {
            Visible = true;
        }
    }
}