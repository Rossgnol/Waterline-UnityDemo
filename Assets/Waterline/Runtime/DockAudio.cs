using UnityEngine;
namespace Waterline
{
    public static class DockAudio
    {
        // Short original procedural placeholder sounds; no third-party recordings.
        public static AudioClip Pulse(string name, float length, float pitch, bool metallic)
        {
            const int rate=22050; var samples=new float[(int)(rate*length)];
            var random=new System.Random(73);
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate, envelope=Mathf.Exp(-t*(metallic?13:30));
                float signal=Mathf.Sin(t*pitch*2*Mathf.PI)+.4f*Mathf.Sin(t*pitch*2.71f*2*Mathf.PI);
                samples[i]=(.3f*signal+(float)(random.NextDouble()-.5)*.15f)*envelope;
            }
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
    }
}
