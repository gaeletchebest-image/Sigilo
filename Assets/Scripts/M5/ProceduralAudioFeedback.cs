using UnityEngine;

/// <summary>Small synthesized UI/gameplay cues; no external audio files are required.</summary>
public sealed class ProceduralAudioFeedback : MonoBehaviour
{
    private enum Cue { Suspicion, Chase, Buzzer, Folder, Victory, Defeat }
    private static ProceduralAudioFeedback instance;
    public static ProceduralAudioFeedback Instance => instance != null ? instance : FindFirstObjectByType<ProceduralAudioFeedback>();

    [SerializeField, Range(0f, 1f)] private float volume = 0.45f;
    private AudioSource source;
    private AudioClip[] clips;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        clips = new[]
        {
            MakeTone("Generated_Suspicion", 520f, 720f, .13f),
            MakeTone("Generated_Chase", 260f, 130f, .42f),
            MakeTone("Generated_Buzzer", 740f, 520f, .34f),
            MakeTone("Generated_Folder", 660f, 990f, .22f),
            MakeTone("Generated_Victory", 440f, 880f, .55f),
            MakeTone("Generated_Defeat", 220f, 90f, .55f)
        };
    }

    public void PlaySuspicion() => Play(Cue.Suspicion);
    public void PlayChase() => Play(Cue.Chase);
    public void PlayBuzzer() => Play(Cue.Buzzer);
    public void PlayFolder() => Play(Cue.Folder);
    public void PlayVictory() => Play(Cue.Victory);
    public void PlayDefeat() => Play(Cue.Defeat);

    private void Play(Cue cue)
    {
        if (source != null && clips != null)
            source.PlayOneShot(clips[(int)cue], volume);
    }

    private static AudioClip MakeTone(string clipName, float startHz, float endHz, float seconds)
    {
        const int sampleRate = 44100;
        int count = Mathf.CeilToInt(sampleRate * seconds);
        var samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float progress = i / (float)Mathf.Max(1, count - 1);
            float frequency = Mathf.Lerp(startHz, endHz, progress);
            float attack = Mathf.Clamp01(t / .015f);
            float release = Mathf.Clamp01((seconds - t) / .08f);
            float envelope = attack * release;
            float fundamental = Mathf.Sin(2f * Mathf.PI * frequency * t);
            float overtone = Mathf.Sin(2f * Mathf.PI * frequency * 2f * t) * .2f;
            samples[i] = (fundamental + overtone) * envelope * .36f;
        }

        AudioClip clip = AudioClip.Create(clipName, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (clips == null) return;
        foreach (AudioClip clip in clips)
            if (clip != null) Destroy(clip);
    }
}
