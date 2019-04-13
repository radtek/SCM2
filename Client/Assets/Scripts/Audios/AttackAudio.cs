using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAudio : MonoBehaviour {

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

    public void Stop()
    {
        if (AS.clip == null)
            return;

        AS.Stop();
    }
}
