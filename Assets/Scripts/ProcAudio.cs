using UnityEngine;

// 오디오 파일 없이 코드로 소리를 합성합니다.
public static class ProcAudio
{
    const int SR = 22050;
    static AudioClip Make(string n, float[] d)
    {
        var c = AudioClip.Create(n, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // 사각! 하는 썰리는 소리 (노이즈 + 아래로 떨어지는 톤 + 바삭한 클릭)
    public static AudioClip Slice(int seed)
    {
        var r = new System.Random(seed);
        int n = (int)(SR * 0.35f);
        var d = new float[n];
        float lp = 0, ph = 0, f0 = 300 + seed * 45;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float ns = (float)(r.NextDouble() * 2 - 1);
            lp += (ns - lp) * 0.3f;
            ph += 2 * Mathf.PI * (f0 * Mathf.Exp(-t * 12f) + 70) / SR;
            d[i] = (lp * Mathf.Exp(-t * 14f) * 0.8f + Mathf.Sin(ph) * Mathf.Exp(-t * 22f) * 0.5f
                    + ns * Mathf.Exp(-t * 60f) * 0.35f) * 0.8f;
        }
        return Make("slice" + seed, d);
    }

    public static AudioClip Whoosh()
    {
        var r = new System.Random(99);
        int n = (int)(SR * 0.2f);
        var d = new float[n];
        float lp = 0;
        for (int i = 0; i < n; i++)
        {
            float p = i / (float)n;
            lp += ((float)(r.NextDouble() * 2 - 1) - lp) * (0.05f + 0.3f * p);
            float env = Mathf.Sin(Mathf.PI * p); env *= env;
            d[i] = lp * env * 2.2f;
        }
        return Make("whoosh", d);
    }

    public static AudioClip Chime()
    {
        int n = (int)(SR * 0.9f);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            d[i] = (Mathf.Sin(2 * Mathf.PI * 880 * t) + 0.6f * Mathf.Sin(2 * Mathf.PI * 1320 * t)) * Mathf.Exp(-t * 5f) * 0.3f;
        }
        return Make("chime", d);
    }

    // 잔잔한 펜타토닉 힐링 BGM (16초 루프)
    public static AudioClip Music()
    {
        float[] sc = { 261.63f, 293.66f, 329.63f, 392f, 440f, 523.25f, 587.33f };
        int[] pat = { 0,2,4,2,5,4,2,1, 0,2,4,6,5,4,2,3, 0,2,4,2,5,4,2,1, 0,3,2,1,0,1,2,4 };
        float bl = 0.5f;
        int n = (int)(SR * bl * pat.Length);
        var d = new float[n];
        for (int b = 0; b < pat.Length; b++)
        {
            Add(d, b * bl, sc[pat[b]], 2f, 0.2f);
            if (b % 8 == 0) Add(d, b * bl, sc[pat[b]] * 0.5f, 3.5f, 0.16f);
        }
        return Make("music", d);
    }

    static void Add(float[] d, float start, float f, float dur, float amp)
    {
        int s0 = (int)(start * SR), len = (int)(dur * SR);
        for (int k = 0; k < len; k++)
        {
            float t = k / (float)SR;
            float v = Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * f * t);
            d[(s0 + k) % d.Length] += v * amp * Mathf.Exp(-t * 3.2f) * Mathf.Min(1f, t * 80f);
        }
    }
}
