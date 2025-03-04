using System.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Define clips that used in game")]
        public AudioClip mainBGM;
        public AudioClip clickSFX;

        [Header("Audio source for playing sounds")]
        public AudioSource sfx;
        public AudioSource bgm;

        // get and set volumes for volumes control
        public float BGM_Volume
        {
            get
            {
                return bgm.volume;
            }
            set
            {
                bgm.volume = value;
            }
        }
        public float SFX_Volume
        {
            get
            {
                return sfx.volume;
            }
            set
            {
                sfx.volume = value;
            }
        }

        public async Task PlaySfxWithDelay(AudioClip sound, float delay)
        {
            await Task.Delay(System.TimeSpan.FromSeconds(delay));

            sfx.PlayOneShot(sound);
        }

        public void PlayMusic(AudioClip music)
        {
            bgm.clip = music;
            bgm.Play();
        }

        public void PauseMusic()
        {
            bgm.Pause();
        }

        public void StopMusic()
        {
            bgm.Stop();
        }
    }
}