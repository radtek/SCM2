using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitCreateAudio : MonoBehaviour {

    private AudioSource AS = null;

    private void Start()
    {
        AS = GetComponent<AudioSource>();

        Play();
    }

    public void Play()
    {
        if (AS.clip == null)
            return;

        AS.Play();
    }

    private void Update()
    {
        if (AS.clip == null)
            return;

        if (AS.isPlaying)
            return;

        Destroy(AS.gameObject);
    }

    public void Stop()
    {
        if (AS.clip == null)
            return;

        AS.Stop();
    }
}
