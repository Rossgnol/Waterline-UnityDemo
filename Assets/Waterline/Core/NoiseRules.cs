namespace Waterline.Core
{
    public enum GroundKind { Concrete, Metal, Rubber }
    public enum MotionKind { Still, Quiet, Walk, Run }
    public static class NoiseRules
    {
        public static float Radius(GroundKind ground, MotionKind motion)
        {
            if (motion == MotionKind.Still) return 0;
            float walk = ground == GroundKind.Metal ? 7 : ground == GroundKind.Rubber ? 2 : 4;
            return walk * (motion == MotionKind.Quiet ? 0.35f : motion == MotionKind.Run ? 2.2f : 1);
        }
    }
}
